using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Linq;

/// <summary>
/// Scans the Art/Animations folder and catalogs available animations by type.
/// Returns animation clips mapped to logical game states (Idle, Run, Attack, etc).
/// </summary>
public class AnimationLibrary
{
    private static readonly string ANIMATIONS_ROOT = "Assets/Art/Animations";
    private static readonly string LOCOMOTION_FOLDER = "Locomotion";
    private static readonly string COMBAT_FOLDER = "Combat";

    public class AnimationSet
    {
        public AnimationClip Idle;
        public AnimationClip Run;
        public AnimationClip Sprint;
        public AnimationClip Jump;
        public AnimationClip Fall;
        public AnimationClip Land;
        public AnimationClip LightPunch;
        public AnimationClip HeavyPunch;
        public AnimationClip LightKick;
        public AnimationClip HeavyKick;
        public AnimationClip ChargeAttack;
    }

    /// <summary>
    /// Load all animations from the Art/Animations folder structure
    /// </summary>
    public static AnimationSet LoadAnimations()
    {
        var set = new AnimationSet();

        // Load locomotion animations
        set.Idle = FindBestMatch(LOCOMOTION_FOLDER, new[] { "Idle" }, exact: true);
        set.Run = FindBestMatch(LOCOMOTION_FOLDER, new[] { "Standing Run Forward" }, exact: true);
        set.Sprint = FindBestMatch(LOCOMOTION_FOLDER, new[] { "Standing Sprint Forward" }, exact: true);
        set.Jump = FindBestMatch(LOCOMOTION_FOLDER, new[] { "Standing Jump" }, exact: true);
        set.Fall = FindBestMatch(LOCOMOTION_FOLDER, new[] { "Fall A Loop" }, exact: true);
        set.Land = FindBestMatch(LOCOMOTION_FOLDER, new[] { "Fall A Land To Standing Idle 01" }, exact: true);

        // Load combat animations — UFC-style punches and kicks
        set.LightPunch   = FindBestMatch(COMBAT_FOLDER, new[] { "Punching" }, exact: true);
        set.HeavyPunch   = FindBestMatch(COMBAT_FOLDER, new[] { "Boxing (1)" }, exact: true);
        set.LightKick    = FindBestMatch(COMBAT_FOLDER, new[] { "Boxing" }, exact: true); // no kick found, mapped to second boxing
        set.HeavyKick    = FindBestMatch(COMBAT_FOLDER, new[] { "Standing Melee Attack Horizontal" }, exact: true);
        set.ChargeAttack = FindBestMatch(COMBAT_FOLDER, new[] { "Standing Aim Overdraw" }, exact: true);

        return set;
    }

    /// <summary>
    /// Find animation that best matches any of the search terms (case-insensitive substring match)
    /// Prefers exact matches over partial matches
    /// </summary>
    private static AnimationClip FindBestMatch(string subfolder, string[] searchTerms, bool exact = false)
    {
        string folderPath = Path.Combine(ANIMATIONS_ROOT, subfolder);

        if (!Directory.Exists(folderPath))
        {
            Debug.LogWarning($"Animation folder not found: {folderPath}");
            return null;
        }

        // Get all FBX files in the folder
        var fbxFiles = Directory.GetFiles(folderPath, "*.fbx", SearchOption.TopDirectoryOnly);

        foreach (var fbxPath in fbxFiles)
        {
            string fbxName = Path.GetFileNameWithoutExtension(fbxPath);

            // Check each search term
            foreach (var term in searchTerms)
            {
                if (exact)
                {
                    if (fbxName.Equals(term, System.StringComparison.OrdinalIgnoreCase))
                    {
                        return LoadAnimationClipFromFBX(fbxPath, fbxName);
                    }
                }
                else
                {
                    if (fbxName.Contains(term, System.StringComparison.OrdinalIgnoreCase))
                    {
                        return LoadAnimationClipFromFBX(fbxPath, fbxName);
                    }
                }
            }
        }

        Debug.LogWarning($"No animation found in {subfolder} for terms: {string.Join(", ", searchTerms)}");
        return null;
    }

    /// <summary>
    /// Load an AnimationClip from an FBX file via Resources.Load
    /// Mixamo FBX files have the animation embedded with the file name as clip name
    /// </summary>
    private static AnimationClip LoadAnimationClipFromFBX(string fbxPath, string fbxName)
    {
        // Convert fbxPath to asset path format for Resources.Load
        // E.g., "Assets/Art/Animations/Locomotion/Idle.fbx" -> load "Idle" animation from that FBX

        // Try loading from asset database first (in-editor)
        #if UNITY_EDITOR
        var clip = UnityEditor.AssetDatabase.LoadAssetAtPath<AnimationClip>(fbxPath);
        if (clip != null) return clip;
        #endif

        // Fallback: Try loading animation clips from the FBX
        // The primary animation clip is usually named after the file
        string resourcePath = fbxPath.Replace("Assets/", "").Replace(".fbx", "");
        var animations = Resources.LoadAll<AnimationClip>(resourcePath);

        if (animations.Length > 0)
        {
            return animations[0];
        }

        Debug.LogWarning($"Could not load animation from: {fbxPath}");
        return null;
    }

    /// <summary>
    /// List all available animation files for debugging
    /// </summary>
    public static void DebugListAnimations()
    {
        Debug.Log("=== LOCOMOTION ANIMATIONS ===");
        ListFolder(Path.Combine(ANIMATIONS_ROOT, LOCOMOTION_FOLDER));

        Debug.Log("=== COMBAT ANIMATIONS ===");
        ListFolder(Path.Combine(ANIMATIONS_ROOT, COMBAT_FOLDER));
    }

    private static void ListFolder(string folderPath)
    {
        if (!Directory.Exists(folderPath))
        {
            Debug.LogWarning($"Folder not found: {folderPath}");
            return;
        }

        var files = Directory.GetFiles(folderPath, "*.fbx");
        foreach (var file in files)
        {
            Debug.Log($"  {Path.GetFileNameWithoutExtension(file)}");
        }
    }
}
