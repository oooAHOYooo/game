using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// NinjaStrike ▶ Setup Animated Characters
///
/// Run this once after importing the Mixamo FBX files.
/// It creates Assets/Resources/Characters/PlayerAnimated.prefab — a
/// clean character root (mesh + Animator) that GameBootstrapper loads
/// at runtime in standalone builds via Resources.Load.
///
/// The prefab contains ONLY the visual/animation layer.
/// All game-logic components (NinjaController, PlayerHealth, etc.) are
/// added at runtime by GameBootstrapper, exactly like in the editor path.
/// </summary>
public static class CharacterSetup
{
    private const string PREFAB_FOLDER   = "Assets/Resources/Characters";
    private const string PREFAB_PATH     = "Assets/Resources/Characters/PlayerAnimated.prefab";
    private const string ANIMATOR_FOLDER = "Assets/Resources/Animator";
    private const string CONTROLLER_SRC  = "Assets/Art/Animator/PlayerController.controller";
    private const string CONTROLLER_DST  = "Assets/Resources/Animator/PlayerController.controller";

    [MenuItem("NinjaStrike/Setup Animated Characters")]
    public static void SetupAnimatedCharacters()
    {
        // ── 1. Find the Mixamo model FBX ──────────────────────────────────
        var modelPrefab = LoadMixamoModel();
        if (modelPrefab == null)
        {
            EditorUtility.DisplayDialog(
                "Mixamo Model Not Found",
                "No character FBX found in Assets/Art/Models/Character/.\n\n" +
                "Download a character from mixamo.com (T-Pose, FBX for Unity), " +
                "place it in Assets/Art/Models/Character/, then run this menu item again.",
                "OK");
            return;
        }

        // ── 2. Ensure Resources sub-folders exist ─────────────────────────
        EnsureFolder("Assets/Resources");
        EnsureFolder(PREFAB_FOLDER);
        EnsureFolder(ANIMATOR_FOLDER);

        // ── 3. Copy the Animator Controller to Resources so builds find it ─
        if (!File.Exists(CONTROLLER_DST))
        {
            AssetDatabase.CopyAsset(CONTROLLER_SRC, CONTROLLER_DST);
            Debug.Log($"[CharacterSetup] Copied controller → {CONTROLLER_DST}");
        }

        // ── 4. Build a temporary scene object to configure the prefab ─────
        var instance = (GameObject)PrefabUtility.InstantiatePrefab(modelPrefab);
        instance.name = "PlayerAnimated";

        // Ensure Animator component + controller
        var anim = instance.GetComponent<Animator>();
        if (anim == null) anim = instance.AddComponent<Animator>();
        anim.applyRootMotion = false;

        // Build (or rebuild) the animator controller from current animation clips
        var animSet = AnimationLibrary.LoadAnimations();
        var controller = AnimatorControllerBuilder.CreateController(
            animSet, "Assets/Art/Animator/PlayerController.controller");
        if (controller != null) anim.runtimeAnimatorController = controller;

        // Add a CapsuleCollider so trigger detection works in-prefab
        var col = instance.GetComponent<CapsuleCollider>();
        if (col == null) col = instance.AddComponent<CapsuleCollider>();
        col.height = 2f; col.radius = 0.4f; col.center = new Vector3(0, 1f, 0);

        // Add Rigidbody so physics is present
        var rb = instance.GetComponent<Rigidbody>();
        if (rb == null) rb = instance.AddComponent<Rigidbody>();
        rb.mass = 70f; rb.linearDamping = 2f; rb.angularDamping = 5f;
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

        // ── 5. Save as prefab ─────────────────────────────────────────────
        bool isNew = !File.Exists(PREFAB_PATH);
        PrefabUtility.SaveAsPrefabAsset(instance, PREFAB_PATH);
        Object.DestroyImmediate(instance);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // ── 6. Also copy controller to Resources ──────────────────────────
        // (overwrite in case the source was updated)
        AssetDatabase.CopyAsset(CONTROLLER_SRC, CONTROLLER_DST);

        string verb = isNew ? "Created" : "Updated";
        Debug.Log($"[CharacterSetup] {verb} prefab at {PREFAB_PATH}");
        EditorUtility.DisplayDialog(
            "Animated Characters Ready",
            $"{verb} {PREFAB_PATH}\n\n" +
            "Both editor Play mode and standalone builds will now use the " +
            "Mixamo character. Build the game normally — no extra steps needed.",
            "Great!");
    }

    [MenuItem("NinjaStrike/Setup Animated Characters", validate = true)]
    public static bool SetupAnimatedCharactersValidate()
    {
        // Only show the menu in the editor (always true here, but keeps it clean)
        return true;
    }

    // ── Helpers ──────────────────────────────────────────────────────────
    static GameObject LoadMixamoModel()
    {
        string folder = "Assets/Art/Models/Character";
        string[] names = {
            "X Bot.fbx", "Y Bot.fbx", "xbot.fbx", "ybot.fbx",
            "Mannequin_Base.fbx", "Mannequin.fbx", "Character.fbx"
        };
        foreach (var name in names)
        {
            var go = AssetDatabase.LoadAssetAtPath<GameObject>($"{folder}/{name}");
            if (go != null) return go;
        }
        // Last resort: any model in the folder
        foreach (var guid in AssetDatabase.FindAssets("t:Model", new[] { folder }))
        {
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
            if (go != null) return go;
        }
        return null;
    }

    static void EnsureFolder(string path)
    {
        if (!AssetDatabase.IsValidFolder(path))
        {
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            string folder = Path.GetFileName(path);
            AssetDatabase.CreateFolder(parent, folder);
        }
    }
}
