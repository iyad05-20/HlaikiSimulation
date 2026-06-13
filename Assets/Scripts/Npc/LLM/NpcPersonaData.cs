using System;
using System.Collections.Generic;

// ─── Root ─────────────────────────────────────────────────────────────────────
[Serializable]
public class NpcPersonaData
{
    public string id;
    public string name;
    public string role;
    public int age;
    public int tier;
    public string location;

    public CycleVariantContainer cycle_variant;
    public PersonaDef persona;
    public List<string> scoring_rules;
    public List<string> emotion_rules;
    public ReputationWords reputation_words;
    public StoryFragment story_fragment;
    public UnlockConditions unlock_conditions;
    public CrossEffect[] cross_effects;
    public ScoringConfig scoring;
    public string scoring_prompt_template;
    public string dialogue_prompt_template;
    public NPCRelations npc_relations;
    public PenaltyPrematureApproach penalty_premature_approach;
}

// ─── Cycle / Variants ─────────────────────────────────────────────────────────
[Serializable]
public class CycleVariantContainer
{
    public int selected;
    public List<Variant> variants;
}

[Serializable]
public class Variant
{
    public int id;
    public string mood;
    public string emotion_start;
    public string instrument_state;
}

// ─── Persona ──────────────────────────────────────────────────────────────────
[Serializable]
public class PersonaDef
{
    // NOTE: "base" is a C# keyword, so we rename it base_text in the JSON before parsing
    public string base_text;
    public EmotionOverlays emotion_overlays;
    public List<string> rules;
}

[Serializable]
public class EmotionOverlays
{
    public string open;
    public string occupied;
    public string closed;
}

// ─── Reputation ───────────────────────────────────────────────────────────────
[Serializable]
public class ReputationWords
{
    public List<string> positive;
    public List<string> negative;
    public List<string> cultural;
}

// ─── Story Fragment ───────────────────────────────────────────────────────────
[Serializable]
public class StoryFragment
{
    public string id;
    public string title;
    public string content;
    public RevealCondition reveal_condition;
    public string reveal_style;
    public string reveal_instruction;
}

[Serializable]
public class RevealCondition
{
    public int favorability_min;
    public List<string> conditions_required;
}

[Serializable]
public class UnlockConditions
{
    public List<string> requires;
    public bool known_to_player;
    public List<string> hint_sources;
}

[Serializable]
public class CrossEffect
{
    public string trigger;
    public string effect;
    public int value;
    public string favorability_cap;
    public bool vouch_active;
    public string global_effect;
}

[Serializable]
public class ScoringConfig
{
    public List<ScoringTrigger> triggers;
    public List<EmotionTransitionRule> emotion_transition_rules;
}

[Serializable]
public class ScoringTrigger
{
    public string id;
    public string description;
    public IntRange favorability_delta;
    public int reputation_delta;
    public string emotion_pressure;
    public string forces_emotion;
}

[Serializable]
public class EmotionTransitionRule
{
    public string condition;
    public string forces_emotion;
    public string global_effect;
}

[Serializable]
public class NPCRelations
{
    public List<string> allies;
    public List<string> cold;
    public List<string> neutral;
}

[Serializable]
public class PenaltyPrematureApproach
{
    public int favorability_delta;
    public int reputation_delta;
    public string forces_emotion;
    public string note;
    public string global_effect;
}

[Serializable]
public class IntRange
{
    public int min;
    public int max;
}
