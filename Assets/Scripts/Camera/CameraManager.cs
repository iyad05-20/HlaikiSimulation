using UnityEngine;
using Cinemachine;
using UnityEngine.InputSystem;

public class CameraManager : MonoBehaviour
{
    [Header("Cameras")]
    [SerializeField] private CinemachineVirtualCamera explorationCamera;
    [SerializeField] private CinemachineVirtualCamera dialogueCamera;
    [SerializeField] private Transform playerTarget;

    [Header("Settings")]
    [SerializeField] private GameInputManager gameInputManager;
    public float sensitivity = 0.5f;          // Adjusted for new logic
    public float verticalSensitivity = 0.5f;
    public float minPitch = -15f;
    public float maxPitch = 50f;
    public float distance = 6f;
    public float heightOffset = 1.6f;
    
    [Header("Professional Smoothing")]
    [Tooltip("The responsiveness of the camera. Higher = more direct. 15-25 is standard.")]
    public float smoothness = 20f; 
    
    private float currentYaw = 0f;
    private float currentPitch = 20f;
    private float targetYaw = 0f;
    private float targetPitch = 20f;
    
    private CinemachineTransposer transposer;
    private Camera mainCam;

    private const int PRIORITY_HIGH = 15;
    private const int PRIORITY_LOW = 5;

    private Vector3 originalCameraPosition;
    private Quaternion originalCameraRotation;
    private bool isCameraSaved = false;

    void Start()
    {
        mainCam = Camera.main;
        
        // 1. Force Cinemachine Brain to LateUpdate for perfect sync
        if (mainCam != null)
        {
            var brain = mainCam.GetComponent<CinemachineBrain>();
            if (brain != null)
            {
                brain.m_UpdateMethod = CinemachineBrain.UpdateMethod.LateUpdate;
                brain.m_BlendUpdateMethod = CinemachineBrain.BrainUpdateMethod.LateUpdate;
            }
        }

        // Auto-assign references
        if (gameInputManager == null) gameInputManager = FindObjectOfType<GameInputManager>();
        if (playerTarget == null)
        {
            PlayerLogic player = FindObjectOfType<PlayerLogic>();
            if (player != null) playerTarget = player.transform;
        }

        if (playerTarget != null)
        {
            explorationCamera.LookAt = playerTarget;
            explorationCamera.Follow = playerTarget;
        }

        if (explorationCamera != null)
        {
            var composer = explorationCamera.GetCinemachineComponent<CinemachineComposer>();
            if (composer == null) composer = explorationCamera.AddCinemachineComponent<CinemachineComposer>();
            
            // Minimal damping for the most responsive feel
            composer.m_HorizontalDamping = 0.05f;
            composer.m_VerticalDamping = 0.05f;

            transposer = explorationCamera.GetCinemachineComponent<CinemachineTransposer>();
            if (transposer != null)
            {
                transposer.m_BindingMode = CinemachineTransposer.BindingMode.WorldSpace;
                
                // Zero internal damping because we handle it in our script
                transposer.m_XDamping = 0;
                transposer.m_YDamping = 0;
                transposer.m_ZDamping = 0;
                
                Vector3 offset = transposer.m_FollowOffset;
                targetYaw = currentYaw = Mathf.Atan2(offset.x, offset.z) * Mathf.Rad2Deg;
                targetPitch = currentPitch = 20f;
            }
        }

        explorationCamera.Priority = PRIORITY_HIGH;
        dialogueCamera.Priority = PRIORITY_LOW;
        LockCursor();
    }

    void Update()
    {
        // Toggle cursor with Escape
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (Cursor.lockState == CursorLockMode.Locked) UnlockCursor();
            else LockCursor();
        }

        // Capture Input in Update for zero-latency response
        if (transposer != null && explorationCamera.Priority == PRIORITY_HIGH && Cursor.lockState == CursorLockMode.Locked)
        {
            Vector2 lookVector = gameInputManager.LookVector();
            
            // We use a sensitivity multiplier to allow the inspector values to be standard (0-1)
            float multiplier = 0.1f;
            targetYaw += lookVector.x * sensitivity * multiplier;
            targetPitch = Mathf.Clamp(targetPitch - lookVector.y * verticalSensitivity * multiplier, minPitch, maxPitch);
        }
    }

    void LateUpdate()
    {
        if (transposer != null && explorationCamera.Priority == PRIORITY_HIGH && Cursor.lockState == CursorLockMode.Locked)
        {
            // 2. Exponential Decay interpolation: mathematically superior to SmoothDamp
            // It feels more "connected" and never overshoots.
            float lerpFactor = 1f - Mathf.Exp(-smoothness * Time.unscaledDeltaTime);
            
            currentYaw = Mathf.LerpAngle(currentYaw, targetYaw, lerpFactor);
            currentPitch = Mathf.Lerp(currentPitch, targetPitch, lerpFactor);

            // 3. Update the camera offset in LateUpdate for perfect tracking
            Quaternion rotation = Quaternion.Euler(currentPitch, currentYaw, 0f);
            Vector3 direction = rotation * Vector3.back; 
            
            transposer.m_FollowOffset = direction * distance + Vector3.up * heightOffset;
        }
    }

    private void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void UnlockCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void SwitchToDialogue(Transform playerTransform, Transform npcTransform)
    {
        explorationCamera.Priority = PRIORITY_LOW;
        dialogueCamera.Priority = PRIORITY_HIGH;
        UnlockCursor();
    }

    public void SwitchToExploration()
    {
        explorationCamera.Priority = PRIORITY_HIGH;
        dialogueCamera.Priority = PRIORITY_LOW;
        LockCursor();

        // Réinitialiser la caméra à son état d'origine
        if (isCameraSaved)
        {
            dialogueCamera.transform.position = originalCameraPosition;
            dialogueCamera.transform.rotation = originalCameraRotation;
            isCameraSaved = false;
        }
    }
}
