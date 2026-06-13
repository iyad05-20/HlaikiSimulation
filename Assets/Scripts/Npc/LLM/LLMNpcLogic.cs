using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using System.Text.RegularExpressions;

// ─── Scoring Data ─────────────────────────────────────────────────────────────
[System.Serializable]
public class NpcScoringData
{
    public int favorability_delta;
    public int reputation_delta;
    public string emotion_pressure;
    public bool curiosity_triggered;
    public string emotion_shift;
    public string reason;
}

// ─── Fragment Reveal Context ───────────────────────────────────────────────────
[System.Serializable]
public class FragmentRevealContext
{
    public int playerFavorabilityWithNpc;
    public int playerGlobalReputation;
    public string npcName;
    public string npcId;
    public int playerConversationCount;
    public List<string> otherNpcsEncounteredByPlayer = new List<string>();
}

// ─── LLMNpcLogic ─────────────────────────────────────────────────────────────
public class LLMNpcLogic : NpcLogic
{
    [Header("NPC Identity")]
    public string npcId = "hamid";

    [Header("NPC State (runtime — read only)")]
    [SerializeField] private string currentEmotion = "open";
    [SerializeField] private string currentMood    = "";
    [SerializeField] private int    favorability   = 0;
    [SerializeField] private int    reputation     = 0;
    // La réputation est maintenant globale et gérée par GameManager.GlobalReputation


    // ─── Events ───────────────────────────────────────────────────────────────
    public event System.Action<string> OnFragmentCollected;

    // ─── Internal State ───────────────────────────────────────────────────────
    private NpcPersonaData personaData;
    private string         interactionSummary = "";
    private Dictionary<string, bool> conditions = new Dictionary<string, bool>
    {
        { "curiosity_shown",   false },
        { "fragment_revealed", false }
    };
    private bool _fragmentAnnounced = false;

    private GroqApiClient     apiClient;
    private List<GroqMessage> chatHistory = new List<GroqMessage>();
    private DialoguePanel     dialoguePanel;
    private string            _lastPlayerMessage = "";

    // ─── Init ─────────────────────────────────────────────────────────────────
    // NOTE: We intentionally do NOT define Start() or Awake() here.
    // NpcLogic (base) uses private Awake() to find references (ButtonE, InputHandler, etc.)
    // and private Start() to subscribe events & deactivate ButtonE.
    // Defining Start()/Awake() here would SHADOW those and break the base class init.
    // Instead we use OnEnable() which runs after Awake() and is safe to use alongside.
    private void OnEnable()
    {
        apiClient = GroqApiClient.Instance;
        if (apiClient == null)
            apiClient = GetComponent<GroqApiClient>();

        LoadPersonaFromJson();
        LoadPreviousSession();
    }

    private void LoadPersonaFromJson()
    {
        string personasDir = Path.Combine(Application.streamingAssetsPath, "personas");
        string path = Path.Combine(personasDir, $"{npcId}.json");

        if (!File.Exists(path))
            path = Path.Combine(personasDir, $"{npcId}_game.json");

        if (!File.Exists(path))
        {
            Debug.LogError($"[LLMNpcLogic] Persona file not found: {path}");
            return;
        }

        string raw = File.ReadAllText(path);
        // "base" is a C# keyword — rename to base_text before parsing
        raw = raw.Replace("\"base\":", "\"base_text\":");
        personaData = JsonUtility.FromJson<NpcPersonaData>(raw);
        Debug.Log($"[LLMNpcLogic] Persona loaded for {personaData.name}");
    }

    private void LoadPreviousSession()
    {
        if (SessionManager.Instance == null) return;

        NPCSessionData session = SessionManager.Instance.LoadSession(npcId);
        if (session == null) return;

        favorability                    = session.favorability;
        // On n'utilise plus session.reputation car elle est globale (chargée par le GameManager)
        reputation                      = GameManager.GlobalReputation;
        currentEmotion                  = session.current_emotion;
        interactionSummary              = session.interaction_summary;
        _fragmentAnnounced              = session.fragment_revealed;
        conditions["fragment_revealed"] = session.fragment_revealed;
        Debug.Log($"[LLMNpcLogic] Resumed session for {npcId}: fav={favorability}, globalRep={GameManager.GlobalReputation}");
    }

