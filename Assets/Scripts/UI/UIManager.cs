using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using TMPro;

public class UIManager : MonoBehaviour
{
    public Camera playerCamera;
    public GameObject damageableMarkerPrefab;
    public Transform markerContainer;
    public float detectionRadius = 200f;
    public HealthComponent playerHealth;
    public TMP_Text primaryWeaponAmmoCountText;
    public TMP_Text primaryWeaponMaxAmmoCountText;

#pragma warning disable IDE0044
    private Dictionary<HealthComponent, HUDMarker> activeDamageableMarkers = new();
    private List<HUDMarker> damageableMarkerPool = new();
#pragma warning restore IDE0044

    [HideInInspector] public Collider[] detectedColliders = new Collider[100];

    private void Start() => InitializeMarkerPool();

    private void Update() => UpdateTargetMarkers();

    private void InitializeMarkerPool()
    {
        for (int i = 0; i < 10; i++)
        {
            GameObject marker = Instantiate(damageableMarkerPrefab, markerContainer);
            marker.SetActive(false);
            HUDMarker hudMarker = marker.GetComponent<HUDMarker>();
            damageableMarkerPool.Add(hudMarker);
        }
    }

    private HUDMarker GetPooledMarker()
    {
        var availableMarker = damageableMarkerPool.FirstOrDefault(m => !m.gameObject.activeSelf);
        if (availableMarker != null)
        {
            availableMarker.gameObject.SetActive(true);
            return availableMarker;
        }

        GameObject newMarker = Instantiate(damageableMarkerPrefab, markerContainer);
        HUDMarker newHudMarker = newMarker.GetComponent<HUDMarker>();
        damageableMarkerPool.Add(newHudMarker);
        return newHudMarker;
    }

    private void UpdateTargetMarkers()
    {
        List<HealthComponent> detectedTargets = GetAllTargetsInRange();

        foreach (var target in detectedTargets)
        {
            if (target == playerHealth)
                continue;
            if (!activeDamageableMarkers.ContainsKey(target))
            {
                HUDMarker marker = GetPooledMarker();
                marker.Initialize(target.gameObject, this);
                activeDamageableMarkers[target] = marker;
            }

            activeDamageableMarkers[target].UpdateMarker(playerCamera);
        }

        List<HealthComponent> toRemove = activeDamageableMarkers.Keys
            .Where(target => !detectedTargets.Contains(target) || IsOutOfView(target))
            .ToList();

        foreach (var target in toRemove)
            RemoveMarker(target);
    }

    private bool IsOutOfView(HealthComponent target)
    {
        Vector3 viewportPos = playerCamera.WorldToViewportPoint(target.transform.position);
        return viewportPos.z < 0 || viewportPos.x < 0 || viewportPos.x > 1 || viewportPos.y < 0 || viewportPos.y > 1;
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
        if (!activeDamageableMarkers.ContainsKey(target))
            return;

        HUDMarker marker = activeDamageableMarkers[target];
        marker.Cleanup(false);
        activeDamageableMarkers.Remove(target);
    }
}
