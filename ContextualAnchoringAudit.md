# Audit Contextual Anchoring — État Actuel vs Idéal

## La Question Critique

**Tu demandes:** "Est-ce que c'est réellement implémenté? Les différences existent-elles?"

**Réponse honnête:** ❌ **Partiellement non. À 40% aujourd'hui.**

---

## Ce qui EST implémenté ✅

### 1. Fragment Core Fixe (100% fait)

```json
{
  "id": "moussa_ancien_musicien",
  "title": "L'Ancien Musicien",
  "content": "Moussa était autrefois un musicien gnawa reconnu, 
             mais il a abandonné son instrument après un deuil."
}
```

✅ **Validé culturellement** (dans StreamingAssets/personas/)
✅ **Réutilisable** à travers les cycles
✅ **Permanent** dans la base de données

---

### 2. Favorabilité & Réputation Tracée (100% fait)

```csharp
// Dans LLMNpcLogic
[SerializeField] private int favorability = 0;      // Par NPC
// Plus:
GameManager.GlobalReputation                        // Globale
```

✅ Chaque NPC a sa favorabilité individuelle
✅ Réputation globale trackée
✅ Sauvegardée en session

**Exemple données joueur:**
```
Moussa favorability: 25
Hamid favorability: 10
GlobalReputation: 35
```

---

### 3. Données Sociales Disponibles (100% fait)

```csharp
// Toutes ces données existent à runtime:
- favorability (Moussa spécifique)
- GameManager.GlobalReputation
- Session history (qui a parlé à qui)
- Conditions (curiosity_shown, fragment_revealed)
- NPCManager events (pour sourcing des relations)
```

---

## Ce qui MANQUE ❌

### 1. Fragment Reveal Narration — N'est PAS Contextualisée

**Situation actuelle:**
```csharp
// Dans LLMNpcLogic.TryEmitFragmentRevealOnce()
if (!GetCondition("fragment_revealed") || _fragmentAnnounced)
    return;

_fragmentAnnounced = true;
string fragmentId = personaData?.story_fragment?.id ?? "unknown_fragment";
GameManager.AddFragment(fragmentId);
OnFragmentCollected?.Invoke(fragmentId);
```

**Ce qui se passe:** 
- Fragment ID ajouté à GameManager._fragments
- Nada de contexte injecté
- Aucun appel LLM pour "raconter" le fragment
- Juste le fait brut: "Fragment collecté"

**Le NPC dit quoi?**
```csharp
// Dans LLMNpcLogic à ce moment:
// Simplement: "Voici mon histoire..."
// Pas personnalisé selon:
//   - Si joueur a déjà parlé à d'autres NPCs
//   - Quelle est la favorabilité actuelle
//   - Quel est le chemin narratif du joueur
```

**Déficit:** Le fragment est collecté, mais **pas sa version contextualisée**.

---

### 2. Halka Composition — Narration Générique

**Situation actuelle (HalkaCompositionEngine.BuildNarrationPrompt):**

```csharp
private string BuildNarrationPrompt(CompositionResult composition)
{
    List<FragmentEntry> selected = composition.selectedFragmentIds
        .Select(id => ResolveFragment(id))
        .Where(f => f != null)
        .ToList();

    string fragmentsText = string.Join("\n\n", selected.Select(f =>
        $"[{f.npcName}] — {f.title}\n{f.content}"));

    return $@"Les fragments suivants ont été rassemblés pour une performance Halka:

{fragmentsText}

Audience estimée : {composition.audienceSize} personnes.
Cohérence globale : {composition.coherenceScore} points.

Compose une narration vivante, ORALE et PUBLIQUE...";
}
```

**Ce qui manque:**
- ❌ Pas de contexte joueur: "Tu connais Moussa depuis longtemps"
- ❌ Pas de position du joueur: "Hamid te fait confiance"
- ❌ Pas de différences relationnelles: "Zahra t'évite depuis..."
- ❌ Pas de chemin narratif: "C'est ta 3e conversation avec..."

