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
    public GameObject hpBlipPrefab;
    public GameObject pickupBlipPrefab;
    public Transform blipContainer;
    public RectTransform playerBlip;

    [Header("Radar Configs")]
    public List<RadarVisualConfig> radarConfigs;

    [Header("Direction Markers")]
    public RectTransform directionMarkerContainer;

    private List<GameObject> hpBlips = new();
    private List<GameObject> pickupBlips = new();
    private int activePickupBlips = 0;
    private int activeHPBlips = 0;
    private RadarVisualConfig localPlayerConfig;

    private UIManager uiManager;

    private void Start()
    {
        foreach (var config in radarConfigs)
        {
            InitializeBlips(config.blipPool, config.blipPrefab, 5);
            InitializeBlips(config.arrowPool, config.arrowPrefab, 5);
        }

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
            col => col.TryGetComponent(out HealthComponent _) && !col.TryGetComponent(out Controller _));
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

        Team team = localPlayer.Team;
        localPlayerConfig = radarConfigs.Find(cfg => cfg.team == team);

        if (localPlayerConfig == null)
        {
            Debug.LogError("No radar config found for local player team");
            return;
        }

        GameObject blip = Instantiate(localPlayerConfig.blipPrefab, radarPanel.transform);
        playerBlip = blip.GetComponent<RectTransform>();

        playerBlip.gameObject.SetActive(true);
        playerBlip.transform.SetParent(radarPanel.transform);
        playerBlip.anchoredPosition = Vector2.zero;
        playerBlip.localRotation = Quaternion.identity;
    }

    private void UpdateRadar()
    {
        if (SpawnManager.Instance == null) return;

        foreach (var config in radarConfigs)
        {
            config.activeBlips = 0;
            config.activeArrows = 0;
        }

        PlayerController localPlayer = player.GetComponent<PlayerController>();

        foreach (Controller otherPlayer in SpawnManager.Instance.activeControllers)
        {
            if (otherPlayer == null || otherPlayer == localPlayer) continue;

            Vector3 relativePosition = otherPlayer.transform.position - player.position;
            float distance = relativePosition.magnitude;

            Team team = otherPlayer.Team;
            RadarVisualConfig config = radarConfigs.Find(cfg => cfg.team == team);
            if (config == null) 
                throw new MissingReferenceException("player doesn't have config");

            Vector2 radarPos = GetBlipPosition(relativePosition, out bool isOffScreen);
            float fadeAmount = Mathf.InverseLerp(maxFadeDistance, minFadeDistance, distance);

            if (isOffScreen)
                HandleDirectionalArrow(radarPos, config);
            else
                HandleBlipVisibility(radarPos, otherPlayer, fadeAmount, config);
        }

        foreach (var config in radarConfigs)
        {
            DeactivateBlipPool(config.blipPool, config.activeBlips);
            DeactivateBlipPool(config.arrowPool, config.activeArrows);
        }
    }

    private void HandleBlipVisibility(Vector2 radarPos, Controller otherPlayer, float fadeAmount, RadarVisualConfig config)
    {
        GameObject blip = GetOrCreateBlip(config.blipPool, config.blipPrefab, ref config.activeBlips);
        RectTransform blipTransform = blip.GetComponent<RectTransform>();

        blipTransform.anchoredPosition = radarPos;
        blipTransform.rotation = Quaternion.Euler(0, 0, -otherPlayer.transform.eulerAngles.y);

        Color blipColor = blip.GetComponent<Image>().color;
        blipColor.a = fadeAmount;
        blip.GetComponent<Image>().color = blipColor;
        blip.SetActive(true);
    }

    private void HandleDirectionalArrow(Vector2 radarPos, RadarVisualConfig config)
    {
        GameObject arrowBlip = GetOrCreateBlip(config.arrowPool, config.arrowPrefab, ref config.activeArrows);
        RectTransform arrowTransform = arrowBlip.GetComponent<RectTransform>();

        arrowTransform.anchoredPosition = radarPos;
        arrowTransform.localRotation = Quaternion.LookRotation(Vector3.forward, radarPos);
        arrowBlip.SetActive(true);
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

    private GameObject GetOrCreateBlip(List<GameObject> pool, GameObject prefab, ref int activeCount)
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
            GameObject blip = GetOrCreateBlip(blipList, prefab, ref activeCount);
            blip.GetComponent<RectTransform>().anchoredPosition = GetBlipPosition(relativePosition, out _);
            blip.SetActive(true);
            activeCount++;
        }

        DeactivateBlipPool(blipList, activeCount);
    }
}
