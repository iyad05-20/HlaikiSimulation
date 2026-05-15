using UnityEditor;
using UnityEngine;

public class ObjectCreationTools
{
    [MenuItem("Tools/Create Cube at Origin")]
    public static void CreateCubeAtOrigin()
    {
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.transform.position = Vector3.zero;
        cube.name = "MyCube";
        
        // Register the object for Undo so it's not "lost" in the editor
        Undo.RegisterCreatedObjectUndo(cube, "Create Cube at Origin");
        
        Debug.Log("Cube created at (0, 0, 0)");
        
        // Focus the scene view on the new cube
        if (SceneView.lastActiveSceneView != null)
        {
            SceneView.lastActiveSceneView.FrameSelected();
        }
    }
}
