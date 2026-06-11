# Halka Story Composition — Approche Détaillée

## Vue d'ensemble : Qu'est-ce que la Halka ?

La **Halka** est le système de composition narrative de fin de cycle. Le joueur a collecté des **fragments d'histoires** auprès des NPCs. À la fin du cycle, il doit les **combiner et raconter** devant un cercle de spectateurs.

```
Cycle en cours:
- NPC 1 révèle un fragment
- NPC 2 révèle un fragment
- ... (0 à 7 NPCs)
    ↓
Fin de cycle (dernière conversation NPC):
    ↓
Halka Composition:
- Joueur sélectionne et ordonne les fragments
- Système score la combinaison
- LLM génère la narration
- Audience réagit
    ↓
Conséquences (reputation, NPC reactions, etc.)
```

---

## 1. Fragments — Ce que le joueur collecte

Chaque NPC a **exactement 1 fragment d'histoire** à révéler:

### Structure d'un fragment
```json
{
  "id": "hamid_gnawa_origins",      // Unique fragment ID
  "npc_id": "hamid",                // Source NPC
  "title": "Les Origines de la Gnawa",
  "content": "Long text...",        // Full story text
  "tier": 2,                        // 1=easy, 2=medium, 3=hard/sensitive
  "is_sensitive": false             // Intimate/private story?
}
```

### Les 7 fragments disponibles

| NPC | Fragment | Tier | Sensible | Thème |
|-----|----------|------|----------|-------|
| **Hamid** | Gnawa origins | 2 | Non | Culture musicale |
| **Lalla Fatima** | Gnawa role in Halka | 2 | Non | Tradition |
| **Driss** | Forgetting & solitude | 3 | **Oui** | Psychologie personnelle |
| **Zahra** | Secret intime | 3 | **Oui** | Vie privée |
| **Si Brahim** | Why Halka exists | 3 | Non | Sagesse culturelle |
| **Moussa** | Commerce & culture clash | 1 | Non | Tension économique |
| **Youssef** | Gossip & stories | 1 | Non | Information |
| **Omar** | Photography & memory | 1 | Non | Art & preservation |

---

## 2. Composition — Comment les fragments se combinent

### Phase 1: Collecte (pendant le cycle)
À chaque conversation avec un NPC:
1. LLMNpcLogic révèle le fragment selon certaines conditions
2. Fragment ID ajouté à `GameManager._fragments` (persistent liste globale)
3. Compteur augmente: `GameManager.FragmentCount`

```csharp
// Dans LLMNpcLogic.TryEmitFragmentRevealOnce()
if (conditions_met) {
    GameManager.AddFragment(fragmentId);  // e.g., "hamid_gnawa_origins"
    // NPC dialogue révèle l'histoire
}
```

**Résultat:** À fin de cycle, joueur a 0-7 fragments collectés.

### Phase 2: Sélection & Ordonnancement (HalkaCompositionPanel)

Le joueur voit la liste des fragments collectés et les sélectionne dans un ordre spécifique:

```
Fragments disponibles:
□ Hamid — "Les Origines de la Gnawa"
□ Lalla Fatima — "Le Rôle de la Gnawa dans la Halka"
☑ Driss — "L'Oubli et la Solitude"
☑ Si Brahim — "Pourquoi la Halka existe"

Sélection du joueur (ordre = importance):
1. Driss (l'oubli établit le ton sombre)
2. Si Brahim (explique le contexte)
```

**Output:** `selectedFragmentIds = ["driss_forgetting", "si_brahim_halka_origins"]`

### Phase 3: Scoring (HalkaCompositionEngine)

Le système score la sélection sur **3 dimensions**:

#### a) **Cohérence** — Les fragments s'enchaînent-ils bien?

Basée sur des **paires de NPCs**:
```csharp
_pairRules[MakePairKey("hamid", "lalla_fatima")] = 24;   // Parfait match
_pairRules[MakePairKey("driss", "si_brahim")] = 14;      // Bon
_pairRules[MakePairKey("driss", "zahra")] = 8;           // Acceptable
_pairRules[MakePairKey("hamid", "moussa")] = 6;          // Faible lien
```

**Logique:**
```csharp
foreach (adjacent pair in selection) {
    if (HasPairRule(pair)) 
        coherence += bonus;     // e.g., +24
    else if (SameTier(pair))    // e.g., tier 2 + tier 2
        coherence += 2;
    else
        coherence += 0;         // Pas de lien
}

// Malus si fragments disparates
if (selection.Count > 1 && coherence == 0)
    coherence -= (selection.Count - 1) * 4;
```

