using System;
using System.Collections.Generic;

// ─── Root ─────────────────────────────────────────────────────────────────────
[Serializable]
public class NpcPersonaData
{
    public string id;
    public string name;
    public string role;
    public CycleVariantContainer cycle_variant;
    public PersonaDef persona;
    public List<string> scoring_rules;
    public ReputationWords reputation_words;
    public StoryFragment story_fragment;
}

// ─── Cycle / Variants ─────────────────────────────────────────────────────────
[Serializable]
public class CycleVariantContainer
{
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
    public RevealCondition reveal_condition;
    public string reveal_instruction;
}

[Serializable]
public class RevealCondition
{
    public int favorability_min;
    public List<string> conditions_required;
}
