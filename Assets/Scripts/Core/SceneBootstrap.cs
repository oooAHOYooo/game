using UnityEngine;

/// <summary>
/// SceneBootstrap — attach to ONE empty GameObject in OutdoorsScene.
/// This is the only manual step required to run the entire game.
/// Everything else is created procedurally at runtime.
/// </summary>
public class SceneBootstrap : MonoBehaviour
{
    void Awake()
    {
        // Ensure only one bootstrapper exists
        var existing = FindObjectsByType<GameBootstrapper>(FindObjectsSortMode.None);
        if (existing.Length == 0)
        {
            var go = new GameObject("GameBootstrapper");
            go.AddComponent<GameBootstrapper>();
        }
    }
}
