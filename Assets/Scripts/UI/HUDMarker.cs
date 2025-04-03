using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class HUDMarker : MonoBehaviour
{
    public HUDMarkerType markerType = HUDMarkerType.Undefined;
    public GameObject targetObject;
    public TMP_Text nameText;
    public TMP_Text distanceText;
    public TMP_Text velocityText;
    public Slider[] hpSliders;
    public RectTransform sliderContainer;
    public HealthComponent targetHealth;
    [SerializeField] private float rotationSpeed = 1f;
    [SerializeField] private float rotationAngle = 5f;
    [SerializeField] private float minScale = 0.5f;
    [SerializeField] private float maxScale = 1.5f;
    [SerializeField] private float fadeOutThreshold = 1000f;
    [SerializeField] private float maxDistance = 1200f;
    [SerializeField] private float midFontScale;

    private RectTransform markerTransform;
    private Rigidbody targetRigidbody;
    private Image[] fillImages;
    private Image[] backgroundImages;

    [Header("Lock-On UI")]
    [SerializeField] private RectTransform[] lockOnRects;
    [SerializeField] private float lockOnDuration = 1.5f;
    [SerializeField] private AnimationCurve scaleCurve;
    [SerializeField] private AnimationCurve alphaCurve;
    [SerializeField] private Color lockOnStartColor = Color.white;
    [SerializeField] private Color lockOnFinalColor = Color.green;

    private Coroutine lockOnRoutine;
    private Coroutine cancelRoutine;

    //original positions of thw lock-on rects
    private static readonly float offsetDistance = 60f;
    private static readonly Vector3[] startOffsets =
    {
        new (0, offsetDistance),  new (0, -offsetDistance),
        new (-offsetDistance, 0), new (offsetDistance, 0)
    };

    private float[] spinSpeeds;
    private bool isLockedOn;
    private void Awake()
    {
        markerTransform = GetComponent<RectTransform>();
        if (markerType == HUDMarkerType.Damageable)
        {
            fillImages = new Image[hpSliders.Length];
            backgroundImages = new Image[hpSliders.Length];
            for (int i = 0; i < hpSliders.Length; i++)
            {
                fillImages[i] = hpSliders[i].fillRect.GetComponent<Image>();
                backgroundImages[i] = hpSliders[i].transform.Find("Background").GetComponent<Image>();
            }
        }
    }
    private void OnDisable()
    {
        StopAllCoroutines();
        CancelInvoke();
    }
    public void Initialize(GameObject target)
    {
        targetObject = target;
        targetRigidbody = target.GetComponent<Rigidbody>();
        nameText.text = target.name;
        if (!target.TryGetComponent(out HealthComponent component))
            return;
        nameText.color = TeamManager.Instance.GetTeamColor(targetObject.GetComponent<Controller>().Team);
        targetHealth = component;
        UpdateHP(targetHealth.CurrentHP, targetHealth.MaxHP);
        targetHealth.OnHealthChanged += UpdateHP;
        targetHealth.OnDeath += Cleanup;
    }

    public void UpdateMarker(Camera playerCamera)
    {
        if (targetObject == null)
        {
            gameObject.SetActive(false);
            return;
        }
        Vector3 worldPosition = targetObject.transform.position;
        markerTransform.position = worldPosition;
        markerTransform.rotation = Quaternion.LookRotation(markerTransform.position - playerCamera.transform.position);
        float distance = Vector3.Distance(playerCamera.transform.position, worldPosition);
        distanceText.text = $"{distance:F1}m";
        float fadeFactor = Mathf.Clamp01(1f - (distance - fadeOutThreshold) / (maxDistance - fadeOutThreshold));

        if (targetRigidbody != null)
        {
            float speed = targetRigidbody.linearVelocity.magnitude;
            velocityText.text = speed > 0.1f ? $"{speed:F1} m/s" : "Stationary";
        }

        ScaleMarkerBasedOnDistance(distance);
        AnimateMarker();
        SetMarkerAlpha(fadeFactor);
    }

    private void ScaleMarkerBasedOnDistance(float distance)
    {
        float scaleFactor = Mathf.Lerp(minScale, maxScale, distance / maxDistance);
        sliderContainer.localScale = Vector3.one * scaleFactor;
        nameText.fontSize = midFontScale * scaleFactor;
        distanceText.fontSize = midFontScale * scaleFactor;
        velocityText.fontSize = midFontScale * scaleFactor;
    }

    private void AnimateMarker()
    {
        if (sliderContainer == null) return;
        float angle = Mathf.Sin(Time.time * rotationSpeed) * rotationAngle;
        sliderContainer.localRotation = Quaternion.Euler(0, 0, angle);
    }

    public void UpdateHP(int currentHP, int maxHP)
    {
        float remainingHP = currentHP;
        float quarterMaxHP = maxHP / 4f;

        for (int i = 3; i >= 0; i--)
        {
            if (remainingHP <= 0)
            {
                hpSliders[i].value = 0;
                fillImages[i].enabled = false;
                backgroundImages[i].color = Color.gray;
                continue;
            }

            hpSliders[i].maxValue = quarterMaxHP;
            hpSliders[i].value = Mathf.Min(remainingHP, quarterMaxHP);
            fillImages[i].enabled = true;
            backgroundImages[i].color = Color.white;
            remainingHP -= quarterMaxHP;
        }
    }

    public void Cleanup(bool isDead)
    {
        if (targetHealth != null)
        {
            targetHealth.OnHealthChanged -= UpdateHP;
            targetHealth.OnDeath -= Cleanup;
        }
        targetObject = null;
        targetHealth = null;
        targetRigidbody = null;
        CancelLockOnSequence();
        gameObject.SetActive(false);
    }

    public void TriggerLockOnSequence()
    {
        if (lockOnRoutine != null || isLockedOn)
            return;
        lockOnRoutine = StartCoroutine(LockOnSequence());
    }

    public void CancelLockOnSequence()
    {
        isLockedOn = false;
        if (lockOnRoutine != null)
        {
            StopCoroutine(lockOnRoutine);
            lockOnRoutine = null;
        }

        if (cancelRoutine != null)
            StopCoroutine(cancelRoutine);
        cancelRoutine = null;
        if(isActiveAndEnabled)
            cancelRoutine = StartCoroutine(FadeOutLockOnRects());
    }

    private IEnumerator FadeOutLockOnRects()
    {
        float duration = 0.5f;
        float elapsed = 0f;
        CacheCurrentLockOnRectStates(out Vector3[] currentPositions, out Color[] currentColors, out float[] currentAlphas);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            UpdateLockOnRectsDuringFade(currentPositions, currentColors, currentAlphas, t);
            yield return null;
        }
        DeactivateLockOnRects();
        cancelRoutine = null;
    }

    private void CacheCurrentLockOnRectStates(out Vector3[] positions, out Color[] colors, out float[] alphas)
    {
        positions = new Vector3[lockOnRects.Length];
        colors = new Color[lockOnRects.Length];
        alphas = new float[lockOnRects.Length];

        for (int i = 0; i < lockOnRects.Length; i++)
        {
            positions[i] = lockOnRects[i].localPosition;
            if (lockOnRects[i].TryGetComponent(out Image image))
                colors[i] = image.color;
            if (lockOnRects[i].TryGetComponent(out CanvasGroup group))
                alphas[i] = group.alpha;
        }
    }

    private void UpdateLockOnRectsDuringFade(Vector3[] startPositions, Color[] startColors, float[] startAlphas, float t)
    {
        for (int i = 0; i < lockOnRects.Length; i++)
        {
            RectTransform rect = lockOnRects[i];
            rect.localPosition = Vector3.Lerp(startPositions[i], startOffsets[i], t);
            if (rect.TryGetComponent(out Image image))
                image.color = Color.Lerp(startColors[i], lockOnStartColor, t);
            if (rect.TryGetComponent(out CanvasGroup group))
                group.alpha = Mathf.Lerp(startAlphas[i], 0.5f, t);
        }
    }

    private void DeactivateLockOnRects()
    {
        foreach (var rect in lockOnRects)
            rect.gameObject.SetActive(false);
    }


    private IEnumerator LockOnSequence()
    {
        yield return SetupLockOnRects();
        yield return AnimateLockOnApproach();
        yield return AnimateLockOnPulse();
        FinalizeLockOn();
        lockOnRoutine = null;
        isLockedOn = true;
    }

    private IEnumerator SetupLockOnRects()
    {
        float rotationOffset = Random.Range(-45f, 45f);
        spinSpeeds = new float[lockOnRects.Length];

        for (int i = 0; i < lockOnRects.Length; i++)
        {
            spinSpeeds[i] = Random.Range(20f, 40f) * (Random.value > 0.5f ? 1f : -1f);
            var rect = lockOnRects[i];
            rect.gameObject.SetActive(true);
            rect.localScale = Vector3.one * 1.5f;
            rect.localPosition = startOffsets[i % startOffsets.Length];
            rect.localRotation = Quaternion.Euler(0, 0, rotationOffset);

            if (rect.TryGetComponent(out Image image))
                image.color = lockOnStartColor;

            SetAlpha(rect, 0f);
        }

        yield break;
    }

    private IEnumerator AnimateLockOnApproach()
    {
        float elapsed = 0f;

        while (elapsed < lockOnDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / lockOnDuration;

            for (int i = 0; i < lockOnRects.Length; i++)
            {
                var rect = lockOnRects[i];
                rect.localPosition = Vector3.Lerp(startOffsets[i % startOffsets.Length], Vector3.zero, t);
                rect.localScale = Vector3.one * Mathf.Lerp(1.5f, 1f, t);
                rect.Rotate(Vector3.forward, spinSpeeds[i] * Time.deltaTime);
                SetAlpha(rect, Mathf.Lerp(0.5f, 1f, alphaCurve.Evaluate(t)));

                if (rect.TryGetComponent(out Image image))
                    image.color = Color.Lerp(lockOnStartColor, lockOnFinalColor, t);
            }

            yield return null;
        }
    }

    private IEnumerator AnimateLockOnPulse()
    {
        float pulseTime = 0.25f;
        float pulseElapsed = 0f;
        while (pulseElapsed < pulseTime)
        {
            pulseElapsed += Time.deltaTime;
            float p = pulseElapsed / pulseTime;
            float scale = Mathf.Lerp(1f, 1.75f, scaleCurve.Evaluate(p));

            foreach (var rect in lockOnRects)
                rect.localScale = Vector3.one * scale;

            yield return null;
        }
    }

    private void FinalizeLockOn()
    {
        for (int i = 0; i < lockOnRects.Length; i++)
        {
            RectTransform rect = lockOnRects[i];
            rect.localScale = Vector3.one;
            rect.localPosition = startOffsets[i] / 2;
            rect.localRotation = Quaternion.identity;
            SetAlpha(rect, 1f);
        }
    }

    private void SetAlpha(RectTransform rect, float alpha)
    {
        if (rect.TryGetComponent(out CanvasGroup group))
            group.alpha = alpha;
    }
    private void SetMarkerAlpha(float alpha)
    {
        foreach (var image in markerTransform.GetComponentsInChildren<Image>())
        {
            //ik what you're thinking
            //don't even bother, you can't modify the alpha value of an image's colour directly so we have to do this
            //bloody muricans btw, have half a mind to force rename this shit
            Color color = image.color;
            color.a = alpha;
            image.color = color;
        }
        nameText.alpha = alpha;
        distanceText.alpha = alpha;
        velocityText.alpha = alpha;
    }
}
