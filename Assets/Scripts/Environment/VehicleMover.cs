using UnityEngine;

public class VehicleMover : MonoBehaviour
{
    [Header("Movement")]
    public float speed = 10f;
    
    [Tooltip("The local axis direction the vehicle should move towards. Usually (0,0,1) for Z-forward or (1,0,0) for X-forward depending on the model's pivot.")]
    public Vector3 localDirection = Vector3.forward;

    [Header("Looping")]
    public bool loopPosition = true;
    public float maxDistance = 300f;

    [Header("Activation")]
    public bool waitForPlayerTouch = true;
    private bool isMoving = false;

    private Vector3 startPosition;
    private float distanceTraveled = 0f;

    private void Start()
    {
        startPosition = transform.position;
        
        if (!waitForPlayerTouch)
        {
            isMoving = true;
        }
        else
        {
            // Ensure the bus has a collider to detect the player
            Collider col = GetComponent<Collider>();
            if (col == null)
            {
                BoxCollider box = gameObject.AddComponent<BoxCollider>();
                box.isTrigger = true; // Make it a trigger so the player can touch it to activate
            }
            else
            {
                col.isTrigger = true;
            }
        }
    }

    private void Update()
    {
        if (!isMoving) return;

        // Move the vehicle in its local direction
        Vector3 movement = transform.TransformDirection(localDirection.normalized) * speed * Time.deltaTime;
        transform.position += movement;
        
        if (loopPosition)
        {
            distanceTraveled += movement.magnitude;
            if (distanceTraveled >= maxDistance)
            {
                // Teleport back to start
                transform.position = startPosition;
                distanceTraveled = 0f;
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (waitForPlayerTouch && !isMoving)
        {
            if (other.CompareTag("Player"))
            {
                isMoving = true;
            }
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (waitForPlayerTouch && !isMoving)
        {
            if (collision.gameObject.CompareTag("Player"))
            {
                isMoving = true;
            }
        }
    }
}