    // ─── SetDialoguePanel (called by InputHandler) ────────────────────────────
    public void SetDialoguePanel(DialoguePanel panel)
    {
        dialoguePanel = panel;
    }

    // ─── Interact (override) ──────────────────────────────────────────────────
    protected override void Interact()
    {
        base.Interact();
        PickCycleVariant();

        if (NpcStatsUI.Instance != null)
        {
            NpcStatsUI.Instance.UpdateMetrics(favorability, GameManager.GlobalReputation);
        }

        if (chatHistory.Count == 0)
        {
            ReceivePlayerMessage("*Le joueur s'approche et te regarde en silence*");
        }
        else
        {
            if (dialoguePanel != null)
                dialoguePanel.DisplayNPCDialogue(chatHistory[chatHistory.Count - 1].content);
        }
    }

    private void PickCycleVariant()
    {
        if (personaData?.cycle_variant?.variants == null || personaData.cycle_variant.variants.Count == 0)
            return;

        var     variants = personaData.cycle_variant.variants;
        int selectedIndex = GameManager.GetCycleVariant(npcId);
        if (selectedIndex < 0 || selectedIndex >= variants.Count)
        {
            selectedIndex = Random.Range(0, variants.Count);
            GameManager.SetCycleVariant(npcId, selectedIndex);
        }

        Variant chosen   = variants[selectedIndex];
        personaData.cycle_variant.selected = selectedIndex;
        currentMood      = chosen.mood;
        currentEmotion   = chosen.emotion_start;
        Debug.Log($"[LLMNpcLogic] Variant #{chosen.id} chosen — mood: {currentMood}, emotion: {currentEmotion}");
    }

    // ─── Receive Player Message ───────────────────────────────────────────────
    public void ReceivePlayerMessage(string message)
    {
        _lastPlayerMessage = message;
        Debug.Log($"[LLMNpcLogic] ReceivePlayerMessage: {message}");

        chatHistory.Add(new GroqMessage { role = "user", content = message });

        if (dialoguePanel != null)
            dialoguePanel.DisplayNPCDialogue("...");

        StartCoroutine(DualCallLlmSequence(message));
    }

    private IEnumerator DualCallLlmSequence(string playerMessage)
    {
        if (apiClient == null)
        {
            apiClient = GroqApiClient.Instance;
        }

        if (apiClient == null)
        {
            Debug.LogError("[LLMNpcLogic] GroqApiClient not found. Cannot run LLM calls.");
            if (dialoguePanel != null)
                dialoguePanel.DisplayNPCDialogue("Erreur: service LLM indisponible.");
            yield break;
        }

        NpcScoringData scoring = null;
        bool scoringDone = false;

        string scoringPrompt = BuildScoringPrompt(playerMessage);
        List<GroqMessage> scoringMessages = new List<GroqMessage>
        {
            new GroqMessage { role = "system", content = "Tu es un moteur de scoring JSON. Réponds UNIQUEMENT avec du JSON valide, sans markdown ni explication." },
            new GroqMessage { role = "user", content = scoringPrompt }
        };

        StartCoroutine(apiClient.SendChatRequest(scoringMessages,
            scoringResult =>
            {
                scoring = ParseScoringJson(scoringResult);
                scoringDone = true;
            },
            error =>
            {
                Debug.LogWarning($"[LLM] Scoring call failed: {error}");
                scoring = new NpcScoringData { favorability_delta = 0, reputation_delta = 0, emotion_pressure = "neutral" };
                scoringDone = true;
            }
        ));

        yield return new WaitUntil(() => scoringDone);

        string dialoguePrompt = BuildDialoguePrompt();
        List<GroqMessage> dialogueMessages = new List<GroqMessage>
        {
            new GroqMessage { role = "system", content = dialoguePrompt }
        };
        dialogueMessages.AddRange(chatHistory);

        string dialogue = null;
        bool dialogueDone = false;

        StartCoroutine(apiClient.SendChatRequest(dialogueMessages,
            dialogueResult =>
            {
                dialogue = dialogueResult.Trim();
                dialogueDone = true;
            },
            error =>
            {
                Debug.LogError($"[LLM] Dialogue call failed: {error}");
                dialogue = "Je dois réfléchir, donne-moi un instant... (Erreur de connexion)";
                dialogueDone = true;
            }
        ));

        yield return new WaitUntil(() => dialogueDone);

        if (scoring != null)
            ApplyScoring(scoring);

        if (!string.IsNullOrEmpty(dialogue))
        {
            chatHistory.Add(new GroqMessage { role = "assistant", content = dialogue });
            if (dialoguePanel != null)
                dialoguePanel.DisplayNPCDialogue(dialogue);
        }
    }

