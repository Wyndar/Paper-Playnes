using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class DamageDirectionUIManager : MonoBehaviour
{
    [SerializeField] private GameEvent damageSourceLocationEvent;
    [SerializeField] private Image[] sectorIndicators = new Image[8];
    [SerializeField] private float indicatorDuration = 0.5f;
    [SerializeField] private Color flashColor = new(1, 0, 0, 0.6f);
    [SerializeField] private Color transparentColor = new(1, 0, 0, 0);

#pragma warning disable IDE0044 // Add readonly modifier
    private Coroutine[] fadeCoroutines = new Coroutine[8];
#pragma warning restore IDE0044 // Add readonly modifier
    private Transform playerTransform;

    public void Initialize(Transform player) => playerTransform = player;

    private void OnEnable() => damageSourceLocationEvent.OnLocationEventRaised += ShowDamageDirection;
    private void OnDisable() => damageSourceLocationEvent.OnLocationEventRaised -= ShowDamageDirection;
    public void ShowDamageDirection(Vector3 attackerPosition)
    {
        if (!playerTransform)
            throw new MissingReferenceException("Player tranform not set");

        Vector3 localDir = playerTransform.InverseTransformDirection((attackerPosition - playerTransform.position).normalized);
        float angle = Mathf.Atan2(localDir.x, localDir.z) * Mathf.Rad2Deg;
        if (angle < 0) angle += 360f;

        int sector = Mathf.FloorToInt(angle / 45f) % 8;
        FlashIndicator(sector);
    }

    private void FlashIndicator(int sectorIndex)
    {
        if (sectorIndex < 0 || sectorIndex >= sectorIndicators.Length) return;

        if (fadeCoroutines[sectorIndex] != null)
            StopCoroutine(fadeCoroutines[sectorIndex]);

        sectorIndicators[sectorIndex].color = flashColor;
        fadeCoroutines[sectorIndex] = StartCoroutine(FadeOut(sectorIndicators[sectorIndex], sectorIndex));
    }

    private IEnumerator FadeOut(Image img, int index)
    {
        float timer = 0f;
        Color startColor = flashColor;

        while (timer < indicatorDuration)
        {
            timer += Time.deltaTime;
            img.color = Color.Lerp(startColor, transparentColor, timer / indicatorDuration);
            yield return null;
        }

        img.color = transparentColor;
        fadeCoroutines[index] = null;
        yield break;
    }
}
