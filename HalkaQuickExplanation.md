# Halka — Explication Rapide (ELI5)

## Qu'est-ce que c'est? 

La **Halka** = Performance narrative de fin de cycle.

Pendant le jeu, tu collectes des **fragments d'histoires** de 7 NPCs. À la fin, tu dois les **combiner et raconter** devant un public.

---

## Les 3 Phases

### Phase 1: COLLECTING (pendant le jeu)

```
Semaine du cycle:
  Jour 1: Parle à Hamid → Il te raconte son histoire 📖
  Jour 2: Parle à Driss → Il te raconte la sienne 📖
  Jour 3: Parle à Zahra → Elle te confie son secret 📖
  ...

À la fin: Tu as 2-3 histoires collectées.
```

### Phase 2: COMPOSING (choix du joueur)

```
Fin du dernier NPC:
  "Attend, tu veux que je raconte la Halka?"
  
Tu choisis et ordonnes tes histoires:
  1. Driss d'abord (établit la tension)
  2. Si Brahim ensuite (explique pourquoi)
  3. Hamid à la fin (résout l'histoire)
```

### Phase 3: PERFORMING (narration LLM + résultats)

```
Système calcule une note:
  • Cohérence: Les histoires s'enchaînent-elles bien?
  • Profondeur: Quel "poids" ont tes histoires?
  • Audience: Combien de gens t'écoutent?
  
Puis l'IA génère la narration de ta performance.

Résultat: Audience réagit (applaudit, murmure, reste silencieuse...)
```

---

## Le Système de Scoring — Simplifié

### Cohérence: Comment les histoires s'enchaînent?

```
Bonne paire = Bonus:
  • Hamid + Lalla Fatima = +24 (parfait match!)
  • Driss + Si Brahim = +14 (ça marche bien)
  • Driss + Zahra = +8 (acceptable)
  
Mauvaises paires:
  • Hamid + Youssef = ? (aucune règle spéciale)
  • Multiple histoires sans lien = MALUS!

Exemple:
  Sélection: [Driss, Si Brahim]
  Cohérence = +14 (leurs histoires se lient)
```

### Profondeur: Quel "poids" spirituel?

```
Tier 1 = Facile (Moussa, Youssef, Omar)        = 2 pts chacun
Tier 2 = Moyen (Hamid, Lalla Fatima)           = 5 pts chacun
Tier 3 = Difficile/Intime (Driss, Zahra, SB)   = 10 pts chacun

Puis on multiplie par 4 pour le résultat final.

Exemple:
  Sélection: [Driss (T3), Si Brahim (T3)]
  Profondeur = (10 + 10) × 4 = 80 pts
```

### Audience: Combien de gens écoutent?

```
Base: 3 personnes

Augmenté par:
  + Reputation globale / 10
    (Si tu as 40 points de réputation → +4 personnes)
  
  + Signaux NPCs:
    • Si Moussa t'aime bien → ×1.35 (grossit l'audience!)
    • Si Youssef est bavard → +3 personnes
    • Si Omar est présent → +5 personnes

Exemple:
  Base: 3
  Rep: +4 (40 points)
  Omar présent: +5
  = 12 personnes écoutent
```

### Note Finale

```
totalScore = Cohérence + Profondeur + Audience

Exemple: 14 + 80 + 12 = 106 points

Interprétation:
  >= 100: "Ovation monumentale!" 🎉
  >= 60:  "Applaudissements!" 👏
  >= 30:  "Quelques sourires" 😊
  < 30:   "L'audience reste prudente" 😐
```

---

## Les Triggers — Quand ça se déclenche?

### Trigger 1: Fragment Collection
```
Pendant la conversation NPC:
  If (profondeur conversation > seuil) {
    GameManager.AddFragment("hamid_story");  // Collecté!
  }
```

### Trigger 2: End of Conversation
```
Joueur appuie sur "Terminer conversation":
  InputHandler.EndConversation() {
    // Sauvegarde la session
    
    // NEW: Vérifie Halka
    if (HalkaOrchestrator.Instance != null) {
      HalkaOrchestrator.TriggerEndOfCycleHalka();
    }
  }
```

