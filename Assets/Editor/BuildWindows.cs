using UnityEditor;
using UnityEditor.Build;
using System.IO;

public class BuildWindows
{
    [MenuItem("Build/Build for Windows")]
    public static void BuildForWindows()
    {
        string buildDir = "Builds/Windows";
        string buildPath = buildDir + "/NinjaStrike.exe";

        if (!Directory.Exists(buildDir))
            Directory.CreateDirectory(buildDir);

        // Reset to x86_64 for Windows build
        PlayerSettings.SetArchitecture(NamedBuildTarget.Standalone, 0);

        EditorUserBuildSettings.selectedStandaloneTarget = BuildTarget.StandaloneWindows64;
        EditorUserBuildSettings.standaloneBuildSubtarget = StandaloneBuildSubtarget.Player;

        // Using the same bootstrapper scene as it's the standard entry point
        string[] scenes = { "Assets/BootstrapperScene.unity" };

        UnityEngine.Debug.Log("🏗️ Starting Windows Build (x86_64)...");
        
        BuildPlayerOptions options = new BuildPlayerOptions();
        options.scenes = scenes;
        options.locationPathName = buildPath;
        options.target = BuildTarget.StandaloneWindows64;
        options.options = BuildOptions.None;

        BuildPipeline.BuildPlayer(options);

        UnityEngine.Debug.Log("✅ Windows Build complete! Output: " + buildPath);
    }

}
