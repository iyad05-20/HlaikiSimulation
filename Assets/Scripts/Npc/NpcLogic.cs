using System;
using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using JemaaGame.UI;

public class NpcLogic : MonoBehaviour
{
    [SerializeField] public string npcName ;
    [SerializeField] public string npcRole;
    
    private GameInputManager gameInputManager;
    private InputHandler inputHandler;
    
    [Header("UI")]
    [SerializeField] public GameObject myBulleNPC;
    [SerializeField] public TMPro.TextMeshProUGUI myTxtBulleNPC;
    private AnchorFollowPlayer anchorFollowPlayer;

    protected bool isPlayerInRange = false;
    protected bool isInteracting = false;
    protected Transform playerTransform;
    private Quaternion rotationBeforeInteraction;
    private Coroutine rotationCoroutine;
    private float smoothRotationSpeed = 8f;

    // Awake runs before Start for ALL objects, and is NOT hidden by derived classes.
    // We find all references here.
    private void Awake()
    {
        gameInputManager = FindAnyObjectByType<GameInputManager>();
        inputHandler = FindAnyObjectByType<InputHandler>();

        GameObject anchorObj = GameObject.FindWithTag("Anchor");
        if (anchorObj != null)
        {
            anchorFollowPlayer = anchorObj.GetComponent<AnchorFollowPlayer>();
            Debug.Log("Anchor found! " + anchorFollowPlayer.name);
        }
        else
        {
            Debug.LogError("No object with tag 'Anchor' found!");
        }
    }

    // Start: subscribe to events.
    private void Start()
    {
        if (gameInputManager != null)
        {
            gameInputManager.OnInteraction += GameInputManager_OnInteraction;
        }
    }
    private void OnDestroy()
    {
        if (gameInputManager != null)
        {
            gameInputManager.OnInteraction -= GameInputManager_OnInteraction;
        }
    }

    private void GameInputManager_OnInteraction(object sender, EventArgs e)
    {
        if (isPlayerInRange && !isInteracting)
        {
            // Hide the interaction popup when interaction starts
            if (InteractionPopup.Instance != null) InteractionPopup.Instance.Hide();
            StartCoroutine(HandleInteractionSequence());
        }
    }

    private IEnumerator HandleInteractionSequence()
    {
        isInteracting = true;

        if (playerTransform != null)
        {
            rotationBeforeInteraction = playerTransform.rotation;
            Vector3 directionToNPC = transform.position - playerTransform.position;
            directionToNPC.y = 0f; // Garder la rotation uniquement sur l'axe Y (horizontale)
            if (directionToNPC != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(directionToNPC);
                if (rotationCoroutine != null) StopCoroutine(rotationCoroutine);
                
                // Wait until the rotation is fully completed
                yield return rotationCoroutine = StartCoroutine(SmoothRotate(targetRotation));
            }
        }
        if (anchorFollowPlayer != null)
        {
            anchorFollowPlayer.SetAnchorRotation(playerTransform); // Align the anchor with the NPC's rotation
            anchorFollowPlayer.SetAnchorTransform(playerTransform); // Ensure the anchor follows the player during dialogue
            Debug.Log("Anchor rotation set to: " + transform.rotation);
        }
        Debug.Log("Player rotation set to: " + playerTransform.rotation);
        yield return null; // Just to ensure we wait a frame after rotation before starting interaction
        // Only switch cameras and start UI AFTER rotation is done
        Interact();
    }

    public void ResetPlayerTransform()
    {
        isInteracting = false;
        if (playerTransform != null)
        {
            if (rotationCoroutine != null) StopCoroutine(rotationCoroutine);
            rotationCoroutine = StartCoroutine(SmoothRotate(rotationBeforeInteraction));
        }
    }

    private IEnumerator SmoothRotate(Quaternion targetRotation)
    {
        while (playerTransform != null && Quaternion.Angle(playerTransform.rotation, targetRotation) > 0.1f)
        {
            playerTransform.rotation = Quaternion.Slerp(playerTransform.rotation, targetRotation, Time.deltaTime * smoothRotationSpeed);
            yield return null;
        }
        
        if (playerTransform != null)
        {
            playerTransform.rotation = targetRotation;
        }
    }
    

    
    
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInRange = true;
            playerTransform = other.transform;
            Debug.Log($"[NPC] Player in range of {npcName}");
            if (InteractionPopup.Instance != null)
            {
                Debug.Log("[NPCLogic] Showing InteractionPopup");
                InteractionPopup.Instance.Show(npcName, npcRole);
            }
        }
    }
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInRange = false;
            if (playerTransform == other.transform)
            {
                playerTransform = null;
            }
            Debug.Log($"[NPC] Player left range of {npcName}");
            if (InteractionPopup.Instance != null)
            {
                InteractionPopup.Instance.Hide();
            }

        }
    }
    
    protected virtual void Interact()
    {
        Debug.Log($"[NPC] Interacting with {npcName} (role; {npcRole})");
        
        if (JemaaGame.UI.NotificationManager.Instance != null)
        {
            JemaaGame.UI.NotificationManager.Instance.Show(
                "INTERACTION", 
                "Interaction initiée avec un habitant de la place.", 
                JemaaGame.UI.NotificationType.System
            );
        }

        if (inputHandler != null)
        {
            inputHandler.StartConversation(this);
        }
        else
        {
            Debug.LogError("InputHandler is not initialized!");
        }
    }
}
