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

        // Each slot lists names in priority order — exact first, then fuzzy fallbacks.
        // This handles Mixamo's slight naming variations across download batches.

        // ── Locomotion ───────────────────────────────────────────────────
        set.Idle   = FindBestMatch(LOCOMOTION_FOLDER, new[] {
                         "Idle", "Standing Idle", "Breathing Idle",
                         "Neutral Idle", "T-Pose" });

        set.Run    = FindBestMatch(LOCOMOTION_FOLDER, new[] {
                         "Standing Run Forward", "Run Forward",
                         "Running", "Run" });

        set.Sprint = FindBestMatch(LOCOMOTION_FOLDER, new[] {
                         "Standing Sprint Forward", "Sprint Forward",
                         "Sprinting", "Sprint" });

        set.Jump   = FindBestMatch(LOCOMOTION_FOLDER, new[] {
                         "Standing Jump", "Jump", "Jumping" });

        set.Fall   = FindBestMatch(LOCOMOTION_FOLDER, new[] {
                         "Fall A Loop", "Falling Idle", "Falling",
                         "Fall Loop", "In Air" });

        set.Land   = FindBestMatch(LOCOMOTION_FOLDER, new[] {
                         "Fall A Land To Standing Idle 01",
                         "Hard Landing", "Landing", "Land",
                         "Fall To Idle" });

        // ── Combat ───────────────────────────────────────────────────────
        set.LightPunch   = FindBestMatch(COMBAT_FOLDER, new[] {
                               "Punching", "Jab", "Straight Punch",
                               "Standing 1H Magic Attack 01" });

        set.HeavyPunch   = FindBestMatch(COMBAT_FOLDER, new[] {
                               "Boxing (1)", "Boxing",
                               "Hook", "Uppercut",
                               "Standing Melee Attack Downward" });

        set.LightKick    = FindBestMatch(COMBAT_FOLDER, new[] {
                               "Jump Kick", "Kick",
                               "Standing Kick", "Low Kick",
                               "Roundhouse Kick" });

        set.HeavyKick    = FindBestMatch(COMBAT_FOLDER, new[] {
                               "Standing Melee Attack Horizontal",
                               "Spinning Kick", "High Kick",
                               "Sweep Kick" });

        set.ChargeAttack = FindBestMatch(COMBAT_FOLDER, new[] {
                               "Standing Aim Overdraw",
                               "Charging", "Power Up",
                               "Standing Aim Recoil", "Yelling" });

        return set;
    }

    /// <summary>
    /// Find the best matching animation clip in a subfolder.
    /// Priority: exact match on first term → exact on later terms → substring on all terms.
    /// </summary>
    private static AnimationClip FindBestMatch(string subfolder, string[] searchTerms, bool exact = false)
    {
        string folderPath = Path.Combine(ANIMATIONS_ROOT, subfolder);

        if (!Directory.Exists(folderPath))
        {
            Debug.Log($"[AnimLib] Folder not present yet: {folderPath}");
            return null;
        }

        var fbxFiles = Directory.GetFiles(folderPath, "*.fbx", SearchOption.TopDirectoryOnly);
        if (fbxFiles.Length == 0)
        {
            Debug.Log($"[AnimLib] No FBX in {folderPath} — add Mixamo clips then run NinjaStrike▶Setup.");
            return null;
        }

        // Pass 1: exact match (case-insensitive) on each term in priority order
        foreach (var term in searchTerms)
            foreach (var fbxPath in fbxFiles)
            {
                string name = Path.GetFileNameWithoutExtension(fbxPath);
                if (name.Equals(term, System.StringComparison.OrdinalIgnoreCase))
                    return LoadAnimationClipFromFBX(fbxPath, name);
            }

        // Pass 2: substring match — catches slight naming variations
        foreach (var term in searchTerms)
            foreach (var fbxPath in fbxFiles)
            {
                string name = Path.GetFileNameWithoutExtension(fbxPath);
                if (name.IndexOf(term, System.StringComparison.OrdinalIgnoreCase) >= 0)
                    return LoadAnimationClipFromFBX(fbxPath, name);
            }

        Debug.LogWarning($"[AnimLib] No match in '{subfolder}' for: {string.Join(" | ", searchTerms)}");
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
            Debug.Log($"  (folder not present: {folderPath})");
            return;
        }

        var files = Directory.GetFiles(folderPath, "*.fbx");
        if (files.Length == 0)
            Debug.Log("  (no FBX files — add Mixamo clips here)");
        else
            foreach (var file in files)
                Debug.Log($"  {Path.GetFileNameWithoutExtension(file)}");
    }
}
