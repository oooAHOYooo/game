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
        // If controller already exists, return it (Commented out to allow rebuilding with new logic)
        // var existing = UnityEditor.AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
        // if (existing != null)
        //     return existing;

        // Create new controller
        var controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);

        var rootStateMachine = controller.layers[0].stateMachine;

        // Parameters
        controller.AddParameter("Speed",         AnimatorControllerParameterType.Float);
        controller.AddParameter("IsGrounded",    AnimatorControllerParameterType.Bool);
        controller.AddParameter("IsFlying",      AnimatorControllerParameterType.Bool);
        controller.AddParameter("IsAttacking",   AnimatorControllerParameterType.Bool);
        controller.AddParameter("AttackType",    AnimatorControllerParameterType.Int);
        controller.AddParameter("IsChargingKi",  AnimatorControllerParameterType.Bool);
        controller.AddParameter("JumpTrigger",   AnimatorControllerParameterType.Trigger);

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

        // Assign animation clips and ensure they loop if they are locomotion
        if (animSet.Idle) { SetLoop(animSet.Idle, true); idleState.motion = animSet.Idle; }
        if (animSet.Run) { SetLoop(animSet.Run, true); runState.motion = animSet.Run; }
        if (animSet.Sprint) { SetLoop(animSet.Sprint, true); sprintState.motion = animSet.Sprint; }
        if (animSet.Jump) { SetLoop(animSet.Jump, false); jumpState.motion = animSet.Jump; }
        if (animSet.Fall) { SetLoop(animSet.Fall, true); fallState.motion = animSet.Fall; }
        if (animSet.Land) { SetLoop(animSet.Land, false); landState.motion = animSet.Land; }
        if (animSet.LightPunch) { SetLoop(animSet.LightPunch, false); lightPunchState.motion = animSet.LightPunch; }
        if (animSet.HeavyPunch) { SetLoop(animSet.HeavyPunch, false); heavyPunchState.motion = animSet.HeavyPunch; }
        if (animSet.LightKick) { SetLoop(animSet.LightKick, false); lightKickState.motion = animSet.LightKick; }
        if (animSet.HeavyKick) { SetLoop(animSet.HeavyKick, false); heavyKickState.motion = animSet.HeavyKick; }
        if (animSet.ChargeAttack) { SetLoop(animSet.ChargeAttack, true); chargeState.motion = animSet.ChargeAttack; }

        rootStateMachine.defaultState = idleState;

        // ── Helpers ──────────────────────────────────────────────────────
        AnimatorStateTransition T(AnimatorState from, AnimatorState to,
            float exit = 0f, float dur = 0.15f, bool hasExit = false)
        {
            var t = from.AddTransition(to);
            t.hasExitTime = hasExit; t.exitTime = exit; t.duration = dur;
            return t;
        }
        AnimatorStateTransition Any(AnimatorState to, float dur = 0.1f)
        {
            var t = rootStateMachine.AddAnyStateTransition(to);
            t.hasExitTime = false; t.duration = dur;
            t.canTransitionToSelf = false;
            return t;
        }

        // ── Locomotion ────────────────────────────────────────────────────
        T(idleState, runState).AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");
        T(runState, idleState).AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");
        T(runState, sprintState).AddCondition(AnimatorConditionMode.Greater, 1.05f, "Speed");
        T(sprintState, runState).AddCondition(AnimatorConditionMode.Less, 1.05f, "Speed");

        // ── Aerial ───────────────────────────────────────────────────────
        // Jump trigger → jump state (plays once, then falls through to fall)
        var anyToJump = Any(jumpState, 0.05f);
        anyToJump.AddCondition(AnimatorConditionMode.If, 0, "JumpTrigger");

        var jumpToFall = T(jumpState, fallState, exit: 0.5f, dur: 0.1f, hasExit: true);

        // Any → Fall when airborne and not intentionally flying
        var anyToFall = Any(fallState, 0.1f);
        anyToFall.AddCondition(AnimatorConditionMode.IfNot, 0, "IsGrounded");
        anyToFall.AddCondition(AnimatorConditionMode.IfNot, 0, "IsFlying");
        anyToFall.AddCondition(AnimatorConditionMode.IfNot, 0, "IsAttacking");

        // Any → airborne flying pose (reuses fall clip) when IsFlying=true
        var anyToFly = Any(fallState, 0.2f);
        anyToFly.AddCondition(AnimatorConditionMode.If, 0, "IsFlying");

        // Land when touching ground
        var fallToLand = T(fallState, landState, dur: 0.05f);
        fallToLand.AddCondition(AnimatorConditionMode.If, 0, "IsGrounded");
        fallToLand.AddCondition(AnimatorConditionMode.IfNot, 0, "IsFlying");

        // Land → Idle after clip finishes
        var landToIdle = T(landState, idleState, exit: 0.85f, dur: 0.15f, hasExit: true);

        // ── Attacks (Any-state, highest priority) ─────────────────────────
        var toPunch = Any(lightPunchState);
        toPunch.AddCondition(AnimatorConditionMode.If, 0, "IsAttacking");
        toPunch.AddCondition(AnimatorConditionMode.Equals, 1, "AttackType");

        var toHeavyPunch = Any(heavyPunchState);
        toHeavyPunch.AddCondition(AnimatorConditionMode.If, 0, "IsAttacking");
        toHeavyPunch.AddCondition(AnimatorConditionMode.Equals, 2, "AttackType");

        var toKick = Any(lightKickState);
        toKick.AddCondition(AnimatorConditionMode.If, 0, "IsAttacking");
        toKick.AddCondition(AnimatorConditionMode.Equals, 3, "AttackType");

        var toHeavyKick = Any(heavyKickState);
        toHeavyKick.AddCondition(AnimatorConditionMode.If, 0, "IsAttacking");
        toHeavyKick.AddCondition(AnimatorConditionMode.Equals, 4, "AttackType");

        foreach (var atk in new[] { lightPunchState, heavyPunchState, lightKickState, heavyKickState })
        {
            var back = T(atk, idleState, exit: 0.75f, dur: 0.1f, hasExit: true);
            back.AddCondition(AnimatorConditionMode.IfNot, 0, "IsAttacking");
        }

        // ── Ki Charge ────────────────────────────────────────────────────
        var toCharge = Any(chargeState, 0.15f);
        toCharge.AddCondition(AnimatorConditionMode.If, 0, "IsChargingKi");

        var chargeToIdle = T(chargeState, idleState, dur: 0.1f);
        chargeToIdle.AddCondition(AnimatorConditionMode.IfNot, 0, "IsChargingKi");

        UnityEditor.EditorUtility.SetDirty(controller);
        UnityEditor.AssetDatabase.SaveAssets();

        Debug.Log($"Created Animator Controller at {controllerPath}");
        return controller;
    }

    private static void SetLoop(AnimationClip clip, bool loop)
    {
        if (clip == null)
        {
            Debug.LogWarning("Attempted to set loop time on a null AnimationClip!");
            return;
        }
        var settings = UnityEditor.AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = loop;
        UnityEditor.AnimationUtility.SetAnimationClipSettings(clip, settings);
        UnityEditor.EditorUtility.SetDirty(clip);
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
