using UnityEngine;
using System.Linq;
using System.Collections.Generic;

public class UIManager : MonoBehaviour
{
    public Camera playerCamera;
    public GameObject markerPrefab;
    public Transform markerContainer;
    public float detectionRadius = 200f;
    public HealthComponent playerHealth;

    private Dictionary<HealthComponent, HUDMarker> activeMarkers = new();
    private List<HUDMarker> markerPool = new();
    private Collider[] detectedColliders = new Collider[20];

    private void Start() => InitializeMarkerPool();

    private void Update() => UpdateTargetMarkers();

    private void InitializeMarkerPool()
    {
        for (int i = 0; i < 10; i++)
        {
            GameObject marker = Instantiate(markerPrefab, markerContainer);
            marker.SetActive(false);
            HUDMarker hudMarker = marker.GetComponent<HUDMarker>();
            markerPool.Add(hudMarker);
        }
    }

    private HUDMarker GetPooledMarker()
    {
        var availableMarker = markerPool.FirstOrDefault(m => !m.gameObject.activeSelf);
        if (availableMarker != null)
        {
            availableMarker.gameObject.SetActive(true);
            return availableMarker;
        }

        GameObject newMarker = Instantiate(markerPrefab, markerContainer);
        HUDMarker newHudMarker = newMarker.GetComponent<HUDMarker>();
        markerPool.Add(newHudMarker);
        return newHudMarker;
    }

    private void UpdateTargetMarkers()
    {
        List<HealthComponent> detectedTargets = GetAllTargetsInRange();

        foreach (var target in detectedTargets)
        {
            if (target == playerHealth)
                continue;
            if (!activeMarkers.ContainsKey(target))
            {
                HUDMarker marker = GetPooledMarker();
                marker.Initialize(target.gameObject, this);
                activeMarkers[target] = marker;
            }

            activeMarkers[target].UpdateMarker(playerCamera);
        }

        List<HealthComponent> toRemove = activeMarkers.Keys.Where(target => !detectedTargets.Contains(target)).ToList();
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
        if (!activeMarkers.ContainsKey(target))
            return;

        HUDMarker marker = activeMarkers[target];
        marker.Cleanup();
        activeMarkers.Remove(target);
    }
}
