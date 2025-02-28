using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AltimeterSystem : MonoBehaviour
{
    [Header("Altimeter Settings")]
    public Transform player;
    public Slider altitudeSlider;
    public TMP_Text altitudeText;
    public float minAltitude = 0f;
    public float maxAltitude = 500f;

    [Header("Warning System")]
    public AudioSource warningSound;
    public Image warningPanel;
    public TMP_Text warningMessage;
    public Color normalColor = Color.white;
    public Color warningColor = Color.red;
    public float warningStartDistance = 50f;

    [Header("Transparency Settings")]
    public float minAlpha = 0.5f;
    private bool isWarningActive = false;
    private float flashSpeed = 1f;

    private void Start()
    {
        if (altitudeSlider)
        {
            altitudeSlider.minValue = minAltitude;
            altitudeSlider.maxValue = maxAltitude;
        }

        if (warningPanel)
            warningPanel.gameObject.SetActive(false);
    }

    private void Update()
    {
        UpdateAltimeter();
        HandleWarnings();
    }

    private void UpdateAltimeter()
    {
        if (!altitudeSlider || !player) return;

        float altitude = Mathf.Clamp(player.position.y, minAltitude, maxAltitude);
        altitudeSlider.value = altitude;

        if (altitudeText)
            altitudeText.text = $"ALT: {Mathf.RoundToInt(altitude)}m";

        UpdateAltitudeTransparency(altitude);
        UpdateAltitudeColor(altitude);
    }

    private void UpdateAltitudeColor(float altitude)
    {
        float distanceToMin = altitude - minAltitude;
        float distanceToMax = maxAltitude - altitude;

        if (altitudeSlider.fillRect == null)
            return;
        Image sliderFill = altitudeSlider.fillRect.GetComponent<Image>();

        if (distanceToMin < warningStartDistance || distanceToMax < warningStartDistance)
            sliderFill.color = Color.Lerp(normalColor, warningColor, 1f - Mathf.Abs(distanceToMin / warningStartDistance));
        else
            sliderFill.color = normalColor;
    }

    private void UpdateAltitudeTransparency(float altitude)
    {
        float midAltitude = (minAltitude + maxAltitude) / 2;
        float alpha;
        if (altitude > midAltitude)
            alpha = Mathf.Lerp(minAlpha, 1f, (altitude - midAltitude) / (maxAltitude - midAltitude));
        else
            alpha = Mathf.Lerp(minAlpha, 1f, (altitude - minAltitude) / (midAltitude - minAltitude));

        SetUIAlpha(altitudeText, alpha);
        SetUIAlpha(altitudeSlider.fillRect.GetComponent<Image>(), alpha);
        SetUIAlpha(altitudeSlider.handleRect.GetComponent<Image>(), alpha);
    }


    private void SetUIAlpha(Graphic uiElement, float alpha)
    {
        if (uiElement == null) return;
        Color color = uiElement.color;
        color.a = alpha;
        uiElement.color = color;
    }

    private void HandleWarnings()
    {
        if (!warningPanel || !player) return;

        float altitude = player.position.y;
        float distanceToMin = altitude - minAltitude;
        float distanceToMax = maxAltitude - altitude;
        bool isTooLow = distanceToMin < warningStartDistance;
        bool isTooHigh = distanceToMax < warningStartDistance;

        if (isTooLow || isTooHigh)
        {
            if (!isWarningActive)
            {
                warningPanel.gameObject.SetActive(true);
                warningSound.Play();
                isWarningActive = true;
            }

            flashSpeed = Mathf.Lerp(1f, 5f, 1f - Mathf.Abs(distanceToMin / warningStartDistance));

            warningMessage.text = isTooLow ? "RISK OF CRASH" : "STALL ALTITUDE";
            warningMessage.color = Color.Lerp(warningColor, normalColor, Mathf.PingPong(Time.time * flashSpeed, 1));
        }
        else
        {
            if (!isWarningActive)
                return;
            warningPanel.gameObject.SetActive(false);
            warningSound.Stop();
            isWarningActive = false;
        }
    }
}
