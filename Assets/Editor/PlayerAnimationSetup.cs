using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public class PlayerAnimationSetup : EditorWindow
{
    [MenuItem("Tools/Setup Player Run Animation")]
    public static void SetupRunAnimation()
    {
        // 1. Find the Player's Animator Controller
        Animator playerAnimator = GameObject.FindObjectOfType<PlayerLogic>()?.GetComponent<Animator>();
        
        if (playerAnimator == null || playerAnimator.runtimeAnimatorController == null)
        {
            Debug.LogError("Could not find PlayerLogic with an active Animator in the scene.");
            return;
        }

        AnimatorController controller = playerAnimator.runtimeAnimatorController as AnimatorController;
        if (controller == null)
        {
            Debug.LogError("The Player's RuntimeAnimatorController is not an AnimatorController asset.");
            return;
        }

        // 2. Add the 'IsRunning' parameter if it doesn't exist
        bool hasParameter = false;
        foreach (var param in controller.parameters)
        {
            if (param.name == "IsRunning")
            {
                hasParameter = true;
                break;
            }
        }

        if (!hasParameter)
        {
            controller.AddParameter("IsRunning", AnimatorControllerParameterType.Bool);
            Debug.Log("Added 'IsRunning' parameter to the Animator Controller.");
        }

        // 3. Find the run animation clip
        AnimationClip runClip = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Animations/run60fram.fbx");
        if (runClip == null)
        {
            // The fbx itself might contain multiple clips. Let's try loading all sub-assets.
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath("Assets/Animations/run60fram.fbx");
            foreach (var asset in assets)
            {
                if (asset is AnimationClip && !asset.name.StartsWith("__preview__"))
                {
                    runClip = asset as AnimationClip;
                    break;
                }
            }
            
            if (runClip == null)
            {
                Debug.LogError("Could not find the 'run60fram.fbx' animation clip at Assets/Animations/run60fram.fbx");
                return;
            }
        }

        // 4. Get the Base Layer
        AnimatorStateMachine rootStateMachine = controller.layers[0].stateMachine;

        // 5. Check if "Run" state already exists, if not create it
        AnimatorState runState = null;
        AnimatorState idleWalkState = null; // Assuming we transition from AnyState or an existing Walk state
        
        foreach (var state in rootStateMachine.states)
        {
            if (state.state.name == "Run" || state.state.name == "Running")
            {
                runState = state.state;
            }
            // Just picking the default state or the first state to transition FROM
            if (state.state == rootStateMachine.defaultState)
            {
                idleWalkState = state.state;
            }
        }

        if (runState == null)
        {
            runState = rootStateMachine.AddState("Run");
            runState.motion = runClip;
            Debug.Log("Added 'Run' state to the Animator Controller.");
            
            // Set up transitions from Any State to Run
            AnimatorStateTransition toRun = rootStateMachine.AddAnyStateTransition(runState);
            toRun.AddCondition(AnimatorConditionMode.If, 0, "IsRunning");
            toRun.duration = 0.1f;
            toRun.hasExitTime = false;
            
            // Transition back to Idle/Walk (Default State) when not running
            if (idleWalkState != null)
            {
                AnimatorStateTransition toIdleWalk = runState.AddTransition(idleWalkState);
                toIdleWalk.AddCondition(AnimatorConditionMode.IfNot, 0, "IsRunning");
                toIdleWalk.duration = 0.1f;
                toIdleWalk.hasExitTime = false;
            }
        }
        else
        {
            // Make sure it has the right motion
            runState.motion = runClip;
            Debug.Log("Updated existing 'Run' state with the new clip.");
        }

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();

        Debug.Log("<color=green><b>Success!</b></color> Player run animation setup is complete. You can now test it in Play Mode.");
    }
}
