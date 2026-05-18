using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace JemaaGame.NPC
{
    /// <summary>
    /// Global game state for one cycle.
    /// Tracks global reputation, collected fragments, and Halka unlock status.
    /// </summary>
    public class GameState : MonoBehaviour
    {
        public static GameState Instance { get; private set; }

        // ── State ──────────────────────────────────────────────────────────

        // Global reputation = weighted sum of all NPC reputations
        public int GlobalReputation { get; private set; }

        // Fragments collected this cycle
        private readonly List<string> _fragments = new();
        public IReadOnlyList<string> Fragments => _fragments;

        // Cycle config variant per NPC (loaded from cycle_config.json)
        private Dictionary<string, int> _cycleVariants = new();

        // Minimum fragments required to attempt Halka
        [SerializeField] private int halkaMinFragments = 3;

        // ── NPC reputation contributions ──────────────────────────────────
        // Updated by NPCManager when a session ends
        private Dictionary<string, int> _npcReputations = new();

        // ── Unity lifecycle ────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        // ── Reputation ─────────────────────────────────────────────────────

        public void UpdateNPCReputation(string npcId, int reputation)
        {
            _npcReputations[npcId] = reputation;
            RecalculateGlobalReputation();
        }

        private void RecalculateGlobalReputation()
        {
            GlobalReputation = _npcReputations.Values.Sum();
            Debug.Log($"[GameState] GlobalReputation = {GlobalReputation}");
        }

        // ── Fragments ──────────────────────────────────────────────────────

        public void AddFragment(string fragmentId)
        {
            if (!_fragments.Contains(fragmentId))
            {
                _fragments.Add(fragmentId);
                Debug.Log($"[GameState] Fragment collected: {fragmentId} ({_fragments.Count} total)");
            }
        }

        public bool HasFragment(string fragmentId) => _fragments.Contains(fragmentId);

        public int FragmentCount => _fragments.Count;

        // ── Halka ──────────────────────────────────────────────────────────

        /// <summary>
        /// Halka is unlocked when player has enough fragments and minimum reputation.
        /// </summary>
        public bool IsHalkaUnlocked()
        {
            if (_fragments.Count < halkaMinFragments)
            {
                Debug.Log($"[GameState] Halka locked: {_fragments.Count}/{halkaMinFragments} fragments");
                return false;
            }
            return true;
        }

        // ── Cycle config ───────────────────────────────────────────────────

        public void SetCycleVariant(string npcId, int variantId)
            => _cycleVariants[npcId] = variantId;

        public int GetCycleVariant(string npcId)
            => _cycleVariants.TryGetValue(npcId, out int v) ? v : 1;

        // ── Cycle reset ────────────────────────────────────────────────────

        public void ResetCycle()
        {
            GlobalReputation = 0;
            _fragments.Clear();
            _npcReputations.Clear();
            _cycleVariants.Clear();
            Debug.Log("[GameState] Cycle reset.");
        }

        // ── Debug ──────────────────────────────────────────────────────────

        public string DebugSummary()
        {
            return $"GlobalRep={GlobalReputation} | Fragments=[{string.Join(", ", _fragments)}] | HalkaUnlocked={IsHalkaUnlocked()}";
        }
    }
}