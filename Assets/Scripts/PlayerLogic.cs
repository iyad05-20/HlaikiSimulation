using Unity.VisualScripting;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerLogic : MonoBehaviour
{
    [SerializeField] private GameInputManager gameInputManager;
    [SerializeField] private float speed = 5f;
    [SerializeField] private float rotationSpeed = 10f;
    [SerializeField] private Animator animator;
    [SerializeField] private float floorOffset = 0f; // Fine-tune this if feet are slightly above/below

    private CharacterController characterController;
    private float velocityY;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        if (characterController == null)
        {
            characterController = gameObject.AddComponent<CharacterController>();
        }
        
        // Force human-appropriate proportions to prevent floating on uneven terrain
        characterController.radius = 0.3f;
        characterController.height = 2f;
        // Shift center UP by floorOffset so the visual mesh moves DOWN relative to the collider
        characterController.center = new Vector3(0, 1f + floorOffset, 0);
        characterController.skinWidth = 0.01f;
    }

    private void Update()
    {
        HandleMovement();
    }
    
    private void HandleMovement()
    {
        Vector2 inputVector = gameInputManager.InputVector();
        
        // Calculate horizontal movement
        Vector3 moveDir = new Vector3(inputVector.x, 0, inputVector.y).normalized;
        bool isMoving = moveDir != Vector3.zero;

        if (animator != null)
        {
            animator.SetBool("IsWalking", isMoving);
        }

        // 1. Apply gravity
        if (characterController.isGrounded && velocityY < 0)
        {
            velocityY = -2f; // Small constant downward force to stay grounded on slopes
        }

        velocityY += Physics.gravity.y * Time.deltaTime;

        // Combine horizontal movement and vertical gravity
        Vector3 velocity = moveDir * speed;
        velocity.y = velocityY;

        // 2. Move (CharacterController handles wall collisions and grounding)
        characterController.Move(velocity * Time.deltaTime);

        // 3. Rotate player to move direction
        if (isMoving)
        {
            transform.forward = Vector3.Slerp(transform.forward, moveDir, Time.deltaTime * rotationSpeed);
        }
    }
}
