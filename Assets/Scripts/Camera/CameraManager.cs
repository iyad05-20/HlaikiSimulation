using UnityEngine;
using Cinemachine;


public class CameraManager : MonoBehaviour
{
    [Header("Cameras")]
    [SerializeField] private CinemachineVirtualCamera explorationCamera;
    [SerializeField] private CinemachineVirtualCamera dialogueCamera;

    private const int PRIORITY_HIGH = 15;
    private const int PRIORITY_LOW = 5;

    private Vector3 originalCameraPosition;
    private Quaternion originalCameraRotation;
    private bool isCameraSaved = false;

    void Start()
    {
        explorationCamera.Priority = PRIORITY_HIGH;
        dialogueCamera.Priority = PRIORITY_LOW;
    }

    public void SwitchToDialogue(Transform playerTransform, Transform npcTransform)
    {
        explorationCamera.Priority = PRIORITY_LOW;
        dialogueCamera.Priority = PRIORITY_HIGH;
        
        /*if (playerTransform != null && npcTransform != null)
        {
            // Sauvegarder la position et la rotation initiales avant de modifier
            if (!isCameraSaved)
            {
                originalCameraPosition = dialogueCamera.transform.position;
                originalCameraRotation = dialogueCamera.transform.rotation;
                isCameraSaved = true;
            }

            // Désactiver le suivi automatique de Cinemachine
            dialogueCamera.Follow = null;
            dialogueCamera.LookAt = null;

            // Placer la caméra un peu derrière le joueur (et légèrement sur le côté droit pour voir par-dessus l'épaule)
            Vector3 backOffset = -playerTransform.forward * 2.4f;
            Vector3 upOffset = Vector3.up * 4.5f;
            Vector3 rightOffset = playerTransform.right * 2.8f;

            dialogueCamera.transform.position = playerTransform.position + backOffset + upOffset + rightOffset;
            
            // Tourner la caméra pour qu'elle regarde le NPC (en ciblant sa tête)
            dialogueCamera.transform.LookAt(npcTransform.position + Vector3.up * 1.5f);
        }*/
    }

    public void SwitchToExploration()
    {
        explorationCamera.Priority = PRIORITY_HIGH;
        dialogueCamera.Priority = PRIORITY_LOW;

        // Réinitialiser la caméra à son état d'origine
        if (isCameraSaved)
        {
            dialogueCamera.transform.position = originalCameraPosition;
            dialogueCamera.transform.rotation = originalCameraRotation;
            isCameraSaved = false;
        }
    }
}
