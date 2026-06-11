using System.Collections.Generic;
using UnityEngine;

namespace JemaaGame.NPC
{
    /// <summary>
    /// Tracks physical events in the game world and applies them to NPCState.
    ///
    /// Called by camarades when physical events occur:
    ///   EventTracker.Instance.TriggerEvent("lalla_fatima", "player_ate_at_stall");
    ///   EventTracker.Instance.TriggerEvent("driss", "player_sat_in_silence");
    ///
    /// Event definitions come from TransitionEvents JSON — no hardcoding here.
    /// </summary>
    public class EventTracker : MonoBehaviour
    {
        public static EventTracker Instance { get; private set; }

        // ── Event definitions — loaded from relations_and_transitions.json ─
        // For now defined here directly, matching the JSON structure exactly.

        private readonly Dictionary<string, List<NPCEvent>> _eventMap = new()
        {
            ["hamid"] = new()
            {
                new("player_mentions_guembri_specifically", favDelta: 12, repDelta: 2,  pressurePositive: true,  conditionId: "player_shows_genuine_curiosity_about_guembri_or_gnawa"),
                new("player_condescending_about_self_taught", favDelta: -15, repDelta: -5, forceEmotion: "closed"),
            },
            ["lalla_fatima"] = new()
            {
                new("player_ate_at_stall",          favDelta: 4,   repDelta: 1,  counter: "ate_count", counterRequired: 2, counterRewardFav: 5),
                new("player_interrupted_working",   favDelta: -20, repDelta: -5, forceEmotion: "closed", permanent: true),
                new("player_compliments_food",      favDelta: 3,   repDelta: 1),
            },
            ["driss"] = new()
            {
                new("player_sat_in_silence",        favDelta: 5,   repDelta: 0,  conditionId: "silence_condition_met"),
                new("player_asked_direct_question", favDelta: -10, repDelta: -2, forceEmotion: "closed"),
                new("player_mentions_place_observationally", favDelta: 4, repDelta: 1),
            },
            ["zahra"] = new()
            {
                new("player_interrupted_npc",       favDelta: -20, repDelta: -8, forceEmotion: "closed", permanent: true),
                new("player_treated_minor_npc_respectfully", favDelta: 0, repDelta: 2, conditionId: "minor_npc_respect_shown"),
            },
            ["moussa"] = new()
            {
                new("player_makes_moussa_laugh",    favDelta: 10,  repDelta: 3),
                new("player_disrespected_npc_publicly", favDelta: -15, repDelta: -8, forceEmotion: "closed", permanent: true),
            },
            ["youssef"] = new() { },
            ["omar"]    = new() { },
            ["si_brahim"] = new()
            {
                new("player_says_wrong_about_halka", favDelta: -10, repDelta: -3),
                new("player_demonstrates_cultural_knowledge", favDelta: 12, repDelta: 2),
            },
        };

        // ── Event counters (e.g. ate_count for Lalla Fatima) ──────────────
        private readonly Dictionary<string, int> _counters = new();

        // ── Permanent closes this cycle ────────────────────────────────────
        private readonly HashSet<string> _permanentClosed = new();

        // ── Unity lifecycle ────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        // ── Public API — called by camarades ──────────────────────────────

        /// <summary>
        /// Trigger a physical event for an NPC.
        /// The NPCManager will apply it to the active state.
        /// </summary>
        public void TriggerEvent(string npcId, string eventId)
        {
            // If NPC is permanently closed this cycle, ignore positive events
            string closeKey = $"{npcId}_permanent_closed";
            if (_permanentClosed.Contains(closeKey))
            {
                Debug.Log($"[EventTracker] {npcId} is permanently closed this cycle — event {eventId} ignored.");
                return;
            }

            if (!_eventMap.TryGetValue(npcId, out var events))
            {
                Debug.LogWarning($"[EventTracker] No events defined for {npcId}");
                return;
            }

            var evt = events.Find(e => e.EventId == eventId);
            if (evt == null)
            {
                Debug.LogWarning($"[EventTracker] Event {eventId} not found for {npcId}");
                return;
            }

            // Handle counter events
            if (!string.IsNullOrEmpty(evt.CounterId))
            {
                string counterKey = $"{npcId}_{evt.CounterId}";
                _counters.TryGetValue(counterKey, out int current);
                _counters[counterKey] = current + 1;

                Debug.Log($"[EventTracker] Counter {counterKey} = {_counters[counterKey]}/{evt.CounterRequired}");

                if (_counters[counterKey] < evt.CounterRequired)
                {
                    // Partial — apply smaller delta
                    NPCManager.Instance?.ApplyEvent(npcId, eventId, evt.FavDelta / 2, evt.RepDelta, null);
                    return;
                }
                else
                {
                    // Counter reached — apply full reward
                    NPCManager.Instance?.ApplyEvent(npcId, eventId, evt.CounterRewardFav, evt.RepDelta, evt.ForceEmotion);
                    NPCManager.Instance?.SetCondition(npcId, evt.ConditionId, true);
                    return;
                }
            }

            // Handle permanent close
            if (evt.Permanent && !string.IsNullOrEmpty(evt.ForceEmotion) && evt.ForceEmotion == "closed")
                _permanentClosed.Add(closeKey);

            // Apply event
            NPCManager.Instance?.ApplyEvent(npcId, eventId, evt.FavDelta, evt.RepDelta, evt.ForceEmotion);

            // Set condition if defined
            if (!string.IsNullOrEmpty(evt.ConditionId))
                NPCManager.Instance?.SetCondition(npcId, evt.ConditionId, true);

            Debug.Log($"[EventTracker] {npcId} ← {eventId} | fav{evt.FavDelta:+#;-#;0} rep{evt.RepDelta:+#;-#;0}");
        }

        // ── Cycle reset ────────────────────────────────────────────────────

        public void ResetCycle()
        {
            _counters.Clear();
            _permanentClosed.Clear();
            Debug.Log("[EventTracker] Cycle reset.");
        }

        // ── Event definition ──────────────────────────────────────────────

        private class NPCEvent
        {
            public string EventId        { get; }
            public int    FavDelta       { get; }
            public int    RepDelta       { get; }
            public string ForceEmotion   { get; }
            public bool   PressurePositive { get; }
            public string ConditionId    { get; }
            public bool   Permanent      { get; }
            public string CounterId      { get; }
            public int    CounterRequired { get; }
            public int    CounterRewardFav { get; }

            public NPCEvent(
                string eventId,
                int    favDelta         = 0,
                int    repDelta         = 0,
                string forceEmotion     = null,
                bool   pressurePositive = false,
                string conditionId      = null,
                bool   permanent        = false,
                string counter          = null,
                int    counterRequired  = 1,
                int    counterRewardFav = 0)
            {
                EventId          = eventId;
                FavDelta         = favDelta;
                RepDelta         = repDelta;
                ForceEmotion     = forceEmotion;
                PressurePositive = pressurePositive;
                ConditionId      = conditionId;
                Permanent        = permanent;
                CounterId        = counter;
                CounterRequired  = counterRequired;
                CounterRewardFav = counterRewardFav;
            }
        }
    }
}