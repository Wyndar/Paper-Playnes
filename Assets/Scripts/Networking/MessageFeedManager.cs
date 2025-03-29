using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class MessageFeedManger : NetworkBehaviour
{
    public static MessageFeedManger Instance { get; private set; }
    [Header("Feed Settings")]
    public Transform messageContainer;
    public GameObject messagePrefab;
    public int maxMessagesCount = 6;
    public float baseLifetime = 5f;
    public float minLifetime = 1.5f;
    public float killStreakResetTime = 10f;
    public float messageSpawnDelay = 0.15f;

    [Header("Animation Settings")]
    public float fadeInDuration = 0.25f;
    public float fadeOutDuration = 0.25f;
    public float popScale = 1.2f;

    [Header("Audio Settings")]
    public AudioSource audioSource;
    public AudioClip defaultClip;
    public AudioClip[] killStreakClips; 

    [Header("Team Colors")]
    public Color redColor;
    public Color blueColor;
    public Color yellowColor;
    public Color greenColor;
    public Color defaultColor = Color.white;

    [Header("Kill Icon Assets")]
    public Sprite[] killIcons;
    private readonly Dictionary<string, Sprite> killIconMap = new();

    private readonly Queue<FeedMessage> messageQueue = new();
    private readonly List<GameObject> pooledMessages = new();
    private readonly Dictionary<string, int> killStreaks = new();
    private readonly Dictionary<string, float> lastKillTime = new();
    private readonly Queue<System.Action<FeedMessage>> pendingMessages = new();

    private bool isProcessingQueue = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;
        Sprite[] loadedIcons = Resources.LoadAll<Sprite>("Sprites/Message Icons");
        killIcons = loadedIcons;

        foreach (var icon in loadedIcons)
            if (!killIconMap.ContainsKey(icon.name))
                killIconMap[icon.name] = icon;
    }

    public void AddKillMessage(string killer, Team killerTeam, string victim, Team victimTeam, string iconName)
    {
        Sprite icon = GetKillIcon(iconName);
        PlaySound(defaultClip);
        EnqueueMessage(msg => msg.DisplayKillMessage(killer, GetTeamColor(killerTeam), victim, GetTeamColor(victimTeam), icon, baseLifetime));

        float currentTime = Time.time;

        if (lastKillTime.TryGetValue(killer, out float lastTime) && currentTime - lastTime <= killStreakResetTime)
            killStreaks[killer] = killStreaks.GetValueOrDefault(killer, 1) + 1;
        else
            killStreaks[killer] = 1;

        lastKillTime[killer] = currentTime;

        int streak = killStreaks[killer];
        if (streak >= 2)
        {
            if (streak - 2 < killStreakClips.Length)
                PlaySound(killStreakClips[streak - 2]);

            string streakMessage = streak switch
            {
                2 => $"{killer} is on a DOUBLE KILL!",
                3 => $"{killer} is on a TRIPLE KILL!",
                4 => $"{killer} is on a RAMPAGE!",
                5 => $"{killer} is UNSTOPPABLE!",
                6 => $"{killer} is GODLIKE!",
                _ => $"{killer} is on a {streak}-kill streak!"
            };

            EnqueueMessage(msg => msg.DisplayStyledEventMessage(streakMessage, GetTeamColor(killerTeam), baseLifetime, 15));
        }
    }

    public void AddJoinMessage(string playerName, Team team)
    {
        PlaySound(defaultClip);
        EnqueueMessage(msg => msg.DisplayEventMessage(playerName + " joined the match", GetTeamColor(team), baseLifetime));
    }

    public void AddLeaveMessage(string playerName, Team team)
    {
        PlaySound(defaultClip);
        EnqueueMessage(msg => msg.DisplayEventMessage(playerName + " left the match", GetTeamColor(team), baseLifetime));
    }

    public void AddEnvironmentDeathMessage(string victim, Team victimTeam, string cause, string iconName)
    {
        Sprite icon = GetKillIcon(iconName);
        PlaySound(defaultClip);
        EnqueueMessage(msg => msg.DisplayEnvironmentDeathMessage(victim, GetTeamColor(victimTeam), cause, icon, baseLifetime));
    }

    private void PlaySound(AudioClip clip)
    {
        if (audioSource && clip)
            audioSource.PlayOneShot(clip);
    }

    private void EnqueueMessage(System.Action<FeedMessage> initializer)
    {
        pendingMessages.Enqueue(initializer);
        if (!isProcessingQueue)
            StartCoroutine(ProcessQueue());
    }

    private IEnumerator ProcessQueue()
    {
        isProcessingQueue = true;

        while (pendingMessages.Count > 0)
        {
            var initializer = pendingMessages.Dequeue();
            SpawnMessage(initializer);
            yield return new WaitForSeconds(messageSpawnDelay);
        }

        isProcessingQueue = false;
    }

    private void SpawnMessage(Action<FeedMessage> initializer)
    {
        GameObject messageGO = GetMessageFromPool();
        messageGO.transform.SetParent(messageContainer, false);

        FeedMessage message = messageGO.GetComponent<FeedMessage>();
        initializer.Invoke(message);
        message.ApplyEntryAnimation(fadeInDuration, popScale);
        message.ApplyExitAnimation(baseLifetime, fadeOutDuration);

        messageGO.SetActive(true);
        messageQueue.Enqueue(message);

        if (messageQueue.Count > maxMessagesCount)
            ShortenOldestMessage();
    }

    private void ShortenOldestMessage()
    {
        if (messageQueue.TryDequeue(out FeedMessage oldest))
            oldest.ReduceLifetimeTo(minLifetime);
    }

    private GameObject GetMessageFromPool()
    {
        foreach (var go in pooledMessages)
            if (!go.activeInHierarchy)
                return go;

        GameObject newGO = Instantiate(messagePrefab);
        pooledMessages.Add(newGO);
        return newGO;
    }

    private Sprite GetKillIcon(string iconName)
    {
        if (killIconMap.TryGetValue(iconName, out Sprite icon))
            return icon;

        Debug.LogWarning($"KillFeed: Icon '{iconName}' not found.");
        return null;
    }

    private Color GetTeamColor(Team team) => team switch
    {
        Team.RedTeam => redColor,
        Team.BlueTeam => blueColor,
        Team.YellowTeam => yellowColor,
        Team.GreenTeam => greenColor,
        _ => defaultColor
    };
    [ServerRpc(RequireOwnership = false)]
    public void RequestKillFeedBroadcastAtServerRpc(string killer, Team killerTeam, string victim, Team victimTeam, string iconName)
    => ReceiveKillFeedBroadcastAtClientRpc(killer, killerTeam, victim, victimTeam, iconName);

    [ClientRpc]
    private void ReceiveKillFeedBroadcastAtClientRpc(string killer, Team killerTeam, string victim, Team victimTeam, string iconName)
        => AddKillMessage(killer, killerTeam, victim, victimTeam, iconName);
    [ServerRpc(RequireOwnership = false)]
    public void RequestEnvironmentKillFeedBroadcastAtServerRpc(string victim, Team victimTeam, string cause, string iconName)
    => ReceiveEnvironmentKillFeedBroadcastAtClientRpc(victim, victimTeam, cause, iconName);

    [ClientRpc]
    private void ReceiveEnvironmentKillFeedBroadcastAtClientRpc(string victim, Team victimTeam, string cause, string iconName)
        => AddEnvironmentDeathMessage(victim, victimTeam, cause, iconName);
    [ServerRpc(RequireOwnership = false)]
    public void RequestJoinFeedBroadcastAtServerRpc(string victim, Team victimTeam)
   => ReceiveJoinFeedBroadcastAtClientRpc(victim, victimTeam);

    [ClientRpc]
    private void ReceiveJoinFeedBroadcastAtClientRpc(string victim, Team victimTeam)
        => AddJoinMessage(victim, victimTeam);
    [ServerRpc(RequireOwnership = false)]
    public void RequestLeaveFeedBroadcastAtServerRpc(string victim, Team victimTeam)
  => ReceiveLeaveFeedBroadcastAtClientRpc(victim, victimTeam);

    [ClientRpc]
    private void ReceiveLeaveFeedBroadcastAtClientRpc(string victim, Team victimTeam)
        => AddLeaveMessage(victim, victimTeam);
}
