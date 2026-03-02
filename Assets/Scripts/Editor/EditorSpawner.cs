#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public class EditorSpawner : MonoBehaviour
{
    [MenuItem("NinjaStrike/Spawn Game Visuals in Editor")]
    public static void SpawnGameVisuals()
    {
        var bootstrapperObj = GameObject.Find("SceneBootstrap");
        if (bootstrapperObj == null)
        {
            Debug.LogError("Could not find SceneBootstrap in scene. Please add it first.");
            return;
        }

        // SceneBootstrap creates GameBootstrapper at runtime. We need to add it temporarily in Edit mode.
        var bootstrapper = bootstrapperObj.GetComponent<GameBootstrapper>();
        bool addedTemporarily = false;
        if (bootstrapper == null)
        {
            bootstrapper = bootstrapperObj.AddComponent<GameBootstrapper>();
            addedTemporarily = true;
        }

        ClearGameVisuals();

        // Use Reflection to invoke private Bootstrapper methods
        var type = bootstrapper.GetType();
        
        type.GetMethod("BuildIsland", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).Invoke(bootstrapper, null);
        type.GetMethod("BuildVillage", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).Invoke(bootstrapper, null);
        type.GetMethod("BuildLighting", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).Invoke(bootstrapper, null);
        type.GetMethod("BuildPlayers", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).Invoke(bootstrapper, null);
        type.GetMethod("BuildCamera", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).Invoke(bootstrapper, null);

        var mainCam = GameObject.Find("Main Camera");
        if (mainCam != null) mainCam.SetActive(false);
        
        if (addedTemporarily)
        {
            DestroyImmediate(bootstrapper);
        }

        // Spawned elements might be marked DontDestroyOnLoad during Play, but in editor they stay in scene
        Debug.Log("Spawned Game Visuals for Editor View.");
    }

    [MenuItem("NinjaStrike/Clear Game Visuals")]
    public static void ClearGameVisuals()
    {
        // Clean up previous test spawns if any
        DestroyImmediate(GameObject.Find("IslandGenerator"));
        DestroyImmediate(GameObject.Find("VillageManager"));
        DestroyImmediate(GameObject.Find("DirectionalLight_Sun"));
        DestroyImmediate(GameObject.Find("DirectionalLight_Fill"));
        DestroyImmediate(GameObject.Find("GlobalVolume"));
        DestroyImmediate(GameObject.Find("Player1"));
        DestroyImmediate(GameObject.Find("Player2"));
        DestroyImmediate(GameObject.Find("CameraRig"));
        DestroyImmediate(GameObject.Find("UI"));
        DestroyImmediate(GameObject.Find("WaveManager"));

        var mainCam = GameObject.Find("Main Camera");
        if (mainCam != null) mainCam.SetActive(true); // Re-enable main camera if it was disabled

        Debug.Log("Cleared Editor Game Visuals.");
    }
}
#endif
