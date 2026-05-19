# Integration Halka System — Revised Architecture

## Overview

The **Halka System** is the end-of-cycle narrative composition mechanic. It comprises:

1. **HalkaCompositionEngine** (`Assets/Scripts/Npc/Halka/HalkaCompositionEngine.cs`)
   - Pure logic: loads fragments, scores coherence/depth/audience
   - Single LLM call for narration (pure narrative, no mechanics)
   - No UI coupling, no singleton pattern

2. **HalkaOrchestrator** (`Assets/Scripts/Npc/Halka/HalkaOrchestrator.cs`)
   - Singleton manager: orchestrates end-of-cycle flow
   - Detects cycle end, triggers composition UI, broadcasts results
   - Owns audience signals (Moussa/Youssef/Omar)

3. **HalkaCompositionPanel** (`Assets/Scripts/Ui/HalkaCompositionPanel.cs`)
   - UI layer: fragment display, selection/ordering, result display
   - Events: `OnCompositionSubmitted`, `OnCancelled`
   - Can be extended for visual polish (drag-drop, preview, etc.)

---

## Workflow

### Cycle End Trigger

```
InputHandler.EndConversation()
    ↓
HalkaOrchestrator.TriggerEndOfCycleHalka()
    ↓ (if fragments available)
HalkaCompositionPanel.ShowUI()
    ↓ (user selects fragments)
HalkaCompositionEngine.ScoreComposition()
    ↓
HalkaCompositionEngine.GenerateNarration() [LLM call]
    ↓
HalkaOrchestrator.OnHalkaCompleted → listeners
    ↓
HalkaResultPanel.Display(result)
```

---

## Integration Points

### 1. **Cycle Management** (GameManager)

Fragment collection happens throughout the cycle:
- `GameManager.AddFragment(fragmentId)` — called by `LLMNpcLogic` when fragment revealed
- `GameManager.GetCollectedFragmentsSnapshot()` — read by HalkaCompositionEngine at composition time
- `GameManager.ResetCycle()` — calls `HalkaOrchestrator.ResetCycle()` at cycle reset

### 2. **End-of-Cycle Hook** (InputHandler)

After `EndConversation()`, the system checks:
```csharp
if (HalkaOrchestrator.Instance != null)
{
    HalkaOrchestrator.Instance.TriggerEndOfCycleHalka();
}
```

Optional: Add end-of-day detection logic (e.g., "all NPCs exhausted" or explicit "end day" button).

### 3. **Audience Signals** (EventTracker / NPCManager)

Trigger these calls during the cycle to modify audience size:
```csharp
HalkaOrchestrator.Instance.SetMoussaAllied(true);     // reputation >= X with Moussa
HalkaOrchestrator.Instance.SetYoussefBavard(true);    // Youssef favorable
HalkaOrchestrator.Instance.SetOmarPresent(true);      // Omar in scene
```

These directly affect the audience multiplier in HalkaCompositionEngine.

### 4. **Sensitive Story Reactions** (Future)

When `HalkaCompositionResult.exposesSensitiveStory == true` and audience >= 10:
- NPC whose story was revealed experiences reputation hit (next cycle)
- Optional: Add narrative consequence (NPC comments on betrayal)

Integration point: Listen to `HalkaOrchestrator.OnHalkaCompleted` and update NPC states accordingly.

---

## Key Data Structures

### HalkaCompositionEngine.FragmentEntry
```csharp
public string fragmentId;      // unique ID
public string npcId;           // "hamid", "lalla_fatima", etc.
public string npcName;         // display name
public string title;           // fragment title
public string content;         // fragment text
public int tier;               // 1, 2, or 3
public bool isSensitive;       // if tier >= 3 or explicit NPC
```

### HalkaCompositionEngine.CompositionResult
```csharp
public List<string> selectedFragmentIds;    // user's selection order
public int coherenceScore;                  // pair bonuses
public int depthScore;                      // sum of tier weights
public int audienceSize;                    // calculated from rep + signals
public int totalScore;                      // coherence + (depth * 4) + audience
public string reactionLabel;                // audience reaction string
public bool exposesSensitiveStory;          // tier3 + audience >= 10
public string narration;                    // LLM-generated performance text
```

---

## Scoring Rules

### Coherence
- Pair bonuses (hardcoded in HalkaCompositionEngine._pairRules):
  - Hamid + Lalla Fatima = +24
  - Driss + Si Brahim = +14
  - Driss + Zahra = +8
  - Hamid + Moussa = +6
  - Same tier = +2 per pair
  - Unrelated = 0 (and -4 per pair if > 1 unrelated pair)

