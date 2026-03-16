using UnityEngine;
using UnityEditor;

/// <summary>
/// NinjaStrike ▶ Build World In Editor
///
/// Generates the full island + village in edit mode so you can inspect the
/// world without hitting Play.  A "GeneratedWorld" root object is created;
/// run "Clear World" to tear it down.
/// </summary>
public static class WorldBuilder
{
    private const string ROOT_NAME = "GeneratedWorld";

    // ── Build ─────────────────────────────────────────────────────────────

    [MenuItem("NinjaStrike/Build World In Editor")]
    public static void BuildWorld()
    {
        ClearWorld();   // tear down any previous preview

        var root = new GameObject(ROOT_NAME);
        Undo.RegisterCreatedObjectUndo(root, "Build World In Editor");

        // ── Island ────────────────────────────────────────────────────────
        var islandObj = new GameObject("IslandGenerator");
        islandObj.transform.SetParent(root.transform);
        var island = islandObj.AddComponent<IslandGenerator>();
        island.Generate();

        // ── Village ───────────────────────────────────────────────────────
        var villageObj = new GameObject("VillageManager");
        villageObj.transform.SetParent(root.transform);
        var village = villageObj.AddComponent<Village>();
        village.Build(Vector3.zero);

        // ── Lighting ──────────────────────────────────────────────────────
        BuildPreviewLighting(root.transform);

        // ── Character preview (T-pose, no game logic) ─────────────────────
        SpawnCharacterPreview(root.transform, new Vector3(-8f, 2f, 0),  GameBootstrapper.PaletteGold,      "P1_Preview");
        SpawnCharacterPreview(root.transform, new Vector3( 8f, 2f, 0),  GameBootstrapper.PaletteGhostBlue, "P2_Preview");

        // ── Focus scene view on the island ────────────────────────────────
        SceneView.lastActiveSceneView?.LookAt(Vector3.up * 8f, Quaternion.Euler(35f, 0f, 0f), 120f);

        EditorUtility.DisplayDialog(
            "World Built",
            "Island, village, and character previews are ready in the scene.\n\n" +
            "Use  NinjaStrike ▶ Clear World  to remove them.",
            "Nice!");

        Debug.Log("[WorldBuilder] Editor world built. Select GeneratedWorld in the hierarchy to explore.");
    }

    [MenuItem("NinjaStrike/Build World In Editor", validate = true)]
    public static bool BuildWorldValidate() => !Application.isPlaying;

    // ── Clear ─────────────────────────────────────────────────────────────

    [MenuItem("NinjaStrike/Clear World")]
    public static void ClearWorld()
    {
        var existing = GameObject.Find(ROOT_NAME);
        if (existing != null)
        {
            Undo.DestroyObjectImmediate(existing);
            Debug.Log("[WorldBuilder] Cleared GeneratedWorld.");
        }
    }

    [MenuItem("NinjaStrike/Clear World", validate = true)]
    public static bool ClearWorldValidate() => GameObject.Find(ROOT_NAME) != null;

    // ── Helpers ───────────────────────────────────────────────────────────

    static void BuildPreviewLighting(Transform parent)
    {
        var sunObj = new GameObject("Sun_Preview");
        sunObj.transform.SetParent(parent);
        sunObj.transform.rotation = Quaternion.Euler(35f, -40f, 0f);
        var light = sunObj.AddComponent<Light>();
        light.type      = LightType.Directional;
        light.color     = new Color(0.9f, 0.85f, 0.7f);
        light.intensity = 1.2f;   // standard intensity — HDRP lux not needed for preview
    }

    static void SpawnCharacterPreview(Transform parent, Vector3 pos, Color color, string name)
    {
        // Try the Mixamo model first
        string folder = "Assets/Art/Models/Character";
        string[] candidates = { "X Bot.fbx", "Y Bot.fbx", "xbot.fbx", "ybot.fbx", "Character.fbx" };
        GameObject modelPrefab = null;
        foreach (var fname in candidates)
        {
            modelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{folder}/{fname}");
            if (modelPrefab != null) break;
        }

        GameObject instance;
        if (modelPrefab != null)
        {
            instance = (GameObject)PrefabUtility.InstantiatePrefab(modelPrefab);
            instance.transform.position = pos;
        }
        else
        {
            // Fallback: simple capsule stand-in
            instance = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            instance.transform.position = pos + Vector3.up;
            var mat = new Material(Shader.Find("HDRP/Lit") ?? Shader.Find("Standard"));
            mat.color = color;
            instance.GetComponent<Renderer>().sharedMaterial = mat;
        }

        instance.name = name;
        instance.transform.SetParent(parent);
        Undo.RegisterCreatedObjectUndo(instance, "Spawn Character Preview");
    }
}
