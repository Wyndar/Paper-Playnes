using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;

public class RadarSystem : MonoBehaviour
{
    [Header("Radar Settings")]
    public RectTransform radarPanel;
    public float radarRange = 500f;
    public Transform player;
    public Camera playerCamera;
    public float radarSize = 100f;
    public RectTransform fovConeUI;
    [SerializeField] private float minFadeDistance = 100f;
    [SerializeField] private float maxFadeDistance = 500f;
    [SerializeField] private float edgeBuffer = 5f;

    [Header("Blip Settings")]
    public GameObject redTeamBlipPrefab;
    public GameObject blueTeamBlipPrefab;
    public GameObject redArrowBlipPrefab;
    public GameObject blueArrowBlipPrefab;
    public GameObject hpBlipPrefab;
    public GameObject pickupBlipPrefab;
    public Transform blipContainer;
    public RectTransform playerBlip;

    [Header("Direction Markers")]
    public RectTransform directionMarkerContainer;

    private bool isRedPlayer = false;
#pragma warning disable IDE0044
    private List<GameObject> redTeamBlips = new();
    private List<GameObject> blueTeamBlips = new();
    private List<GameObject> redTeamArrows = new();
    private List<GameObject> blueTeamArrows = new();
    private List<GameObject> hpBlips = new();
    private List<GameObject> pickupBlips = new();
#pragma warning restore IDE0044

    private int activeRedBlips = 0;
    private int activeBlueBlips = 0;
    private int activeRedArrows = 0;
    private int activeBlueArrows = 0;
    private int activePickupBlips = 0;
    private int activeHPBlips = 0;

    private UIManager uiManager;

    private void Start()
    {
        InitializeBlips(redTeamBlips, redTeamBlipPrefab, 5);
        InitializeBlips(blueTeamBlips, blueTeamBlipPrefab, 5);
        InitializeBlips(redTeamArrows, redArrowBlipPrefab, 5);
        InitializeBlips(blueTeamArrows, blueArrowBlipPrefab, 5);
        InitializeBlips(hpBlips, hpBlipPrefab, 50);
        InitializeBlips(pickupBlips, pickupBlipPrefab, 50);

        uiManager = GetComponent<UIManager>();
        if (uiManager == null)
        {
            Debug.LogError("UIManager is missing on RadarSystem.");
            return;
        }
        InitializeLocalPlayerBlip();
    }

    private void Update()
    {
        if (uiManager == null) return;
        UpdateRadar();
        UpdateDirectionMarkers();
        UpdateBlips(uiManager.detectedColliders, hpBlips, hpBlipPrefab, ref activeHPBlips,
            col => col.TryGetComponent(out HealthComponent _) && !col.TryGetComponent(out PlayerController _));
        UpdateBlips(uiManager.detectedColliders, pickupBlips, pickupBlipPrefab, ref activePickupBlips,
            col => col.TryGetComponent(out PickUp pickup) && pickup.isActive);
    }

    private void InitializeBlips(List<GameObject> blipList, GameObject prefab, int count)
    {
        for (int i = 0; i < count; i++)
            blipList.Add(CreateBlip(prefab));
    }

    private void InitializeLocalPlayerBlip()
    {
        if (SpawnManager.Instance == null || !player.TryGetComponent(out PlayerController localPlayer))
            return;

        isRedPlayer = TeamManager.Instance.GetTeam(localPlayer) == Team.RedTeam;
        GameObject blip = isRedPlayer ? Instantiate(redTeamBlipPrefab, radarPanel.transform) : Instantiate(blueTeamBlipPrefab, radarPanel.transform);
        playerBlip = blip.GetComponent<RectTransform>();

        playerBlip.gameObject.SetActive(true);
        playerBlip.transform.SetParent(radarPanel.transform);
        playerBlip.anchoredPosition = Vector2.zero;
        playerBlip.localRotation = Quaternion.identity;
    }

