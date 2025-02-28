using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RadarSystem : MonoBehaviour
{
    [Header("Radar Settings")]
    public RectTransform radarPanel;
    public float radarRange = 500f;
    public Transform player;
    public Camera playerCamera;
    public float radarSize = 100f;

    [Header("Blip Settings")]
    public GameObject redTeamBlipPrefab;
    public GameObject blueTeamBlipPrefab;
    public GameObject redArrowBlipPrefab;
    public GameObject blueArrowBlipPrefab;
    public Transform blipContainer;
    public RectTransform playerBlip;
    public RectTransform fovConeUI;
    public Color playerHighlightColor = Color.white;
    [SerializeField] private float minFadeDistance = 100f;
    [SerializeField] private float maxFadeDistance = 500f;
    [SerializeField] private float flashSpeed = 2f;
    [SerializeField] private float edgeBuffer = 5f;

    [Header("Direction Markers")]
    public RectTransform directionMarkerContainer;

    private bool isRedPlayer = false;
    private List<GameObject> redTeamBlips = new();
    private List<GameObject> blueTeamBlips = new();
    private List<GameObject> redTeamArrows = new();
    private List<GameObject> blueTeamArrows = new();
    private int activeRedBlips = 0;
    private int activeBlueBlips = 0;
    private int activeRedArrows = 0;
    private int activeBlueArrows = 0;

    private void Start()
    {
        int initialPoolSize = 20;
        for (int i = 0; i < initialPoolSize; i++)
        {
            redTeamBlips.Add(CreateBlip(redTeamBlipPrefab));
            blueTeamBlips.Add(CreateBlip(blueTeamBlipPrefab));
            redTeamArrows.Add(CreateBlip(redArrowBlipPrefab));
            blueTeamArrows.Add(CreateBlip(blueArrowBlipPrefab));
        }

        InitializeLocalPlayerBlip();
    }

    private void Update()
    {
        UpdateRadar();
        UpdateDirectionMarkers();
    }

    private void InitializeLocalPlayerBlip()
    {
        if (SpawnManager.Instance == null || !player.TryGetComponent(out PlayerController localPlayer))
            return;

        isRedPlayer = TeamManager.Instance.GetTeam(localPlayer.OwnerClientId) == Team.RedTeam;
        GameObject blip = GetOrCreateBlip(isRedPlayer);
        playerBlip = blip.GetComponent<RectTransform>();

        playerBlip.gameObject.SetActive(true);
        playerBlip.anchoredPosition = Vector2.zero;
        playerBlip.localRotation = Quaternion.identity;
    }

    private void UpdateRadar()
    {
        if (SpawnManager.Instance == null) return;

        activeRedBlips = isRedPlayer ? 1 : 0;
        activeBlueBlips = isRedPlayer ? 0 : 1;
        activeRedArrows = 0;
        activeBlueArrows = 0;

        float playerYaw = player.eulerAngles.y;
        PlayerController localPlayer = player.GetComponent<PlayerController>();

        foreach (PlayerController otherPlayer in SpawnManager.Instance.activePlayers)
        {
            if (otherPlayer == null || otherPlayer == localPlayer) continue;

            Vector3 relativePosition = otherPlayer.transform.position - player.position;
            float distance = relativePosition.magnitude;
            if (distance > radarRange) continue;

            bool isRedTeam = TeamManager.Instance.GetTeam(otherPlayer.OwnerClientId) == Team.RedTeam;
            Vector2 radarPos = GetBlipPosition(relativePosition, out bool isOffScreen);
            float fadeAmount = CalculateBlipFade(relativePosition, distance);

            if (isOffScreen)
                HandleDirectionalArrow(radarPos, isRedTeam, playerYaw);
            else
                HandleBlipVisibility(radarPos, otherPlayer, fadeAmount, isRedTeam, playerYaw);
        }

        DeactivateUnusedBlips();
    }
    private void HandleBlipVisibility(Vector2 radarPos, PlayerController otherPlayer, float fadeAmount, bool isRedTeam, float playerYaw)
    {
        GameObject blip = GetOrCreateBlip(isRedTeam);
        RectTransform blipTransform = blip.GetComponent<RectTransform>();

        blipTransform.anchoredPosition = RotateBlipPosition(radarPos, playerYaw);
        blipTransform.rotation = Quaternion.Euler(0, 0, -otherPlayer.transform.eulerAngles.y);

        Color blipColor = blip.GetComponent<Image>().color;
        blipColor.a = fadeAmount;
        blip.GetComponent<Image>().color = blipColor;
        blip.SetActive(true);
    }

    private void HandleDirectionalArrow(Vector2 radarPos, bool isRedTeam, float playerYaw)
    {
        GameObject arrowBlip = GetOrCreateArrowBlip(isRedTeam);
        RectTransform arrowTransform = arrowBlip.GetComponent<RectTransform>();

        arrowTransform.anchoredPosition = RotateBlipPosition(radarPos, playerYaw);
        arrowTransform.localRotation = Quaternion.LookRotation(Vector3.forward, radarPos);

        arrowBlip.SetActive(true);
    }
    private Vector2 RotateBlipPosition(Vector2 position, float playerYaw)
    {
        float angle = -playerYaw * Mathf.Deg2Rad;
        float cos = Mathf.Cos(angle);
        float sin = Mathf.Sin(angle);

        return new Vector2(
            cos * position.x - sin * position.y,
            sin * position.x + cos * position.y
        );
    }
    private void UpdateDirectionMarkers()
    {
        if (!directionMarkerContainer) return;

        float playerYaw = player.eulerAngles.y;
        directionMarkerContainer.localRotation = Quaternion.Euler(0, 0, -playerYaw);
    }

    private Vector2 GetBlipPosition(Vector3 relativePosition, out bool isOffScreen)
    {
        Vector2 radarPos = new(
            (relativePosition.x / radarRange) * radarSize,
            (relativePosition.z / radarRange) * radarSize
        );

        isOffScreen = radarPos.magnitude > radarSize * 0.9f;
        if (isOffScreen)
            radarPos = radarPos.normalized * ((radarSize * 0.9f) - edgeBuffer);
        return radarPos;
    }

    private GameObject GetOrCreateBlip(bool isRedTeam)
    {
        List<GameObject> pool = isRedTeam ? redTeamBlips : blueTeamBlips;
        int activeCount = isRedTeam ? activeRedBlips++ : activeBlueBlips++;

        if (activeCount < pool.Count) return pool[activeCount];

        GameObject newBlip = CreateBlip(isRedTeam ? redTeamBlipPrefab : blueTeamBlipPrefab);
        pool.Add(newBlip);
        return newBlip;
    }

    private GameObject GetOrCreateArrowBlip(bool isRedTeam)
    {
        List<GameObject> pool = isRedTeam ? redTeamArrows : blueTeamArrows;
        int activeCount = isRedTeam ? activeRedArrows++ : activeBlueArrows++;

        if (activeCount < pool.Count) return pool[activeCount];

        GameObject newBlip = CreateBlip(isRedTeam ? redArrowBlipPrefab : blueArrowBlipPrefab);
        pool.Add(newBlip);
        return newBlip;
    }

    private GameObject CreateBlip(GameObject prefab)
    {
        GameObject blip = Instantiate(prefab, blipContainer);
        blip.SetActive(false);
        return blip;
    }

    private float CalculateBlipFade(Vector3 relativePosition, float distance)
    {
        return Mathf.InverseLerp(maxFadeDistance, minFadeDistance, distance);
    }

    private void DeactivateUnusedBlips()
    {
        DeactivateBlipPool(redTeamBlips, activeRedBlips);
        DeactivateBlipPool(blueTeamBlips, activeBlueBlips);
        DeactivateBlipPool(redTeamArrows, activeRedArrows);
        DeactivateBlipPool(blueTeamArrows, activeBlueArrows);
    }

    private void DeactivateBlipPool(List<GameObject> pool, int activeCount)
    {
        for (int i = activeCount; i < pool.Count; i++)
            if (pool[i].activeSelf) pool[i].SetActive(false);
    }
}
