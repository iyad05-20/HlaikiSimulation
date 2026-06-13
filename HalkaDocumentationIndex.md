# 📖 Halka System Documentation Index

## Quick Navigation

### 🎯 Start Here

1. **`HalkaQuickExplanation.md`** (⭐ ELI5 Version)
   - **Pour qui:** Tout le monde (designers, devs, camarades)
   - **Contenu:** Explication simple, exemple concret, TL;DR
   - **Temps:** 5-10 min de lecture
   - **Bon si:** Tu veux comprendre rapidement "ça marche comment?"

2. **`HalkaCompositionDiagrams.md`** (Visual Reference)
   - **Pour qui:** Visual learners
   - **Contenu:** Flowcharts, state machines, scoring breakdown visuals
   - **Temps:** 5-10 min
   - **Bon si:** Tu comprends mieux avec des diagrammes

### 📘 Deep Dives

3. **`HalkaStoryCompositionApproach.md`** (Complete Technical)
   - **Pour qui:** Developers, architects
   - **Contenu:** Approche complète, règles détaillées, testing, customization
   - **Temps:** 20-30 min
   - **Bon si:** Tu veux tout savoir: fragments, scoring, triggers, LLM integration

4. **`HalkaSystemIntegrationGuide.md`** (API Reference)
   - **Pour qui:** Backend/integration team
   - **Contenu:** Architecture, data structures, integration points, customization
   - **Temps:** 15-20 min
   - **Bon si:** Tu dois intégrer Halka avec d'autres systèmes

### 🚀 Deployment & Implementation

5. **`HalkaDeploymentGuide.md`** (Setup for UI/3D Team)
   - **Pour qui:** UI designers, 3D artists
   - **Contenu:** Prefab setup, scene configuration, debugging, visual polish ideas
   - **Temps:** 10-15 min
   - **Bon si:** Tu dois créer les UI et prefabs

6. **`HalkaRefactoringCompleteLog.md`** (Change Log)
   - **Pour qui:** Git history, architecture review
   - **Contenu:** What changed, before/after, backward compat
   - **Temps:** 10-15 min
   - **Bon si:** Tu veux savoir pourquoi la structure a changé

---

## Reading Paths by Role

### 🎮 Game Designer
```
1. HalkaQuickExplanation.md      (understand mechanic)
2. HalkaCompositionDiagrams.md   (see flow visually)
3. HalkaStoryCompositionApproach.md (review scoring rules)
→ Done! You understand the system.
```

### 👨‍💻 Backend Developer / Integration
```
1. HalkaQuickExplanation.md           (overview)
2. HalkaSystemIntegrationGuide.md    (full API)
3. HalkaStoryCompositionApproach.md  (deep dive scoring)
→ Review code in HalkaCompositionEngine.cs + HalkaOrchestrator.cs
```

### 🎨 UI/3D Artist
```
1. HalkaQuickExplanation.md      (understand the flow)
2. HalkaDeploymentGuide.md       (setup prefabs & scene)
3. HalkaCompositionDiagrams.md   (visual reference)
→ Create panels, assign UI elements, test in scene
```

### 🏗️ Architect / Code Review
```
1. HalkaRefactoringCompleteLog.md  (what changed)
2. HalkaSystemIntegrationGuide.md  (architecture)
3. HalkaCompositionDiagrams.md     (state machine)
4. Review: Assets/Scripts/Npc/Halka/*.cs
```

### 🤝 Camarade Joining Project
```
1. HalkaQuickExplanation.md     (understand it exists)
2. HalkaDeploymentGuide.md      (what you need to do)
3. HalkaCompositionDiagrams.md  (understand flow)
4. Ask questions! Link to these docs.
```

---

## Document Comparison Matrix

