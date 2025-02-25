using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.Collections;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public Camera playerCamera;
    public GameObject healthBarPrefab;
    public Transform healthBarContainer;
    public float detectionRadius;
    public HealthComponent playerHealth;

    private List<GameObject> healthBarPool = new();
    private Dictionary<HealthComponent, GameObject> activeHealthBars = new();
    private Dictionary<HealthComponent, Slider> healthBarSliders = new();

    private const int POOL_SIZE = 7;
    private const int MAX_ENEMIES = 20;
    private Collider[] detectedColliders = new Collider[MAX_ENEMIES];

    private void Start() => InitializeHealthBarPool();

    private void Update() => UpdateHealthBars();

    private void InitializeHealthBarPool()
    {
        for (int i = 0; i < POOL_SIZE; i++)
        {
            GameObject healthBar = Instantiate(healthBarPrefab, healthBarContainer);
            healthBar.SetActive(false);
            healthBarPool.Add(healthBar);
        }
    }

    private GameObject GetPooledHealthBar()
    {
        var availableHealthBar = healthBarPool.FirstOrDefault(hb => !hb.activeSelf);
        if (availableHealthBar != null)
        {
            availableHealthBar.SetActive(true);
            return availableHealthBar;
        }

        var newHealthBar = Instantiate(healthBarPrefab, healthBarContainer);
        healthBarPool.Add(newHealthBar);
        return newHealthBar;
    }

    private void UpdateHealthBars()
    {
        List<HealthComponent> visibleEnemies = GetEnemiesInView();

        foreach (var enemy in visibleEnemies)
        {
            if (!activeHealthBars.ContainsKey(enemy))
            {
                GameObject healthBar = GetPooledHealthBar();
                activeHealthBars[enemy] = healthBar;

                if (healthBar.TryGetComponent(out Slider slider))
                {
                    healthBarSliders[enemy] = slider;
                    slider.maxValue = enemy.maxHP;
                    slider.value = enemy.currentHP;

                    enemy.OnHealthChanged += UpdateHealthBar;
                    enemy.OnDeath += HandleEnemyDeath;
                }
            }
            activeHealthBars[enemy].SetActive(true);
            UpdateHealthBarPosition(enemy);
        }

        List<HealthComponent> toRemove = new();
        foreach (var kvp in activeHealthBars)
        {
            if (visibleEnemies.Contains(kvp.Key))
                continue;
            StartCoroutine(FadeOutAndDisable(kvp.Value));
            toRemove.Add(kvp.Key);
        }
        foreach (var enemy in toRemove)
            RemoveHealthBar(enemy);
    }

    private List<HealthComponent> GetEnemiesInView()
    {
        List<HealthComponent> enemiesInView = new();
        int numColliders = Physics.OverlapSphereNonAlloc(playerCamera.transform.position, detectionRadius, detectedColliders);

        for (int i = 0; i < numColliders; i++)
        {
            var health = detectedColliders[i].GetComponent<HealthComponent>();
            if (health != playerHealth && health != null && IsInFront(health.transform))
                enemiesInView.Add(health);
        }
        return enemiesInView;
    }

    private bool IsInFront(Transform target)
    {
        Vector3 toTarget = (target.position - playerCamera.transform.position).normalized;
        return Vector3.Dot(playerCamera.transform.forward, toTarget) > 0.5f;
    }

    private void UpdateHealthBarPosition(HealthComponent enemy)
    {
        if (!activeHealthBars.TryGetValue(enemy, out var healthBar))
            return;

        RectTransform rectTransform = healthBar.GetComponent<RectTransform>();

        Vector3 screenPos = playerCamera.WorldToScreenPoint(enemy.transform.position + Vector3.up * 2f);

        if (screenPos.z < 0)
        {
            StartCoroutine(FadeOutAndDisable(healthBar));
            return;
        }

        healthBar.SetActive(true);
        if (healthBar.TryGetComponent(out CanvasGroup canvasGroup))
            canvasGroup.alpha = 1;

        rectTransform.position = screenPos;

        float distance = Vector3.Distance(playerCamera.transform.position, enemy.transform.position);
        float scaleFactor = Mathf.Clamp(1 / distance, 0.5f, 1.5f);
        rectTransform.localScale = Vector3.one * scaleFactor;
    }

    private void UpdateHealthBar(int currentHP, int maxHP)
    {
        var enemy = healthBarSliders.FirstOrDefault(x => x.Value.maxValue == maxHP).Key;
        if (enemy == null) return;

        if (healthBarSliders.TryGetValue(enemy, out var slider))
            slider.value = currentHP;
    }

    private void HandleEnemyDeath(bool isDead)
    {
        if (!isDead) return;

        var enemy = activeHealthBars.Keys.FirstOrDefault(e => e.IsDead);
        if (enemy != null)
            RemoveHealthBar(enemy);
    }

    private void RemoveHealthBar(HealthComponent enemy)
    {
        if (!activeHealthBars.ContainsKey(enemy))
            return;

        GameObject healthBar = activeHealthBars[enemy];

        if (healthBarSliders.ContainsKey(enemy))
        {
            enemy.OnHealthChanged -= UpdateHealthBar;
            enemy.OnDeath -= HandleEnemyDeath;
            healthBarSliders.Remove(enemy);
        }

        StartCoroutine(FadeOutAndDisable(healthBar));

        activeHealthBars.Remove(enemy);
    }

    private IEnumerator FadeOutAndDisable(GameObject healthBar)
    {
        if (!healthBar.TryGetComponent(out CanvasGroup canvasGroup))
            canvasGroup = healthBar.AddComponent<CanvasGroup>();

        float fadeTime = 0.5f;
        while (canvasGroup.alpha > 0)
        {
            canvasGroup.alpha -= Time.deltaTime / fadeTime;
            yield return null;
        }

        healthBar.SetActive(false);
    }
}
