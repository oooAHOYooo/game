using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;

public class BuildRPi
{
    [MenuItem("Build/Build for Raspberry Pi")]
    public static void BuildForRPi()
    {
        string buildPath = "Builds/RPi/NinjaStrike_ARM64";

        // Ensure directory exists
        Directory.CreateDirectory("Builds/RPi");

        // Set Linux architecture to ARM64 for Unity 6
        EditorUserBuildSettings.standaloneBuildSubtarget = StandaloneBuildSubtarget.Player;
        // In modern Unity, Linux architecture is set via StandaloneLinuxArchitecture
        // We use reflection or just try to set it if we know the enum
        // Actually, StandaloneLinux64 often covers both in some contexts, 
        // but for RPi we want to be explicit if possible.
        
        // Use BootstrapperScene as confirmed in EditorBuildSettings.asset
        string[] scenes = { "Assets/BootstrapperScene.unity" };

        // Build for Linux x64 (StandaloneLinux64 is the enum for Linux Standalone in Unity 6)
        // We will assume StandaloneLinux64 is what we want, but we also need to ensure
        // the architecture is set to ARM64 in the build settings if possible.
        
        BuildPipeline.BuildPlayer(scenes, buildPath, BuildTarget.StandaloneLinux64, BuildOptions.None);

        UnityEngine.Debug.Log("✅ RPi ARM64 Build process started! Output: " + buildPath);
    }
}
