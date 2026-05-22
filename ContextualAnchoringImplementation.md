# Contextual Anchoring Implementation Plan

## Objectif

Faire passer le système de **Générique (40%)** à **Contextualisé (100%)**

Chaque joueur doit ressentir: "Cette histoire a été racontée pour moi."

---

## Phase 1: Fragment Reveal Contextualization (PRIORITÉ 1)

### Où
`Assets/Scripts/Npc/LLM/LLMNpcLogic.cs` — Méthode `TryEmitFragmentRevealOnce()`

### Quoi

Actuellement:
```csharp
private void TryEmitFragmentRevealOnce()
{
    if (!GetCondition("fragment_revealed") || _fragmentAnnounced)
        return;

    _fragmentAnnounced = true;
    string fragmentId = personaData?.story_fragment?.id ?? "unknown_fragment";
    GameManager.AddFragment(fragmentId);  // ← Juste l'ID, pas de contexte
    OnFragmentCollected?.Invoke(fragmentId);
}
```

Doit devenir:
```csharp
private void TryEmitFragmentRevealOnce()
{
    if (!GetCondition("fragment_revealed") || _fragmentAnnounced)
        return;

    _fragmentAnnounced = true;
    string fragmentId = personaData?.story_fragment?.id ?? "unknown_fragment";
    
    // NOUVEAU: Générer la présentation contextualisée
    StartCoroutine(GenerateAndSaveContextualizedFragment(fragmentId));
}

private IEnumerator GenerateAndSaveContextualizedFragment(string fragmentId)
{
    // 1. Construire le contexte social
    var context = new FragmentRevealContext
    {
        playerFavorabilityWithNpc = favorability,
        playerGlobalReputation = GameManager.GlobalReputation,
        npcName = npcName,
        npcId = npcId,
        playerConversationCount = GetPlayerConversationCount(),
        otherNpcsEncounteredByPlayer = GetOtherNpcsEncounteredByPlayer()
    };

    // 2. Appel LLM: "Comment ce NPC raconte son histoire À CE JOUEUR?"
    string contextualizedNarration = null;
    bool done = false;

    List<GroqMessage> messages = new List<GroqMessage>
    {
        new GroqMessage
        {
            role = "system",
            content = $"Tu es {npcName}, un personnage de jeu vidéo. " +
                     $"Tu dois présenter une histoire intime au joueur. " +
                     $"Adapte ton ton et ta façon de la présenter selon votre relation."
        },
        new GroqMessage
        {
            role = "user",
            content = BuildFragmentRevealPrompt(fragmentId, context)
        }
    };

    StartCoroutine(GroqApiClient.Instance.SendChatRequest(messages,
        result => { contextualizedNarration = result; done = true; },
        error => { 
            Debug.LogWarning($"[LLM Fragment Reveal] Failed: {error}");
            done = true; 
        }
    ));

    yield return new WaitUntil(() => done);

    // 3. Fallback si LLM échoue
    if (string.IsNullOrEmpty(contextualizedNarration))
    {
        contextualizedNarration = BuildFallbackFragmentNarration(fragmentId, context);
    }

    // 4. Sauvegarder avec le contexte
    GameManager.AddFragmentWithContext(fragmentId, contextualizedNarration);
    OnFragmentCollected?.Invoke(fragmentId);
    
    Debug.Log($"[LLM] Fragment collected with context: {fragmentId}");
}

private string BuildFragmentRevealPrompt(string fragmentId, FragmentRevealContext context)
{
    var fragment = personaData?.story_fragment;
    if (fragment == null)
        return "Tu dois partager une histoire importante.";

    string relationshipStatus = context.playerFavorabilityWithNpc switch
    {
        > 30 => "tu fais confiance à ce joueur",
        > 10 => "tu trouves ce joueur intéressant",
        >= 0 => "tu restes neutre",
        > -10 => "tu es méfiant envers ce joueur",
        _ => "tu ne fais pas confiance à ce joueur"
    };

    string otherNpcsMention = context.otherNpcsEncounteredByPlayer.Count > 0
        ? $"Le joueur connaît aussi: {string.Join(", ", context.otherNpcsEncounteredByPlayer)}"
        : "Le joueur ne connaît que toi pour l'instant.";

    return $@"CONTEXTE:
- Relation avec joueur: {relationshipStatus} (favorabilité: {context.playerFavorabilityWithNpc}/100)
- Réputation globale du joueur: {context.playerGlobalReputation}/100
- Nombre de conversations: {context.playerConversationCount}
- {otherNpcsMention}

HISTOIRE À RACONTER:
Titre: {fragment.title}
Contenu core: {fragment.content}

INSTRUCTION:
Tu dois présenter cette histoire en 2-3 phrases. Adapte:
1. La façon dont tu l'introduis selon la relation
2. Le niveau de détail que tu révèles
3. Le ton émotionnel (confiance, méfiance, neutralité)

Exemples:
- Si favorable: ""Tu sembles comprendre. Voilà ce que peu savent...""
- Si neutre: ""Il y a quelque chose que tu devrais savoir...""
- Si hostile: ""Pourquoi je te raconte ça? Parce que...""

Réponds JUSTE avec la présentation (2-3 phrases), rien d'autre.";
}

private string BuildFallbackFragmentNarration(string fragmentId, FragmentRevealContext context)
{
    var fragment = personaData?.story_fragment;
    if (fragment == null)
        return "";

    string intro = context.playerFavorabilityWithNpc > 10
        ? $"Tu sembles comprendre. Ce que tu dois savoir sur {npcName}..."
        : $"Il y a quelque chose de {npcName} que tu dois entendre...";

    return $"{intro} {fragment.content}";
}

private int GetPlayerConversationCount()
{
    // Compter les messages chat_history
    int npcMessageCount = chatHistory.FindAll(m => m.role == "assistant").Count;
    return Mathf.Max(1, npcMessageCount);  // Min 1
}

private List<string> GetOtherNpcsEncounteredByPlayer()
{
    // Query SessionManager pour voir qui d'autre le joueur a rencontré
    List<string> encountered = new List<string>();
    
    if (SessionManager.Instance != null)
    {
        var allSessions = SessionManager.Instance.GetAllSessions();
        foreach (var session in allSessions)
        {
            if (session.npc_id != npcId && !encountered.Contains(session.npc_id))
                encountered.Add(session.npc_id);
        }
    }
    
    return encountered;
}

// Data class pour passer le contexte
[System.Serializable]
public class FragmentRevealContext
{
    public int playerFavorabilityWithNpc;
    public int playerGlobalReputation;
    public string npcName;
    public string npcId;
    public int playerConversationCount;
    public List<string> otherNpcsEncounteredByPlayer;
}
```

