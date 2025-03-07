using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using System.Collections;
using TMPro;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public GameEvent respawnEvent;
    public Camera playerCamera;
    public GameObject respawningPanel;
    public GameObject damageableMarkerPrefab;
    public GameObject pickUpMarkerPrefab;
    public Transform markerContainer;
    public float detectionRadius = 200f;
    public HealthComponent playerHealth;
    public Slider playerHealthBar;
    public TMP_Text primaryWeaponAmmoCountText;
    public TMP_Text primaryWeaponMaxAmmoCountText;

#pragma warning disable IDE0044
    private Dictionary<HealthComponent, HUDMarker> activeDamageableMarkers = new();
    private List<HUDMarker> damageableMarkerPool = new();
    private List<HealthComponent> damageableTargets = new();
    private Dictionary<PickUp, HUDMarker> activePickUpMarkers = new();
    private List<HUDMarker> pickUpMarkerPool = new();
    private List<PickUp> pickUpTargets = new();
#pragma warning restore IDE0044

    [HideInInspector] public Collider[] detectedColliders = new Collider[100];

    private void Start()
    {
        InitializeMarkerPool();
        respawnEvent.OnEventRaised += EnableRespawnPanel;
    }
    private void OnDisable() => respawnEvent.OnEventRaised -= EnableRespawnPanel;
    private void EnableRespawnPanel(GameObject go)
    {
        respawningPanel.SetActive(true);
        go.SetActive(false);
        StartCoroutine(RespawnCoroutine(5f, go));
    }

    private IEnumerator RespawnCoroutine(float respawnTime, GameObject go)
    {
        while (respawnTime > 0)
        {
            respawnTime -= Time.deltaTime;
            yield return null;
        }
        go.SetActive(true);
        respawningPanel.SetActive(false);
        yield break;
    }
    private void Update() => UpdateTargetMarkers();

    private void InitializeMarkerPool()
    {
        for (int i = 0; i < 10; i++)
        {
            CreateNewMarker(damageableMarkerPool, damageableMarkerPrefab);
            CreateNewMarker(pickUpMarkerPool, pickUpMarkerPrefab);
        }
    }
    private HUDMarker GetPooledMarker(List<HUDMarker> markerPool, GameObject markerPrefab)
    {
        var availableMarker = markerPool.FirstOrDefault(m => !m.gameObject.activeSelf);
        if (availableMarker != null)
        {
            availableMarker.gameObject.SetActive(true);
            return availableMarker;
        }

       return CreateNewMarker(markerPool, markerPrefab);
    }
    private HUDMarker CreateNewMarker(List<HUDMarker> markerPool, GameObject markerPrefab)
    {
        GameObject newMarker = Instantiate(markerPrefab, markerContainer);
        HUDMarker newHudMarker = newMarker.GetComponent<HUDMarker>();
        markerPool.Add(newHudMarker);
        return newHudMarker;
    }
    private void UpdateTargetMarkers()
    {
        GetAllTargetsInRange();

        UpdateMarkers(damageableTargets, activeDamageableMarkers,damageableMarkerPool,damageableMarkerPrefab);
        UpdateMarkers(pickUpTargets, activePickUpMarkers, pickUpMarkerPool, pickUpMarkerPrefab);
    }

    private void UpdateMarkers<T>(List<T> targets, Dictionary<T, HUDMarker> activeMarkers, List<HUDMarker> markerPool, GameObject markerPrefab) where T : MonoBehaviour
    {
        foreach (var target in targets)
        {
            if (target is HealthComponent healthTarget && healthTarget == playerHealth)
                continue;

            if (!activeMarkers.ContainsKey(target))
            {
                HUDMarker marker = GetPooledMarker(markerPool, markerPrefab);
                marker.gameObject.SetActive(true);
                marker.Initialize(target.gameObject, this);
                activeMarkers[target] = marker;
            }

            activeMarkers[target].UpdateMarker(playerCamera);
        }

        List<T> toRemove = activeMarkers.Keys
            .Where(target => !targets.Contains(target) || IsOutOfView(target.transform))
            .ToList();

        foreach (var target in toRemove)
            RemoveItem(activeMarkers, target);
    }

    private bool IsOutOfView(Transform target)
    {
        Vector3 viewportPos = playerCamera.WorldToViewportPoint(target.position);
        return viewportPos.z < 0 || viewportPos.x < 0 || viewportPos.x > 1 || viewportPos.y < 0 || viewportPos.y > 1;
    }

    private void GetAllTargetsInRange()
    {
        damageableTargets.Clear();
        pickUpTargets.Clear();
        int numColliders = Physics.OverlapSphereNonAlloc(playerCamera.transform.position, detectionRadius, detectedColliders);

        for (int i = 0; i < numColliders; i++)
            if (detectedColliders[i].TryGetComponent(out HealthComponent health))
                damageableTargets.Add(health);
            else if (detectedColliders[i].TryGetComponent(out PickUp pickUp))
                pickUpTargets.Add(pickUp);
    }

    private void RemoveItem<TKey, TValue>(Dictionary<TKey, TValue> dictionary, TKey target) where TValue : HUDMarker
    {
        if (!dictionary.ContainsKey(target))
            return;

        dictionary[target].Cleanup(false);
        dictionary.Remove(target);
    }
}
