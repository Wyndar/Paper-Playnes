using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using TMPro;

public class UIManager : MonoBehaviour
{
    public Camera playerCamera;
    public GameObject destructibleMarkerPrefab;
    public Transform markerContainer;
    public float detectionRadius = 200f;
    public HealthComponent playerHealth;
    public TMP_Text primaryWeaponAmmoCountText;
    public TMP_Text primaryWeaponMaxAmmoCountText;

    private Dictionary<HealthComponent, HUDMarker> activedestructibleMarkers = new();
    private List<HUDMarker> destructibleMarkerPool = new();

    [HideInInspector] public Collider[] detectedColliders = new Collider[100];

    private void Start() => InitializeMarkerPool();

    private void Update() => UpdateTargetMarkers();

    private void InitializeMarkerPool()
    {
        for (int i = 0; i < 10; i++)
        {
            GameObject marker = Instantiate(destructibleMarkerPrefab, markerContainer);
            marker.SetActive(false);
            HUDMarker hudMarker = marker.GetComponent<HUDMarker>();
            destructibleMarkerPool.Add(hudMarker);
        }
    }

    private HUDMarker GetPooledMarker()
    {
        var availableMarker = destructibleMarkerPool.FirstOrDefault(m => !m.gameObject.activeSelf);
        if (availableMarker != null)
        {
            availableMarker.gameObject.SetActive(true);
            return availableMarker;
        }

        GameObject newMarker = Instantiate(destructibleMarkerPrefab, markerContainer);
        HUDMarker newHudMarker = newMarker.GetComponent<HUDMarker>();
        destructibleMarkerPool.Add(newHudMarker);
        return newHudMarker;
    }

    private void UpdateTargetMarkers()
    {
        List<HealthComponent> detectedTargets = GetAllTargetsInRange();

        foreach (var target in detectedTargets)
        {
            if (target == playerHealth)
                continue;
            if (!activedestructibleMarkers.ContainsKey(target))
            {
                HUDMarker marker = GetPooledMarker();
                marker.Initialize(target.gameObject, this);
                activedestructibleMarkers[target] = marker;
            }

            activedestructibleMarkers[target].UpdateMarker(playerCamera);
        }

        List<HealthComponent> toRemove = activedestructibleMarkers.Keys.Where(target => !detectedTargets.Contains(target)).ToList();
        foreach (var target in toRemove)
            RemoveMarker(target);
    }

    private List<HealthComponent> GetAllTargetsInRange()
    {
        List<HealthComponent> targets = new();
        int numColliders = Physics.OverlapSphereNonAlloc(playerCamera.transform.position, detectionRadius, detectedColliders);

        for (int i = 0; i < numColliders; i++)
            if (detectedColliders[i].TryGetComponent(out HealthComponent health))
                targets.Add(health);
        return targets;
    }

    private void RemoveMarker(HealthComponent target)
    {
        if (!activedestructibleMarkers.ContainsKey(target))
            return;

        HUDMarker marker = activedestructibleMarkers[target];
        marker.Cleanup(false);
        activedestructibleMarkers.Remove(target);
    }
}
