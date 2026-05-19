# Halka System — Deployment Summary

## What Changed

The Halka system has been **restructured to follow project architecture standards** (SRP, Singletons, namespace `JemaaGame.NPC`).

### Old Structure (Deprecated)
```
Assets/Scripts/Halka/HalkaManager.cs ← isolated, mixed responsibilities
```

### New Structure (Active)
```
Assets/Scripts/Npc/Halka/HalkaCompositionEngine.cs    ← pure logic (scoring, narration)
Assets/Scripts/Npc/Halka/HalkaOrchestrator.cs         ← orchestrator (singleton, cycle flow)
Assets/Scripts/Ui/HalkaCompositionPanel.cs            ← UI (fragment selection, result display)
```

---

## Integration Already Done

### ✅ InputHandler (Modified)
**File**: `Assets/Scripts/Ui/InputHandler.cs`
- **Line 87-90**: Added call to `HalkaOrchestrator.Instance.TriggerEndOfCycleHalka()`
- **Trigger**: After `EndConversation()` completes
- **Effect**: Opens Halka composition panel if fragments collected

### ✅ GameManager (Modified)
**File**: `Assets/Scripts/GameManager.cs`
- **Line 113**: Changed `HalkaManager.Instance` → `HalkaOrchestrator.Instance`
- **Effect**: Cycle reset now clears Halka state properly

---

## What You Need to Do

### Phase 1: Create Prefabs (5-10 min)

1. **HalkaCompositionPanelPrefab**
   - Create a Canvas-based panel with:
     - **ScrollRect** for fragments (name: `fragmentScrollArea`)
     - **Fragment Button Template** (name: `fragmentButtonPrefab`) — should instantiate multiple times
     - **Text field** for selected order (name: `selectedOrderText`)
     - **Submit Button** (name: `submitButton`)
     - **Cancel Button** (name: `cancelButton`)
   - Assign to `HalkaOrchestrator.halkaCompositionPanelPrefab` in inspector

2. **ResultPanelPrefab** (Optional but recommended)
   - Canvas with:
     - **Large TextMeshProUGUI** for narration (name: `narrationText`)
     - **TextMeshProUGUI** for reaction (name: `reactionText`)
     - **TextMeshProUGUI** for scores breakdown (name: `scoreText`)
     - **Close Button** (name: `resultCloseButton`)
   - Assign to `HalkaOrchestrator.halkaCompositionPanelPrefab` (or create separate slot)

### Phase 2: Add Prefab Reference

1. Create an empty GameObject in your scene hierarchy
2. Add script component: `HalkaOrchestrator`
3. In inspector, assign:
   - **Composition Engine**: Drag the same GameObject (it needs HalkaCompositionEngine too)
   - **Halka Composition Panel Prefab**: Your new panel prefab

4. (Optional) Add `HalkaCompositionEngine` component to the same GameObject if not auto-referenced

### Phase 3: Test Fragment Collection

1. Start a cycle, talk to any NPC
2. Trigger fragment reveal (depends on LLMNpcLogic implementation)
3. End conversation → Halka panel should appear
4. Select fragments → Submit → Narration should generate (or fallback if LLM fails)

---

## Scene Setup Checklist

- [ ] Scene has a GameObject with `HalkaOrchestrator` component
- [ ] HalkaOrchestrator has `HalkaCompositionEngine` assigned (same or linked)
- [ ] `halkaCompositionPanelPrefab` assigned
- [ ] Panel prefab has all required UI fields named correctly
- [ ] GameManager in scene
- [ ] InputHandler in scene
- [ ] At least one NPC with fragment reveals implemented

---

## Customization Points

### Adjust Audience Parameters
Edit **HalkaCompositionEngine** inspector in scene:
```
Base Audience: 3
Reputation Audience Step: 10
Moussa Audience Multiplier: 1.35
Youssef Audience Bonus: 3
Omar Audience Bonus: 5
```

### Add / Modify Pair Bonuses
Edit `HalkaCompositionEngine.InitializePairRules()` (line ~67):
```csharp
_pairRules[MakePairKey("npc1", "npc2")] = bonus_score;
```

### Change Reaction Labels
Edit `HalkaCompositionEngine.GetReactionLabel()` (line ~210).

---

## Events for Advanced Features

Listen to these events for downstream logic:

### HalkaOrchestrator.OnHalkaCompleted
```csharp
HalkaOrchestrator.Instance.OnHalkaCompleted += (result) => 
{
    Debug.Log($"Halka done! Score: {result.totalScore}");
    if (result.exposesSensitiveStory)
    {
        // Handle betrayal consequences (reputation hit, NPC reactions)
    }
};
```

### HalkaOrchestrator.OnHalkaStartRequested
```csharp
HalkaOrchestrator.Instance.OnHalkaStartRequested += () =>
{
    Debug.Log("Halka composition starting...");
};
```

---

## Debugging

### No Halka Panel Appears
- Check: Are fragments collected? (`GameManager.GetCollectedFragmentsSnapshot().Count > 0`)
- Check: Is HalkaOrchestrator in scene and halka panel prefab assigned?
- Log: `[HalkaOrchestrator] Halka triggered. Fragments={count}`

### LLM Narration Fails
- Fallback narration auto-generates
- Check console for `[HalkaCompositionEngine] Narration LLM failed`
- Verify GroqApiClient is in scene and API key valid

### UI References Missing
- All SerializeFields must be assigned in inspector
- Field names MUST match exactly (fragmentScrollArea, fragmentButtonPrefab, etc.)
- Check console for `[HalkaCompositionPanel] Fragment container or prefab not assigned`

---

## Backward Compatibility

Old `HalkaManager` (in `Assets/Scripts/Halka/`) is **deprecated**.
- Remove if present from scene
- No existing code should reference it (only InputHandler and GameManager did, now updated)
- New system is **drop-in replacement** with same output structure

---

## File Locations

- **Documentation**: `HlaikiSimulation/HalkaSystemIntegrationGuide.md`
- **Composition Engine**: `Assets/Scripts/Npc/Halka/HalkaCompositionEngine.cs`
- **Orchestrator**: `Assets/Scripts/Npc/Halka/HalkaOrchestrator.cs`
- **UI Panel**: `Assets/Scripts/Ui/HalkaCompositionPanel.cs`

---

## Next: Visual Polish

Once basic flow works:

1. **Fragment Preview** — Click fragment to preview full text
2. **Drag-and-Drop** — Reorder fragments by dragging
3. **Live Scoring** — Update score display as user selects
4. **3D Integration** — Pan camera to audience during narration
5. **NPC Reactions** — If sensitive story exposed, show affected NPC's reaction
6. **Audio** — Background Gnawa music, narration voice-over

See **HalkaSystemIntegrationGuide.md** for more details.

---

## Questions?

1. Check **HalkaSystemIntegrationGuide.md** for full API reference
2. Look at console logs for error messages
3. Review `HalkaCompositionEngine` comments for scoring logic
4. Compare UI setup with existing DialoguePanel for reference

Good luck! 🎭