    private string BuildScoringPrompt(string playerMessage)
    {
        if (personaData == null || string.IsNullOrEmpty(personaData.scoring_prompt_template))
            return $"Évalue ce message: \"{playerMessage}\" État: {currentEmotion}, Fav: {favorability}";

        string prompt = personaData.scoring_prompt_template
            .Replace("{player_input}", playerMessage)
            .Replace("{emotion}", currentEmotion)
            .Replace("{favorability}", favorability.ToString())
            .Replace("{reputation}", reputation.ToString())
            .Replace("{curiosity_shown}", conditions["curiosity_shown"].ToString().ToLower())
            .Replace("{mood}", currentMood);

        // NOUVEAU : Le template JSON des personas oubliait de demander "curiosity_triggered"
        // On l'injecte de force dans le schéma JSON attendu pour réparer la récolte de fragments
        if (!prompt.Contains("curiosity_triggered"))
        {
            prompt = prompt.Replace(
                "\"reason\": \"<3 mots max>\"", 
                "\"reason\": \"<3 mots max>\",\n  \"curiosity_triggered\": <true si le joueur pose une question sincère sur le passé, le métier ou le secret du NPC, false sinon>"
            );
        }

        return prompt;
    }

    private string BuildDialoguePrompt()
    {
        if (personaData == null || string.IsNullOrEmpty(personaData.dialogue_prompt_template))
            return BuildSystemPrompt();

        string fragmentInst = (conditions["fragment_revealed"] && !_fragmentAnnounced && personaData.story_fragment != null)
            ? personaData.story_fragment.reveal_instruction
            : "";

        string rulesInjection = "";
        if (personaData.persona?.rules != null && personaData.persona.rules.Count > 0)
            rulesInjection = "RÈGLES:\n" + string.Join("\n", personaData.persona.rules);

        return personaData.dialogue_prompt_template
            .Replace("{base_persona}", personaData.persona.base_text)
            .Replace("{emotion}", currentEmotion)
            .Replace("{emotion_overlay}", GetEmotionOverlay())
            .Replace("{mood}", currentMood)
            .Replace("{favorability}", favorability.ToString())
            .Replace("{fragment_instruction}", fragmentInst)
            .Replace("{rules}", rulesInjection);
    }

    private NpcScoringData ParseScoringJson(string jsonText)
    {
        try
        {
            return JsonUtility.FromJson<NpcScoringData>(jsonText);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[LLM] Failed to parse scoring JSON: {e.Message}\nRaw: {jsonText}");
            return new NpcScoringData { favorability_delta = 0, reputation_delta = 0, emotion_pressure = "neutral" };
        }
    }

    // ─── BuildSystemPrompt ────────────────────────────────────────────────────
    private string BuildSystemPrompt()
    {
        if (personaData == null)
        {
            return $"ÉTAT: {currentEmotion.ToUpper()} | FAV: {favorability}/100 | RÉPUTATION: {GameManager.GlobalReputation}/100\n\n" +
                   "FORMAT OBLIGATOIRE:\nRESPONSE: [réponse]\n" +
                   "SCORING: {\"favorability_delta\": 0, \"emotion_pressure\": \"neutral\", " +
                   "\"curiosity_triggered\": false, \"emotion_shift\": \"null\", \"reason\": \"no data\"}";
        }

        var    p      = personaData.persona;
        string prompt = p.base_text + "\n\n";

        // Previous session context (only if non-empty)
        if (!string.IsNullOrEmpty(interactionSummary))
            prompt += $"CONTEXTE RENCONTRE PRÉCÉDENTE:\n{interactionSummary}\n\n";

        // Current state line
        prompt += $"ÉTAT: {currentEmotion.ToUpper()} | HUMEUR: {currentMood} | FAV: {favorability}/100 | RÉPUTATION: {GameManager.GlobalReputation}/100\n";

        // Emotion overlay behaviour
        string overlay = GetEmotionOverlay();
        if (!string.IsNullOrEmpty(overlay))
            prompt += $"COMPORTEMENT: {overlay}\n";

        // Story fragment instruction (once unlocked, before announcing)
        if (conditions["fragment_revealed"] && !_fragmentAnnounced && personaData.story_fragment != null)
            prompt += $"\n{personaData.story_fragment.reveal_instruction}\n";

        prompt += "\n";

        // Rules from JSON
        if (p.rules != null && p.rules.Count > 0)
            prompt += string.Join("\n", p.rules) + "\n";

        prompt += "\n---\n";
        prompt += "FORMAT OBLIGATOIRE — toujours sans exception:\n";
        prompt += "RESPONSE: [ta réponse ici, une seule ligne]\n";
        prompt += "SCORING: {\"favorability_delta\": <entier>, \"emotion_pressure\": \"<positive|negative|neutral>\", " +
                  "\"curiosity_triggered\": <true|false>, \"emotion_shift\": \"<open|occupied|closed|null>\", \"reason\": \"<3 mots>\"}";

        return prompt;
    }

