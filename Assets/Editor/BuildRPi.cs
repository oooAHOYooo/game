using UnityEditor;
using System.IO;

public class BuildRPi
{
    [MenuItem("Build/Build for Raspberry Pi (ARM64)")]
    public static void BuildForRPi()
    {
        string buildDir = "Builds/RPi";
        string buildPath = buildDir + "/NinjaStrike_ARM64";

        if (!Directory.Exists(buildDir))
            Directory.CreateDirectory(buildDir);

        // Force ARM64 architecture (0 = x86_64, 1 = ARM64)
        PlayerSettings.SetArchitecture(UnityEditor.Build.NamedBuildTarget.Standalone, 1);

        EditorUserBuildSettings.selectedStandaloneTarget = BuildTarget.StandaloneLinux64;
        EditorUserBuildSettings.standaloneBuildSubtarget = StandaloneBuildSubtarget.Player;

        string[] scenes = { "Assets/BootstrapperScene.unity" };

        BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
        buildPlayerOptions.scenes = scenes;
        buildPlayerOptions.locationPathName = buildPath;
        buildPlayerOptions.target = BuildTarget.StandaloneLinux64;
        buildPlayerOptions.options = BuildOptions.None;

        BuildPipeline.BuildPlayer(buildPlayerOptions);

        // Restore to x86_64 so other builds aren't affected
        PlayerSettings.SetArchitecture(UnityEditor.Build.NamedBuildTarget.Standalone, 0);

        UnityEngine.Debug.Log("✅ RPi ARM64 Build complete! Output: " + buildPath);
    }
}
