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

        // ── Locomotion blend tree (Idle → Run → Sprint driven by Speed) ──
        var locoState = controller.CreateBlendTreeInController("Locomotion", out var blendTree, 0);
        blendTree.blendType = BlendTreeType.Simple1D;
        blendTree.blendParameter = "Speed";
        blendTree.useAutomaticThresholds = false;
        if (animSet.Idle)   { SetLoop(animSet.Idle,   true);  blendTree.AddChild(animSet.Idle,   0f); }
        if (animSet.Run)    { SetLoop(animSet.Run,    true);  blendTree.AddChild(animSet.Run,    1f); }
        if (animSet.Sprint) { SetLoop(animSet.Sprint, true);  blendTree.AddChild(animSet.Sprint, 1.2f); }
        else if (animSet.Run) blendTree.AddChild(animSet.Run, 1.2f); // fallback if no sprint clip

        // ── Other states ─────────────────────────────────────────────────
        var jumpState      = rootStateMachine.AddState("Jump");
        var fallState      = rootStateMachine.AddState("Fall");
        var landState      = rootStateMachine.AddState("Land");
        var lightPunchState = rootStateMachine.AddState("Attack Light Punch");
        var heavyPunchState = rootStateMachine.AddState("Attack Heavy Punch");
        var lightKickState  = rootStateMachine.AddState("Attack Light Kick");
        var heavyKickState  = rootStateMachine.AddState("Attack Heavy Kick");
        var chargeState     = rootStateMachine.AddState("Charge");

        if (animSet.Jump)        { SetLoop(animSet.Jump,        false); jumpState.motion       = animSet.Jump; }
        if (animSet.Fall)        { SetLoop(animSet.Fall,        true);  fallState.motion       = animSet.Fall; }
        if (animSet.Land)        { SetLoop(animSet.Land,        false); landState.motion       = animSet.Land; }
        if (animSet.LightPunch)  { SetLoop(animSet.LightPunch,  false); lightPunchState.motion = animSet.LightPunch; }
        if (animSet.HeavyPunch)  { SetLoop(animSet.HeavyPunch,  false); heavyPunchState.motion = animSet.HeavyPunch; }
        if (animSet.LightKick)   { SetLoop(animSet.LightKick,   false); lightKickState.motion  = animSet.LightKick; }
        if (animSet.HeavyKick)   { SetLoop(animSet.HeavyKick,   false); heavyKickState.motion  = animSet.HeavyKick; }
        if (animSet.ChargeAttack){ SetLoop(animSet.ChargeAttack,true);  chargeState.motion     = animSet.ChargeAttack; }

        rootStateMachine.defaultState = locoState;

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

        // ── Aerial ───────────────────────────────────────────────────────
        var anyToJump = Any(jumpState, 0.05f);
        anyToJump.AddCondition(AnimatorConditionMode.If, 0, "JumpTrigger");

        T(jumpState, fallState, exit: 0.5f, dur: 0.1f, hasExit: true);

        // Any → Fall when airborne (not attacking, not flying)
        var anyToFall = Any(fallState, 0.2f);
        anyToFall.AddCondition(AnimatorConditionMode.IfNot, 0, "IsGrounded");
        anyToFall.AddCondition(AnimatorConditionMode.IfNot, 0, "IsFlying");
        anyToFall.AddCondition(AnimatorConditionMode.IfNot, 0, "IsAttacking");

        // Any → airborne flying pose when IsFlying=true
        var anyToFly = Any(fallState, 0.2f);
        anyToFly.AddCondition(AnimatorConditionMode.If, 0, "IsFlying");

        // Fall → Land
        var fallToLand = T(fallState, landState, dur: 0.05f);
        fallToLand.AddCondition(AnimatorConditionMode.If, 0, "IsGrounded");
        fallToLand.AddCondition(AnimatorConditionMode.IfNot, 0, "IsFlying");

        // Land → Locomotion after clip finishes
        T(landState, locoState, exit: 0.85f, dur: 0.15f, hasExit: true);

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
            var back = T(atk, locoState, exit: 0.75f, dur: 0.15f, hasExit: true);
            back.AddCondition(AnimatorConditionMode.IfNot, 0, "IsAttacking");
        }

        // ── Ki Charge ────────────────────────────────────────────────────
        var toCharge = Any(chargeState, 0.15f);
        toCharge.AddCondition(AnimatorConditionMode.If, 0, "IsChargingKi");

        var chargeToLoco = T(chargeState, locoState, dur: 0.15f);
        chargeToLoco.AddCondition(AnimatorConditionMode.IfNot, 0, "IsChargingKi");

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