**Résultat:** 
- Joueur A obtient la même narration que Joueur B
- Même s'ils ont des favorabilités complètement différentes
- Le LLM ignore complètement qui est le joueur dans cet univers social

**Déficit:** La narration est 100% générique, 0% personalisée.

---

### 3. Story Collector — Capture Juste le Core

**Situation actuelle (SessionManager):**

```csharp
NPCSessionData data = new NPCSessionData
{
    npc_id = npcId,
    favorability = favorability,
    current_emotion = currentEmotion,
    interaction_summary = summary,     // ← Generic summary
    fragment_revealed = conditions["fragment_revealed"]
};
SessionManager.Instance.SaveSession(data);
```

**Ce qui est sauvegardé:**
```json
{
  "npc_id": "moussa",
  "favorability": 25,
  "fragment_revealed": true,
  "interaction_summary": "Joueur curieux, relation positive."
}
```

**Ce qui manque:**
- ❌ La version contextualisée du fragment
- ❌ Comment Moussa a présenté son histoire AU JOUEUR
- ❌ Les nuances de ton selon la relation
- ❌ La "vraie" histoire vécue par le joueur

**Exemple idéal à sauvegarder:**
```json
{
  "npc_id": "moussa",
  "favorability": 25,
  "fragment_revealed": true,
  "fragment_context": {
    "core": "Moussa était autrefois musicien...",
    "presented_as": "Cet homme Moussa, avec qui tu sembles avoir parlé...",
    "tone": "complicit, knowing the player understands"
  },
  "interaction_summary": "..."
}
```

**Déficit:** Story Collector capture le fait, pas l'expérience.

---

## Matrice Comparée: Idéal vs Réalité

```
┌─────────────────────┬──────────────────┬─────────────────────┐
│ Aspect              │ Idéal (Contextual│ Réel (Actuel)       │
│                     │ Anchoring)       │                     │
├─────────────────────┼──────────────────┼─────────────────────┤
│ Fragment Core       │ ✅ Fixe validé   │ ✅ Fixe validé      │
├─────────────────────┼──────────────────┼─────────────────────┤
│ Fragment Reveal     │ ✅ Contextualisé │ ❌ Generic          │
│ Narration           │   par favorabilité│   (pas de LLM call)│
├─────────────────────┼──────────────────┼─────────────────────┤
│ Halka Narration     │ ✅ Varie selon   │ ❌ Même pour tous   │
│                     │   relations      │   les joueurs       │
├─────────────────────┼──────────────────┼─────────────────────┤
│ Données Sociales    │ ✅ Injectées     │ ❌ Pas injectées    │
│ dans LLM Prompt     │   dans prompt    │   dans prompt       │
├─────────────────────┼──────────────────┼─────────────────────┤
│ Story Collector     │ ✅ Capture       │ ❌ Capture juste    │
│                     │   contexte       │   le core           │
├─────────────────────┼──────────────────┼─────────────────────┤
│ Joueur Ressent      │ ✅ Immersion:    │ ❌ Distance: "Le    │
│                     │ "Tu sais, c'est  │   système me parle, │
│                     │   POUR toi"      │   pas à moi"        │
└─────────────────────┴──────────────────┴─────────────────────┘
```

---

## Où ça Casse? 3 Points Critiques

### Point 1: Fragment Reveal (Conversation NPC)

**Actuellement:**
```
Joueur: "Raconte-moi une histoire"
NPC: [LLM genère dialogues normaux pendant la conversation]
[À un moment, favorabilité >= seuil]
→ Fragment déverrouillé (ajout silencieux à GameManager._fragments)
→ NPC continue peut-être: "Voilà, tu sais tout"
→ Fin de conversation
```

**Le problème:** Pas de moment spécial où le fragment est *présenté*. Pas d'appel LLM pour "comment ce NPC raconte son histoire à ce joueur spécifique".

**Idéalement:**
```
[Favorabilité unlock atteint]
→ LLM call: "Raconte ton histoire à ce joueur qui:
   - T'a parlé 3 fois
   - Connaît aussi Hamid
   - A une bonne réputation générale
   - T'a montré de la curiosité"
→ NPC génère sa version personnalisée
→ Joueur entend: "[Présentation contextualisée]"
→ Fragment ajouté avec sa "metadata de présentation"
```

