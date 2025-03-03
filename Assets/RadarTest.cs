using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RadarTest: MonoBehaviour
{
    public RectTransform radarUI;  // Assign a UI panel as radar background
    public Transform player;       // Assign the player object
    public float radarRange = 50f;
    public float radarSize = 100f;
    public GameObject blipPrefab;  // Assign a small UI element for blips
    public Transform blipContainer;

    private List<GameObject> activeBlips = new();

    private void Update()
    {
        UpdateRadar();
        blipContainer.localRotation = Quaternion.Euler(0, 0, player.eulerAngles.y);
    }

    private void UpdateRadar()
    {
        foreach (var blip in activeBlips)
            Destroy(blip);
        activeBlips.Clear();

        foreach (GameObject obj in GameObject.FindGameObjectsWithTag("RadarTarget"))
        {
            Vector3 relativePos = obj.transform.position - player.position;
            if (relativePos.magnitude > radarRange) continue;

            Vector2 radarPos = new Vector2(relativePos.x, relativePos.z) / radarRange * radarSize;
            GameObject blip = Instantiate(blipPrefab, blipContainer);
            blip.GetComponent<RectTransform>().anchoredPosition = radarPos;
            activeBlips.Add(blip);
        }
    }
}
