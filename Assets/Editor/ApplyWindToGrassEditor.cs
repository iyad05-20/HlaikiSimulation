using UnityEngine;
using UnityEditor;

public class ApplyWindToGrassEditor : EditorWindow
{
    [MenuItem("Tools/Environment/Apply Wind To Grass")]
    public static void ApplyWind()
    {
        // Find all objects in the active scene
        GameObject[] allObjects = GameObject.FindObjectsOfType<GameObject>();
        int count = 0;

        foreach (GameObject go in allObjects)
        {
            if (go.name.Contains("green_field_greener"))
            {
                // Remove the static flags so the object can move
                GameObjectUtility.SetStaticEditorFlags(go, 0);

                // Add the wind animator script if it doesn't already have one
                if (go.GetComponent<GrassWindAnimator>() == null)
                {
                    go.AddComponent<GrassWindAnimator>();
                }
                
                count++;
            }
        }

        Debug.Log($"[GrassWind] Successfully applied Wind script and removed Static flags from {count} grass objects.");
    }
}