    private void UpdateRadar()
    {
        if (SpawnManager.Instance == null) return;

        ResetActiveBlipCounts();

        float playerYaw = player.eulerAngles.y;
        PlayerController localPlayer = player.GetComponent<PlayerController>();

        foreach (PlayerController otherPlayer in SpawnManager.Instance.activeControllers)
        {
            if (otherPlayer == null || otherPlayer == localPlayer) continue;

            Vector3 relativePosition = otherPlayer.transform.position - player.position;
            float distance = relativePosition.magnitude;

            bool isRedTeam = TeamManager.Instance.GetTeam(otherPlayer) == Team.RedTeam;
            Vector2 radarPos = GetBlipPosition(relativePosition, out bool isOffScreen);
            float fadeAmount = Mathf.InverseLerp(maxFadeDistance, minFadeDistance, distance);

            if (isOffScreen)
                HandleDirectionalArrow(radarPos, isRedTeam, playerYaw);
            else
                HandleBlipVisibility(radarPos, otherPlayer, fadeAmount, isRedTeam, playerYaw);
        }

        DeactivateUnusedBlips();
    }

    private void ResetActiveBlipCounts()
    {
        activeRedBlips = 0;
        activeBlueBlips = 0;
        activeRedArrows = 0;
        activeBlueArrows = 0;
    }

    private void HandleBlipVisibility(Vector2 radarPos, PlayerController otherPlayer, float fadeAmount, bool isRedTeam, float playerYaw)
    {
        GameObject blip = GetOrCreateBlip(isRedTeam ? redTeamBlips : blueTeamBlips, isRedTeam ? redTeamBlipPrefab : blueTeamBlipPrefab, ref activeRedBlips, ref activeBlueBlips);
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
        GameObject arrowBlip = GetOrCreateBlip(isRedTeam ? redTeamArrows : blueTeamArrows, isRedTeam ? redArrowBlipPrefab : blueArrowBlipPrefab, ref activeRedArrows, ref activeBlueArrows);
        RectTransform arrowTransform = arrowBlip.GetComponent<RectTransform>();

        arrowTransform.anchoredPosition = RotateBlipPosition(radarPos, playerYaw);
        arrowTransform.localRotation = Quaternion.LookRotation(Vector3.forward, radarPos);

        arrowBlip.SetActive(true);
    }

    private Vector2 RotateBlipPosition(Vector2 position, float playerYaw)
    {
        float angle = playerYaw * Mathf.Deg2Rad;
        float cos = Mathf.Cos(angle);
        float sin = Mathf.Sin(angle);

        return new Vector2(cos * position.x - sin * position.y, sin * position.x + cos * position.y);
    }

    private void UpdateDirectionMarkers()
    {
        if (!directionMarkerContainer) return;

        float playerYaw = player.eulerAngles.y;
        directionMarkerContainer.localRotation = Quaternion.Euler(0, 0, playerYaw);
        blipContainer.localRotation = Quaternion.Euler(0, 0, playerYaw);
    }

    private Vector2 GetBlipPosition(Vector3 relativePos, out bool isOffScreen)
    {
        Vector2 radarPos = new Vector2(relativePos.x, relativePos.z) / radarRange * radarSize;
        isOffScreen = radarPos.magnitude > radarSize * 0.9f;
        if (isOffScreen)
            radarPos = radarPos.normalized * ((radarSize * 0.9f) - edgeBuffer);
        return radarPos;
    }

    //there's two refs to check which one we're using when called
    #pragma warning disable IDE0060
    private GameObject GetOrCreateBlip(List<GameObject> pool, GameObject prefab, ref int activeCount, ref int activeOtherCount)
    #pragma warning restore IDE0060 
    {
        if (activeCount < pool.Count) return pool[activeCount++];

        GameObject newBlip = CreateBlip(prefab);
        pool.Add(newBlip);
        return newBlip;
    }

    private GameObject CreateBlip(GameObject prefab)
    {
        GameObject blip = Instantiate(prefab, blipContainer);
        blip.SetActive(false);
        return blip;
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

    private void UpdateBlips(Collider[] detectedColliders, List<GameObject> blipList, GameObject prefab, ref int activeCount, Func<Collider, bool> filter)
    {
        activeCount = 0;

        foreach (Collider col in detectedColliders)
        {
            if (col == null || !filter(col)) continue;
            Vector3 relativePosition = col.transform.position - player.position;
            if (relativePosition.magnitude > radarRange) continue;
            GameObject blip = GetOrCreateBlip(blipList, prefab, ref activeCount, ref activeCount);
            blip.GetComponent<RectTransform>().anchoredPosition = GetBlipPosition(relativePosition, out _);
            blip.SetActive(true);
            activeCount++;
        }

        DeactivateBlipPool(blipList, activeCount);
    }
}