### Point 2: Halka Composition Prompt

**Actuellement:**
```csharp
// BuildNarrationPrompt() fait JUSTE:
"Voici les fragments: [core content]
Audience: 9 personnes
Cohérence: 14 points
Raconte une Halka."
```

**Manque du contexte:**
```
❌ "Le joueur connaît Hamid depuis longtemps (fav: 30)"
❌ "Zahra lui fait confiance (fav: 40)"
❌ "Moussa l'ignore (fav: -5)"
❌ "Réputation globale: 35/100"
❌ "Chemin narratif: [liste des NPCs rencontrés]"
```

**Impact:** 
- Deux joueurs avec favorabilités inverses → même narration
- LLM ignore complètement la position du joueur dans l'univers social

### Point 3: Session Storage

**Actuellement sauvegardé:**
```json
{
  "npc_id": "moussa",
  "interaction_summary": "Relation positive."
}
```

**Devrait sauvegarder:**
```json
{
  "npc_id": "moussa",
  "interaction_summary": "Relation positive.",
  "fragment_presented_as": "[LLM-generated contextualized introduction]",
  "favorability_at_reveal": 25,
  "other_npcs_known_by_player": ["hamid", "lalla_fatima"]
}
```

---

## Plan de Correction

### Phase 1: Fragment Reveal Contextualization (CRITIQUE)

**Fichier à modifier:** `LLMNpcLogic.cs` → `TryEmitFragmentRevealOnce()`

```csharp
private void TryEmitFragmentRevealOnce()
{
    if (!GetCondition("fragment_revealed") || _fragmentAnnounced)
        return;

    _fragmentAnnounced = true;
    string fragmentId = personaData?.story_fragment?.id ?? "unknown_fragment";
    
    // NEW: Generate contextualized presentation
    StartCoroutine(GenerateContextualizedFragmentPresentation(fragmentId));
}

private IEnumerator GenerateContextualizedFragmentPresentation(string fragmentId)
{
    // Build context data
    var context = new Dictionary<string, object> {
        { "favorability", favorability },
        { "globalReputation", GameManager.GlobalReputation },
        { "npcName", npcName },
        { "npcId", npcId },
        { "playerInteractionCount", GetPlayerInteractionCount() },
        { "otherNpcsKnownByPlayer", GetOtherNpcsKnownByPlayer() }
    };
    
    // Call LLM to generate contextualized introduction
    string contextualizedPresentation = yield return 
        StartCoroutine(GenerateFragmentPresentation(fragmentId, context));
    
    // Add to session (NEW: store contextualized version)
    GameManager.AddFragmentWithContext(fragmentId, contextualizedPresentation);
}
```

### Phase 2: Halka Prompt Enrichment

**Fichier à modifier:** `HalkaCompositionEngine.cs` → `BuildNarrationPrompt()`

```csharp
private string BuildNarrationPrompt(CompositionResult composition)
{
    // Existing: get fragments
    List<FragmentEntry> selected = ...;
    string fragmentsText = ...;
    
    // NEW: Inject player social context
    string playerContext = BuildPlayerSocialContext();
    
    return $@"Les fragments suivants ont été rassemblés pour une performance Halka:

{fragmentsText}

CONTEXTE SOCIAL DU JOUEUR:
{playerContext}

Audience estimée : {composition.audienceSize} personnes.
Cohérence globale : {composition.coherenceScore} points.

Compose une narration qui RECONNAÎT:
1. La position du joueur dans ce cercle social
2. Les relations spécifiques aux NPCs mentionnés
3. Le chemin narratif du joueur (qui a rencontré)
4. L'authenticité des voix narratives

...";
}

private string BuildPlayerSocialContext()
{
    var sb = new System.Text.StringBuilder();
    
    sb.AppendLine($"Réputation globale: {GameManager.GlobalReputation}/100");
    
    // For each NPC in fragments:
    foreach (var npc in GetNpcsInComposition())
    {
        var llmLogic = NPCManager.GetNpcLogic(npc.id);
        if (llmLogic != null)
        {
            int fav = llmLogic.GetFavorability();
            sb.AppendLine($"- {npc.name}: favorabilité {fav}/100");
        }
    }
    
    sb.AppendLine($"NPCs rencontrés: {string.Join(", ", GetEncounteredNpcs())}");
    
    return sb.ToString();
}
```