**Exemple:** Driss → Si Brahim = +14 pts (bonne transition)

#### b) **Profondeur** — Qualité narrative des fragments

Basée sur **tier des NPCs**:
```csharp
int GetTierWeight(int tier) {
    return tier switch {
        1 => 2,     // Facile (Moussa, Youssef, Omar)
        2 => 5,     // Moyen (Hamid, Lalla Fatima)
        3 => 10,    // Difficile/Sensible (Driss, Zahra, Si Brahim)
    };
}
```

**Logique:**
```csharp
foreach (fragment in selection)
    depth += GetTierWeight(fragment.tier);
```

**Exemple:** 
- Sélection: Driss (tier 3) + Si Brahim (tier 3)
- Profondeur = 10 + 10 = 20

#### c) **Audience** — Combien de gens écoutent?

Basée sur:
1. **Réputation globale** (`GameManager.GlobalReputation`)
   ```csharp
   int audience = baseAudience (3) + (reputation / 10);
   // Si reputation=30 → audience = 3 + 3 = 6 personnes
   ```

2. **Signaux NPCs** (pendant le cycle):
   ```csharp
   if (MoussaAllied)
       audience = (int)(audience * 1.35);     // ×1.35 si Moussa favorable
   if (YoussefBavard)
       audience += 3;                         // +3 si Youssef bavard
   if (OmarPresent)
       audience += 5;                         // +5 si Omar présent
   ```

**Exemple:**
- Base: 3 personnes
- Réputation: +4 (40 points)
- Moussa allié: ×1.35 = 9.45 ≈ 9 personnes

### Phase 4: Score Total & Réaction

```csharp
totalScore = coherence + (depth * 4) + audience;

reactionLabel = switch(totalScore) {
    >= 100 => "Ovation monumentale — le cercle entier est transporté.",
    >= 60  => "Réaction forte — des applaudissements sincères.",
    >= 30  => "Appréciation modérée — quelques sourires.",
    _      => "Réaction mitigée — l'audience reste prudente."
};
```

**Exemple final:**
- Cohérence: 14
- Profondeur: 20 × 4 = 80
- Audience: 9
- **Total: 103 points** → "Ovation monumentale"

---

## 3. Génération Narrative (LLM)

Après le scoring, le système appelle **une seule fois** le LLM pour générer la narration:

### Prompt envoyé
```
Les fragments suivants ont été rassemblés pour une performance Halka:

[Driss] — "L'Oubli et la Solitude"
Il y a des jours où je me sens invisible...
[long text of fragment]

[Si Brahim] — "Pourquoi la Halka existe"
La Halka est née d'un besoin...
[long text of fragment]

Audience estimée: 9 personnes.
Cohérence globale: 14 points.

Compose une narration vivante, ORALE et PUBLIQUE de ces fragments...
```

### Output LLM
```
Le narrateur prend une longue respiration.

"Vous savez, parfois je me sens comme Driss m'a parlé...
invisible, perdu dans la foule. Mais Si Brahim m'a rappelé 
pourquoi nous faisons cela. La Halka existe pour créer des 
ponts entre les âmes..."

[continues for full performance text]
```

**Si LLM échoue:** Fallback généré localement:
```csharp
return "Le joueur raconte des histoires entrecroisées. " + 
       (coherence > 10 
           ? "L'audience suit, captivée par les connexions."
           : "L'audience reste attentive.");
```

---

## 4. Détection de Révélation Sensible

Si le joueur combine un fragment sensible (**tier 3**) avec une **large audience (≥10 personnes)**:

```csharp
result.exposesSensitiveStory = 
    (has_tier3_fragment) && (audienceSize >= 10);
```

**Conséquence narrative:**
- LLM ajoute du suspense autour du moment
- NPC dont l'histoire fut révélée le sait (next cycle)
- Réputation hit possible

---

## 5. Résultat Final (HalkaCompositionResult)

Structure retournée après composition:

```csharp
public class CompositionResult {
    public List<string> selectedFragmentIds;        // Sélection du joueur
    public int coherenceScore;                      // Bonus paires
    public int depthScore;                          // Poids tier
    public int audienceSize;                        // Nombre spectateurs
    public int totalScore;                          // Cohérence + (Profondeur×4) + Audience
    public string reactionLabel;                    // Texte réaction
    public bool exposesSensitiveStory;              // Trahison?
    public string narration;                        // LLM output
}
```