### Où Sauvegarder

**Modifier:** `Assets/Scripts/GameManager.cs`

```csharp
// Ajouter cette méthode à GameManager:
public static void AddFragmentWithContext(string fragmentId, string contextualizedPresentation)
{
    if (!_fragments.Contains(fragmentId))
    {
        _fragments.Add(fragmentId);
    }
    
    // NOUVEAU: Stocker aussi la version contextualisée
    if (!_fragmentContexts.ContainsKey(fragmentId))
    {
        _fragmentContexts[fragmentId] = new FragmentContext();
    }
    
    _fragmentContexts[fragmentId].presentations.Add(contextualizedPresentation);
    SaveGlobalData();
}

// Structure à ajouter dans GameManager:
[System.Serializable]
public class FragmentContext
{
    public List<string> presentations = new List<string>();  // Peut avoir plusieurs présentations
}

// Déclarer au top:
private static Dictionary<string, FragmentContext> _fragmentContexts = 
    new Dictionary<string, FragmentContext>();
```

---

## Phase 2: Halka Composition Contextualization (PRIORITÉ 2)

### Où
`Assets/Scripts/Npc/Halka/HalkaCompositionEngine.cs` — Méthode `BuildNarrationPrompt()`

### Quoi

Actuellement:
```csharp
private string BuildNarrationPrompt(CompositionResult composition)
{
    // ... build fragments text ...
    
    return $@"Les fragments suivants ont été rassemblés pour une performance Halka:

{fragmentsText}

Audience estimée : {composition.audienceSize} personnes.
Cohérence globale : {composition.coherenceScore} points.

Compose une narration vivante...";
}
```