### Depth
- Tier 1 = 2 pts
- Tier 2 = 5 pts
- Tier 3 = 10 pts

### Audience
- Base = 3 people
- Global reputation / 10 (per 10 rep = +1 person)
- Moussa allied = 1.35× multiplier
- Youssef bavard = +3
- Omar present = +5

### Total Score
```
totalScore = coherence + (depth * 4) + audience
```

### Reactions
- \>= 100: "Ovation monumentale"
- \>= 60: "Réaction forte"
- \>= 30: "Appréciation modérée"
- < 30: "Réaction mitigée"

---

## Customization Points

### 1. Add / Modify Pair Rules
Edit `HalkaCompositionEngine.InitializePairRules()`:
```csharp
_pairRules[MakePairKey("npc1", "npc2")] = bonus;
```

### 2. Adjust Audience Multipliers
Change inspector fields on HalkaCompositionEngine:
- `baseAudience`
- `reputationAudienceStep`
- `moussaAudienceMultiplier`
- `youssefAudienceBonus`
- `omarAudienceBonus`

### 3. Customize Tier Weights
Edit `GetTierWeight(int tier)` in HalkaCompositionEngine.

### 4. Extend Result Display
Subclass `HalkaResultPanel` or create custom panel that listens to `HalkaOrchestrator.OnHalkaCompleted`.

### 5. NPC Reaction Consequences
Listen to `OnHalkaCompleted` and read `result.exposesSensitiveStory`:
- If true, apply reputation penalty to sensitive NPCs
- Trigger dialogue next cycle: "Je ne peux pas croire que tu as raconté ça..."

---

## Testing Checklist

- [ ] Fragments load correctly from `Assets/StreamingAssets/personas/`
- [ ] Composition panel displays available fragments
- [ ] Selection order updates correctly
- [ ] Score calculation matches expected values
- [ ] LLM narration generates without errors (fallback works if API fails)
- [ ] Result panel displays narration + scores
- [ ] Audience signals correctly modify audience size
- [ ] Cycle reset clears Halka state
- [ ] Multiple cycles work without memory leaks

---

## Prefabs Required

In scene or assignable to HalkaOrchestrator:
1. **HalkaCompositionPanelPrefab** — root panel for fragment selection
   - Must contain: ScrollRect (for fragments), fragment button prefab, submit/cancel buttons
2. **ResultPanelPrefab** — result display (narration + scores)
   - Must contain: TextMeshProUGUI fields for narration, reaction, scores, close button

Template fragments available in `HalkaCompositionPanel` + `HalkaResultPanel`.

---

## Debugging

Enable debug logs:
```csharp
// In HalkaCompositionEngine.cs
Debug.Log($"[HalkaCompositionEngine] Scoring: coherence={coherence}, depth={depth}, audience={audience}");

// In HalkaOrchestrator.cs
Debug.Log($"[HalkaOrchestrator] Halka triggered. Fragments={available.Count}");
```

Check console for:
- `[HalkaCompositionEngine] Narration LLM failed` → fallback narration used
- `[HalkaOrchestrator] No fragments collected. Skipping Halka.` → no stories revealed
- `[HalkaCompositionPanel] Fragment button prefab missing...` → missing UI setup

---

## Next Steps for UI/3D Team

1. **Create prefabs** for HalkaCompositionPanel and HalkaResultPanel
   - Fragment button template with NPC portrait, title, tier indicator
   - Result panel with large narration display, reaction animation
   
2. **Add visual polish**
   - Drag-drop reordering of fragments
   - Real-time score preview
   - Audience visualization (silhouettes, crowd reactions)
   - Sensitive fragment warning badge
   
3. **Integrate with 3D camera**
   - Pan to audience during Halka
   - NPC reactions if present (e.g., Zahra's face if her secret revealed)
   
4. **Audio / ambient**
   - Background music during Halka
   - Narration TTS or voice-over

---

## Backward Compatibility

Old `HalkaManager.cs` (in `Assets/Scripts/Halka/`) is **deprecated**. Use HalkaOrchestrator/HalkaCompositionEngine instead.

To migrate:
- Remove references to `HalkaManager.Instance`
- Replace with `HalkaOrchestrator.Instance`
- Reuse fragment loading (logic identical)
