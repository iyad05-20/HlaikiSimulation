# Halka Composition — Diagrammes et Flowcharts

## 1. Fragment Collection Flow

```
┌─────────────────────────────────────────────────────────┐
│ CYCLE START                                             │
└────────────────────────┬────────────────────────────────┘
                         │
         ┌───────────────┼───────────────┐
         ▼               ▼               ▼
    ┌─────────┐     ┌─────────┐     ┌─────────┐
    │ Hamid   │     │ Driss   │     │ Zahra   │
    └────┬────┘     └────┬────┘     └────┬────┘
         │               │               │
    Conversation    Conversation    Conversation
         │               │               │
    Favor >= 5?     Depth >= 3?    Secret unlocked?
    (Condition 1)   (Condition 2)   (Condition 3)
         │               │               │
        YES              NO              YES
         │               │               │
    Reveal ✓        Skip ✗          Reveal ✓
         │               │               │
    ┌────▼───┐          │        ┌──────▼──┐
    │Fragment │          │        │Fragment  │
    │Collected│          │        │Collected │
    │(Tier 2) │          │        │(Tier 3)  │
    └────┬────┘          │        └──────┬───┘
         │               │               │
         └───────────────┼───────────────┘
                         │
         ┌───────────────┴───────────────┐
         │                               │
    GameManager._fragments         Continue
    ["hamid_gnawa",                Collecting?
     "zahra_secret"]                   │
                                   YES │ (more NPCs)
                                   NO │
                                      ▼
                        ┌──────────────────────┐
                        │ END OF LAST CONV.    │
                        │ FragmentCount = 2    │
                        └──────────┬───────────┘
                                   │
                                   ▼
                        [HALKA TRIGGERED]
```

---

## 2. Story Composition Scoring Breakdown

```
╔═════════════════════════════════════════════════════════════╗
║               HALKA COMPOSITION SCORING                     ║
╚═════════════════════════════════════════════════════════════╝

PLAYER INPUT:
Selected Fragments (in order):
[1] Driss → "L'Oubli et la Solitude" (Tier 3)
[2] Si Brahim → "Pourquoi la Halka existe" (Tier 3)


╔════════════════════════════════════════════════════════════╗
║ 1. COHERENCE SCORING — Do fragments link well?             ║
╚════════════════════════════════════════════════════════════╝

Check pair rules:
    Driss + Si Brahim = ?
    
Hardcoded rules:
    ├─ hamid + lalla_fatima = +24 (perfect match)
    ├─ driss + si_brahim = +14 ✓ FOUND!
    ├─ driss + zahra = +8
    ├─ hamid + moussa = +6
    └─ [other tier combinations default to +2]

Result: coherenceScore = +14

IF (no pairs found AND count > 1):
    coherenceScore -= (count - 1) * 4  // Malus for disjointed


╔════════════════════════════════════════════════════════════╗
║ 2. DEPTH SCORING — Quality of selected tiers               ║
╚════════════════════════════════════════════════════════════╝

Tier weights:
    Tier 1 (easy) = 2 pts
    Tier 2 (medium) = 5 pts
    Tier 3 (hard) = 10 pts

Calculate for each fragment:
    [1] Driss (Tier 3) → +10
    [2] Si Brahim (Tier 3) → +10
    
depthScore = 10 + 10 = 20

Then multiply by 4 for total contribution:
    depth_contribution = 20 × 4 = 80


╔════════════════════════════════════════════════════════════╗
║ 3. AUDIENCE SCORING — How many listeners?                  ║
╚════════════════════════════════════════════════════════════╝

Base audience: 3 people

Add from reputation:
    GlobalReputation = 40 points
    Step = 10
    bonus = 40 / 10 = 4
    audience = 3 + 4 = 7

Apply audience multipliers:
    MoussaAllied = false
    YoussefBavard = false
    OmarPresent = true → +5
    
Final audience = 7 + 5 = 12 people


╔════════════════════════════════════════════════════════════╗
║ TOTAL SCORE CALCULATION                                    ║
╚════════════════════════════════════════════════════════════╝

totalScore = coherence + depth_contribution + audience
           = 14 + 80 + 12
           = 106 points

Reaction mapping:
    >= 100 → "Ovation monumentale"  ✓ RESULT
    >= 60  → "Réaction forte"
    >= 30  → "Appréciation modérée"
    < 30   → "Réaction mitigée"

reactionLabel = "Ovation monumentale — le cercle entier est transporté."


╔════════════════════════════════════════════════════════════╗
║ SENSITIVE STORY CHECK                                      ║
╚════════════════════════════════════════════════════════════╝

Has Tier 3? YES (Driss & Si Brahim both Tier 3)
Audience >= 10? YES (12 people)

exposesSensitiveStory = true
→ LLM adds narrative tension around sensitive moments
```

