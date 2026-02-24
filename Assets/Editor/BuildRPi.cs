using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;

public class BuildRPi
{
    [MenuItem("Build/Build for Raspberry Pi")]
    public static void BuildForRPi()
    {
        string buildPath = "Builds/RPi/NinjaStrike";

        // Ensure directory exists
        Directory.CreateDirectory("Builds/RPi");

        // Get all scenes
        string[] scenes = { "Assets/OutdoorsScene.unity" };

        // Build for Linux
        BuildPipeline.BuildPlayer(scenes, buildPath, BuildTarget.StandaloneLinux64, BuildOptions.Development);

        UnityEngine.Debug.Log("✅ Build process started! Output: " + buildPath);
    }
}
