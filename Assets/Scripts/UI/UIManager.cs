using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using System.Collections;
using TMPro;
using UnityEngine.UI;
using Unity.Netcode;

public class UIManager : MonoBehaviour
{
    [Header("Game Events")]
    public GameEvent respawnEvent;
    public GameEvent primaryWepaonAmmoUpdateEvent;
    public GameEvent secondaryWeaponAmmoUpdateEvent;
    public GameEvent updateSelectedSecondaryWeapon;
    public GameEvent updateTeamScoreEvent;

    [Header("Prefabs")]
    public GameObject damageableMarkerPrefab;
    public GameObject pickUpMarkerPrefab;

    [Header("Player Data")]
    public Camera playerCamera;
    public HealthComponent playerHealth;
    public Slider playerHealthBar;
    public RectTransform crosshairUI;
    public Slider boostSlider;

    [Header("Ammunition UI")]
    public TMP_Text primaryWeaponAmmoCountText;
    public TMP_Text primaryWeaponMaxAmmoCountText;
    public TMP_Text secondaryWeaponAmmoCountText;
    public TMP_Text secondaryWeaponMaxAmmoCountText;
    public Image selectedSecondaryWeaponImage;

    [Header("Score UI")]
    public TMP_Text redTeamScore;
    public TMP_Text blueTeamScore;
    public TMP_Text greenTeamScore;
    public TMP_Text yellowTeamScore;

    public GameObject gameOverPanel;
    public GameObject respawningPanel;
    public Transform markerContainer;
    public float detectionRadius = 200f;

#pragma warning disable IDE0044
    public Dictionary<HealthComponent, HUDMarker> activeDamageableMarkers = new();
    private List<HUDMarker> damageableMarkerPool = new();
    private List<HealthComponent> damageableTargets = new();
    private Dictionary<PickUp, HUDMarker> activePickUpMarkers = new();
    private List<HUDMarker> pickUpMarkerPool = new();
    private List<PickUp> pickUpTargets = new();
    private List<Sprite> secondaryWeaponImages = new();
#pragma warning restore IDE0044

    [HideInInspector] public Collider[] detectedColliders = new Collider[100];

    public void InitializeResources()
    {
        for (int i = 0; i < 10; i++)
        {
            CreateNewMarker(damageableMarkerPool, damageableMarkerPrefab);
            CreateNewMarker(pickUpMarkerPool, pickUpMarkerPrefab);
        }
        secondaryWeaponImages = new(Resources.LoadAll<Sprite>("Sprites/Secondary Weapons").ToList());
    }
    private void OnEnable()
    {
        respawnEvent.OnGameObjectEventRaised += EnableRespawnPanel;
        primaryWepaonAmmoUpdateEvent.OnStatEventRaised += UpdatePrimaryMagText;
        secondaryWeaponAmmoUpdateEvent.OnStatEventRaised += UpdateSecondaryMagText;
        updateSelectedSecondaryWeapon.OnWeaponEventRaised += UpdateSelectedSecondaryWeapon;
        updateTeamScoreEvent.OnTeamEventRaised += UpdateTeamScoreText;
    }
    private void OnDisable()
    {
        respawnEvent.OnGameObjectEventRaised -= EnableRespawnPanel;
        primaryWepaonAmmoUpdateEvent.OnStatEventRaised -= UpdatePrimaryMagText;
        secondaryWeaponAmmoUpdateEvent.OnStatEventRaised -= UpdateSecondaryMagText;
        updateSelectedSecondaryWeapon.OnWeaponEventRaised -= UpdateSelectedSecondaryWeapon;
        updateTeamScoreEvent.OnTeamEventRaised -= UpdateTeamScoreText;
    }

    private void EnableRespawnPanel(GameObject go)
    {
        if (go == playerHealth.gameObject)
            respawningPanel.SetActive(true);
        StartCoroutine(RespawnCoroutine(5f, go));
    }

    private void UpdatePrimaryMagText(int currentMagCount, int magCount)
    {
        primaryWeaponAmmoCountText.text = currentMagCount.ToString();
        primaryWeaponMaxAmmoCountText.text = magCount.ToString();
    }
    private void UpdateSecondaryMagText(int currentMagCount, int magCount)
    {
        secondaryWeaponAmmoCountText.text = currentMagCount.ToString();
        secondaryWeaponMaxAmmoCountText.text = magCount.ToString();
    }
    private void UpdateSelectedSecondaryWeapon(Weapon weapon)
    {
        if (weapon == null) return;
        selectedSecondaryWeaponImage.sprite = secondaryWeaponImages.Find(s => weapon.name.Contains(s.name));
        selectedSecondaryWeaponImage.gameObject.SetActive(selectedSecondaryWeaponImage.sprite != null);
        secondaryWeaponAmmoCountText.gameObject.SetActive(selectedSecondaryWeaponImage.sprite != null);
        secondaryWeaponMaxAmmoCountText.gameObject.SetActive(selectedSecondaryWeaponImage.sprite != null);
        UpdateSecondaryMagText(weapon.ammoInCurrentMagCount, weapon.magInHoldCount);
    }
    private void UpdateTeamScoreText(Team team, int currentTeamScore, int previousTeamScore)
    {
        switch (team)
        {
            case Team.RedTeam:
                redTeamScore.text = currentTeamScore.ToString();
                break;
            case Team.BlueTeam:
                blueTeamScore.text = currentTeamScore.ToString();
                break;
            case Team.GreenTeam:
                greenTeamScore.text = currentTeamScore.ToString();
                break;
            case Team.YellowTeam:
                yellowTeamScore.text = currentTeamScore.ToString();
                break;
            default:
                throw new MissingReferenceException("Team not found");
        }
    }
    private IEnumerator RespawnCoroutine(float respawnTime, GameObject go)
    {
        while (respawnTime > 0)
        {
            respawnTime -= Time.deltaTime;
            yield return null;
        }
        DestructibleNetworkManager.Instance.RequestGameObjectStateChangeAtServerRpc(go.GetComponent<NetworkObject>(), true);
        yield return new WaitForEndOfFrame();
        go.GetComponent<Controller>().Respawn();
        yield return new WaitForEndOfFrame();
        respawningPanel.SetActive(false);
        yield break;
    }
    private void Update() => UpdateTargetMarkers();
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
                marker.Initialize(target.gameObject);
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
        if (!dictionary.ContainsKey(target)) return;

        dictionary[target].Cleanup(false);
        dictionary.Remove(target);
    }
}
