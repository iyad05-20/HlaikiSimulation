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

// ─── LLMNpcLogic ─────────────────────────────────────────────────────────────
[RequireComponent(typeof(GroqApiClient))]
public class LLMNpcLogic : NpcLogic
{
    [Header("NPC Identity")]
    public string npcId = "hamid";

    [Header("NPC State (runtime — read only)")]
    [SerializeField] private string currentEmotion = "open";
    [SerializeField] private string currentMood    = "";
    [SerializeField] private int    favorability   = 0;
    [SerializeField] private int    reputation     = 0;

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
        apiClient = GetComponent<GroqApiClient>();
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

        // Fix #2: Initialize cycle_variant (select random variant 0-2)
        if (personaData?.cycle_variant?.variants != null && personaData.cycle_variant.variants.Length > 0)
        {
            int variantIndex = Random.Range(0, personaData.cycle_variant.variants.Length);
            personaData.cycle_variant.selected = variantIndex;
            
            var selectedVariant = personaData.cycle_variant.variants[variantIndex];
            currentMood = selectedVariant.mood;
            currentEmotion = selectedVariant.emotion_start;
            
            Debug.Log($"[LLMNpcLogic] Variant {variantIndex} selected: {selectedVariant.mood}");
        }
    }

    private void LoadPreviousSession()
    {
        if (SessionManager.Instance == null) return;

        NPCSessionData session = SessionManager.Instance.LoadSession(npcId);
        if (session == null) return;

        favorability                    = session.favorability;
        reputation                      = session.reputation;
        currentEmotion                  = session.current_emotion;
        interactionSummary              = session.interaction_summary;
        _fragmentAnnounced              = session.fragment_revealed;
        conditions["fragment_revealed"] = session.fragment_revealed;
        Debug.Log($"[LLMNpcLogic] Resumed session: fav={favorability}, emotion={currentEmotion}");
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

        // Dual-call architecture:
        // 1. First call scoring_prompt_template to evaluate player message
        // 2. Then call dialogue_prompt_template to generate response
        StartCoroutine(DualCallLlmSequence(message));
    }

    // ─── Dual-Call LLM Sequence ───────────────────────────────────────────────
    private IEnumerator DualCallLlmSequence(string playerMessage)
    {
        NpcScoringData scoring = null;
        bool scoringDone = false;

        // CALL 1: Scoring evaluation
        string scoringPrompt = BuildScoringPrompt(playerMessage);
        var scoringMessages = new List<GroqMessage>
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
                scoring = new NpcScoringData { favorability_delta = 0, emotion_pressure = "neutral" };
                scoringDone = true;
            }
        ));

        yield return new WaitUntil(() => scoringDone);

        // CALL 2: Dialogue generation (with scoring context)
        string dialoguePrompt = BuildDialoguePrompt();
        var dialogueMessages = new List<GroqMessage>
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

        // Process both results
        if (scoring != null)
            ApplyScoring(scoring);

        if (!string.IsNullOrEmpty(dialogue))
        {
            chatHistory.Add(new GroqMessage { role = "assistant", content = dialogue });
            if (dialoguePanel != null)
                dialoguePanel.DisplayNPCDialogue(dialogue);
        }
    }

    // ─── Build Scoring Prompt (P4) ────────────────────────────────────────────
    private string BuildScoringPrompt(string playerMessage)
    {
        if (personaData == null || string.IsNullOrEmpty(personaData.scoring_prompt_template))
            return $"Évalue ce message: \"{playerMessage}\" État: {currentEmotion}, Fav: {favorability}";
 
        return personaData.scoring_prompt_template
            .Replace("{player_input}", playerMessage)
            .Replace("{emotion}", currentEmotion)
            .Replace("{favorability}", favorability.ToString())
            .Replace("{reputation}", reputation.ToString())
            .Replace("{curiosity_shown}", conditions["curiosity_shown"].ToString().ToLower())
            .Replace("{mood}", currentMood);
    }

    // ─── Build Dialogue Prompt (P3) ────────────────────────────────────────────
    // Fix #7: Inject persona.rules[] dynamically into dialogue_prompt_template
    private string BuildDialoguePrompt()
    {
        if (personaData == null || string.IsNullOrEmpty(personaData.dialogue_prompt_template))
            return BuildSystemPrompt(); // Fallback to old logic if template missing
 
        string overlay = GetEmotionOverlay();
        string fragmentInst = (conditions["fragment_revealed"] && !_fragmentAnnounced && personaData.story_fragment != null)
            ? personaData.story_fragment.reveal_instruction
            : "";
         
        // Build rules injection from persona.rules[]
        string rulesInjection = "";
        if (personaData.persona?.rules != null && personaData.persona.rules.Length > 0)
        {
            rulesInjection = "RÈGLES:\n" + string.Join("\n", personaData.persona.rules);
        }
 
        return personaData.dialogue_prompt_template
            .Replace("{base_persona}", personaData.persona.base_text)
            .Replace("{emotion}", currentEmotion)
            .Replace("{emotion_overlay}", overlay)
            .Replace("{mood}", currentMood)
            .Replace("{favorability}", favorability.ToString())
            .Replace("{fragment_instruction}", fragmentInst)
            .Replace("{rules}", rulesInjection);  // Fix #7: Add rules placeholder support
    }

    // ─── Parse Scoring JSON (P2 helper) ───────────────────────────────────────
    private NpcScoringData ParseScoringJson(string jsonText)
    {
        try
        {
            return JsonUtility.FromJson<NpcScoringData>(jsonText);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[LLM] Failed to parse scoring JSON: {e.Message}\nRaw: {jsonText}");
            return new NpcScoringData { favorability_delta = 0, emotion_pressure = "neutral" };
        }
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

    // ─── Fallback BuildSystemPrompt (for when templates are missing) ───────────
    private string BuildSystemPrompt()
    {
        if (personaData == null)
        {
            return $"ÉTAT: {currentEmotion.ToUpper()} | FAV: {favorability}/100\n\n" +
                   "Réponds comme un personnage réel.";
        }

        var p = personaData.persona;
        string prompt = p.base_text + "\n\n";

        if (!string.IsNullOrEmpty(interactionSummary))
            prompt += $"CONTEXTE RENCONTRE PRÉCÉDENTE:\n{interactionSummary}\n\n";

        prompt += $"ÉTAT: {currentEmotion.ToUpper()} | HUMEUR: {currentMood} | FAV: {favorability}/100\n";

        string overlay = GetEmotionOverlay();
        if (!string.IsNullOrEmpty(overlay))
            prompt += $"COMPORTEMENT: {overlay}\n";

        if (p.rules != null && p.rules.Count > 0)
            prompt += "\n" + string.Join("\n", p.rules);

        return prompt;
    }

    // ─── Apply Scoring (Problem 4) ────────────────────────────────────────────
    private void ApplyScoring(NpcScoringData scoring)
    {
        // 1 — Favorability
        favorability += scoring.favorability_delta;
        favorability  = Mathf.Clamp(favorability, -30, 100);
  
        // 2 — Reputation (LLM-driven from scoring.reputation_delta)
        reputation += scoring.reputation_delta;
        reputation = Mathf.Clamp(reputation, -50, 150);
 
        Debug.Log($"[LLM] Fav: {favorability}, Rep: {reputation}, Emotion: {currentEmotion}, Pressure: {scoring.emotion_pressure}, Reason: {scoring.reason}");
 
        // 3 — Curiosity flag
        if (scoring.curiosity_triggered)
            conditions["curiosity_shown"] = true;
 
        // 4 — Fragment unlock condition
        if (conditions["curiosity_shown"] && favorability >= 60)
            conditions["fragment_revealed"] = true;
 
        // 5 — Emotion transition (Fix #3: Use emotion_pressure as tie-breaker)
        if (!string.IsNullOrEmpty(scoring.emotion_shift) && scoring.emotion_shift != "null")
        {
            currentEmotion = scoring.emotion_shift;
        }
        else
        {
            // Use emotion_pressure to influence transitions
            if (scoring.emotion_pressure == "negative" && favorability < -10)
                currentEmotion = "closed";
            else if (scoring.emotion_pressure == "positive" && currentEmotion == "closed" && favorability >= -5)
                currentEmotion = "occupied";
            else if (scoring.emotion_pressure == "positive" && currentEmotion == "occupied" && favorability >= 10)
                currentEmotion = "open";
            else
            {
                // Default fallback if no emotion_pressure guidance
                if (favorability < -10)
                    currentEmotion = "closed";
                else if (currentEmotion == "closed" && favorability >= -5)
                    currentEmotion = "occupied";
                else if (currentEmotion == "occupied" && favorability >= 10)
                    currentEmotion = "open";
            }
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
        // P2 FIX: Use scoring.triggers instead of non-existent reputation_words
        if (personaData?.scoring?.triggers == null || string.IsNullOrEmpty(input))
            return 0;

        string lower = input.ToLower();
        
        // Scan all triggers and return first match's reputation_delta
        // In a real implementation, you'd accumulate or select best match
        foreach (var trigger in personaData.scoring.triggers)
        {
            // Simple heuristic: check if trigger description keywords appear in input
            if (!string.IsNullOrEmpty(trigger.description) && lower.Contains(trigger.description.ToLower()))
                return trigger.reputation_delta;
        }

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

        StartCoroutine(apiClient.SendChatRequest(msgs,
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
            reputation          = reputation,
            current_emotion     = currentEmotion,
            interaction_summary = summary,
            fragment_revealed   = conditions["fragment_revealed"]
        };

        SessionManager.Instance.SaveSession(data);
    }
}
