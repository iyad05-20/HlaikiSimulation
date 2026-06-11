using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using System.Linq;

public class RestoreSellerModel : EditorWindow
{
    [MenuItem("Tools/Restore Seller Model")]
    public static void RestoreSeller()
    {
        // 1. Find the seller.fbx model
        string fbxPath = "Assets/ScenesAssets/seller.fbx";
        GameObject sellerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);

        if (sellerPrefab == null)
        {
            Debug.LogError($"[RestoreSeller] Could not find seller.fbx at {fbxPath}. Please ensure the file exists.");
            return;
        }

        // 2. Instantiate in the scene
        GameObject sellerInstance = (GameObject)PrefabUtility.InstantiatePrefab(sellerPrefab);
        sellerInstance.name = "Seller";
        sellerInstance.transform.position = new Vector3(2.78f, 0f, 4.17f); // Default to a reasonable position

        // 3. Attach scripts
        if (sellerInstance.GetComponent<seller>() == null)
        {
            sellerInstance.AddComponent<seller>();
        }

        // 4. Configure the Animator and Controller
        Animator animator = sellerInstance.GetComponent<Animator>();
        if (animator == null)
        {
            animator = sellerInstance.AddComponent<Animator>();
        }

        string controllerPath = "Assets/Animations/SellerController.controller";
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);

        if (controller == null)
        {
            controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            Debug.Log($"[RestoreSeller] Created new SellerController at {controllerPath}");
        }

        animator.runtimeAnimatorController = controller;

        // Ensure the Avatar is assigned to the Animator
        Avatar avatar = AssetDatabase.LoadAssetAtPath<Avatar>(fbxPath);
        if (avatar != null)
        {
            animator.avatar = avatar;
            Debug.Log("[RestoreSeller] Assigned Avatar to Animator.");
        }

        // 5. Setup Animation Data
        AnimationClip idleClip = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Animations/Breathingidle.anim");
        
        if (idleClip == null)
        {
            Debug.LogError("[RestoreSeller] Could not find Breathingidle.anim at Assets/Animations/Breathingidle.anim");
        }

        // Add the animation to the controller if it's empty
        if (controller.layers.Length == 0)
        {
            controller.AddLayer("Base Layer");
        }

        AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;

        bool hasIdle = false;
        foreach(var childState in stateMachine.states)
        {
            if(childState.state.name == "Idle") 
            {
                childState.state.motion = idleClip;
                stateMachine.defaultState = childState.state;
                hasIdle = true;
            }
        }

        if (!hasIdle && idleClip != null)
        {
            AnimatorState idleState = stateMachine.AddState("Idle");
            idleState.motion = idleClip;
            stateMachine.defaultState = idleState;
            Debug.Log($"[RestoreSeller] Added 'Breathingidle' as the default Idle animation.");
        }

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();

        // Focus and select the restored seller
        Selection.activeGameObject = sellerInstance;
        SceneView.FrameLastActiveSceneView();

        Debug.Log("<color=green><b>Success!</b></color> Seller model has been restored to the scene with its data animations configured.");
    }
}