Doit ajouter le contexte social:
```csharp
private string BuildNarrationPrompt(CompositionResult composition)
{
    List<FragmentEntry> selected = composition.selectedFragmentIds
        .Select(id => ResolveFragment(id))
        .Where(f => f != null)
        .ToList();

    string fragmentsText = string.Join("\n\n", selected.Select(f =>
        $"[{f.npcName}] — {f.title}\n{f.content}"));

    // NOUVEAU: Injecter le contexte social du joueur
    string playerSocialContext = BuildPlayerSocialContext(selected);
    
    bool exposesSensitive = composition.exposesSensitiveStory;
    string sensitivityNote = exposesSensitive
        ? "\n⚠️ Tu dois être conscient que l'audience remarquera la révélation d'une histoire intime. " +
          "Ajoute de la tension narrative autour de ce moment — des réactions murmurées, de la gêne, " +
          "ou une réflexion sur la confiance brisée."
        : "";

    return $@"Les fragments suivants ont été rassemblés pour une performance Halka:

{fragmentsText}

CONTEXTE SOCIAL DU JOUEUR:
{playerSocialContext}

Audience estimée : {composition.audienceSize} personnes.
Cohérence globale : {composition.coherenceScore} points.

Compose une narration vivante, ORALE et PUBLIQUE de ces fragments, comme si le joueur 
les racontait maintenant devant le cercle. 

La narration doit :
1. Être fluide et naturelle (comme parlée, non écrite)
2. Créer une continuité entre les fragments
3. Capturer l'atmosphère des histoires
4. Rester authentique aux voix et cultures représentées
5. RECONNAÎTRE la position du joueur dans ce cercle social
6. Adapter le ton selon les relations du joueur avec les NPCs mentionnés
{sensitivityNote}

Commence directement par la narration, sans introduction. 
Utilise des transitions orales (« Et puis... », « C'est là que... », « Voilà ce que... »).";
}

private string BuildPlayerSocialContext(List<FragmentEntry> fragmentsInComposition)
{
    var sb = new System.Text.StringBuilder();
    
    sb.AppendLine($"📊 Réputation globale: {GameManager.GlobalReputation}/100");
    sb.AppendLine();
    
    sb.AppendLine("🤝 Relations avec les NPCs de cette performance:");
    
    foreach (var fragment in fragmentsInComposition)
    {
        // Trouver le NPC et sa favorabilité
        int npcFav = GetNpcFavorability(fragment.npcId);
        string favorabilityLabel = npcFav switch
        {
            > 40 => "Confiance forte",
            > 20 => "Amical",
            >= 0 => "Neutre",
            > -15 => "Méfiant",
            _ => "Hostile"
        };
        
        sb.AppendLine($"  • {fragment.npcName}: {favorabilityLabel} ({npcFav}/100)");
    }
    
    sb.AppendLine();
    sb.AppendLine("🎭 Autres NPCs rencontrés: " + 
        string.Join(", ", GetEncounteredNpcs()) + 
        " (Ces histoires COMPLÈTENT ton parcours avec eux)");
    
    return sb.ToString();
}

private int GetNpcFavorability(string npcId)
{
    // Chercher dans les sessions sauvegardées
    if (SessionManager.Instance != null)
    {
        var session = SessionManager.Instance.GetLatestSessionFor(npcId);
        if (session != null)
            return session.favorability;
    }
    
    return 0;  // Défaut: neutre
}

private List<string> GetEncounteredNpcs()
{
    // List tous les NPCs avec lesquels le joueur a parlé
    if (SessionManager.Instance != null)
    {
        var allSessions = SessionManager.Instance.GetAllSessions();
        var npcIds = new HashSet<string>();
        
        foreach (var session in allSessions)
        {
            npcIds.Add(session.npc_id);
        }
        
        return new List<string>(npcIds);
    }
    
    return new List<string>();
}
```

---

## Phase 3: Session Storage Enrichment (PRIORITÉ 3)

### Où
`Assets/Scripts/Session/SessionManager.cs` + `NPCSessionData.cs`

### Quoi

**Étendre NPCSessionData:**

```csharp
[System.Serializable]
public class NPCSessionData
{
    // Existing:
    public string npc_id;
    public int favorability;
    public string current_emotion;
    public string interaction_summary;
    public bool fragment_revealed;
    
    // NOUVEAU: Contexte de révélation
    public string fragment_contextual_presentation;      // ← Texte généré par LLM
    public int favorability_at_fragment_reveal;          // ← État au moment de la révélation
    [System.Serializable]
    public class RelationshipSnapshot
    {
        public List<string> npcs_known_by_player;
        public int global_reputation_at_reveal;
    }
    public RelationshipSnapshot relationship_snapshot;   // ← État du monde social
}
```

**Modifier SaveSession():**

```csharp
private void SaveSession(string summary)
{
    if (SessionManager.Instance == null)
    {
        Debug.LogWarning("[LLM] SessionManager not found in scene.");
        return;
    }

    NPCSessionData data = new NPCSessionData
    {
        npc_id = npcId,
        favorability = favorability,
        current_emotion = currentEmotion,
        interaction_summary = summary,
        fragment_revealed = conditions["fragment_revealed"],
        
        // NOUVEAU:
        fragment_contextual_presentation = GetLatestFragmentPresentation(),
        favorability_at_fragment_reveal = GetFavorabilityAtFragmentReveal(),
        relationship_snapshot = new NPCSessionData.RelationshipSnapshot
        {
            npcs_known_by_player = GetOtherNpcsEncounteredByPlayer(),
            global_reputation_at_reveal = GameManager.GlobalReputation
        }
    };

    SessionManager.Instance.SaveSession(data);
}

private string GetLatestFragmentPresentation()
{
    // Récupérer la présentation contextualisée du fragment
    if (GameManager._fragmentContexts.TryGetValue(personaData?.story_fragment?.id, out var ctx))
    {
        if (ctx.presentations.Count > 0)
            return ctx.presentations[ctx.presentations.Count - 1];
    }
    
    return "";
}

private int GetFavorabilityAtFragmentReveal()
{
    // Retourner la favorabilité au moment de la révélation
    // (Déjà dans 'favorability' local, mais on le documente ici)
    return favorability;
}
```

