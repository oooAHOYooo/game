using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;

public class BuildRPi
{
    [MenuItem("Build/Build for Raspberry Pi")]
    public static void BuildForRPi()
    {
        string buildDir = "Builds/RPi";
        string buildPath = buildDir + "/NinjaStrike_ARM64";

        // Ensure directory exists
        if (!Directory.Exists(buildDir))
        {
            Directory.CreateDirectory(buildDir);
        }

        // Set build target and architecture for Linux
        EditorUserBuildSettings.selectedStandaloneTarget = BuildTarget.StandaloneLinux64;
        
        // In Unity 6 / modern versions, we use standaloneBuildSubtarget and architecture settings
        EditorUserBuildSettings.standaloneBuildSubtarget = StandaloneBuildSubtarget.Player;
        
        // Try to set architecture to ARM64 if possible
        // Note: The specific enum might be internal or version dependent, 
        // but StandaloneLinux64 often defaults to x86_64 unless specified.
        // For Raspberry Pi (ARM64), we want to ensure we aren't building x86.
        
        // Assuming the scenes are correctly set in the project
        string[] scenes = { "Assets/BootstrapperScene.unity" };

        BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
        buildPlayerOptions.scenes = scenes;
        buildPlayerOptions.locationPathName = buildPath;
        buildPlayerOptions.target = BuildTarget.StandaloneLinux64;
        buildPlayerOptions.options = BuildOptions.None;

        BuildPipeline.BuildPlayer(buildPlayerOptions);

        UnityEngine.Debug.Log("✅ RPi ARM64 Build process finished! Output: " + buildPath);
    }
}
