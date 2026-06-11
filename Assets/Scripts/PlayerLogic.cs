using Unity.VisualScripting;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerLogic : MonoBehaviour
{
    [SerializeField] private GameInputManager gameInputManager;
    [SerializeField] private float speed = 4f;      // Walking speed
    [SerializeField] private float runSpeed = 10f;  // Running speed
    [SerializeField] private float rotationSpeed = 15f; 
    [SerializeField] private float jumpHeight = 1.5f; // New Jump Height
    [SerializeField] private float gravityScale = 1.5f; // For a better feeling fall
    [SerializeField] private Animator animator;
    [SerializeField] private float floorOffset = 0f;

    private CharacterController characterController;
    private float velocityY;

    private void Awake()
    {
        // ... (existing CharacterController setup)
        characterController = GetComponent<CharacterController>();
        if (characterController == null)
        {
            characterController = gameObject.AddComponent<CharacterController>();
        }
        
        characterController.radius = 0.3f;
        characterController.height = 1.8f;
        characterController.center = new Vector3(0, 0.9f + floorOffset, 0);
        characterController.skinWidth = 0.08f; 
        characterController.stepOffset = 0.3f;
    }

    private void Update()
    {
        HandleMovement();
    }
    
    private void HandleMovement()
    {
        Vector2 inputVector = gameInputManager.InputVector();
        
        Transform cameraTransform = Camera.main.transform;
        Vector3 cameraForward = cameraTransform.forward;
        Vector3 cameraRight = cameraTransform.right;

        cameraForward.y = 0f;
        cameraRight.y = 0f;
        cameraForward.Normalize();
        cameraRight.Normalize();

        Vector3 moveDir = (cameraForward * inputVector.y + cameraRight * inputVector.x).normalized;
        bool isMoving = inputVector.sqrMagnitude > 0.01f;

        // By default, moving triggers running. Holding Shift (IsSprinting) triggers walking.
        bool isWalking = isMoving && gameInputManager.IsSprinting(); 
        bool isRunning = isMoving && !isWalking;

        // 1. Gravity and Grounding
        if (characterController.isGrounded && velocityY < 0)
        {
            velocityY = -2f; 
        }

        // 2. Jump Logic
        if (characterController.isGrounded && gameInputManager.WasJumpPressed())
        {
            // Physics formula for jumping to a specific height: v = sqrt(h * -2 * g)
            velocityY = Mathf.Sqrt(jumpHeight * -2f * Physics.gravity.y);
            
            if (animator != null)
            {
                animator.SetTrigger("Jump");
            }
        }

        // Apply gravity (multiplied by gravityScale for a snappier feel)
        velocityY += Physics.gravity.y * gravityScale * Time.deltaTime;

        if (animator != null)
        {
            animator.SetBool("IsWalking", isWalking);
            animator.SetBool("IsRunning", isRunning);
            animator.SetBool("IsGrounded", characterController.isGrounded);
            
            if (isRunning) animator.speed = 1.2f; 
            else if (isWalking) animator.speed = 1.0f;
            else animator.speed = 1.0f;
        }

        // 3. Movement
        float currentSpeed = isRunning ? runSpeed : (isWalking ? speed : 0f);
        Vector3 velocity = (isMoving ? moveDir : Vector3.zero) * currentSpeed;
        velocity.y = velocityY;

        characterController.Move(velocity * Time.deltaTime);

        // 4. Rotation
        if (isMoving)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDir);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
        }
    }
}