---

## 6. Triggers — Quand tout ça se passe?

### Trigger 1: Fragment Reveal (pendant conversation NPC)

```csharp
// Dans LLMNpcLogic
if (conversation_depth > 3 && favorability > 5) {
    GameManager.AddFragment(npcFragmentId);
    // NPC: "Il y a quelque chose que je dois te confier..."
}
```

### Trigger 2: End of Conversation

```csharp
// Dans InputHandler.EndConversation()
activeNpc.ResetPlayerTransform();
_currentNpc?.EndConversationLogic();  // Save session

// NEW: Check if Halka should start
if (HalkaOrchestrator.Instance != null) {
    HalkaOrchestrator.Instance.TriggerEndOfCycleHalka();
}
```

### Trigger 3: Halka Composition Check

```csharp
// Dans HalkaOrchestrator.TriggerEndOfCycleHalka()
List<FragmentEntry> available = compositionEngine.GetAvailableFragments();

if (available.Count == 0) {
    Debug.Log("No fragments collected. Skipping Halka.");
    return;  // Pas de Halka ce cycle
}

ShowCompositionPanel();  // Affiche le UI
```

### Trigger 4: Score & Generate Narration

```csharp
// Après soumission du joueur
var result = compositionEngine.ScoreComposition(selectedFragmentIds);
yield return StartCoroutine(compositionEngine.GenerateNarration(result, 
    narration => { result.narration = narration; },
    error => { /* fallback */ }
));
```

### Trigger 5: Cycle Reset

```csharp
// Dans GameManager.ResetCycle()
if (HalkaOrchestrator.Instance != null)
    HalkaOrchestrator.Instance.ResetCycle();  // Clear state for next cycle

_fragments.Clear();  // Reset collected fragments
_globalReputation = 0;  // Could start fresh or carry over
```

---

## 7. Flux Complet — Exemple Concret

### Cycle 1

**Jour:**
```
Player:  Parle à Hamid
Hamid:   "Écoute, la Gnawa vient de loin..." (Révèle fragment)
Système: GameManager.AddFragment("hamid_gnawa_origins")

Player:  Parle à Si Brahim
Si Brahim: "Tu sais pourquoi on fait la Halka?" (Révèle fragment)
Système: GameManager.AddFragment("si_brahim_halka_origins")

Player:  Parle à Driss
Driss:   [Pas encore favorable, pas de révélation]
Système: FragmentCount = 2
```

**Fin du cycle (après dernière conversation):**
```
InputHandler.EndConversation()
    ↓
HalkaOrchestrator.TriggerEndOfCycleHalka()
    ↓ (2 fragments available)
HalkaCompositionPanel affiche:
    ☑ Hamid — "Les Origines de la Gnawa"
    ☑ Si Brahim — "Pourquoi la Halka existe"
    
Player sélectionne l'ordre:
    1. Si Brahim (contexte d'abord)
    2. Hamid (histoire ensuite)
    ↓
HalkaCompositionEngine.ScoreComposition():
    - Cohérence: 6 pts (Hamid+Si Brahim pair rule)
    - Profondeur: (2 + 3) × 4 = 20 pts
    - Audience: 3 + (rep/10) = 4 pts
    - Total: 30 pts → "Appréciation modérée"
    
    - exposesSensitiveStory: false (aucun tier 3)
    ↓
LLM Narration générée...
    ↓
HalkaResultPanel affiche résultat
```

**Après Halka:**
```
GameManager.ResetCycle()
    ↓
HalkaOrchestrator.ResetCycle()  // Clear _halkaInProgress, audience signals
EventTracker.ResetCycle()
SessionManager.DeleteAllSessions()

_fragments.Clear()  // Prêt pour cycle 2
```

---

## 8. Customization Points

### Modifier les Pair Bonuses

```csharp
// Dans HalkaCompositionEngine.InitializePairRules()
_pairRules[MakePairKey("npc1", "npc2")] = bonus;

// Exemple: Ajouter une relation
_pairRules[MakePairKey("zahra", "lalla_fatima")] = 18;  // Femmes de culture
```

### Changer les Poids de Tier

```csharp
private int GetTierWeight(int tier) {
    return tier switch {
        1 => 3,      // Au lieu de 2
        2 => 6,      // Au lieu de 5
        3 => 12,     // Au lieu de 10
    };
}
```

### Ajuster les Paramètres d'Audience

