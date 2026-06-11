using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public class PlayerAnimationSetup : EditorWindow
{
    [MenuItem("Tools/Setup Player Animations")]
    public static void SetupPlayerAnimations()
    {
        // 1. Find the Player's Animator in the scene
        PlayerLogic playerLogic = GameObject.FindObjectOfType<PlayerLogic>();
        if (playerLogic == null)
        {
            Debug.LogError("Could not find a GameObject with PlayerLogic component in the scene.");
            return;
        }

        Animator playerAnimator = playerLogic.GetComponent<Animator>();
        if (playerAnimator == null)
        {
            Debug.LogError("The Player GameObject does not have an Animator component.");
            return;
        }

        // 2. Get or Create the Animator Controller
        AnimatorController controller = playerAnimator.runtimeAnimatorController as AnimatorController;
        if (controller == null)
        {
            // If it's empty or not an asset, create a new one
            string path = "Assets/Animations/PlayerAnimator.controller";
            controller = AnimatorController.CreateAnimatorControllerAtPath(path);
            playerAnimator.runtimeAnimatorController = controller;
            Debug.Log($"Created new Animator Controller at {path}");
        }

        // 3. Ensure Parameters exist
        EnsureParameter(controller, "IsWalking", AnimatorControllerParameterType.Bool);
        EnsureParameter(controller, "IsRunning", AnimatorControllerParameterType.Bool);
        EnsureParameter(controller, "IsGrounded", AnimatorControllerParameterType.Bool);
        EnsureParameter(controller, "Jump", AnimatorControllerParameterType.Trigger);

        // 4. Load Animation Clips
        AnimationClip idleClip = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Animations/Breathingidle.anim");
        AnimationClip walkClip = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Animations/walk.anim");
        AnimationClip runClip = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Animations/Run.anim");

        if (idleClip == null) Debug.LogWarning("Missing Assets/Animations/Breathingidle.anim");
        if (walkClip == null) Debug.LogWarning("Missing Assets/Animations/walk.anim");
        if (runClip == null) Debug.LogWarning("Missing Assets/Animations/Run.anim");

        // 5. Setup State Machine
        AnimatorStateMachine rootStateMachine = controller.layers[0].stateMachine;
        
        // Clear existing states to avoid mess (Optional, but cleaner for "returning" to a known state)
        // rootStateMachine.states = new ChildAnimatorState[0]; 

        AnimatorState idleState = GetOrCreateState(rootStateMachine, "Idle", idleClip);
        AnimatorState walkState = GetOrCreateState(rootStateMachine, "Walk", walkClip);
        AnimatorState runState = GetOrCreateState(rootStateMachine, "Run", runClip);

        rootStateMachine.defaultState = idleState;

        // 6. Setup Transitions
        
        // Idle -> Walk
        AddTransitionIfMissing(idleState, walkState, new (string, AnimatorConditionMode, float)[] { 
            ("IsWalking", AnimatorConditionMode.If, 0),
            ("IsRunning", AnimatorConditionMode.IfNot, 0)
        });

        // Idle -> Run
        AddTransitionIfMissing(idleState, runState, new (string, AnimatorConditionMode, float)[] { 
            ("IsRunning", AnimatorConditionMode.If, 0)
        });

        // Walk -> Idle
        AddTransitionIfMissing(walkState, idleState, new (string, AnimatorConditionMode, float)[] { 
            ("IsWalking", AnimatorConditionMode.IfNot, 0),
            ("IsRunning", AnimatorConditionMode.IfNot, 0)
        });

        // Walk -> Run
        AddTransitionIfMissing(walkState, runState, new (string, AnimatorConditionMode, float)[] { 
            ("IsRunning", AnimatorConditionMode.If, 0)
        });

        // Run -> Idle
        AddTransitionIfMissing(runState, idleState, new (string, AnimatorConditionMode, float)[] { 
            ("IsRunning", AnimatorConditionMode.IfNot, 0),
            ("IsWalking", AnimatorConditionMode.IfNot, 0)
        });

        // Run -> Walk
        AddTransitionIfMissing(runState, walkState, new (string, AnimatorConditionMode, float)[] { 
            ("IsRunning", AnimatorConditionMode.IfNot, 0),
            ("IsWalking", AnimatorConditionMode.If, 0)
        });

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();

        Debug.Log("<color=green><b>Success!</b></color> Player animations have been restored and configured. Check 'PlayerAnimator.controller'.");
    }

    private static void EnsureParameter(AnimatorController controller, string name, AnimatorControllerParameterType type)
    {
        foreach (var param in controller.parameters)
        {
            if (param.name == name) return;
        }
        controller.AddParameter(name, type);
    }

    private static AnimatorState GetOrCreateState(AnimatorStateMachine stateMachine, string name, AnimationClip clip)
    {
        foreach (var childState in stateMachine.states)
        {
            if (childState.state.name == name)
            {
                childState.state.motion = clip;
                return childState.state;
            }
        }
        AnimatorState state = stateMachine.AddState(name);
        state.motion = clip;
        return state;
    }

    private static void AddTransitionIfMissing(AnimatorState from, AnimatorState to, (string name, AnimatorConditionMode mode, float threshold)[] conditions)
    {
        foreach (var trans in from.transitions)
        {
            if (trans.destinationState == to) return;
        }

        AnimatorStateTransition transition = from.AddTransition(to);
        transition.hasExitTime = false;
        transition.duration = 0.15f;
        
        foreach (var cond in conditions)
        {
            transition.AddCondition(cond.mode, cond.threshold, cond.name);
        }
    }
}
