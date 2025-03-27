using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
public class FeedMessage : MonoBehaviour
{
    public TMP_Text messageText;
    public Image iconImage;
    public float scrollSpeed = 20f;
    public float defaultFontSize = 12f;

    private float lifetime;
    private float elapsed;
    private CanvasGroup canvasGroup;

    private void Awake() => canvasGroup = GetComponent<CanvasGroup>();

    public void ApplyEntryAnimation(float duration, float popScale)
    {
        if (isActiveAndEnabled)
            StartCoroutine(AnimateIn(duration, popScale));
    }

    public void ApplyExitAnimation(float lifetimeDuration, float fadeOutDuration)
    {
        if (isActiveAndEnabled)
            StartCoroutine(AnimateOutAfterDelay(lifetimeDuration, fadeOutDuration));
    }

    private IEnumerator AnimateIn(float duration, float popScale)
    {
        float t = 0f;
        Vector3 originalScale = transform.localScale;
        Vector3 startScale = originalScale * popScale;
        transform.localScale = startScale;
        canvasGroup.alpha = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;
            float normalized = t / duration;
            transform.localScale = Vector3.Lerp(startScale, originalScale, normalized);
            canvasGroup.alpha = normalized;
            yield return null;
        }

        transform.localScale = originalScale;
        canvasGroup.alpha = 1f;
    }

    private IEnumerator AnimateOutAfterDelay(float delay, float duration)
    {
        yield return new WaitForSeconds(delay - duration);

        float t = 0f;
        float startAlpha = canvasGroup.alpha;

        while (t < duration)
        {
            t += Time.deltaTime;
            float normalized = t / duration;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, normalized);
            yield return null;
        }

        gameObject.SetActive(false);
    }
    private void InitializeMessage(float duration, Sprite icon, float fontSize)
    {
        elapsed = 0f;
        lifetime = duration;
        iconImage.enabled = icon != null;
        iconImage.sprite = icon;
        messageText.fontSize = fontSize;
    }
    public void DisplayKillMessage(string killer, Color killerColor, string victim, Color victimColor, Sprite icon, float duration)
    {
        InitializeMessage(duration, icon, defaultFontSize);
        messageText.text = "<color=#" + ColorUtility.ToHtmlStringRGBA(killerColor) + ">" + killer + 
            "</color> eliminated <color=#" + ColorUtility.ToHtmlStringRGBA(victimColor) + ">" + victim + "</color>";
    }

    public void DisplayEventMessage(string text, Color color, float duration)
    {
        InitializeMessage(duration, null, defaultFontSize);
        messageText.text = "<color=#" + ColorUtility.ToHtmlStringRGBA(color) + ">" + text + "</color>";
    }

    public void DisplayStyledEventMessage(string text, Color color, float duration, float fontSize)
    {
        InitializeMessage(duration,null,fontSize);
        messageText.text = "<b><i><color=#" + ColorUtility.ToHtmlStringRGBA(color) + ">" + text + "</color></i></b>";
    }

    public void DisplayEnvironmentDeathMessage(string victim, Color victimColor, string cause, Sprite icon, float duration)
    {
       InitializeMessage(duration,icon,defaultFontSize);
        messageText.text = "<color=#" + ColorUtility.ToHtmlStringRGBA(victimColor) + ">" + victim + "</color> died to <i>" + cause + "</i>";
    }

    private void Update()
    {
        elapsed += Time.deltaTime;
        if (elapsed >= lifetime)
        {
            StopAllCoroutines();
            gameObject.SetActive(false);
        }
    }

    public void ReduceLifetimeTo(float newLifetime) => lifetime = Mathf.Min(lifetime, newLifetime);
}