| Document | Level | Code | Visuals | Examples | Setup |
|----------|-------|------|---------|----------|-------|
| QuickExplanation | ⭐ Beginner | ❌ | ✅✅ | ✅✅✅ | ❌ |
| Diagrams | ⭐ Beginner | ❌ | ✅✅✅ | ✅ | ❌ |
| Approach | ⭐⭐ Intermediate | ⚠️ refs | ✅ | ✅✅✅ | ❌ |
| IntegrationGuide | ⭐⭐⭐ Advanced | ✅✅ | ✅ | ✅ | ❌ |
| DeploymentGuide | ⭐⭐ Intermediate | ⚠️ setup | ❌ | ✅ | ✅✅ |
| ChangeLog | ⭐⭐⭐ Advanced | ✅ | ✅ | ⚠️ | ❌ |

---

## Code Structure — Files to Review

### Main System Files

```
Assets/Scripts/Npc/Halka/
├── HalkaCompositionEngine.cs
│   └── Pure logic: scoring, LLM narration
│   └── Classes: FragmentEntry, CompositionResult
│   └── Methods: ScoreComposition(), GenerateNarration()
│
└── HalkaOrchestrator.cs
    └── Singleton orchestrator: cycle flow, event broadcasting
    └── Methods: TriggerEndOfCycleHalka(), ResetCycle()
    └── Events: OnHalkaCompleted, OnHalkaStartRequested

Assets/Scripts/Ui/
└── HalkaCompositionPanel.cs
    ├── HalkaCompositionPanel class
    │   └── Fragment display, selection/ordering UI
    │   └── Events: OnCompositionSubmitted, OnCancelled
    │
    └── HalkaResultPanel class
        └── Result display: narration + scores
```

### Integration Points

```
Assets/Scripts/Ui/InputHandler.cs
└── Line 87-90: Calls HalkaOrchestrator.TriggerEndOfCycleHalka()
    └── After EndConversation()

Assets/Scripts/GameManager.cs
└── Line 113: Calls HalkaOrchestrator.Instance.ResetCycle()
    └── During ResetCycle()
```

---

## Key Concepts Glossary

| Term | Definition | Location |
|------|-----------|----------|
| **Fragment** | A story piece from an NPC | HalkaCompositionEngine.FragmentEntry |
| **Tier** | Difficulty/depth level (1, 2, 3) | NPC persona JSON |
| **Coherence** | How well fragments link narratively | HalkaCompositionEngine.ScorePair() |
| **Depth** | Total narrative weight of selection | HalkaCompositionEngine.GetTierWeight() |
| **Audience** | Number of listeners for performance | HalkaCompositionEngine.ComputeAudienceSize() |
| **Composition Result** | Final score + narration | HalkaCompositionEngine.CompositionResult |
| **Sensitive Story** | Intimate/private fragment exposed | tier >= 3 && audience >= 10 |
| **Halka Trigger** | Cycle end condition to start | InputHandler.EndConversation() |
| **Pair Rule** | Bonus for specific NPC combinations | _pairRules dict |
| **Fallback Narration** | Auto-generated if LLM fails | HalkaCompositionEngine.BuildFallbackNarration() |

---

## Common Questions

### Q: Where do I start if I'm new to this?
**A:** Read `HalkaQuickExplanation.md` first (5 min), then based on your role, follow the reading path.

### Q: How do I add a new pair bonus?
**A:** Edit `HalkaCompositionEngine.InitializePairRules()`. See `HalkaSystemIntegrationGuide.md` section "Customization Points".

### Q: What's the difference between coherence and depth?
**A:** 
- **Coherence** = Do the *stories connect narratively?*
- **Depth** = What's the *narrative weight* of the stories?
See `HalkaCompositionDiagrams.md` Section 2 for scoring breakdown.

### Q: Can I change the audience size formula?
**A:** Yes. Edit `HalkaCompositionEngine.ComputeAudienceSize()` or adjust inspector fields. See `HalkaDeploymentGuide.md` "Customization Points".

### Q: How does the LLM narration work?
**A:** Called once at composition submission with all selected fragments. Prompt built in `BuildNarrationPrompt()`. See `HalkaStoryCompositionApproach.md` Section 3.