### Trigger 3: Show Composition UI
```
HalkaOrchestrator vérifie:
  If (fragments.Count > 0) {
    ShowCompositionPanel();  // Affiche liste fragments
  } else {
    Skip Halka this cycle
  }
```

### Trigger 4: Submit & Score
```
Joueur clic "SUBMIT":
  selectedFragments = [user_selection]
  
  result = ComputeScore(selectedFragments)
    // Calcule coherence + depth + audience
    
  narration = CallLLM(result)
    // Génère la narration
    
  DisplayResult(result, narration)
    // Affiche performance
```

### Trigger 5: Cycle Reset
```
Fin de Halka (ou si joueur skip):
  GameManager.ResetCycle() {
    _fragments.Clear();           // Reset histoires collectées
    _globalReputation = 0;        // Reset réputation (ou carry-over)
    HalkaOrchestrator.Reset();    // Reset état Halka
  }
  
  Cycle 2 recommence...
```

---

## Exemple Concret — Pas à Pas

```
CYCLE 1 — Lundi matin

14h: Player parle à HAMID
  Hamid: "Tu sais, la Gnawa vient d'Afrique du Nord..."
  System: GameManager.AddFragment("hamid_gnawa_origins")
  ✓ Fragment 1 collected

16h: Player parle à DRISS
  Driss n'est pas encore favorable → pas de fragment
  ✗ Rien collecté

18h: Player parle à SI BRAHIM
  Si Brahim: "La Halka est née d'un besoin de partager..."
  System: GameManager.AddFragment("si_brahim_halka_origins")
  ✓ Fragment 2 collected

20h: Player appuie "Terminer conversation" avec Moussa
  InputHandler.EndConversation()
    → HalkaOrchestrator.TriggerEndOfCycleHalka()
      → Fragments available? YES (2 fragments)
      → ShowCompositionPanel()

UI AFFICHE:
  ☑ Hamid — "Les Origines de la Gnawa" (Tier 2)
  ☑ Si Brahim — "Pourquoi la Halka existe" (Tier 3)
  
  [SÉLECTIONNER L'ORDRE]

PLAYER CHOISIT:
  1. Si Brahim d'abord (contexte)
  2. Hamid ensuite (histoire)
  [SUBMIT]

SCORING:
  Cohérence: Si Brahim + Hamid = 0 (pas de pair rule)
             Mais même tier? Non (T3 + T2)
             Résultat: +0 → coherence = 0
  
  Profondeur: Si Brahim (T3, 10) + Hamid (T2, 5)
              = 15 × 4 = 60
  
  Audience: Base 3 + rep 0 + no signals = 3
  
  TOTAL: 0 + 60 + 3 = 63 points
  Réaction: "Réaction forte — applaudissements sincères!" 👏

LLM CALLED ONCE:
  "Les fragments suivants..."
  [Génère narration vivante, orale]
  Output: ~800 mots

RESULT PANEL AFFICHE:
  ┌─────────────────────────────┐
  │ NARRATION:                  │
  │ "Le cercle se resserre...   │
  │  vous commencez à raconter. │
  │  Si Brahim dit..."          │
  │  [full narration]           │
  │                             │
  │ AUDIENCE REACTION:          │
  │ "Réaction forte — des       │
  │  applaudissements sincères" │
  │                             │
  │ SCORES:                     │
  │ Coherence: 0                │
  │ Depth: 60                   │
  │ Audience: 3                 │
  │ Total: 63                   │
  └─────────────────────────────┘

CYCLE RESET:
  _fragments = []
  _halkaInProgress = false
  
CYCLE 2 COMMENCE...
```

---

## Les Cas Spéciaux

### Case 1: Révélation Sensible

