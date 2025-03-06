using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HUDMarker : MonoBehaviour
{
    public enum HUDMarkerType { Undefined, Damageable, PickUp }
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
    private UIManager manager;
    [SerializeField] private float rotationSpeed = 1f;
    [SerializeField] private float rotationAngle = 5f;
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

    public void Initialize(GameObject target, UIManager manager)
    {
        this.manager = manager;
        targetObject = target;
        targetRigidbody = target.GetComponent<Rigidbody>();
        nameText.text = target.name;
        if (!target.TryGetComponent(out HealthComponent targetHealth))
            return;
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

        Vector3 screenPos = playerCamera.WorldToScreenPoint(targetObject.transform.position);
        markerTransform.position = screenPos;
        markerTransform.rotation = Quaternion.identity;

        float distance = Vector3.Distance(playerCamera.transform.position, targetObject.transform.position);
        distanceText.text = $"{distance:F1}m";

        if (targetRigidbody != null)
        {
            float speed = targetRigidbody.linearVelocity.magnitude;
            velocityText.text = speed > 0.1f ? $"{speed:F1} m/s" : "Stationary";
        }

        ScaleMarkerBasedOnDistance(distance);
        AnimateMarker();
    }

    private void ScaleMarkerBasedOnDistance(float distance)
    {
        float scaleFactor = Mathf.Lerp(1.5f, 0.5f, distance / 200f);
        sliderContainer.localScale = Vector3.one * scaleFactor;
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
        gameObject.SetActive(false);
    }
}
