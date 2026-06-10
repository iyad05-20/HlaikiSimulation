using Unity.VisualScripting;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerLogic : MonoBehaviour
{
    [SerializeField] private GameInputManager gameInputManager;
    [SerializeField] private float speed = 4f;      // Walking speed
    [SerializeField] private float runSpeed = 10f;  // Running speed (Increased for better feel)
    [SerializeField] private float rotationSpeed = 15f; // Increased for professional responsiveness
    [SerializeField] private Animator animator;
    [SerializeField] private float floorOffset = 0f;

    private CharacterController characterController;
    private float velocityY;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        if (characterController == null)
        {
            characterController = gameObject.AddComponent<CharacterController>();
        }
        
        // CharacterController adjustments for stability and to prevent "crashing" (jitter)
        characterController.radius = 0.3f;
        characterController.height = 1.8f;
        characterController.center = new Vector3(0, 0.9f + floorOffset, 0);
        characterController.skinWidth = 0.08f; // Increased skinWidth helps with stability against walls
        characterController.stepOffset = 0.3f;
    }

    private void Update()
    {
        HandleMovement();
    }
    
    private void HandleMovement()
    {
        Vector2 inputVector = gameInputManager.InputVector();
        
        // IMPORTANT: Calculate horizontal movement relative to the ACTIVE camera
        // We use Camera.main but ensure we handle its rotation correctly
        Transform cameraTransform = Camera.main.transform;
        Vector3 cameraForward = cameraTransform.forward;
        Vector3 cameraRight = cameraTransform.right;

        cameraForward.y = 0f;
        cameraRight.y = 0f;
        cameraForward.Normalize();
        cameraRight.Normalize();

        // Direction player wants to move based on WASD + Camera orientation
        Vector3 moveDir = (cameraForward * inputVector.y + cameraRight * inputVector.x).normalized;
        bool isMoving = inputVector.sqrMagnitude > 0.01f;

        // Inverted Logic: Run by default, Shift to Walk
        bool isWalking = isMoving && gameInputManager.IsSprinting(); 
        bool isRunning = isMoving && !isWalking;

        if (animator != null)
        {
            animator.SetBool("IsWalking", isWalking);
            animator.SetBool("IsRunning", isRunning);
            
            if (isRunning) animator.speed = 1.2f; 
            else if (isWalking) animator.speed = 1.0f;
            else animator.speed = 1.0f;
        }

        // 1. Apply gravity
        if (characterController.isGrounded && velocityY < 0)
        {
            velocityY = -2f; 
        }

        velocityY += Physics.gravity.y * Time.deltaTime;

        // 2. Combine horizontal movement and vertical gravity
        float currentSpeed = isRunning ? runSpeed : (isWalking ? speed : 0f);
        Vector3 velocity = (isMoving ? moveDir : Vector3.zero) * currentSpeed;
        velocity.y = velocityY;

        // 3. Move 
        characterController.Move(velocity * Time.deltaTime);

        // 4. Rotate player to face the direction of movement relative to camera
        if (isMoving)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDir);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
        }
    }
}