---

## 3. Order Matters — Impact of Fragment Sequencing

```
SAME FRAGMENTS, DIFFERENT ORDERS:

┌─────────────────────────────────────────────────────┐
│ SCENARIO A: Driss → Si Brahim                       │
├─────────────────────────────────────────────────────┤
│ Pair: Driss + Si Brahim = +14                       │
│ Narrative flow:                                      │
│   "I feel forgotten... But why do we gather here?" │
│ Coherence: STRONG (existential crisis → purpose)    │
│ Score: 14                                           │
└─────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────┐
│ SCENARIO B: Si Brahim → Driss                       │
├─────────────────────────────────────────────────────┤
│ Pair: Si Brahim + Driss = +14 (same bonus)         │
│ Narrative flow:                                      │
│   "We gather to remember... Now I feel lost."      │
│ Coherence: IRONIC (hope → despair)                  │
│ Score: 14 (same score, different meaning!)         │
└─────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────┐
│ SCENARIO C: Driss → Hamid + Moussa → Si Brahim    │
├─────────────────────────────────────────────────────┤
│ Pairs:                                              │
│   Driss + Hamid = 0 (no rule, diff tiers)          │
│   Hamid + Moussa = +6 (weak pair)                   │
│   Moussa + Si Brahim = 0 (no rule)                 │
│ Malus: (4 fragments - 1) * 4 = -12 (unfocused)    │
│ Coherence: 6 - 12 = -6 (NEGATIVE!)                 │
│ Score: -6 (weak narrative)                         │
└─────────────────────────────────────────────────────┘

KEY INSIGHT:
  - Pair bonuses reward INTENTIONAL COMBINATIONS
  - Disjointed fragments are PENALIZED
  - ORDER affects NARRATIVE MEANING even if score same
```

---

## 4. Audience Impact on Game Loop

```
START OF CYCLE:

Global Reputation = 0
Audience signals = [false, false, false]  (Moussa, Youssef, Omar)

         ↓ (During cycle)

Player talks to Moussa with high favorability:
    LLMNpcLogic: if (favorability > 8) {
        NPCManager.TriggerEvent("moussa_allied");
    }
    
EventTracker.OnMoussaAllied += () => {
    HalkaOrchestrator.SetMoussaAllied(true);
}

         ↓ (Later, end of cycle)

Audience calculation in HalkaCompositionEngine:
    base = 3
    rep_bonus = GlobalReputation / 10
    
    if (MoussaAllied)
        audience = (int)(audience * 1.35f);  // MULTIPLIER
    if (YoussefBavard)
        audience += 3;
    if (OmarPresent)
        audience += 5;

Result: Audience size varies based on cycle interactions
```

---

## 5. Complete State Machine

```
┌─────────────────────────────────────────────────────────────┐
│ HALKA SYSTEM STATE MACHINE                                  │
└─────────────────────────────────────────────────────────────┘

                START
                 │
                 ▼
        ┌──────────────────┐
        │ COLLECTING STATE │
        │ (HalkaInProgress │
        │   = false)       │
        └────────┬─────────┘
                 │
         [NPC Conversations Happen]
         [Fragments Accumulated]
         [Audience Signals Set]
                 │
                 ▼
        ┌──────────────────┐
        │ END CONVERSATION │
        │ (InputHandler    │
        │  .EndConversation)
        └────────┬─────────┘
                 │
                 ▼
        ┌──────────────────┐
        │ CHECK: Have      │
        │ fragments?       │
        └──┬──────────┬────┘
           │          │
          YES         NO
           │          │
           ▼          ▼
    ┌──────────┐  ┌────────────┐
    │ SHOW UI  │  │ SKIP HALKA │
    └────┬─────┘  └──────┬─────┘
         │               │
    [User selects]       │
         │               │
         ▼               │
    ┌──────────────┐     │
    │ COMPOSITION  │     │
    │ STATE        │     │
    │ (inProgress  │     │
    │  = true)     │     │
    └────┬─────────┘     │
         │               │
    [Submit button]      │
         │               │
         ▼               │
    ┌──────────────┐     │
    │ SCORING &    │     │
    │ LLM CALL     │     │
    └────┬─────────┘     │
         │               │
    [Result ready]       │
         │               │
         ▼               │
    ┌──────────────┐     │
    │ DISPLAY      │     │
    │ RESULT       │     │
    └────┬─────────┘     │
         │               │
         └───────┬───────┘
                 │
                 ▼
        ┌──────────────────┐
        │ CYCLE RESET      │
        │ (GameManager     │
        │  .ResetCycle)    │
        └────────┬─────────┘
                 │
    - Clear _fragments
    - Clear _halkaInProgress
    - Clear audience signals
    - Start new cycle
                 │
                 ▼
                [REPEAT]
```

