using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tracks world events and applies them to runtime NPC logic through NPCManager.
/// Keeps event logic centralized so gameplay scripts only trigger event IDs.
/// </summary>
public class EventTracker : MonoBehaviour
{
    public static EventTracker Instance { get; private set; }

    private readonly Dictionary<string, List<NPCEvent>> _eventMap = new Dictionary<string, List<NPCEvent>>
    {
        ["hamid"] = new List<NPCEvent>
        {
            new NPCEvent("player_mentions_guembri_specifically", favDelta: 12, repDelta: 2, pressurePositive: true, conditionId: "player_shows_genuine_curiosity_about_guembri_or_gnawa"),
            new NPCEvent("player_condescending_about_self_taught", favDelta: -15, repDelta: -5, forceEmotion: "closed"),
        },
        ["lalla_fatima"] = new List<NPCEvent>
        {
            new NPCEvent("player_ate_at_stall", favDelta: 4, repDelta: 1, counter: "ate_count", counterRequired: 2, counterRewardFav: 5),
            new NPCEvent("player_interrupted_working", favDelta: -20, repDelta: -5, forceEmotion: "closed", permanent: true),
            new NPCEvent("player_compliments_food", favDelta: 3, repDelta: 1),
        },
        ["driss"] = new List<NPCEvent>
        {
            new NPCEvent("player_sat_in_silence", favDelta: 5, repDelta: 0, conditionId: "silence_condition_met"),
            new NPCEvent("player_asked_direct_question", favDelta: -10, repDelta: -2, forceEmotion: "closed"),
            new NPCEvent("player_mentions_place_observationally", favDelta: 4, repDelta: 1),
        },
        ["zahra"] = new List<NPCEvent>
        {
            new NPCEvent("player_interrupted_npc", favDelta: -20, repDelta: -8, forceEmotion: "closed", permanent: true),
            new NPCEvent("player_treated_minor_npc_respectfully", favDelta: 0, repDelta: 2, conditionId: "minor_npc_respect_shown"),
        },
        ["moussa"] = new List<NPCEvent>
        {
            new NPCEvent("player_makes_moussa_laugh", favDelta: 10, repDelta: 3),
            new NPCEvent("player_disrespected_npc_publicly", favDelta: -15, repDelta: -8, forceEmotion: "closed", permanent: true),
        },
        ["youssef"] = new List<NPCEvent>(),
        ["omar"] = new List<NPCEvent>(),
        ["si_brahim"] = new List<NPCEvent>
        {
            new NPCEvent("player_says_wrong_about_halka", favDelta: -10, repDelta: -3),
            new NPCEvent("player_demonstrates_cultural_knowledge", favDelta: 12, repDelta: 2),
        },
    };

    private readonly Dictionary<string, int> _counters = new Dictionary<string, int>();
    private readonly HashSet<string> _permanentClosed = new HashSet<string>();

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

    public void TriggerEvent(string npcId, string eventId)
    {
        string closeKey = $"{npcId}_permanent_closed";
        if (_permanentClosed.Contains(closeKey))
        {
            Debug.Log($"[EventTracker] {npcId} is permanently closed this cycle — event {eventId} ignored.");
            return;
        }

        List<NPCEvent> events;
        if (!_eventMap.TryGetValue(npcId, out events))
        {
            Debug.LogWarning($"[EventTracker] No events defined for {npcId}");
            return;
        }

        NPCEvent evt = events.Find(e => e.EventId == eventId);
        if (evt == null)
        {
            Debug.LogWarning($"[EventTracker] Event {eventId} not found for {npcId}");
            return;
        }

        if (!string.IsNullOrEmpty(evt.CounterId))
        {
            string counterKey = $"{npcId}_{evt.CounterId}";
            int current = 0;
            _counters.TryGetValue(counterKey, out current);
            _counters[counterKey] = current + 1;

            Debug.Log($"[EventTracker] Counter {counterKey} = {_counters[counterKey]}/{evt.CounterRequired}");

            if (_counters[counterKey] < evt.CounterRequired)
            {
                NPCManager.Instance?.ApplyEvent(npcId, eventId, evt.FavDelta / 2, evt.RepDelta, null);
                return;
            }

            NPCManager.Instance?.ApplyEvent(npcId, eventId, evt.CounterRewardFav, evt.RepDelta, evt.ForceEmotion);
            if (!string.IsNullOrEmpty(evt.ConditionId))
                NPCManager.Instance?.SetCondition(npcId, evt.ConditionId, true);
            return;
        }

        if (evt.Permanent && evt.ForceEmotion == "closed")
            _permanentClosed.Add(closeKey);

        NPCManager.Instance?.ApplyEvent(npcId, eventId, evt.FavDelta, evt.RepDelta, evt.ForceEmotion);
        if (!string.IsNullOrEmpty(evt.ConditionId))
            NPCManager.Instance?.SetCondition(npcId, evt.ConditionId, true);

        Debug.Log($"[EventTracker] {npcId} <- {eventId} | fav{evt.FavDelta:+#;-#;0} rep{evt.RepDelta:+#;-#;0}");
    }

    public void ResetCycle()
    {
        _counters.Clear();
        _permanentClosed.Clear();
        Debug.Log("[EventTracker] Cycle reset.");
    }

    // Notifie le EventTracker qu'un fragment a été collecté par le joueur.
    // Utilisé pour centraliser les réactions gameplay sans dupliquer la logique LLM.
    public void NotifyFragmentCollected(string npcId, string fragmentId)
    {
        Debug.Log($"[EventTracker] Fragment collected: {npcId} / {fragmentId}");

        // Marquer une condition sur le NPC afin que la logique du NPC puisse réagir si besoin.
        NPCManager.Instance?.SetCondition(npcId, "fragment_collected", true);

        // Si un mapping d'événement explicite 'fragment_collected' existe pour ce NPC, l'appliquer.
        List<NPCEvent> events;
        if (_eventMap.TryGetValue(npcId, out events))
        {
            NPCEvent evt = events.Find(e => e.EventId == "fragment_collected");
            if (evt != null)
            {
                NPCManager.Instance?.ApplyEvent(npcId, evt.EventId, evt.FavDelta, evt.RepDelta, evt.ForceEmotion);
            }
        }
    }

    private class NPCEvent
    {
        public string EventId { get; }
        public int FavDelta { get; }
        public int RepDelta { get; }
        public string ForceEmotion { get; }
        public bool PressurePositive { get; }
        public string ConditionId { get; }
        public bool Permanent { get; }
        public string CounterId { get; }
        public int CounterRequired { get; }
        public int CounterRewardFav { get; }

        public NPCEvent(
            string eventId,
            int favDelta = 0,
            int repDelta = 0,
            string forceEmotion = null,
            bool pressurePositive = false,
            string conditionId = null,
            bool permanent = false,
            string counter = null,
            int counterRequired = 1,
            int counterRewardFav = 0)
        {
            EventId = eventId;
            FavDelta = favDelta;
            RepDelta = repDelta;
            ForceEmotion = forceEmotion;
            PressurePositive = pressurePositive;
            ConditionId = conditionId;
            Permanent = permanent;
            CounterId = counter;
            CounterRequired = counterRequired;
            CounterRewardFav = counterRewardFav;
        }
    }
}