    private string GetEmotionOverlay()
    {
        if (personaData?.persona?.emotion_overlays == null) return "";
        return currentEmotion switch
        {
            "open"     => personaData.persona.emotion_overlays.open,
            "occupied" => personaData.persona.emotion_overlays.occupied,
            "closed"   => personaData.persona.emotion_overlays.closed,
            _          => ""
        };
    }

    // ─── LLM Success ─────────────────────────────────────────────────────────
    private void OnLlmSuccess(string responseText)
    {
        string dialogue    = responseText;
        string scoringJson = "";

        var scoringMatch = Regex.Match(responseText, @"SCORING:\s*(\{.*?\})", RegexOptions.Singleline);
        if (scoringMatch.Success)
        {
            scoringJson = scoringMatch.Groups[1].Value;
            dialogue    = responseText.Substring(0, scoringMatch.Index).Trim();
        }

        // Robust cleanup: find RESPONSE anywhere and take what follows
        var responseMatch = Regex.Match(dialogue, @"(?i)RESPONSE[:\.]?\s*(.*)", RegexOptions.Singleline);
        if (responseMatch.Success)
        {
            dialogue = responseMatch.Groups[1].Value.Trim();
        }
        else
        {
            // Fallback: if RESPONSE keyword is missing, just ensure SCORING is gone
            dialogue = Regex.Replace(dialogue, @"(?i)^.*RESPONSE[:\.]?\s*", "").Trim();
        }

        if (string.IsNullOrEmpty(dialogue)) dialogue = responseText;

        if (!string.IsNullOrEmpty(scoringJson))
        {
            try
            {
                NpcScoringData scoring = JsonUtility.FromJson<NpcScoringData>(scoringJson);
                if (scoring != null) ApplyScoring(scoring);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[LLM] Failed to parse scoring JSON: {e.Message}\nRaw: {scoringJson}");
            }
        }

        chatHistory.Add(new GroqMessage { role = "assistant", content = dialogue });

        if (dialoguePanel != null)
            dialoguePanel.DisplayNPCDialogue(dialogue);
    }

    // ─── Apply Scoring (Problem 4) ────────────────────────────────────────────
    private void ApplyScoring(NpcScoringData scoring)
    {
        // 1 — Favorability
        favorability += scoring.favorability_delta;
        favorability  = Mathf.Clamp(favorability, -30, 100);

        // 2 — Reputation: LLM output first, fallback to local scan if absent
        int repDelta = scoring.reputation_delta;
        if (repDelta == 0)
            repDelta = ComputeReputationDelta(_lastPlayerMessage);
        GameManager.AddReputation(repDelta);
        reputation = GameManager.GlobalReputation;

        Debug.Log($"[LLM] NPC: {npcId} | Fav: {favorability} | GlobalRep: {GameManager.GlobalReputation} | Reason: {scoring.reason}");

        if (NpcStatsUI.Instance != null)
        {
            NpcStatsUI.Instance.UpdateMetrics(favorability, GameManager.GlobalReputation);
        }

        // 3 — Curiosity flag
        if (scoring.curiosity_triggered)
            conditions["curiosity_shown"] = true;

        // 4 — Fragment unlock condition
        if (conditions["curiosity_shown"] && favorability >=7)
            conditions["fragment_revealed"] = true;

        // 5 — Emotion transition
        if (!string.IsNullOrEmpty(scoring.emotion_shift) && scoring.emotion_shift != "null")
        {
            currentEmotion = scoring.emotion_shift;
        }
        else
        {
            // Local fallback rules
            if (favorability < -10)
                currentEmotion = "closed";
            else if (currentEmotion == "closed" && favorability >= -5)
                currentEmotion = "occupied";
            else if (currentEmotion == "occupied" && favorability >= 10)
                currentEmotion = "open";
        }

        // 6 — Fire fragment event once
        TryEmitFragmentRevealOnce();
    }

