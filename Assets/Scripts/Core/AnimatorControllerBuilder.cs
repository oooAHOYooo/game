using UnityEngine;

#if UNITY_EDITOR
using UnityEditor.Animations;
#endif

/// <summary>
/// Builds an Animator Controller from code with states and transitions.
/// In-editor: Creates controller asset + animates transitions
/// Runtime: Applies basic animation logic via direct clip playing (if no controller exists)
/// </summary>
public class AnimatorControllerBuilder
{
    #if UNITY_EDITOR
    /// <summary>
    /// Create an Animator Controller from animation clips
    /// Called during initialization to build the controller asset
    /// </summary>
    public static AnimatorController CreateController(AnimationLibrary.AnimationSet animSet, string controllerPath)
    {
        // If controller already exists, return it
        var existing = UnityEditor.AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
        if (existing != null)
            return existing;

        // Create new controller
        var controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);

        var rootStateMachine = controller.layers[0].stateMachine;

        // Create parameter for locomotion
        controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
        controller.AddParameter("IsFlying", AnimatorControllerParameterType.Bool);
        controller.AddParameter("IsAttacking", AnimatorControllerParameterType.Bool);
        controller.AddParameter("AttackType", AnimatorControllerParameterType.Int);  // 0=none, 1=light, 2=heavy
        controller.AddParameter("IsChargingKi", AnimatorControllerParameterType.Bool);

        // Create states
        var idleState = rootStateMachine.AddState("Idle");
        var runState = rootStateMachine.AddState("Run");
        var sprintState = rootStateMachine.AddState("Sprint");
        var jumpState = rootStateMachine.AddState("Jump");
        var fallState = rootStateMachine.AddState("Fall");
        var landState = rootStateMachine.AddState("Land");

        // UFC-style attack states (punch/kick)
        var lightPunchState = rootStateMachine.AddState("Attack Light Punch");
        var heavyPunchState = rootStateMachine.AddState("Attack Heavy Punch");
        var lightKickState = rootStateMachine.AddState("Attack Light Kick");
        var heavyKickState = rootStateMachine.AddState("Attack Heavy Kick");
        var chargeState = rootStateMachine.AddState("Charge");

        // Assign animation clips
        if (animSet.Idle) idleState.motion = animSet.Idle;
        if (animSet.Run) runState.motion = animSet.Run;
        if (animSet.Sprint) sprintState.motion = animSet.Sprint;
        if (animSet.Jump) jumpState.motion = animSet.Jump;
        if (animSet.Fall) fallState.motion = animSet.Fall;
        if (animSet.Land) landState.motion = animSet.Land;
        if (animSet.LightPunch) lightPunchState.motion = animSet.LightPunch;
        if (animSet.HeavyPunch) heavyPunchState.motion = animSet.HeavyPunch;
        if (animSet.LightKick) lightKickState.motion = animSet.LightKick;
        if (animSet.HeavyKick) heavyKickState.motion = animSet.HeavyKick;
        if (animSet.ChargeAttack) chargeState.motion = animSet.ChargeAttack;

        // Set default state
        rootStateMachine.defaultState = idleState;

        // Create transitions: Attacks from any state (based on AttackType parameter)
        var anyToLightPunch = rootStateMachine.AddAnyStateTransition(lightPunchState);
        anyToLightPunch.AddCondition(AnimatorConditionMode.If, 0, "IsAttacking");
        anyToLightPunch.AddCondition(AnimatorConditionMode.Equals, 1, "AttackType");

        var anyToHeavyPunch = rootStateMachine.AddAnyStateTransition(heavyPunchState);
        anyToHeavyPunch.AddCondition(AnimatorConditionMode.If, 0, "IsAttacking");
        anyToHeavyPunch.AddCondition(AnimatorConditionMode.Equals, 2, "AttackType");

        var anyToLightKick = rootStateMachine.AddAnyStateTransition(lightKickState);
        anyToLightKick.AddCondition(AnimatorConditionMode.If, 0, "IsAttacking");
        anyToLightKick.AddCondition(AnimatorConditionMode.Equals, 3, "AttackType");

