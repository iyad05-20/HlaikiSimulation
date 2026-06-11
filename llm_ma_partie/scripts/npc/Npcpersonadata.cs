using System;
using UnityEngine;

namespace JemaaGame.NPC
{
    // ── These classes match the *_game.json structure exactly ─────────────
    // Unity loads them with JsonUtility.FromJson<NPCPersonaData>()

    [Serializable]
    public class NPCPersonaData
    {
        public string   id;
        public string   name;
        public string   role;
        public int      tier;
        public string   location;

        public CycleVariantConfig cycle_variant;
        public PersonaConfig      persona;
        public StoryFragment      story_fragment;
        public UnlockConditions   unlock_conditions;
        public CrossEffect[]      cross_effects;

        public string[] scoring_rules;
        public string[] emotion_rules;

        public ReputationWords reputation_words;
    }

    [Serializable]
    public class UnlockConditions
    {
        public string[] requires;
        public bool     known_to_player;
        public string[] hint_sources;
    }

    [Serializable]
    public class CrossEffect
    {
        public string trigger;
        public string effect;
        public int    value;
        public bool   vouch_active;
        public string favorability_cap;
    }

    [Serializable]
    public class CycleVariantConfig
    {
        public int selected;
        public CycleVariant[] variants;
    }

    [Serializable]
    public class CycleVariant
    {
        public int    id;
        public string mood;
        public string emotion_start;
        public string instrument_state;  // NPC-specific context (optional)
    }

    [Serializable]
    public class PersonaConfig
    {
        public string   base_text;          // "base" in JSON — renamed to avoid C# keyword
        public Overlays emotion_overlays;
        public string[] rules;
    }

    [Serializable]
    public class Overlays
    {
        public string open;
        public string occupied;
        public string closed;

        public string Get(EmotionState emotion) => emotion switch
        {
            EmotionState.Open     => open,
            EmotionState.Occupied => occupied,
            EmotionState.Closed   => closed,
            _                     => open,
        };
    }

    [Serializable]
    public class StoryFragment
    {
        public string id;
        public string reveal_instruction;
    }

    [Serializable]
    public class ReputationWords
    {
        public string[] negative;
        public string[] positive;
        public string[] cultural;
    }
}