### Q: What happens if fragments aren't collected?
**A:** Halka is skipped entirely, cycle resets normally. See `HalkaQuickExplanation.md` "Case 2".

### Q: Where are UI prefabs?
**A:** Created by UI team and assigned to `HalkaOrchestrator.halkaCompositionPanelPrefab`. See `HalkaDeploymentGuide.md` Phase 1.

---

## Debugging Checklist

- [ ] Read `HalkaDeploymentGuide.md` "Debugging" section first
- [ ] Check console for `[HalkaOrchestrator]` or `[HalkaCompositionEngine]` logs
- [ ] Verify fragments collected: `GameManager.GetCollectedFragmentsSnapshot().Count > 0`
- [ ] Verify HalkaOrchestrator in scene and prefab assigned
- [ ] Verify UI prefab has all required SerializeFields
- [ ] Check if LLM is working: test GroqApiClient separately
- [ ] Review flowchart in `HalkaCompositionDiagrams.md` Section 5 (State Machine)

---

## Quick Links Within Docs

### HalkaQuickExplanation.md
- `## Qu'est-ce que c'est?` — Start here
- `## Les 3 Phases` — Overview flow
- `## Le Système de Scoring — Simplifié` — Scoring explained
- `## Exemple Concret — Pas à Pas` — Full walkthrough
- `## TL;DR` — One-page summary

### HalkaCompositionDiagrams.md
- `## 1. Fragment Collection Flow` — Collection phase diagram
- `## 2. Story Composition Scoring Breakdown` — Score calculation
- `## 3. Order Matters` — Why order affects narrative
- `## 5. Complete State Machine` — Full state flow
- `## 8. Scoring Visualization` — All factors at a glance

### HalkaStoryCompositionApproach.md
- `## 1. Fragments — Ce que le joueur collecte` — Fragment structure
- `## 2. Composition — Comment les fragments se combinent` — 4 phases
- `## 3. Génération Narrative (LLM)` — LLM integration
- `## 4. Détection de Révélation Sensible` — Betrayal mechanic
- `## 7. Flux Complet — Exemple Concret` — End-to-end walkthrough

### HalkaSystemIntegrationGuide.md
- `## Workflow` — Complete cycle flow
- `## Integration Points` — Where Halka connects
- `## Key Data Structures` — API reference
- `## Scoring Rules` — Mathematical formulas
- `## Customization Points` — How to extend

---

## Files Organization

```
HlaikiSimulation/
├── HalkaQuickExplanation.md          ← START HERE
├── HalkaCompositionDiagrams.md       ← Visual reference
├── HalkaStoryCompositionApproach.md  ← Deep dive
├── HalkaSystemIntegrationGuide.md    ← Full API
├── HalkaDeploymentGuide.md           ← Setup guide
├── HalkaRefactoringCompleteLog.md    ← What changed
│
├── Assets/Scripts/Npc/Halka/
│   ├── HalkaCompositionEngine.cs     ← Pure logic
│   └── HalkaOrchestrator.cs          ← Orchestration
│
└── Assets/Scripts/Ui/
    └── HalkaCompositionPanel.cs      ← UI layer
```

---

## Version Info

- **Documentation Date**: 2026-05-22
- **System Status**: ✅ Restructuring Complete
- **Code Status**: ✅ Ready for UI Integration
- **Last Updated**: [Current Session]

---

## Feedback & Issues

If you find:
- ❌ Docs unclear → Add comment in this index
- ❌ Code bugs → Review HalkaCompositionEngine.cs / HalkaOrchestrator.cs
- ❌ Missing info → Suggest new section

---

## Archive

**Deprecated files** (safe to delete):
- `Assets/Scripts/Halka/HalkaManager.cs` ← Old monolithic version
  - Replaced by `HalkaCompositionEngine.cs` + `HalkaOrchestrator.cs`
  - All references in codebase already updated

---

**Happy reading! 🎭**

Choose your starting document based on your role and return here for reference.
