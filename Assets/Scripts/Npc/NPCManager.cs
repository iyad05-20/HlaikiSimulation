using System.Collections.Generic;
using UnityEngine;

public class NPCManager : MonoBehaviour
{
    public static NPCManager Instance { get; private set; }

    private readonly Dictionary<string, LLMNpcLogic> _npcById = new Dictionary<string, LLMNpcLogic>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        RefreshCache();
    }

    public void RefreshCache()
    {
        _npcById.Clear();
        var npcs = FindObjectsByType<LLMNpcLogic>(FindObjectsSortMode.None);
        foreach (var npc in npcs)
        {
            if (npc == null || string.IsNullOrEmpty(npc.npcId))
                continue;

            _npcById[npc.npcId] = npc;
        }
    }

    public void ApplyEvent(string npcId, string eventId, int favDelta, int repDelta, string forceEmotion)
    {
        var npc = ResolveNpc(npcId);
        if (npc == null)
            return;

        npc.ApplyExternalEvent(eventId, favDelta, repDelta, forceEmotion);
    }

    public void SetCondition(string npcId, string conditionKey, bool value)
    {
        var npc = ResolveNpc(npcId);
        if (npc == null)
            return;

        npc.SetCondition(conditionKey, value);
    }

    public bool GetCondition(string npcId, string conditionKey)
    {
        var npc = ResolveNpc(npcId);
        if (npc == null)
            return false;

        return npc.GetCondition(conditionKey);
    }

    public void ResetCycle()
    {
        RefreshCache();
    }

    private LLMNpcLogic ResolveNpc(string npcId)
    {
        if (string.IsNullOrEmpty(npcId))
        {
            Debug.LogWarning("[NPCManager] npcId is empty.");
            return null;
        }

        LLMNpcLogic npc;
        if (_npcById.TryGetValue(npcId, out npc) && npc != null)
            return npc;

        RefreshCache();
        if (_npcById.TryGetValue(npcId, out npc) && npc != null)
            return npc;

        Debug.LogWarning($"[NPCManager] NPC '{npcId}' not found in scene.");
        return null;
    }
}