```csharp
baseAudience = 5;                    // Minimum 5 au lieu de 3
reputationAudienceStep = 8;          // +1 personne par 8 rep (au lieu de 10)
moussaAudienceMultiplier = 1.5f;     // ×1.5 au lieu de ×1.35
```

---

## 9. Testing — Vérifier la Mécanique

### Test 1: Pair Scoring
```csharp
// Input: ["hamid_gnawa_origins", "lalla_fatima_gnawa_role"]
// Expected: coherenceScore >= 24

var result = engine.ScoreComposition(
    new List<string> { "hamid", "lalla_fatima" }
);
Assert.That(result.coherenceScore, Is.GreaterThanOrEqualTo(24));
```

### Test 2: Audience Calculation
```csharp
// Setup: reputation = 30, Moussa allied
engine.MoussaAllied = true;
GameManager._globalReputation = 30;

// Expected: 3 + 3 + (×1.35) = ~8
var audience = engine.ComputeAudienceSize();
Assert.That(audience, Is.GreaterThanOrEqualTo(8));
```

### Test 3: Sensitive Story Detection
```csharp
// Input: Zahra (tier 3) + audience = 10
var result = engine.ScoreComposition(
    new List<string> { "zahra_secret", "hamid_gnawa" }
);
Assert.That(result.exposesSensitiveStory, Is.True);  // audience >= 10 + tier 3
```

---

## 10. Questions Fréquentes

**Q: Qu'arrive-t-il si le joueur ne sélectionne aucun fragment?**
A: Le système défaut à tous les fragments collectés dans l'ordre:
```csharp
if (selectedOrder.Count == 0)
    _selectedOrder = _availableFragments
        .Select(f => f.fragmentId)
        .ToList();
```

**Q: Le score affecte-t-il la reputation du prochain cycle?**
A: Pas directement. Le `totalScore` détermine la réaction narrative (audience réagit), mais les conséquences sur réputation dépendent des événements (e.g., Si Zahra est trahie, elle baisse reputation).

**Q: Peut-on modifier l'ordre des fragments après sélection?**
A: Oui — l'UI permet drag-drop (à implémenter). À chaque changement d'ordre, le score recalcule en temps réel.

**Q: Que se passe-t-il si plusieurs cycles se suivent rapidement?**
A: `GameManager.ResetCycle()` efface tous les fragments précédents. Chaque cycle redémarre de 0.

---

## Résumé Visual

```
┌─────────────────────────────────────────────────────┐
│                      CYCLE                           │
├─────────────────────────────────────────────────────┤
│                                                      │
│  Conversation NPC 1 → Fragment 1 collected          │
│  Conversation NPC 2 → Fragment 2 collected          │
│  ... (max 7)                                         │
│                                                      │
│  End of Last Conversation                            │
│         ↓                                            │
│  ┌──────────────────────────┐                       │
│  │ Halka Composition Panel   │                       │
│  │ ─────────────────────────│                       │
│  │ ☑ Fragment 1             │                       │
│  │ ☑ Fragment 2             │ ← Joueur choisit     │
│  │ ORDER: 2 → 1              │                       │
│  │ [SUBMIT]                 │                       │
│  └──────────────────────────┘                       │
│         ↓                                            │
│  ┌──────────────────────────┐                       │
│  │ Score Composition         │                       │
│  │ ─────────────────────────│                       │
│  │ Coherence: 14             │                       │
│  │ Depth: 20 ×4 = 80         │                       │
│  │ Audience: 9               │                       │
│  │ Total: 103                │                       │
│  └──────────────────────────┘                       │
│         ↓                                            │
│  ┌──────────────────────────┐                       │
│  │ LLM Narration (1 call)    │                       │
│  │ ─────────────────────────│                       │
│  │ "Le joueur raconte..."    │                       │
│  │ [Full narrative text]     │                       │
│  └──────────────────────────┘                       │
│         ↓                                            │
│  ┌──────────────────────────┐                       │
│  │ Result Display            │                       │
│  │ ─────────────────────────│                       │
│  │ Narration + Scores        │                       │
│  │ Audience Reaction         │                       │
│  └──────────────────────────┘                       │
│         ↓                                            │
│  Reset Cycle → Start New Cycle                      │
│                                                      │
└─────────────────────────────────────────────────────┘
```

---

**Ce système enseigne au joueur:** collecter ne suffit pas. Il faut **comprendre** les histoires pour les **restituer avec sens**.