---

## Checklist d'Implémentation

### Phase 1: Fragment Reveal
- [ ] Ajouter `FragmentRevealContext` class
- [ ] Implémenter `GenerateAndSaveContextualizedFragment()`
- [ ] Implémenter `BuildFragmentRevealPrompt()`
- [ ] Implémenter `BuildFallbackFragmentNarration()`
- [ ] Modifier `TryEmitFragmentRevealOnce()` pour appeler le coroutine
- [ ] Ajouter `AddFragmentWithContext()` à GameManager
- [ ] Tester: 1 NPC, vérifier LLM call, vérifier stockage

### Phase 2: Halka Context
- [ ] Implémenter `BuildPlayerSocialContext()`
- [ ] Implémenter `GetNpcFavorability()`, `GetEncounteredNpcs()`
- [ ] Modifier `BuildNarrationPrompt()` pour injecter contexte
- [ ] Tester: Halka avec 2 favorabilités différentes, vérifier narrations divergent

### Phase 3: Session Storage
- [ ] Étendre `NPCSessionData` avec nouveaux champs
- [ ] Ajouter `RelationshipSnapshot` class
- [ ] Modifier `SaveSession()` pour remplir les nouveaux champs
- [ ] Tester: Charger session, vérifier contexte sauvegardé

---

## Testing Strategy

### Test 1: Fragment Reveal Variation
```
Setup:
  - Joueur A: fav(Moussa) = 40
  - Joueur B: fav(Moussa) = -10

Collecte fragments avec chacun
Comparer les presentations générées

Expected:
  - A: Ton complice, accès, détails intimes
  - B: Ton méfiant, retrait, mystère
  
Assert: presentations[A] != presentations[B]
```

### Test 2: Halka Narration Variation
```
Setup:
  - Joueur A: fav(Moussa)=40, fav(Hamid)=20
  - Joueur B: fav(Moussa)=-10, fav(Hamid)=40

Compose Halka avec [Moussa, Hamid] pour les deux
Comparer narrations générées

Expected:
  - A: "Moussa, que tu sembles connaître..."
  - B: "Hamid, ton allié, d'un côté. De l'autre, Moussa..."
  
Assert: narration[A] != narration[B]
```

### Test 3: Session Persistence
```
Setup:
  - Joueur joue, collecte fragment contextualisé
  - Save session
  - Load session
  
Expected:
  - fragment_contextual_presentation non vide
  - relationship_snapshot rempli
  
Assert: Load == original context
```

---

## Impact & Risk

### Impact
✅ Immersion: +40% (joueurs sentent la personnalisation)
✅ Replay Value: +50% (même joueur joue différemment)
✅ Story Authenticity: +60% (chaque histoire est unique)

### Risk
⚠️ API Calls: +1 per fragment reveal (mais batch-able)
⚠️ Storage: +~500 chars par session (négligeable)
⚠️ Complexity: Modéré (3 fichiers, ~200 lines new code)

### Mitigation
✅ Fallback narrations si LLM échoue
✅ Backward compat avec old sessions (champs optionnels)
✅ LLM calls async, pas de blocking

---

## Timeline

| Phase | Effort | Temps | Start | End |
|-------|--------|-------|-------|-----|
| 1 | 🟡 Moyen | 1.5h | Jour 1 | Jour 1 |
| 2 | 🟢 Faible | 1h | Jour 1 | Jour 1 |
| 3 | 🟢 Faible | 0.5h | Jour 2 | Jour 2 |
| Testing | 🟡 Moyen | 1h | Jour 2 | Jour 2 |
| **Total** | | **3.5h** | | |

---

## Success Criteria

✅ Fragment narrations varient selon favorabilité
✅ Halka narrations reflètent relations joueur
✅ Sessions stockent contexte social
✅ Fallback fonctionne si LLM échoue
✅ Tests passent (no regressions)
✅ Story Collector peut reconstruire "joueur parcours"

---

**Prêt à implémenter?** 🚀
