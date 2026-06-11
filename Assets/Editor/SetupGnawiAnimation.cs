using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public class SetupGnawiAnimation : EditorWindow
{
    [MenuItem("Tools/Setup Gnawi Animation")]
    public static void SetupGnawi()
    {
        // 1. Find the Gnawi GameObject in the scene using the Gnawi script component
        Gnawi gnawiScript = GameObject.FindObjectOfType<Gnawi>();

        if (gnawiScript == null)
        {
            Debug.LogError("[SetupGnawi] Could not find any GameObject with the 'Gnawi' script in the scene.");
            return;
        }

        GameObject gnawiInstance = gnawiScript.gameObject;

        // 2. Ensure Animator exists
        Animator animator = gnawiInstance.GetComponent<Animator>();
        if (animator == null)
        {
            animator = gnawiInstance.AddComponent<Animator>();
            Debug.Log("[SetupGnawi] Added missing Animator component to Gnawi.");
        }

        // 3. Ensure the Avatar is assigned to the Animator
        string fbxPath = "Assets/ScenesAssets/Gnawi.fbx";
        Avatar avatar = AssetDatabase.LoadAssetAtPath<Avatar>(fbxPath);
        if (avatar != null)
        {
            animator.avatar = avatar;
            Debug.Log("[SetupGnawi] Assigned Humanoid Avatar to Animator.");
        }
        else
        {
            Debug.LogWarning($"[SetupGnawi] Could not find Avatar in {fbxPath}. Unity might still be importing it as Humanoid.");
        }

        // 4. Create or Load the Animator Controller
        string controllerPath = "Assets/Animations/GnawiController.controller";
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);

        if (controller == null)
        {
            controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            Debug.Log($"[SetupGnawi] Created new Animator Controller at {controllerPath}");
        }

        animator.runtimeAnimatorController = controller;

        // 5. Setup Animation Data
        AnimationClip idleClip = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Animations/Breathingidle.anim");
        
        if (idleClip == null)
        {
            Debug.LogError("[SetupGnawi] Could not find Breathingidle.anim at Assets/Animations/Breathingidle.anim");
            return;
        }

        // Ensure we have a base layer
        if (controller.layers.Length == 0)
        {
            controller.AddLayer("Base Layer");
        }

        AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;

        // Look for existing Idle state
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

        // Add Idle state if missing
        if (!hasIdle)
        {
            AnimatorState idleState = stateMachine.AddState("Idle");
            idleState.motion = idleClip;
            stateMachine.defaultState = idleState;
            Debug.Log("[SetupGnawi] Added 'Breathingidle' as the default Idle animation.");
        }

        EditorUtility.SetDirty(controller);
        EditorUtility.SetDirty(gnawiInstance);
        AssetDatabase.SaveAssets();

        // Select the Gnawi object to show the user
        Selection.activeGameObject = gnawiInstance;
        SceneView.FrameLastActiveSceneView();

        Debug.Log("<color=green><b>Success!</b></color> Gnawi animation has been correctly configured with Breathingidle.");
    }
}