    private int ComputeReputationDelta(string input)
    {
        if (personaData?.reputation_words == null || string.IsNullOrEmpty(input))
            return 0;

        string lower = input.ToLower();

        // Negative checked first
        if (personaData.reputation_words.negative != null)
            foreach (string word in personaData.reputation_words.negative)
                if (lower.Contains(word.ToLower()))
                    return Random.Range(-8, -3);

        // Positive
        if (personaData.reputation_words.positive != null)
            foreach (string word in personaData.reputation_words.positive)
                if (lower.Contains(word.ToLower()))
                    return Random.Range(1, 5);

        // Cultural
        if (personaData.reputation_words.cultural != null)
            foreach (string word in personaData.reputation_words.cultural)
                if (lower.Contains(word.ToLower()))
                    return 1;

        return 0;
    }

    // ─── External Event Hooks (EventTracker/NPCManager) ───────────────────────
    public void ApplyExternalEvent(string eventId, int favorabilityDelta, int reputationDelta, string forceEmotion)
    {
        favorability += favorabilityDelta;
        favorability = Mathf.Clamp(favorability, -30, 100);

        if (reputationDelta != 0)
            GameManager.AddReputation(reputationDelta);
        reputation = GameManager.GlobalReputation;

        if (!string.IsNullOrEmpty(forceEmotion))
            currentEmotion = forceEmotion;

        Debug.Log($"[LLMNpcLogic] External event {eventId} applied on {npcId}: fav {favorabilityDelta:+#;-#;0}, rep {reputationDelta:+#;-#;0}");

        if (NpcStatsUI.Instance != null)
        {
            NpcStatsUI.Instance.UpdateMetrics(favorability, GameManager.GlobalReputation);
        }
    }

    public void SetCondition(string conditionKey, bool value)
    {
        if (string.IsNullOrEmpty(conditionKey))
            return;

        conditions[conditionKey] = value;
        if (conditionKey == "fragment_revealed")
            TryEmitFragmentRevealOnce();
    }

    public bool GetCondition(string conditionKey)
    {
        if (string.IsNullOrEmpty(conditionKey))
            return false;

        bool value;
        return conditions.TryGetValue(conditionKey, out value) && value;
    }

    private void TryEmitFragmentRevealOnce()
    {
        if (!GetCondition("fragment_revealed") || _fragmentAnnounced)
            return;

        _fragmentAnnounced = true;
        string fragmentId = personaData?.story_fragment?.id ?? "unknown_fragment";
        
        // NOUVEAU: Générer la présentation contextualisée via LLM
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
        
        // NOUVEAU : Sauvegarder dans player_stories.json pour le Menu Codex
        if (SessionManager.Instance != null && personaData?.story_fragment != null)
        {
            SessionManager.Instance.CaptureStory(
                fragmentId, 
                personaData.story_fragment.title, 
                contextualizedNarration
            );
        }

        OnFragmentCollected?.Invoke(fragmentId);

        // Notifier EventTracker pour centraliser les réactions gameplay (Contextual Anchoring aware)
        EventTracker.Instance?.NotifyFragmentCollected(npcId, fragmentId);

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
- Nombre de conversations avec toi: {context.playerConversationCount}
- {otherNpcsMention}

HISTOIRE À RACONTER:
Titre: {fragment.title}
Contenu: {fragment.content}

INSTRUCTION:
Tu dois présenter cette histoire en 2-3 phrases. Adapte:
1. La façon dont tu l'introduis selon la relation
2. Le niveau de détail que tu révèles
3. Le ton émotionnel (confiance, méfiance, neutralité)

Exemples par relation:
- Si favorable (fav > 30): ""Tu sembles comprendre. Voilà ce que peu savent...""
- Si neutre (fav 0-10): ""Il y a quelque chose que tu devrais savoir...""
- Si hostile (fav < 0): ""Pourquoi je te raconte ça? Parce que...""

Réponds JUSTE avec la présentation (2-3 phrases), rien d'autre.";
    }