### Phase 3: Session Context Enrichment

**Fichier à modifier:** `SessionManager.cs`

```csharp
[Serializable]
public class NPCSessionData
{
    public string npc_id;
    public int favorability;
    public string current_emotion;
    public string interaction_summary;
    public bool fragment_revealed;
    
    // NEW: Contextualized data
    public string fragment_presented_as;        // ← How LLM presented the story
    public int favorability_at_reveal;
    public List<string> other_npcs_known_by_player;
    public int player_interaction_count;
}
```

---

## Exemple Avant/Après

### AVANT (Aujourd'hui)

**Joueur A** (favorable avec Moussa):
```
Moussa: [generic dialogue]
→ Fragment: "moussa_ancien_musicien"
→ Halka narration: 
   "Moussa était autrefois musicien, il a abandonné..."
   [Même texte pour tous]
```

**Joueur B** (hostile avec Moussa):
```
Moussa: [generic dialogue]
→ Fragment: "moussa_ancien_musicien"
→ Halka narration:
   "Moussa était autrefois musicien, il a abandonné..."
   [Identique! Pas de nuance]
```

### APRÈS (Avec Contextual Anchoring)

**Joueur A** (favorable avec Moussa, fav=40):
```
Moussa: [dialogue optimiste]
→ Fragment reveal:
   LLM: "Comment Moussa présente SON histoire à quelqu'un qu'il apprécie?"
   → "Écoute, tu sembles comprendre. Ce que peu savent, 
      c'est que j'étais musicien..."
→ Fragment + contexte sauvegardé
→ Halka narration:
   "Cet homme Moussa, que tu sembles connaître, 
    était autrefois un musicien gnawa reconnu..."
```

**Joueur B** (hostile avec Moussa, fav=-10):
```
Moussa: [dialogue méfiant]
→ Fragment reveal:
   LLM: "Comment Moussa protège-t-il son histoire face à quelqu'un
        qu'il ne fait pas confiance?"
   → "Je ne devrais pas, mais puisque tu poses la question...
      J'étais musicien. C'est tout ce que tu dois savoir."
→ Fragment + contexte sauvegardé
→ Halka narration:
   "Il y a cet homme Moussa. Il cache quelque chose.
    On dit qu'il fut musicien, mais pourquoi ce silence?"
```

---

## Coût d'Implémentation

| Phase | Effort | Fichiers | Risque |
|-------|--------|----------|--------|
| 1: Fragment Reveal | 🟡 Moyen | LLMNpcLogic | Bas (isolé) |
| 2: Halka Context | 🟢 Faible | HalkaCompositionEngine | Bas (additionnel) |
| 3: Session Storage | 🟢 Faible | SessionManager | Très bas (backward compat) |

**Total:** ~3-4 heures d'implémentation + test

---

## Recommandation

**C'est CRITIQUE pour respecter la philosophie du jeu:**

> "Chaque joueur doit avoir l'impression que les histoires ont été racontées pour lui."

Sans ça, on a un système qui:
- ✅ Collecte des histoires
- ❌ Les raconte générique-ment
- ❌ Perd l'immersion

**Action:** Implémenter les 3 phases avant fin d'intégration UI.

---

## Summary Table

```
AUJOURD'HUI:
- Fragment core: fixe ✅
- Fragment reveal: générique ❌
- Halka narration: générique ❌
- Story Collector: incomplet ❌
→ 40% Contextual Anchoring

IDÉALEMENT:
- Fragment core: fixe ✅
- Fragment reveal: contextualisé ✅
- Halka narration: contextualisée ✅
- Story Collector: complet ✅
→ 100% Contextual Anchoring

TRAVAIL: Fermer les 3 gaps ❌❌❌
```
