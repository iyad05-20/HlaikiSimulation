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
    public string emotion_pressure;
    public bool curiosity_triggered;
    public string emotion_shift;
    public string reason;
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
        LoadPersonaFromJson();
        LoadPreviousSession();
    }

    private void LoadPersonaFromJson()
    {
        string path = Path.Combine(Application.streamingAssetsPath, "personas", $"{npcId}_game.json");
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
        Variant chosen   = variants[Random.Range(0, variants.Count)];
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

        List<GroqMessage> messagesToSend = new List<GroqMessage>
        {
            new GroqMessage { role = "system", content = BuildSystemPrompt() }
        };
        messagesToSend.AddRange(chatHistory);

        StartCoroutine(GroqApiClient.Instance.SendChatRequest(messagesToSend, OnLlmSuccess, OnLlmError));
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

        // 2 — Reputation GLOBALE via local word scan
        int repDelta = ComputeReputationDelta(_lastPlayerMessage);
        GameManager.AddReputation(repDelta);

        Debug.Log($"[LLM] NPC: {npcId} | Fav: {favorability} | GlobalRep: {GameManager.GlobalReputation} | Reason: {scoring.reason}");

        // 3 — Curiosity flag
        if (scoring.curiosity_triggered)
            conditions["curiosity_shown"] = true;

        // 4 — Fragment unlock condition
        if (conditions["curiosity_shown"] && favorability >= 60)
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
        if (conditions["fragment_revealed"] && !_fragmentAnnounced)
        {
            _fragmentAnnounced = true;
            string fragmentId  = personaData?.story_fragment?.id ?? "unknown_fragment";
            OnFragmentCollected?.Invoke(fragmentId);
            Debug.Log($"[LLM] Fragment unlocked: {fragmentId}");
        }
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
