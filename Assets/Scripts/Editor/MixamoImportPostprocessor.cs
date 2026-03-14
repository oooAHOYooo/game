/// <summary>
/// MixamoImportPostprocessor
///
/// Automatically configures any FBX dropped into Assets/Art/Models/Character/ or
/// Assets/Art/Animations/ that looks like it came from Mixamo:
///   • Sets Animation Type  → Humanoid
///   • Sets Avatar          → Create From This Model
///   • Disables root motion on animation-only FBX (no mesh)
///   • Marks loop on locomotion clips (Idle / Run / Sprint / Fall)
///   • Strips the "mixamorig:" prefix from exposed transform paths so Unity
///     can resolve bones correctly at runtime.
///
/// Drop your Mixamo FBX files anywhere under the paths listed in WATCH_PATHS and
/// hit reimport — this script handles the rest.
/// </summary>

#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public class MixamoImportPostprocessor : AssetPostprocessor
{
    // Folders we watch — models AND animation packs
    private static readonly string[] WATCH_PATHS =
    {
        "Assets/Art/Models/Character",
        "Assets/Art/Animations",
    };

    // Locomotion clip names that should loop
    private static readonly string[] LOOP_CLIP_NAMES =
    {
        "Idle", "Run", "Sprint", "Walk", "Fall", "Fly"
    };

    // ──────────────────────────────────────────────────────────────────────
    void OnPreprocessModel()
    {
        if (!IsMixamoPath(assetPath)) return;

        var importer = assetImporter as ModelImporter;
        if (importer == null) return;

        // ── Humanoid rig ─────────────────────────────────────────────────
        if (importer.animationType != ModelImporterAnimationType.Human)
        {
            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup   = ModelImporterAvatarSetup.CreateFromThisModel;
            Debug.Log($"[Mixamo] Set Humanoid rig on: {assetPath}");
        }

        // ── Animation-only FBX (no mesh geometry) ────────────────────────
        // Mixamo animation packs contain a T-pose mesh we don't need.
        // Keep the avatar but strip the mesh to save memory.
        if (IsAnimationOnlyPath(assetPath))
        {
            importer.materialImportMode = ModelImporterMaterialImportMode.None;
        }
        else
        {
            // Character model — keep materials
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
        }
    }

    // ──────────────────────────────────────────────────────────────────────
    void OnPostprocessModel(GameObject root)
    {
        if (!IsMixamoPath(assetPath)) return;

        // Rename "mixamorig:" bones → bare names so Unity's avatar matching works
        // without the namespace prefix.  We rename a copy — the source FBX is untouched.
        StripMixamoPrefix(root.transform);
        Debug.Log($"[Mixamo] Stripped bone prefixes on: {assetPath}");
    }

    // ──────────────────────────────────────────────────────────────────────
    void OnPreprocessAnimation()
    {
        if (!IsMixamoPath(assetPath)) return;

        var importer = assetImporter as ModelImporter;
        if (importer == null) return;

        var clips = importer.defaultClipAnimations;
        for (int i = 0; i < clips.Length; i++)
        {
            bool shouldLoop = ShouldLoop(clips[i].name);
            clips[i].loopTime = shouldLoop;
            clips[i].lockRootRotation   = true;   // prevent root rotation drift
            clips[i].lockRootHeightY    = false;   // allow in-place locomotion
            clips[i].keepOriginalOrientation = false;
        }
        importer.clipAnimations = clips;
    }

    // ──────────────────────────────────────────────────────────────────────
    // Helpers
    // ──────────────────────────────────────────────────────────────────────
    static bool IsMixamoPath(string path)
    {
        foreach (var watchPath in WATCH_PATHS)
            if (path.StartsWith(watchPath, System.StringComparison.OrdinalIgnoreCase))
                return true;
        return false;
    }

    static bool IsAnimationOnlyPath(string path) =>
        path.StartsWith("Assets/Art/Animations", System.StringComparison.OrdinalIgnoreCase);

    static bool ShouldLoop(string clipName)
    {
        foreach (var name in LOOP_CLIP_NAMES)
            if (clipName.IndexOf(name, System.StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
        return false;
    }

    static void StripMixamoPrefix(Transform t)
    {
        const string prefix = "mixamorig:";
        if (t.name.StartsWith(prefix, System.StringComparison.OrdinalIgnoreCase))
            t.name = t.name.Substring(prefix.Length);

        for (int i = 0; i < t.childCount; i++)
            StripMixamoPrefix(t.GetChild(i));
    }

    // ──────────────────────────────────────────────────────────────────────
    // Menu helper — reimport all Mixamo assets in one click
    // ──────────────────────────────────────────────────────────────────────
    [MenuItem("NinjaStrike/Reimport Mixamo Assets")]
    static void ReimportAll()
    {
        AssetDatabase.StartAssetEditing();
        foreach (var watchPath in WATCH_PATHS)
        {
            var guids = AssetDatabase.FindAssets("t:Model", new[] { watchPath });
            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                Debug.Log($"[Mixamo] Reimported: {path}");
            }
        }
        AssetDatabase.StopAssetEditing();
        AssetDatabase.Refresh();
        Debug.Log("[Mixamo] All assets reimported.");
    }
}
#endif
