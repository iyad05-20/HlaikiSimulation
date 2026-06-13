using UnityEditor;
using UnityEngine;

public class SetupPlayerAudio : EditorWindow
{
    [MenuItem("Tools/Setup Player Audio")]
    public static void SetupAudio()
    {
        // 1. Find the Player in the scene
        PlayerLogic playerLogic = Object.FindFirstObjectByType<PlayerLogic>();
        
        if (playerLogic == null)
        {
            Debug.LogError("[SetupPlayerAudio] Could not find any GameObject with the 'PlayerLogic' script in the current scene.");
            return;
        }

        GameObject player = playerLogic.gameObject;

        // 2. Add an AudioSource if one doesn't exist
        AudioSource audioSource = player.GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = player.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1f; // 3D Sound
            Debug.Log("[SetupPlayerAudio] Added AudioSource component to the Player.");
        }

        // 3. Load the Audio Clip
        string clipPath = "Assets/Audio/RunningSound.mp3";
        AudioClip runningClip = AssetDatabase.LoadAssetAtPath<AudioClip>(clipPath);

        if (runningClip == null)
        {
            Debug.LogError($"[SetupPlayerAudio] Could not find audio clip at '{clipPath}'. Make sure it's an mp3/wav/ogg file.");
            return;
        }

        // 4. Assign the references in PlayerLogic
        Undo.RecordObject(playerLogic, "Setup Player Audio");
        
        playerLogic.footstepSource = audioSource;
        playerLogic.footstepSound = runningClip;
        
        PrefabUtility.RecordPrefabInstancePropertyModifications(playerLogic);
        EditorUtility.SetDirty(playerLogic);
        
        Debug.Log("<color=green><b>Success!</b></color> Running sound and AudioSource have been successfully attached to the Player.");
    }
}
