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
    public GameObject hpBlipPrefab;
    public GameObject pickupBlipPrefab;
    public Transform blipContainer;
    public RectTransform playerBlip;
    public RectTransform fovConeUI;
    [SerializeField] private float minFadeDistance = 100f;
    [SerializeField] private float maxFadeDistance = 500f;
    //[SerializeField] private float flashSpeed = 2f;
    [SerializeField] private float edgeBuffer = 5f;

    [Header("Direction Markers")]
    public RectTransform directionMarkerContainer;

    private bool isRedPlayer = false;
    private List<GameObject> redTeamBlips = new();
    private List<GameObject> blueTeamBlips = new();
    private List<GameObject> redTeamArrows = new();
    private List<GameObject> blueTeamArrows = new();
    private List<GameObject> hpBlips = new();
    private List<GameObject> pickupBlips = new();
   
    private int activeRedBlips = 0;
    private int activeBlueBlips = 0;
    private int activeRedArrows = 0;
    private int activeBlueArrows = 0;
    private int activePickupBlips = 0;
    private int activeHPBlips = 0;

    private UIManager uiManager;
    private void Start()
    {
        for (int i = 0; i < 5; i++)
        {
            redTeamBlips.Add(CreateBlip(redTeamBlipPrefab));
            blueTeamBlips.Add(CreateBlip(blueTeamBlipPrefab));
            redTeamArrows.Add(CreateBlip(redArrowBlipPrefab));
            blueTeamArrows.Add(CreateBlip(blueArrowBlipPrefab));
        }
        for (int i = 0; i < 50; i++)
        {
            hpBlips.Add(CreateBlip(hpBlipPrefab));
            pickupBlips.Add(CreateBlip(pickupBlipPrefab));
        }
        uiManager = GetComponent<UIManager>();
        InitializeLocalPlayerBlip();
    }

    private void Update()
    {
        UpdateRadar();
        UpdateDirectionMarkers();
        if (uiManager == null)
            return; 
        UpdateHPBlips(uiManager);
        UpdatePickupBlips(uiManager);
    }

    private void InitializeLocalPlayerBlip()
    {
        if (SpawnManager.Instance == null || !player.TryGetComponent(out PlayerController localPlayer))
            return;

        isRedPlayer = TeamManager.Instance.GetTeam(localPlayer.OwnerClientId) == Team.RedTeam;
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

        activeRedBlips = 0;
        activeBlueBlips = 0;
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
            float fadeAmount = CalculateBlipFade(distance);

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
        //if (isOffScreen)
        //    radarPos = radarPos.normalized * ((radarSize * 0.9f) - edgeBuffer);
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
    private GameObject GetOrCreateBlip(List<GameObject> pool, GameObject prefab, int count)
    {
        if(count<pool.Count) return pool[count];

        GameObject newBlip = Instantiate(prefab, blipContainer);
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

    private float CalculateBlipFade(float distance) => Mathf.InverseLerp(maxFadeDistance, minFadeDistance, distance);

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
    private void UpdateHPBlips(UIManager uiManager)
    {
        activeHPBlips = 0;

        foreach (Collider col in uiManager.detectedColliders)
        {
            if (col == null || !col.TryGetComponent(out HealthComponent _) || col.TryGetComponent(out PlayerController _)) continue;
            Vector3 relativePosition = col.transform.position - player.position;
            if (relativePosition.magnitude > radarRange) continue;
            GameObject blip = GetOrCreateBlip(hpBlips, hpBlipPrefab, activeHPBlips);
            blip.GetComponent<RectTransform>().anchoredPosition = GetBlipPosition(relativePosition, out _);
            blip.SetActive(true);
            activeHPBlips++;
        }
        DeactivateBlipPool(hpBlips, activeHPBlips);
    }

    private void UpdatePickupBlips(UIManager uiManager)
    {
        activePickupBlips = 0;

        foreach (Collider col in uiManager.detectedColliders)
        {
            if (col == null || !col.TryGetComponent(out PickUp pickup) || !pickup.isActive) continue;
            Vector3 relativePosition = col.transform.position - player.position;
            if (relativePosition.magnitude > radarRange) continue;
            GameObject blip = GetOrCreateBlip(pickupBlips, pickupBlipPrefab, activePickupBlips);
            blip.GetComponent<RectTransform>().anchoredPosition = GetBlipPosition(relativePosition, out _);
            blip.SetActive(true);
            activePickupBlips++;
        }

        DeactivateBlipPool(pickupBlips, activePickupBlips);
    }

}
