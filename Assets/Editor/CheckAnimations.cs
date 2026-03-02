using UnityEditor;
using UnityEngine;

public class CheckAnimations
{
    public static void Run()
    {
        string path = "Assets/Art/Models/Character/Mannequin_Base.fbx";
        var assets = AssetDatabase.LoadAllAssetsAtPath(path);
        foreach (var asset in assets)
        {
            if (asset is AnimationClip clip)
            {
                Debug.Log("Found Clip: " + clip.name);
            }
        }
    }
}