    private string BuildFallbackFragmentNarration(string fragmentId, FragmentRevealContext context)
    {
        var fragment = personaData?.story_fragment;
        if (fragment == null)
            return "";

        string intro = context.playerFavorabilityWithNpc > 10
            ? $"Tu sembles comprendre. Ce que tu dois savoir sur moi..."
            : $"Il y a quelque chose que tu dois entendre...";

        return $"{intro} {fragment.content}";
    }

    private int GetPlayerConversationCount()
    {
        // Compter les messages assistant (répliques du NPC)
        int npcMessageCount = chatHistory.FindAll(m => m.role == "assistant").Count;
        return Mathf.Max(1, npcMessageCount);
    }

    private List<string> GetOtherNpcsEncounteredByPlayer()
    {
        // Query SessionManager pour voir qui d'autre le joueur a rencontré
        List<string> encountered = new List<string>();
        
        if (SessionManager.Instance != null)
        {
            // Get all NPC IDs encountered
            var allSessions = SessionManager.Instance.GetAllSessions();
            foreach (var session in allSessions)
            {
                if (session.npc_id != npcId && !encountered.Contains(session.npc_id))
                    encountered.Add(session.npc_id);
            }
        }
        
        return encountered;
    }

    // ─── End Conversation (Problem 5) ─────────────────────────────────────────
    public void EndConversationLogic()
    {
        if (chatHistory.Count == 0) return;
        StartCoroutine(GenerateAndSaveSessionSummary());
    }

    private IEnumerator GenerateAndSaveSessionSummary()
    {
        // Build history string
        string history = "";
        foreach (var msg in chatHistory)
        {
            if      (msg.role == "user")      history += $"Joueur: {msg.content}\n";
            else if (msg.role == "assistant")  history += $"{npcName}: {msg.content}\n";
        }

        string summaryPrompt =
            $"Résume cette conversation en 2-3 phrases. Attitude du joueur, points importants, état de la relation.\n" +
            $"Fav finale: {favorability}\n\n{history}";

        List<GroqMessage> msgs = new List<GroqMessage>
        {
            new GroqMessage { role = "system", content = "Tu es un assistant qui résume des conversations de jeu vidéo en 2-3 phrases factuelles." },
            new GroqMessage { role = "user",   content = summaryPrompt }
        };

        string summary  = null;
        bool   callDone = false;

        StartCoroutine(GroqApiClient.Instance.SendChatRequest(msgs,
            result => { summary = result; callDone = true; },
            error  => { Debug.LogWarning($"[LLM] Summary generation failed: {error}"); callDone = true; }
        ));

        yield return new WaitUntil(() => callDone);

        // Fallback
        if (string.IsNullOrEmpty(summary))
        {
            if      (favorability > 10)  summary = $"Relation positive (fav {favorability}).";
            else if (favorability < -5)  summary = $"Relation tendue (fav {favorability}).";
            else                         summary = "Rencontre neutre.";
        }

        SaveSession(summary);
    }

    private void SaveSession(string summary)
    {
        if (SessionManager.Instance == null)
        {
            Debug.LogWarning("[LLM] SessionManager not found in scene. Session not saved.");
            return;
        }

        NPCSessionData data = new NPCSessionData
        {
            npc_id              = npcId,
            favorability        = favorability,
            current_emotion     = currentEmotion,
            interaction_summary = summary,
            fragment_revealed   = conditions["fragment_revealed"]
        };

        SessionManager.Instance.SaveSession(data);
    }

    // ─── LLM Error ────────────────────────────────────────────────────────────
    private void OnLlmError(string error)
    {
        Debug.LogError($"[LLM Error] {error}");
        if (chatHistory.Count > 0 && chatHistory[chatHistory.Count - 1].role == "user")
            chatHistory.RemoveAt(chatHistory.Count - 1);

        if (dialoguePanel != null)
            dialoguePanel.DisplayNPCDialogue("Je dois réfléchir, donne-moi un instant... (Erreur de connexion)");
    }
}
