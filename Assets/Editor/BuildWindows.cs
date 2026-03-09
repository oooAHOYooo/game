using UnityEditor;
using System.IO;

public class BuildWindows
{
    [MenuItem("Build/Build for Windows")]
    public static void BuildForWindows()
    {
        string buildPath = "Builds/Windows/NinjaStrike.exe";

        // Ensure directory exists
        Directory.CreateDirectory("Builds/Windows");

        // Get all scenes enabled in Build Settings
        string[] scenes = GetEnabledScenes();

        if (scenes.Length == 0)
        {
            UnityEngine.Debug.LogError("❌ No scenes are enabled in Build Settings!");
            return;
        }

        UnityEngine.Debug.Log("🏗️ Starting Windows Build (x86_64)...");
        
        BuildPlayerOptions options = new BuildPlayerOptions();
        options.scenes = scenes;
        options.locationPathName = buildPath;
        options.target = BuildTarget.StandaloneWindows64;
        options.options = BuildOptions.None;

        BuildPipeline.BuildPlayer(options);

        UnityEngine.Debug.Log("✅ Windows Build complete! Output: " + buildPath);
    }

    private static string[] GetEnabledScenes()
    {
        var scenes = EditorBuildSettings.scenes;
        var enabledScenes = new System.Collections.Generic.List<string>();
        foreach (var scene in scenes)
        {
            if (scene.enabled)
                enabledScenes.Add(scene.path);
        }
        return enabledScenes.ToArray();
    }
}
