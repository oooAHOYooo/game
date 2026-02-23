using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using System.Collections.Generic;

/// <summary>
/// GameBootstrapper – Procedurally constructs the entire game scene at runtime.
/// Attach this to a single empty GameObject in OutdoorsScene and hit Play.
/// </summary>
public class GameBootstrapper : MonoBehaviour
{
    // ── Cinematic colour palette (Akira / Blade Runner / Dragon Ball Z) ──
    public static readonly Color PaletteDeepNavy     = new Color(0.05f, 0.07f, 0.18f);
    public static readonly Color PaletteMidnightBlue = new Color(0.08f, 0.12f, 0.30f);
    public static readonly Color PaletteCrimson      = new Color(0.85f, 0.08f, 0.12f);
    public static readonly Color PaletteGold         = new Color(1.00f, 0.78f, 0.10f);
    public static readonly Color PaletteCyan         = new Color(0.10f, 0.90f, 1.00f);
    public static readonly Color PalettePurple       = new Color(0.55f, 0.15f, 0.90f);
    public static readonly Color PaletteGhostBlue    = new Color(0.40f, 0.70f, 1.00f, 0.45f);

    [Header("Runtime References (filled automatically)")]
    public static GameBootstrapper Instance;

    private IslandGenerator _island;
    private Village         _village;
    private GameObject      _player1;
    private GameObject      _player2Ghost;
    private GameObject      _waveManagerObj;
    private GameObject      _cameraRig;
    private GameObject      _uiRoot;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        Application.targetFrameRate = 60;
        QualitySettings.vSyncCount  = 1;

        BuildIsland();
        BuildVillage();
        BuildLighting();
        BuildPlayers();
        BuildCamera();
        BuildUI();
        BuildWaveManager();

        Debug.Log("[GameBootstrapper] Open-world island constructed. Hit Play to test!");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // ISLAND (replaces flat arena)
    // ─────────────────────────────────────────────────────────────────────────
    void BuildIsland()
    {
        var islandObj = new GameObject("IslandGenerator");
        _island = islandObj.AddComponent<IslandGenerator>();
        _island.Generate();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // VILLAGE  (centre of island)
    // ─────────────────────────────────────────────────────────────────────────
    void BuildVillage()
    {
        var villageObj = new GameObject("VillageManager");
        _village = villageObj.AddComponent<Village>();
        _village.Build(Vector3.zero);  // village at island centre
    }

    // ─────────────────────────────────────────────────────────────────────────
    // LIGHTING
    // ─────────────────────────────────────────────────────────────────────────
    void BuildLighting()
    {
        // Ambient — cinematic golden-hour island feel
        RenderSettings.ambientMode  = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.10f, 0.08f, 0.05f);
        RenderSettings.fogColor     = new Color(0.20f, 0.15f, 0.10f);
        RenderSettings.fog          = true;
        RenderSettings.fogMode      = FogMode.ExponentialSquared;
        RenderSettings.fogDensity   = 0.003f;  // lighter fog for open world

        // Sun — warm cinematic sunset
        var sunObj = new GameObject("DirectionalLight_Sun");
        var sunLight = sunObj.AddComponent<Light>();
        sunLight.type      = LightType.Directional;
        sunLight.color     = new Color(1.00f, 0.85f, 0.55f);
        sunLight.intensity = 1f;
        sunObj.transform.rotation = Quaternion.Euler(25f, -30f, 0);
        var hdLight = sunObj.AddComponent<HDAdditionalLightData>();
        hdLight.intensity = 30000f;

        // Cool fill (blue sky bounce)
        var fillObj = new GameObject("DirectionalLight_Fill");
        var fillLight = fillObj.AddComponent<Light>();
        fillLight.type      = LightType.Directional;
        fillLight.color     = new Color(0.40f, 0.55f, 0.90f);
        fillLight.intensity = 0.15f;
        fillObj.transform.rotation = Quaternion.Euler(70f, 120f, 0);
        var hdFill = fillObj.AddComponent<HDAdditionalLightData>();
        hdFill.intensity = 5000f;

        // Cinematic accent point lights around the island
        CreatePointLight(new Vector3( 60f, 15f,  60f), PaletteCrimson, 8000f, 60f);
        CreatePointLight(new Vector3(-60f, 15f, -60f), PaletteCyan,    8000f, 60f);
        CreatePointLight(new Vector3( 60f, 15f, -60f), PaletteGold,    6000f, 50f);
        CreatePointLight(new Vector3(-60f, 15f,  60f), PalettePurple,  6000f, 50f);
        CreatePointLight(Vector3.up * 30f,             PaletteGold,    4000f, 80f);  // overhead warm
    }

