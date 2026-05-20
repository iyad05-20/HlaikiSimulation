using UnityEngine;
using UnityEditor;

public class ApplyVehicleMoverEditor : EditorWindow
{
    [MenuItem("Tools/Environment/Apply Mover To Bus")]
    public static void ApplyMover()
    {
        // Find all objects in the active scene
        GameObject[] allObjects = GameObject.FindObjectsOfType<GameObject>();
        int count = 0;

        foreach (GameObject go in allObjects)
        {
            if (go.name.Contains("BusMarrakech", System.StringComparison.OrdinalIgnoreCase) || 
                go.name.Contains("Bus", System.StringComparison.OrdinalIgnoreCase) && !go.name.Contains("stop", System.StringComparison.OrdinalIgnoreCase) && !go.name.Contains("shelter", System.StringComparison.OrdinalIgnoreCase))
            {
                // Remove the static flags so the bus can move
                GameObjectUtility.SetStaticEditorFlags(go, 0);

                // Add the mover script if it doesn't already have one
                VehicleMover mover = go.GetComponent<VehicleMover>();
                if (mover == null)
                {
                    mover = go.AddComponent<VehicleMover>();
                    
                    // Set default speed for a bus
                    mover.speed = 12f; 
                    
                    // Usually 3D models from external software might be facing X or Z. 
                    // By default we leave it as Vector3.forward (0,0,1). The user can adjust if it drives sideways.
                    mover.localDirection = Vector3.forward; 
                }
                
                count++;
            }
        }

        Debug.Log($"[VehicleMover] Successfully applied VehicleMover script and removed Static flags from {count} bus objects.");
    }
}