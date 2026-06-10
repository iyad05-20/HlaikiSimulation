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
    public float sensitivity = 0.15f;          // Reduced for finer control
    public float verticalSensitivity = 0.15f;
    public float minPitch = -15f;
    public float maxPitch = 50f;
    public float distance = 6f;
    public float heightOffset = 1.6f;
    
    [Header("Professional Smoothing")]
    [Tooltip("The lower the value, the more responsive. 0.05 is standard for pro games.")]
    public float smoothTime = 0.05f; 
    
    private float currentYaw = 0f;
    private float currentPitch = 20f;
    private float targetYaw = 0f;
    private float targetPitch = 20f;
    
    private float yawVelocity;
    private float pitchVelocity;
    
    private CinemachineTransposer transposer;

    private const int PRIORITY_HIGH = 15;
    private const int PRIORITY_LOW = 5;

    private Vector3 originalCameraPosition;
    private Quaternion originalCameraRotation;
    private bool isCameraSaved = false;

    void Start()
    {
        // 1. Force Cinemachine Brain to LateUpdate to eliminate "crashes" / jitter
        var brain = Camera.main.GetComponent<CinemachineBrain>();
        if (brain != null)
        {
            brain.m_UpdateMethod = CinemachineBrain.UpdateMethod.LateUpdate;
            brain.m_BlendUpdateMethod = CinemachineBrain.BrainUpdateMethod.LateUpdate;
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
            // Ensure Aim component (Composer) exists
            var composer = explorationCamera.GetCinemachineComponent<CinemachineComposer>();
            if (composer == null) composer = explorationCamera.AddCinemachineComponent<CinemachineComposer>();
            
            // Professional damping values
            composer.m_HorizontalDamping = 0.1f;
            composer.m_VerticalDamping = 0.1f;

            transposer = explorationCamera.GetCinemachineComponent<CinemachineTransposer>();
            if (transposer != null)
            {
                transposer.m_BindingMode = CinemachineTransposer.BindingMode.WorldSpace;
                
                // Zero internal damping to prevent conflicts with our script
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
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (Cursor.lockState == CursorLockMode.Locked) UnlockCursor();
            else LockCursor();
        }
    }

    void LateUpdate()
    {
        if (transposer != null && explorationCamera.Priority == PRIORITY_HIGH && Cursor.lockState == CursorLockMode.Locked)
        {
            Vector2 lookVector = gameInputManager.LookVector();
            
            // 1. Accumulate target rotation (Yaw/Pitch)
            targetYaw += lookVector.x * sensitivity;
            targetPitch = Mathf.Clamp(targetPitch - lookVector.y * verticalSensitivity, minPitch, maxPitch);

            // 2. Ultra-Smooth interpolation using SmoothDampAngle with unscaledDeltaTime.
            // unscaledDeltaTime ensures smoothness even during frame-rate spikes or lag.
            currentYaw = Mathf.SmoothDampAngle(currentYaw, targetYaw, ref yawVelocity, smoothTime, Mathf.Infinity, Time.unscaledDeltaTime);
            currentPitch = Mathf.SmoothDampAngle(currentPitch, targetPitch, ref pitchVelocity, smoothTime, Mathf.Infinity, Time.unscaledDeltaTime);

            // 3. Calculate stable orbit position
            Quaternion rotation = Quaternion.Euler(currentPitch, currentYaw, 0f);
            Vector3 direction = rotation * Vector3.back; 
            
            // 4. Apply absolute offset relative to the player
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