```
Si tu combines:
  • Une histoire Tier 3 (Driss, Zahra, Si Brahim)
  • ET l'audience >= 10 personnes
  
Alors: exposesSensitiveStory = TRUE

Conséquence:
  • LLM ajoute de la tension narrative
  • NPC dont l'histoire fut révélée le découvre (next cycle)
  • Possible réputation malus
  
Exemple:
  Tu racontes le secret intime de Zahra devant 15 personnes
  → Zahra le sait au cycle suivant
  → "Je ne peux pas croire que tu as raconté ça..."
```

### Case 2: Aucun Fragment Collecté

```
Si fragments.Count == 0:
  Halka est complètement skippée
  Cycle reset commence directement
  
→ Prochaine fois, collecte au moins 1 fragment!
```

### Case 3: Pas de Sélection (Submit sans choix)

```
Si joueur clique Submit sans rien sélectionner:
  Système défaut à: "Tous les fragments disponibles"
  Dans l'ordre de collection
  
→ Moyen par défaut de jouer
```

### Case 4: Erreur LLM

```
Si appel LLM échoue:
  narration = fallback généré localement
  
  Si coherenceScore > 10:
    "L'audience suit, captivée par les connexions..."
  Sinon:
    "L'audience reste attentive, bien que les histoires ne se lient pas toujours."
    
→ Le jeu continue, pas de crash
```

---

## Points Clés à Retenir

### 1. C'est une Composition Mini-Game
Les joueurs ne reçoivent pas une "note chiffrée". Ils reçoivent:
- Une **narration vivante** (ce que l'audience entend)
- Une **réaction narrative** (comment ils réagissent)
- Des **scores cachés** (pour tuning futur, seeds, etc.)

### 2. L'Ordre Compte
Même sélection, ordre différent = signification différente
```
Driss (despair) → Si Brahim (hope) = "Crisis → Purpose"
Si Brahim (hope) → Driss (despair) = "Purpose → Crisis"
```

### 3. Pair Bonuses Récompensent l'Intentionalité
Certaines paires valent +24, d'autres +0. C'est intentionnel.
→ Encourage joueurs à explorer les **connexions narratives**

### 4. Profondeur > Largeur
10 fragments faiblement liés < 3 fragments profonds bien liés

### 5. Audience Matters
Une bonne Halka devant 3 gens < une Halka faible devant 20 gens
→ Encourage joueur à cultiver des relations (Moussa, Youssef, Omar)

---

## Workflow Developers

### Pour Integrer Halka:

1. ✅ Créer HalkaCompositionPanel prefab (UI)
2. ✅ Ajouter HalkaOrchestrator à scene
3. ✅ Assigner prefab à orchestrator
4. ✅ Tester: End conversation → Panel appears → Submit → Narration

### Pour Customizer:

1. Modifier `HalkaCompositionEngine._pairRules` pour changer pair bonuses
2. Ajuster `GetTierWeight()` pour changer profondeur
3. Changer `baseAudience`, multipliers pour tuner audience size

### Pour Déboguer:

- Pas de panel? Check: fragments collectés? Prefab assigné?
- Narration vide? Check: GroqApiClient en scene? LLM working?
- Score bizarre? Add logs dans `ScoreComposition()`

---

## TL;DR

```
HALKA = Compose narratives at cycle end

FLOW:
  Collect fragments (0-7)
    ↓
  End conversation
    ↓
  Select & order fragments
    ↓
  System scores (coherence + depth + audience)
    ↓
  LLM generates narration (1 call)
    ↓
  Display result + audience reaction
    ↓
  Reset cycle → repeat

SCORING:
  Coherence: Do fragments link?
  Depth: How much narrative weight?
  Audience: How many listeners?
  Total = Coherence + (Depth × 4) + Audience

KEY: Rewards intentional combinations, punishes scattering.
     Teaches "Understanding > Collecting"
```

---

**Questions?** Consulte:
- `HalkaSystemIntegrationGuide.md` — Full technical API
- `HalkaCompositionDiagrams.md` — Flowcharts & visuals
- `HalkaStoryCompositionApproach.md` — Deep dive examples
