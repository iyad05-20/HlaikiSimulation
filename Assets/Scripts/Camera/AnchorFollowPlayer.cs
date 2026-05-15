using UnityEngine;

public class AnchorFollowPlayer : MonoBehaviour
{
    [SerializeField] private Transform playerTransform;
    [SerializeField] private Vector3 offset = new Vector3(0f, 2f, -3f);

    void LateUpdate()
    {
        if (playerTransform != null)
        {
            //transform.position = playerTransform.position + offset;
            //Debug.Log("Player rotation set to: " + playerTransform.rotation);
            //transform.forward = playerTransform.forward;
            // On ne force plus la rotation ici, pour que SetAnchorRotation fonctionne !
        }
    }
    public void SetAnchorTransform(Transform newPlayerTransform)
    {
        // TransformPoint applique l'offset en fonction de la rotation du joueur (donc toujours derrière lui)
        transform.position = newPlayerTransform.TransformPoint(offset);
    }
    public void SetAnchorRotation(Transform newPlayerTransform)
    {
        transform.rotation = newPlayerTransform.rotation;
    }
}