        var anyToHeavyKick = rootStateMachine.AddAnyStateTransition(heavyKickState);
        anyToHeavyKick.AddCondition(AnimatorConditionMode.If, 0, "IsAttacking");
        anyToHeavyKick.AddCondition(AnimatorConditionMode.Equals, 4, "AttackType");

        rootStateMachine.AddAnyStateTransition(chargeState).AddCondition(AnimatorConditionMode.If, 0, "IsChargingKi");

        // Locomotion transitions
        var idleToRun = idleState.AddTransition(runState);
        idleToRun.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");
        idleToRun.exitTime = 0.1f;
        idleToRun.duration = 0.2f;

        var runToIdle = runState.AddTransition(idleState);
        runToIdle.AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");
        runToIdle.exitTime = 0.1f;
        runToIdle.duration = 0.2f;

        var runToSprint = runState.AddTransition(sprintState);
        runToSprint.AddCondition(AnimatorConditionMode.Greater, 0.7f, "Speed");
        runToSprint.exitTime = 0.1f;
        runToSprint.duration = 0.2f;

        var sprintToRun = sprintState.AddTransition(runState);
        sprintToRun.AddCondition(AnimatorConditionMode.Less, 0.7f, "Speed");
        sprintToRun.exitTime = 0.1f;
        sprintToRun.duration = 0.2f;

        // Flight transitions
        var anyToFall = rootStateMachine.AddAnyStateTransition(fallState);
        anyToFall.AddCondition(AnimatorConditionMode.If, 0, "IsFlying");
        anyToFall.exitTime = 0.1f;

        var fallToLand = fallState.AddTransition(landState);
        fallToLand.AddCondition(AnimatorConditionMode.If, 0, "IsFlying");  // When flying becomes false
        fallToLand.exitTime = 0f;
        fallToLand.duration = 0.1f;

        // Attack to idle transitions
        var lightPunchToIdle = lightPunchState.AddTransition(idleState);
        lightPunchToIdle.AddCondition(AnimatorConditionMode.IfNot, 0, "IsAttacking");
        lightPunchToIdle.exitTime = 0.8f;
        lightPunchToIdle.duration = 0.1f;

        var heavyPunchToIdle = heavyPunchState.AddTransition(idleState);
        heavyPunchToIdle.AddCondition(AnimatorConditionMode.IfNot, 0, "IsAttacking");
        heavyPunchToIdle.exitTime = 0.8f;
        heavyPunchToIdle.duration = 0.1f;

        var lightKickToIdle = lightKickState.AddTransition(idleState);
        lightKickToIdle.AddCondition(AnimatorConditionMode.IfNot, 0, "IsAttacking");
        lightKickToIdle.exitTime = 0.8f;
        lightKickToIdle.duration = 0.1f;

        var heavyKickToIdle = heavyKickState.AddTransition(idleState);
        heavyKickToIdle.AddCondition(AnimatorConditionMode.IfNot, 0, "IsAttacking");
        heavyKickToIdle.exitTime = 0.8f;
        heavyKickToIdle.duration = 0.1f;

        // Charge to idle
        var chargeToIdle = chargeState.AddTransition(idleState);
        chargeToIdle.AddCondition(AnimatorConditionMode.IfNot, 0, "IsChargingKi");
        chargeToIdle.exitTime = 0.1f;
        chargeToIdle.duration = 0.1f;

        UnityEditor.EditorUtility.SetDirty(controller);
        UnityEditor.AssetDatabase.SaveAssets();

        Debug.Log($"Created Animator Controller at {controllerPath}");
        return controller;
    }
    #endif

    /// <summary>
    /// Load or create the animator controller
    /// </summary>
    public static RuntimeAnimatorController GetOrCreateController(AnimationLibrary.AnimationSet animSet)
    {
        string controllerPath = "Assets/Art/Animator/PlayerController.controller";

        #if UNITY_EDITOR
        return CreateController(animSet, controllerPath);
        #else
        // Runtime: Load from asset database
        var controller = Resources.Load<RuntimeAnimatorController>("Animator/PlayerController");
        if (controller) return controller;

        Debug.LogWarning("Animator Controller not found. Run animation setup in Editor first.");
        return null;
        #endif
    }
}