---

## 6. Scoring Visualization — All Factors

```
                    HALKA SCORE
                         |
           ┌─────────────┼─────────────┐
           ▼             ▼             ▼
      COHERENCE       DEPTH        AUDIENCE
      (0-24+)         (×4)           (1-20+)
         │             │              │
         │             │              │
    Pair Rules:   Tier Weights:  Global Rep + Signals:
    ├─ H+LF: +24  ├─ T1: 2×4=8   ├─ Base: 3
    ├─ D+SB: +14  ├─ T2: 5×4=20  ├─ Rep: /10
    ├─ D+Z: +8    └─ T3: 10×4=40 ├─ Moussa: ×1.35
    ├─ H+M: +6    (sum all)      ├─ Youssef: +3
    ├─ SameTier: +2             └─ Omar: +5
    └─ NoLink: 0
        (malus if scattered)

Example Selection: [Driss(T3), Si Brahim(T3)]
    Coherence: D+SB pair = 14
    Depth: 10+10 = 20 → 20×4 = 80
    Audience: 3 + (40/10) + 5 = 12
    
    TOTAL = 14 + 80 + 12 = 106
    
    Reaction: "Ovation monumentale"
```

---

## 7. LLM Narration Context

```
┌─────────────────────────────────────────────────────┐
│ PROMPT CONSTRUCTION FOR NARRATION                   │
├─────────────────────────────────────────────────────┤
│                                                     │
│ System Message:                                     │
│ "Tu es un narrateur de performance orale. Tu       │
│  rédiges une Halka vivante, publique et cohérente. │
│  Ne mentionne jamais de score."                    │
│                                                     │
│ User Message = BuildNarrationPrompt():             │
│                                                     │
│ ┌───────────────────────────────────────┐          │
│ │ INPUTS:                               │          │
│ │ • Fragment 1 title + content          │          │
│ │ • Fragment 2 title + content          │          │
│ │ • Audience size (9)                   │          │
│ │ • Coherence score (14)                │          │
│ │ • Flag: exposesSensitive (true/false) │          │
│ │                                       │          │
│ │ INSTRUCTIONS:                         │          │
│ │ 1. Be fluid and natural               │          │
│ │ 2. Create continuity                  │          │
│ │ 3. Capture atmosphere                 │          │
│ │ 4. Stay authentic to voices           │          │
│ │ 5. If sensitive: add tension          │          │
│ │                                       │          │
│ │ OUTPUT: Full narration as if spoken   │          │
│ └───────────────────────────────────────┘          │
│                                                     │
│ Result: ~500-1000 words of narration                │
│                                                     │
└─────────────────────────────────────────────────────┘
```

---

## 8. Error Paths

```
┌──────────────────────────────────────────┐
│ ERROR HANDLING FLOW                      │
└──────────────────────────────────────────┘

Start composition flow
    │
    ├─→ LLM FAILS to generate narration
    │       │
    │       ├─→ exposesSensitiveStory = true
    │       │   result.coherenceScore > 10?
    │       │
    │       ├─→ YES: "Les histoires captivantes..."
    │       └─→ NO: "L'audience reste attentive..."
    │
    ├─→ Panel PREFAB MISSING
    │   └─→ Log error, disable Halka, continue game
    │
    ├─→ UI FIELDS NOT ASSIGNED
    │   └─→ Log warning, fallback to minimal display
    │
    └─→ No fragments collected
        └─→ Skip Halka entirely, ready for next cycle
```

---

## Summary

**Key Takeaway:** 
- Halka is a **composition mini-game** where player chooses and orders story fragments
- Scoring rewards **intentional pairing** and **depth** while considering **audience size**
- **LLM called once** to narrate the selected combination
- Result is **narrative + scores**, not numeric feedback to player
- Everything resets for next cycle

This teaches: **Understanding > Collecting**
