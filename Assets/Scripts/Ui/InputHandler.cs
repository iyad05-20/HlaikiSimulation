using UnityEngine;
using TMPro;

public class InputHandler : MonoBehaviour
{
    [Header("References")]
    public DialoguePanel dialoguePanel;
    public TMP_InputField playerInputField;
    [SerializeField] private GameInputManager gameInputManager;
    [SerializeField] private CameraManager cameraManager;

    private bool isConversationActive = false;
    private NpcLogic activeNpc;
    private LLMNpcLogic _currentNpc;

    // ─── Activation ────────────────────────────────────────
    private void Start()
    {
        gameInputManager.OnSubmitUI += GameInputManager_OnSubmitUI;
        gameInputManager.OnEndUI += GameInputManager_OnEndUI;

        if (playerInputField != null)
        {
            playerInputField.onSubmit.AddListener(OnInputFieldSubmit);
        }
    }

    private void OnInputFieldSubmit(string text)
    {
        if (isConversationActive)
        {
            SubmitMessage();
        }
    }
    private void GameInputManager_OnSubmitUI(object sender, System.EventArgs e)
    {
        if (isConversationActive)
        {
            SubmitMessage();
        }
    }
    private void GameInputManager_OnEndUI(object sender, System.EventArgs e)
    {
        if (isConversationActive)
        {
            EndConversation();
        }
    }
    public void StartConversation(NpcLogic npc)
    {
        isConversationActive = true;
        activeNpc = npc;
        _currentNpc = npc as LLMNpcLogic;
        
        gameInputManager.DisablePlayerMap();
        
        Transform playerTransform = GameObject.FindGameObjectWithTag("Player").transform;
        cameraManager.SwitchToDialogue(playerTransform, npc.transform);
        
        dialoguePanel.ShowPanel(npc.npcName, npc.npcRole, null, npc.myBulleNPC, npc.myTxtBulleNPC);

        if (_currentNpc != null)
        {
            _currentNpc.SetDialoguePanel(dialoguePanel);
        }
    }

    public void EndConversation()
    {
        if (activeNpc != null)
        {
            activeNpc.ResetPlayerTransform();
        }

        // Trigger session summary + save before clearing the reference
        _currentNpc?.EndConversationLogic();

        isConversationActive = false;
        activeNpc    = null;
        _currentNpc  = null;
        
        gameInputManager.EnablePlayerMap();
        cameraManager.SwitchToExploration();
        dialoguePanel.HidePanel();

        // Trigger Halka composition if end-of-cycle and fragments collected
        if (HalkaOrchestrator.Instance != null)
        {
            HalkaOrchestrator.Instance.TriggerEndOfCycleHalka();
        }
    }

    // ─── Input ─────────────────────────────────────────────

    

    private void SubmitMessage()
    {
        string message = playerInputField.text.Trim();
        if (string.IsNullOrEmpty(message)) return;

        Debug.Log($"[InputHandler] SubmitMessage called with: {message}");

        try
        {
            // Display player message in bulle
            dialoguePanel.DisplayPlayerMessage(message);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[InputHandler] Error displaying player message (Missing reference?): {e.Message}");
        }

        // Clear input
        playerInputField.text = "";
        playerInputField.ActivateInputField();

        // Send to LLM if the active NPC supports it
        if (_currentNpc != null)
        {
            _currentNpc.ReceivePlayerMessage(message);
        }
        else
        {
            Debug.LogError($"[InputHandler] _currentNpc is NULL! The active NPC does not have LLMNpcLogic attached.");
        }
    }
}