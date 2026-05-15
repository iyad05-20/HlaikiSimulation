using Unity.VisualScripting;
using UnityEngine;

public class PlayerLogic : MonoBehaviour
{
    [SerializeField] private GameInputManager gameInputManager;
    [SerializeField] private float speed=5f;
    [SerializeField] private float rotationSpeed=10f;

    
    private void Update()
    {
        HandleMovement();
    }
    private void HandleMovement()
    {
        Vector2 inputVector = gameInputManager.InputVector();
        
        Vector3 moveDir= new Vector3(inputVector.x, 0, inputVector.y).normalized;

        //if no input then exit
        if (moveDir == Vector3.zero)
        {
            return;
        }
    
        //rotate player to move direction
        transform.forward = Vector3.Slerp(transform.forward, moveDir, Time.deltaTime * rotationSpeed);
        
        int layerMask = ~LayerMask.GetMask("TriggerZone");
        float moveDistance = speed * Time.deltaTime;
        float playerRadius = .7f;
        float playerHeight = 2f;
        
        //check if player can move in that direction
        bool canMove = !Physics.CapsuleCast(transform.position,transform.position + Vector3.up*playerHeight,playerRadius,moveDir,moveDistance,layerMask,QueryTriggerInteraction.Ignore);

        //bool canMove = !Physics.CapsuleCast(transform.position,transform.position + Vector3.up*playerHeight,playerRadius,movement,moveDistance);
        if (!canMove){
            //if cant move in move direction

            //try to move only in x direction
            Vector3 moveDirX = new Vector3(moveDir.x, 0, 0).normalized;
            canMove = !Physics.CapsuleCast(transform.position,transform.position + Vector3.up*playerHeight,playerRadius,moveDirX,moveDistance,layerMask,QueryTriggerInteraction.Ignore);
            if (canMove)
            //can move in x direction
            {
                moveDir = moveDirX;
            }
            else //cant move in x dir
            {
                //try move in z direction
                Vector3 moveDirZ = new Vector3(0, 0, moveDir.z).normalized;
                canMove = !Physics.CapsuleCast(transform.position,transform.position + Vector3.up*playerHeight,playerRadius,moveDirZ,moveDistance,layerMask,QueryTriggerInteraction.Ignore);
                // try to move only in z direction
                if (canMove)
                //can move in z direction only
                {
                     moveDir = moveDirZ;
                }
            }
        }
         if (canMove)
        {
            //if can move in move direction, move player
            transform.position += moveDir *moveDistance ; 
        }
    }
    
    
}

