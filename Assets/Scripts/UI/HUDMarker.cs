using TMPro;
using UnityEngine;
using UnityEngine.UI;

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
    private RectTransform markerTransform;
    private Rigidbody targetRigidbody;
    [SerializeField] private float rotationSpeed = 1f;
    [SerializeField] private float rotationAngle = 5f;
    [SerializeField] private float minScale = 0.5f;
    [SerializeField] private float maxScale = 1.5f;
    [SerializeField] private float fadeOutThreshold = 1000f;
    [SerializeField] private float maxDistance = 1200f;
    [SerializeField] private float midFontScale;
    private Image[] fillImages;
    private Image[] backgroundImages;

    private void Awake()
    {
        markerTransform = GetComponent<RectTransform>();
        if (markerType != HUDMarkerType.Damageable)
            return;
        fillImages = new Image[hpSliders.Length];
        backgroundImages = new Image[hpSliders.Length];
        for (int i = 0; i < hpSliders.Length; i++)
        {
            fillImages[i] = hpSliders[i].fillRect.GetComponent<Image>();
            backgroundImages[i] = hpSliders[i].transform.Find("Background").GetComponent<Image>();
        }
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
        gameObject.SetActive(false);
    }
}