    void CreatePointLight(Vector3 pos, Color color, float intensity, float range)
    {
        var obj = new GameObject("PointLight");
        var l   = obj.AddComponent<Light>();
        l.type  = LightType.Point;
        l.color = color;
        obj.transform.position = pos;
        var hd = obj.AddComponent<HDAdditionalLightData>();
        hd.intensity = intensity;
        hd.range     = range;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // PLAYERS
    // ─────────────────────────────────────────────────────────────────────────
    void BuildPlayers()
    {
        // Find terrain height near village centre for spawn
        float spawnY = 3f;
        if (Physics.Raycast(new Vector3(-8f, 100f, 0), Vector3.down, out RaycastHit h1, 200f))
            spawnY = h1.point.y + 0.5f;

        // Player 1 – always active (god-sized ninja on the island)
        _player1 = BuildNinja("Player1", new Vector3(-8f, spawnY, 0), PaletteGold, false, 0);

        float spawn2Y = 3f;
        if (Physics.Raycast(new Vector3(8f, 100f, 0), Vector3.down, out RaycastHit h2, 200f))
            spawn2Y = h2.point.y + 0.5f;

        // Player 2 – ghost if no second controller
        bool hasSecondController = Gamepad.all.Count >= 2;
        _player2Ghost = BuildNinja("Player2", new Vector3(8f, spawn2Y, 0), PaletteGhostBlue, !hasSecondController, 1);
    }

    GameObject BuildNinja(string playerName, Vector3 spawnPos, Color bodyColor, bool isGhost, int playerIndex)
    {
        var root = new GameObject(playerName);
        root.transform.position = spawnPos;

        // ── Body (capsule) ──
        var body    = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        body.name   = "Body";
        body.transform.SetParent(root.transform);
        body.transform.localPosition = new Vector3(0, 1f, 0);
        body.transform.localScale    = new Vector3(0.7f, 0.85f, 0.7f);
        var bodyMat = new Material(GetHDRPLitShader());
        bodyMat.color = new Color(0.06f, 0.06f, 0.10f);
        if (isGhost)
        {
            bodyMat.SetFloat("_SurfaceType", 1); // transparent
            bodyMat.color = bodyColor;
        }
        SetHDRPEmission(bodyMat, bodyColor, isGhost ? 3f : 1.5f);
        body.GetComponent<Renderer>().material = bodyMat;

        // ── Head (sphere) ──
        var head    = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        head.name   = "Head";
        head.transform.SetParent(root.transform);
        head.transform.localPosition = new Vector3(0, 2.05f, 0);
        head.transform.localScale    = new Vector3(0.55f, 0.55f, 0.55f);
        Destroy(head.GetComponent<Collider>());
        head.GetComponent<Renderer>().material = bodyMat;

        // ── Eyes (glowing spheres) ──
        CreateEye(root.transform, bodyColor, new Vector3( 0.12f, 2.08f, 0.22f));
        CreateEye(root.transform, bodyColor, new Vector3(-0.12f, 2.08f, 0.22f));

        // ── Headband ──
        var band = GameObject.CreatePrimitive(PrimitiveType.Cube);
        band.name = "Headband";
        band.transform.SetParent(root.transform);
        band.transform.localPosition = new Vector3(0, 2.05f, 0.19f);
        band.transform.localScale    = new Vector3(0.56f, 0.08f, 0.22f);
        Destroy(band.GetComponent<Collider>());
        var bandMat = new Material(GetHDRPLitShader());
        bandMat.color = PaletteCrimson;
        SetHDRPEmission(bandMat, PaletteCrimson, 4f);
        band.GetComponent<Renderer>().material = bandMat;

        // ── Scarf (elongated cube behind) ──
        var scarf = GameObject.CreatePrimitive(PrimitiveType.Cube);
        scarf.name = "Scarf";
        scarf.transform.SetParent(root.transform);
        scarf.transform.localPosition = new Vector3(0, 1.6f, -0.25f);
        scarf.transform.localScale    = new Vector3(0.4f, 0.5f, 0.08f);
        Destroy(scarf.GetComponent<Collider>());
        var scarfMat = new Material(GetHDRPLitShader());
        scarfMat.color = bodyColor;
        SetHDRPEmission(scarfMat, bodyColor, 2f);
        scarf.GetComponent<Renderer>().material = scarfMat;

        // ── Weapon (sword/staff) ──
        var weaponHolder = new GameObject("WeaponHolder");
        weaponHolder.transform.SetParent(root.transform);
        weaponHolder.transform.localPosition = new Vector3(0.5f, 1.2f, 0);

        var sword = BuildWeapon(weaponHolder.transform, false, bodyColor);

        // ── Physics ──
        var rb         = root.AddComponent<Rigidbody>();
        rb.mass        = 70f;
        rb.linearDamping        = 2f;
        rb.angularDamping = 5f;
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

        var col = root.AddComponent<CapsuleCollider>();
        col.height = 2f;
        col.radius = 0.4f;
        col.center = new Vector3(0, 1f, 0);

        // ── Player Controller Component ──
        var ctrl = root.AddComponent<NinjaController>();
        ctrl.PlayerIndex      = playerIndex;
        ctrl.IsGhost          = isGhost;
        ctrl.BodyColor        = bodyColor;
        ctrl.WeaponHolder     = weaponHolder.transform;
        ctrl.SwordRoot        = sword;

        // ── Health ──
        var health = root.AddComponent<PlayerHealth>();
        health.PlayerIndex = playerIndex;

        // ── Aura VFX (particle system) ──
        BuildAuraVFX(root.transform, bodyColor, isGhost);

        return root;
    }

    void CreateEye(Transform parent, Color color, Vector3 localPos)
    {
        var eye = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        eye.name = "Eye";
        eye.transform.SetParent(parent);
        eye.transform.localPosition = localPos;
        eye.transform.localScale    = new Vector3(0.07f, 0.07f, 0.07f);
        Destroy(eye.GetComponent<Collider>());
        var mat = new Material(GetHDRPLitShader());
        mat.color = Color.white;
        SetHDRPEmission(mat, color, 12f);
        eye.GetComponent<Renderer>().material = mat;
    }

    GameObject BuildWeapon(Transform parent, bool asStaff, Color playerColor)
    {
        var root = new GameObject(asStaff ? "Staff" : "Sword");
        root.transform.SetParent(parent);
        root.transform.localPosition = Vector3.zero;
        root.transform.localRotation = Quaternion.identity;

        if (asStaff)
        {
            // Staff – long pole
            var pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pole.transform.SetParent(root.transform);
            pole.transform.localPosition = new Vector3(0, 0.6f, 0);
            pole.transform.localScale    = new Vector3(0.06f, 0.75f, 0.06f);
            Destroy(pole.GetComponent<Collider>());
            var mat = new Material(GetHDRPLitShader());
            mat.color = new Color(0.3f, 0.15f, 0.05f);
            SetHDRPEmission(mat, playerColor, 3f);
            pole.GetComponent<Renderer>().material = mat;

            // Staff orbs at each end
            CreateStaffOrb(root.transform, new Vector3(0,  1.38f, 0), playerColor);
            CreateStaffOrb(root.transform, new Vector3(0, -0.18f, 0), playerColor);
        }
        else
        {
            // Sword – blade + guard + handle
            var blade = GameObject.CreatePrimitive(PrimitiveType.Cube);
            blade.name = "Blade";
            blade.transform.SetParent(root.transform);
            blade.transform.localPosition = new Vector3(0, 0.55f, 0);
            blade.transform.localScale    = new Vector3(0.05f, 0.7f, 0.008f);
            Destroy(blade.GetComponent<Collider>());
            var bladeMat = new Material(GetHDRPLitShader());
            bladeMat.color = new Color(0.85f, 0.92f, 1.00f);
            SetHDRPEmission(bladeMat, playerColor, 6f);
            blade.GetComponent<Renderer>().material = bladeMat;

            // Guard
            var guard = GameObject.CreatePrimitive(PrimitiveType.Cube);
            guard.name = "Guard";
            guard.transform.SetParent(root.transform);
            guard.transform.localPosition = new Vector3(0, 0.08f, 0);
            guard.transform.localScale    = new Vector3(0.22f, 0.04f, 0.04f);
            Destroy(guard.GetComponent<Collider>());
            var guardMat = new Material(GetHDRPLitShader());
            guardMat.color = PaletteGold;
            SetHDRPEmission(guardMat, PaletteGold, 4f);
            guard.GetComponent<Renderer>().material = guardMat;

            // Handle
            var handle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            handle.name = "Handle";
            handle.transform.SetParent(root.transform);
            handle.transform.localPosition = new Vector3(0, -0.12f, 0);
            handle.transform.localScale    = new Vector3(0.04f, 0.12f, 0.04f);
            Destroy(handle.GetComponent<Collider>());
            var hMat = new Material(GetHDRPLitShader());
            hMat.color = new Color(0.15f, 0.08f, 0.02f);
            handle.GetComponent<Renderer>().material = hMat;
        }

        // Weapon hitbox (trigger)
        var hb = new GameObject("WeaponHitbox");
        hb.transform.SetParent(root.transform);
        hb.transform.localPosition = new Vector3(0, 0.5f, 0);
        var bc = hb.AddComponent<BoxCollider>();
        bc.isTrigger = true;
        bc.size      = asStaff ? new Vector3(0.12f, 1.5f, 0.12f) : new Vector3(0.08f, 1.2f, 0.08f);
        hb.AddComponent<WeaponHitbox>();

        return root;
    }

    void CreateStaffOrb(Transform parent, Vector3 localPos, Color color)
    {
        var orb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        orb.name = "StaffOrb";
        orb.transform.SetParent(parent);
        orb.transform.localPosition = localPos;
        orb.transform.localScale    = new Vector3(0.12f, 0.12f, 0.12f);
        Destroy(orb.GetComponent<Collider>());
        var mat = new Material(GetHDRPLitShader());
        mat.color = color;
        SetHDRPEmission(mat, color, 15f);
        orb.GetComponent<Renderer>().material = mat;
    }

    void BuildAuraVFX(Transform parent, Color color, bool isGhost)
    {
        var auraObj = new GameObject("Aura");
        auraObj.transform.SetParent(parent);
        auraObj.transform.localPosition = new Vector3(0, 1f, 0);
        var ps   = auraObj.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.loop             = true;
        main.duration         = 1f;
        main.startLifetime    = 0.6f;
        main.startSpeed       = 2f;
        main.startSize        = isGhost ? 0.4f : 0.25f;
        main.startColor       = new ParticleSystem.MinMaxGradient(color, Color.white);
        main.simulationSpace  = ParticleSystemSimulationSpace.World;
        main.maxParticles     = 80;

        var emission = ps.emission;
        emission.rateOverTime = 30f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius    = 0.55f;

        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;

        ps.Play();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // CAMERA
    // ─────────────────────────────────────────────────────────────────────────
    void BuildCamera()
    {
        _cameraRig = new GameObject("CameraRig");

        // Player 1 camera – left half (or full if single cam)
        var cam1Obj  = new GameObject("Camera_P1");
        cam1Obj.transform.SetParent(_cameraRig.transform);
        var cam1 = cam1Obj.AddComponent<Camera>();
        cam1.rect = new Rect(0f, 0f, 0.5f, 1f);
        cam1Obj.transform.position = new Vector3(-8f, 20f, -28f);
        cam1Obj.transform.LookAt(Vector3.zero);
        var camCtrl1 = cam1Obj.AddComponent<SplitScreenCamera>();
        camCtrl1.TargetTransform = _player1 != null ? _player1.transform : null;
        camCtrl1.Offset          = new Vector3(0, 14f, -22f);  // higher + further for open world
        cam1Obj.AddComponent<HDAdditionalCameraData>();
        cam1.farClipPlane = 600f;  // see the whole island

        // Player 2 camera – right half
        var cam2Obj  = new GameObject("Camera_P2");
        cam2Obj.transform.SetParent(_cameraRig.transform);
        var cam2 = cam2Obj.AddComponent<Camera>();
        cam2.rect = new Rect(0.5f, 0f, 0.5f, 1f);
        cam2Obj.transform.position = new Vector3(8f, 20f, -28f);
        cam2Obj.transform.LookAt(Vector3.zero);
        var camCtrl2 = cam2Obj.AddComponent<SplitScreenCamera>();
        camCtrl2.TargetTransform = _player2Ghost != null ? _player2Ghost.transform : null;
        camCtrl2.Offset          = new Vector3(0, 14f, -22f);
        cam2Obj.AddComponent<HDAdditionalCameraData>();
        cam2.farClipPlane = 600f;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // UI
    // ─────────────────────────────────────────────────────────────────────────
    void BuildUI()
    {
        _uiRoot = new GameObject("UI");
        var canvas = _uiRoot.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;
        _uiRoot.AddComponent<UnityEngine.UI.CanvasScaler>();
        _uiRoot.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        _uiRoot.AddComponent<GameHUD>();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // WAVE MANAGER
    // ─────────────────────────────────────────────────────────────────────────
    void BuildWaveManager()
    {
        _waveManagerObj = new GameObject("WaveManager");
        var wm = _waveManagerObj.AddComponent<WaveManager>();
        wm.ArenaRoot = _island != null ? _island.IslandRoot : null;
        wm.IslandVillage = _village;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // HELPERS
    // ─────────────────────────────────────────────────────────────────────────
    public static Shader GetHDRPLitShader()
    {
        var s = Shader.Find("HDRP/Lit");
        if (s == null) s = Shader.Find("Standard");
        return s;
    }

    public static void SetMaterialColor(GameObject obj, Color color)
    {
        var mat = new Material(GetHDRPLitShader());
        mat.color = color;
        obj.GetComponent<Renderer>().material = mat;
    }

    public static void SetHDRPEmission(Material mat, Color emissionColor, float intensity)
    {
        mat.EnableKeyword("_EMISSION");
        // HDRP uses _EmissiveColor
        mat.SetColor("_EmissiveColor", emissionColor * intensity);
        // Fallback for Standard shader
        mat.SetColor("_EmissionColor", emissionColor * intensity);
    }
}
