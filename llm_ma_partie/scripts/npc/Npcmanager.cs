using System.Collections.Generic;
using UnityEngine;

namespace JemaaGame.NPC
{
    /// <summary>
    /// Central coordinator — holds all NPCStates for the current cycle,
    /// routes events from EventTracker, and bridges to DialogueManager.
    ///
    /// Single entry point for camarades who need NPC state info:
    ///   NPCManager.Instance.GetFavorability("hamid")
    ///   NPCManager.Instance.GetEmotion("driss")
    ///   NPCManager.Instance.IsUnlocked("moussa")
    /// </summary>
    public class NPCManager : MonoBehaviour
    {
        public static NPCManager Instance { get; private set; }

        // ── All NPC IDs in the game ────────────────────────────────────────
        private static readonly string[] AllNPCIds =
        {
            "hamid", "youssef", "omar", "moussa",
            "lalla_fatima", "zahra", "driss", "si_brahim"
        };

        // ── State per NPC ──────────────────────────────────────────────────
        private readonly Dictionary<string, NPCState> _states = new();

        // ── Reputation thresholds for unlock conditions ────────────────────
        // These mirror the unlock_conditions in each *_game.json
        private readonly Dictionary<string, int> _reputationUnlockThreshold = new()
        {
            ["omar"]      = 20,
            ["moussa"]    = 30,
            ["zahra"]     = 25,
        };

        // ── Unity lifecycle ────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            InitializeCycle();
        }

        // ── Cycle init ─────────────────────────────────────────────────────

        /// <summary>
        /// Load all NPC states from sessions at cycle start.
        /// Called once per cycle.
        /// </summary>
        public void InitializeCycle()
        {
            _states.Clear();
            foreach (string npcId in AllNPCIds)
            {
                var session = SessionManager.Instance?.LoadSession(npcId);
                _states[npcId] = new NPCState(npcId, session);
                GameState.Instance?.UpdateNPCReputation(npcId, _states[npcId].Reputation);
            }
            Debug.Log("[NPCManager] Cycle initialized — all NPC states loaded.");
        }

        public void ResetCycle()
        {
            SessionManager.Instance?.DeleteAllSessions();
            EventTracker.Instance?.ResetCycle();
            GameState.Instance?.ResetCycle();
            InitializeCycle();
            Debug.Log("[NPCManager] Full cycle reset.");
        }

        // ── State access — for camarades ──────────────────────────────────

        public int          GetFavorability(string npcId) => GetState(npcId)?.Favorability ?? 0;
        public int          GetReputation(string npcId)   => GetState(npcId)?.Reputation   ?? 0;
        public EmotionState GetEmotion(string npcId)      => GetState(npcId)?.Emotion       ?? EmotionState.Open;
        public bool         GetCondition(string npcId, string conditionKey) => GetState(npcId)?.GetCondition(conditionKey) ?? false;

        /// <summary>
        /// Check if NPC is unlocked — based on reputation threshold + conditions.
        /// Tier 1 NPCs are always unlocked.
        /// Fix #6: JSON-driven from unlock_conditions instead of hardcoded switch.
        /// </summary>
        public bool IsUnlocked(string npcId)
        {
            var state = GetState(npcId);
            if (state == null) return false;

            int globalRep = GameState.Instance?.GlobalReputation ?? 0;

            // Load persona JSON to get unlock_conditions
            string path = System.IO.Path.Combine(UnityEngine.Application.streamingAssetsPath, "personas", $"{npcId}_game.json");
            if (!System.IO.File.Exists(path)) return true; // Fallback: assume unlocked

            string raw = System.IO.File.ReadAllText(path);
            raw = raw.Replace("\"base\":", "\"base_text\":");
            var personaData = JsonUtility.FromJson<NPCPersonaData>(raw);

            if (personaData?.unlock_conditions?.requires == null || personaData.unlock_conditions.requires.Length == 0)
                return true; // No conditions = always unlocked (Tier 1)

            // Evaluate all required conditions
            foreach (string condition in personaData.unlock_conditions.requires)
            {
                if (!EvaluateCondition(npcId, condition, globalRep))
                    return false; // Any failed condition = locked
            }

            return true; // All conditions met
        }

        private bool EvaluateCondition(string npcId, string condition, int globalRep)
        {
            // Parse condition strings dynamically
            // Examples: "global_reputation >= 20", "zahra_vouch_active", "player_has_3_fragments"
            
            if (condition.Contains("global_reputation"))
            {
                // Parse "global_reputation >= 20"
                var parts = condition.Split(new[] { ">=", "<=", ">", "<", "==" }, System.StringSplitOptions.None);
                if (parts.Length == 2 && int.TryParse(parts[1].Trim(), out int threshold))
                {
                    if (condition.Contains(">=")) return globalRep >= threshold;
                    if (condition.Contains("<=")) return globalRep <= threshold;
                    if (condition.Contains("==")) return globalRep == threshold;
                    if (condition.Contains(">")) return globalRep > threshold;
                    if (condition.Contains("<")) return globalRep < threshold;
                }
            }

            if (condition.Contains("zahra_vouch"))
                return GetFavorability("zahra") >= 75;

            if (condition.Contains("player_has_3_fragments"))
                return (GameState.Instance?.FragmentCount ?? 0) >= 3;

            if (condition.Contains("player_shows_genuine_curiosity"))
                return GetCondition(npcId, "curiosity_shown");

            if (condition.Contains("player_ate_at_stall_twice"))
                return GetCondition(npcId, "stall_meals") && GetState(npcId)?.GetCondition("stall_meals_count") != null;

            // Unknown condition — assume true for safety
            return true;
        }

        // ── Event routing — called by EventTracker ─────────────────────────

        public void ApplyEvent(string npcId, string eventId, int favDelta, int repDelta, string forceEmotion)
        {
            var state = GetState(npcId);
            if (state == null) return;

            state.ApplyEvent(eventId, favDelta, repDelta, forceEmotion);
            GameState.Instance?.UpdateNPCReputation(npcId, state.Reputation);
        }

        public void SetCondition(string npcId, string conditionKey, bool value)
        {
            GetState(npcId)?.SetCondition(conditionKey, value);
        }

        // ── Cross-NPC effects ──────────────────────────────────────────────
        // Called after favorability changes to propagate cross-NPC effects
        // Fix #5: JSON-driven from cross_effects[] instead of hardcoded switch

        public void EvaluateCrossEffects(string sourceNpcId)
        {
            // Load persona JSON to get cross_effects
            string path = System.IO.Path.Combine(UnityEngine.Application.streamingAssetsPath, "personas", $"{sourceNpcId}_game.json");
            if (!System.IO.File.Exists(path)) return;

            string raw = System.IO.File.ReadAllText(path);
            raw = raw.Replace("\"base\":", "\"base_text\":");
            var personaData = JsonUtility.FromJson<NPCPersonaData>(raw);

            if (personaData?.cross_effects == null || personaData.cross_effects.Length == 0)
                return; // No cross-effects defined

            // Evaluate each cross-effect trigger
            foreach (var effect in personaData.cross_effects)
            {
                if (EvaluateCrossEffectTrigger(sourceNpcId, effect))
                {
                    ApplyCrossEffect(sourceNpcId, effect);
                }
            }
        }

        private bool EvaluateCrossEffectTrigger(string sourceNpcId, CrossEffect effect)
        {
            // Parse trigger strings: "zahra_favorability >= 75", "player_allied_with_lalla_fatima", etc.
            int fav = GetFavorability(sourceNpcId);

            if (effect.trigger.Contains("favorability"))
            {
                // Parse "zahra_favorability >= 75"
                var parts = effect.trigger.Split(new[] { ">=", "<=", ">", "<", "==" }, System.StringSplitOptions.None);
                if (parts.Length == 2 && int.TryParse(parts[1].Trim(), out int threshold))
                {
                    if (effect.trigger.Contains(">=")) return fav >= threshold;
                    if (effect.trigger.Contains("<=")) return fav <= threshold;
                    if (effect.trigger.Contains("==")) return fav == threshold;
                    if (effect.trigger.Contains(">")) return fav > threshold;
                    if (effect.trigger.Contains("<")) return fav < threshold;
                }
            }

            if (effect.trigger.Contains("player_allied_with"))
            {
                // Example: "player_allied_with_lalla_fatima" = fav >= 50
                return fav >= 50;
            }

            if (effect.trigger.Contains("player_disrespected"))
            {
                // Example: "player_disrespected_lalla_fatima"
                return GetCondition(sourceNpcId, "player_was_disrespectful");
            }

            return false;
        }

        private void ApplyCrossEffect(string sourceNpcId, CrossEffect effect)
        {
            // Apply the effect based on type
            if (effect.effect == "favorability_start_bonus")
            {
                // Extract target NPC from trigger
                string targetNpc = ExtractTargetNpc(effect.trigger);
                var targetState = GetState(targetNpc);
                if (targetState != null)
                {
                    targetState.ApplyEvent($"cross_effect_{effect.trigger}", effect.value, 0, null);
                    Debug.Log($"[NPCManager] Cross-effect: {sourceNpcId} → {targetNpc} (+{effect.value} fav)");
                }
            }
            else if (effect.effect == "hamid_becomes_reserved")
            {
                var state = GetState(sourceNpcId);
                if (state != null && !string.IsNullOrEmpty(effect.favorability_cap))
                {
                    Debug.Log($"[NPCManager] Cross-effect: {sourceNpcId} favorability capped to {effect.favorability_cap}");
                    // Cap implementation would go here (store in NPCState)
                }
            }
            else if (effect.effect == "si_brahim_unlock_condition_active")
            {
                Debug.Log($"[NPCManager] Cross-effect: Si Brahim unlock condition activated via {sourceNpcId} vouch");
            }
        }

        private string ExtractTargetNpc(string trigger)
        {
            // Extract NPC name from trigger strings like "player_allied_with_lalla_fatima"
            if (trigger.Contains("lalla_fatima")) return "lalla_fatima";
            if (trigger.Contains("zahra")) return "zahra";
            if (trigger.Contains("hamid")) return "hamid";
            if (trigger.Contains("driss")) return "driss";
            if (trigger.Contains("moussa")) return "moussa";
            return "";
        }

        // ── Utility ───────────────────────────────────────────────────────

        private NPCState GetState(string npcId)
        {
            if (_states.TryGetValue(npcId, out var state)) return state;
            Debug.LogWarning($"[NPCManager] NPCState not found for {npcId}");
            return null;
        }

        public string DebugAll()
        {
            var sb = new System.Text.StringBuilder();
            foreach (var (id, state) in _states)
                sb.AppendLine($"  {id}: {state}");
            return sb.ToString();
        }
    }
}