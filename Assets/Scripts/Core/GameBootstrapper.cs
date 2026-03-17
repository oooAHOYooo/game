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
    public static readonly Color PaletteMagenta      = new Color(1.00f, 0.00f, 1.00f);

    [Header("Settings")]
    public bool skipStartMenu = false;

    [Header("Runtime References (filled automatically)")]
    public static GameBootstrapper Instance;
    public static bool IsTwoPlayerMode = true;  // 1-player by default uses AI companion

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

        // Load animations early so they're available for character builds
        AnimationLibrary.DebugListAnimations();

        BuildIsland();
        BuildVillage();
        BuildLighting();
        BuildPlayers();
        BuildCamera();
        BuildUI();
        BuildWaveManager();

        // ─── Systems ───────────────────────────────────────────────────────
        var systemsObj = new GameObject("Systems");
        systemsObj.transform.SetParent(transform);
        systemsObj.AddComponent<ImpactFeedback>();
        systemsObj.AddComponent<SoundManager>();
        systemsObj.AddComponent<DayNightCycle>();
        systemsObj.AddComponent<ComboSystem>();
        systemsObj.AddComponent<VillageFireSystem>();

        BuildMythologicalObjects();


        // ─── Menus ───────────────────────────────────────────────────────
        if (FindAnyObjectByType<PauseMenuManager>() == null)
        {
            var pauseObj = new GameObject("PauseMenuManager");
            pauseObj.transform.SetParent(transform);
            pauseObj.AddComponent<PauseMenuManager>();
        }

        if (skipStartMenu)
        {
            StartGame();
        }
        else if (FindAnyObjectByType<MenuManager>() == null)
        {
            var menuObj = new GameObject("MenuManager");
            menuObj.transform.SetParent(transform);
            menuObj.AddComponent<MenuManager>();
        }

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

    void BuildMythologicalObjects()
    {
        // 1. The Divine Goal (at the heart of the village)
        var goalObj = new GameObject("BallGoal");
        goalObj.transform.position = new Vector3(0, 0, 0); // Village center
        goalObj.AddComponent<BallGoal>();

        // 2. The Mythological Ball (Spawns in a dramatic pedestal or just high up)
        var ballObj = new GameObject("MythologicalBall");
        ballObj.transform.position = new Vector3(0, 20f, 15f); // High up, so it falls in!
        var col = ballObj.AddComponent<SphereCollider>();
        col.radius = 0.5f;
        ballObj.AddComponent<MythologicalBall>();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // LIGHTING
    // ─────────────────────────────────────────────────────────────────────────

    void BuildLighting()
    {
        // Ambient — Dusty, red desert atmosphere
        RenderSettings.ambientMode  = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.05f, 0.1f, 0.2f); // Deep blue ambient
        RenderSettings.fogColor     = new Color(0.05f, 0.15f, 0.25f); // Teal/blue fog
        RenderSettings.fog          = true;
        RenderSettings.fogMode      = FogMode.ExponentialSquared;
        RenderSettings.fogDensity   = 0.003f; // Slightly thicker fog

        // Sun — High intensity for "glow" look
        var sunObj = new GameObject("DirectionalLight_Sun");
        var sunLight = sunObj.AddComponent<Light>();
        sunLight.type      = LightType.Directional;
        sunLight.color     = new Color(0.6f, 0.8f, 1.0f); // Cool cyan sun
        sunLight.intensity = 100000f; // Significantly higher for HDRP bloom targets
        sunObj.transform.rotation = Quaternion.Euler(35f, -40f, 0);
        var hdSun = sunObj.AddComponent<HDAdditionalLightData>();

        // Fill Light - Warm orange bounce
        var fillObj = new GameObject("DirectionalLight_Fill");
        var fillLight = fillObj.AddComponent<Light>();
        fillLight.type      = LightType.Directional;
        fillLight.color     = new Color(0.8f, 0.2f, 0.6f); // Magenta fill
        fillLight.intensity = 20000f;
        fillObj.transform.rotation = Quaternion.Euler(60f, 150f, 0);
        var hdFill = fillObj.AddComponent<HDAdditionalLightData>();

        // Procedural Post-Processing (Bloom & Color)
        var volObj = new GameObject("GlobalVolume");
        var vol = volObj.AddComponent<Volume>();
        vol.isGlobal = true;
        var profile = ScriptableObject.CreateInstance<VolumeProfile>();
        
        var bloom = profile.Add<Bloom>();
        bloom.intensity.Override(1.5f);
        bloom.threshold.Override(0.5f);
        
        var colorAdjust = profile.Add<ColorAdjustments>();
        colorAdjust.saturation.Override(30f);
        colorAdjust.contrast.Override(20f);

        // -- Cinematic Additions --
        var vignette = profile.Add<Vignette>();
        vignette.intensity.Override(0.20f);
        vignette.smoothness.Override(0.8f);

        var chromatic = profile.Add<ChromaticAberration>();
        chromatic.intensity.Override(0.3f);
        
        var grain = profile.Add<FilmGrain>();
        grain.type.Override(FilmGrainLookup.Thin1);
        grain.intensity.Override(0.15f);
        
        vol.profile = profile;


        // Add Atmospheric Ash/Embers
        BuildAtmosphericParticles();
    }

    void BuildAtmosphericParticles()
    {
        var ash = new GameObject("AtmosphericAsh");
        // Center it roughly in the sky above the island
        ash.transform.position = new Vector3(0, 40f, 0); 

        var ps = ash.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.duration = 10f;
        main.loop = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(5f, 15f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.5f, 2f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.15f);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.4f, 0.1f, 0.8f), new Color(0.1f, 0.1f, 0.1f, 0.5f));
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 1000;
        main.gravityModifier = 0.05f;

        var emission = ps.emission;
        emission.rateOverTime = 100f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(200f, 30f, 200f);

        // Add some noise to make them flutter like real ash
        var noise = ps.noise;
        noise.enabled = true;
        noise.strength = 1.5f;
        noise.frequency = 0.5f;
        noise.scrollSpeed = 0.5f;

        ps.Play();
    }

    void CreatePointLight(Vector3 pos, Color color, float intensity, float range)
    {
        var obj = new GameObject("PointLight");
        var l   = obj.AddComponent<Light>();
        l.type  = LightType.Point;
        l.color = color;
        l.intensity = intensity;
        obj.transform.position = pos;
        var hd = obj.AddComponent<HDAdditionalLightData>();
        hd.range     = range;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // PLAYERS
    // ─────────────────────────────────────────────────────────────────────────
    void BuildPlayers()
    {
        float spawnY = 300f;

        // Try animated Mixamo character first, fall back to primitives.
        // Editor:   uses AssetDatabase (no Resources overhead).
        // Runtime:  uses Resources.Load from Assets/Resources/Characters/.
        //           The prefab is created by  NinjaStrike ▶ Setup Animated Characters
        //           in the editor menu — run it once after importing the Mixamo FBX.
        #if UNITY_EDITOR
        _player1 = TryBuildAnimatedNinja("Player1", new Vector3(-8f, spawnY, 0), PaletteGold, false, 0)
                ?? BuildNinja("Player1", new Vector3(-8f, spawnY, 0), PaletteGold, false, 0);
        // Player 2: in 1-player mode, isGhost=true (AI); in 2-player mode, isGhost=false (human input)
        bool player2IsGhost = !IsTwoPlayerMode;
        Color player2Color = player2IsGhost ? new Color(0.8f, 0.8f, 1f, 0.8f) : PaletteGhostBlue;  // lighter color for AI
        _player2Ghost = TryBuildAnimatedNinja("Player2", new Vector3(8f, spawnY, 0), player2Color, player2IsGhost, 1)
                     ?? BuildNinja("Player2", new Vector3(8f, spawnY, 0), player2Color, player2IsGhost, 1);
        #else
        _player1 = TryBuildAnimatedNinjaRuntime("Player1", new Vector3(-8f, spawnY, 0), PaletteGold, false, 0)
                ?? BuildNinja("Player1", new Vector3(-8f, spawnY, 0), PaletteGold, false, 0);
        bool player2IsGhost = !IsTwoPlayerMode;
        Color player2Color = player2IsGhost ? new Color(0.8f, 0.8f, 1f, 0.8f) : PaletteGhostBlue;
        _player2Ghost = TryBuildAnimatedNinjaRuntime("Player2", new Vector3(8f, spawnY, 0), player2Color, player2IsGhost, 1)
                     ?? BuildNinja("Player2", new Vector3(8f, spawnY, 0), player2Color, player2IsGhost, 1);
        #endif
    }

    /// <summary>
    /// Runtime (build) version of TryBuildAnimatedNinja.
    /// Loads the player prefab created by the "NinjaStrike/Setup Animated Characters"
    /// editor menu item from Assets/Resources/Characters/PlayerAnimated.prefab.
    /// Returns null if the prefab hasn't been created yet (falls back to primitives).
    /// </summary>
    GameObject TryBuildAnimatedNinjaRuntime(string playerName, Vector3 spawnPos, Color bodyColor, bool isGhost, int playerIndex)
    {
        var modelPrefab = Resources.Load<GameObject>("Characters/PlayerAnimated");
        if (modelPrefab == null) return null;

        var root = Instantiate(modelPrefab, spawnPos, Quaternion.identity);
        root.name = playerName;

        var animSet = Resources.Load<RuntimeAnimatorController>("Animator/PlayerController");

        var animator = root.GetComponent<Animator>();
        if (animator == null) animator = root.AddComponent<Animator>();
        if (animSet != null) animator.runtimeAnimatorController = animSet;
        animator.applyRootMotion = false;

        var rb = root.GetComponent<Rigidbody>();
        if (rb == null) rb = root.AddComponent<Rigidbody>();
        rb.mass = 70f; rb.linearDamping = 2f; rb.angularDamping = 5f;
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

        var col = root.GetComponent<CapsuleCollider>();
        if (col == null) col = root.AddComponent<CapsuleCollider>();
        col.height = 2f; col.radius = 0.4f; col.center = new Vector3(0, 1f, 0);

        var ctrl = root.GetComponent<NinjaController>();
        if (ctrl == null) ctrl = root.AddComponent<NinjaController>();
        ctrl.PlayerIndex = playerIndex;
        ctrl.IsGhost     = isGhost;
        ctrl.BodyColor   = bodyColor;

        var weaponHolder = new GameObject("WeaponHolder").transform;
        weaponHolder.SetParent(root.transform);
        weaponHolder.localPosition = new Vector3(0.5f, 1.2f, 0);
        ctrl.WeaponHolder = weaponHolder;
        ctrl.SwordRoot    = BuildWeapon(weaponHolder, false, bodyColor);

        root.AddComponent<EnvironmentInteractor>();
        var health = root.AddComponent<PlayerHealth>();
        health.PlayerIndex = playerIndex;

        Debug.Log($"[GameBootstrapper] Built animated ninja {playerName} from Resources prefab");
        return root;
    }

    /// <summary>
    /// Apply branded player materials (Gold for P1, Cyan for P2)
    /// </summary>
    void ApplyPlayerBrandMaterial(GameObject characterRoot, int playerIndex)
    {
        #if UNITY_EDITOR
        string materialName = playerIndex == 0 ? "mat_P1_Gold" : "mat_P2_Cyan";
        var material = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>($"Assets/Art/Materials/{materialName}.mat");

        if (material != null)
        {
            // Apply to all mesh renderers
            var renderers = characterRoot.GetComponentsInChildren<Renderer>();
            foreach (var renderer in renderers)
            {
                renderer.material = material;
            }
            Debug.Log($"Applied {materialName} to {characterRoot.name}");
        }
        else
        {
            Debug.LogWarning($"Material {materialName} not found. Run 'NinjaStrike/Create Player Materials' from menu.");
        }
        #endif
    }

    #if UNITY_EDITOR
    // ── Mixamo model filenames to try in priority order ───────────────────
    // Drop any of these into Assets/Art/Models/Character/ and it will be used.
    private static readonly string[] MixamoModelNames =
    {
        "X Bot.fbx", "Y Bot.fbx", "xbot.fbx", "ybot.fbx",
        "Mannequin_Base.fbx", "Mannequin.fbx", "Character.fbx",
    };

    /// <summary>
    /// Find the first available Mixamo model in the Character folder.
    /// </summary>
    static GameObject LoadMixamoModel()
    {
        string folder = "Assets/Art/Models/Character";
        foreach (var name in MixamoModelNames)
        {
            var go = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>($"{folder}/{name}");
            if (go != null)
            {
                Debug.Log($"[Mixamo] Using model: {name}");
                return go;
            }
        }
        // Last resort: grab the first FBX in the folder
        var guids = UnityEditor.AssetDatabase.FindAssets("t:Model", new[] { folder });
        foreach (var g in guids)
        {
            var go = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(UnityEditor.AssetDatabase.GUIDToAssetPath(g));
            if (go != null) return go;
        }
        return null;
    }

    /// <summary>
    /// Find a bone by checking Mixamo-prefixed names and bare names.
    /// Mixamo rigs name bones as "mixamorig:RightHand" — the postprocessor strips
    /// the prefix at import time, but we handle both for safety.
    /// </summary>
    static Transform FindMixamoBone(Transform root, params string[] bareNames)
    {
        foreach (var t in root.GetComponentsInChildren<Transform>())
        {
            string lower = t.name.ToLower().Replace("mixamorig:", "");
            foreach (var name in bareNames)
                if (lower == name.ToLower()) return t;
        }
        return null;
    }

    /// <summary>
    /// Try to build an animated character using a Mixamo T-pose model.
    /// Returns null if no model can be loaded (falls back to primitives).
    /// </summary>
    GameObject TryBuildAnimatedNinja(string playerName, Vector3 spawnPos, Color bodyColor, bool isGhost, int playerIndex)
    {
        var modelPrefab = LoadMixamoModel();
        if (modelPrefab == null)
        {
            Debug.LogWarning($"[Mixamo] No character FBX found in Assets/Art/Models/Character/, falling back to primitives for {playerName}");
            return null;
        }

        var root = Instantiate(modelPrefab, spawnPos, Quaternion.identity);
        root.name = playerName;

        ApplyPlayerBrandMaterial(root, playerIndex);

        var animSet = AnimationLibrary.LoadAnimations();

        var animator = root.GetComponent<Animator>();
        if (animator == null) animator = root.AddComponent<Animator>();
        animator.runtimeAnimatorController =
            AnimatorControllerBuilder.CreateController(animSet, "Assets/Art/Animator/PlayerController.controller");
        animator.applyRootMotion = false;

        var rb = root.GetComponent<Rigidbody>();
        if (rb == null) rb = root.AddComponent<Rigidbody>();
        rb.mass = 70f;
        rb.linearDamping = 2f;
        rb.angularDamping = 5f;
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

        var col = root.GetComponent<CapsuleCollider>();
        if (col == null) col = root.AddComponent<CapsuleCollider>();
        col.height = 2f;
        col.radius = 0.4f;
        col.center = new Vector3(0, 1f, 0);

        var ctrl = root.GetComponent<NinjaController>();
        if (ctrl == null) ctrl = root.AddComponent<NinjaController>();
        ctrl.PlayerIndex = playerIndex;
        ctrl.IsGhost     = isGhost;
        ctrl.BodyColor   = bodyColor;

        // ── Weapon holder — attach to Mixamo right hand bone ─────────────
        Transform hand = FindMixamoBone(root.transform, "RightHand", "Hand_R", "hand_r");

        var weaponHolder = root.transform.Find("WeaponHolder");
        if (weaponHolder == null)
        {
            weaponHolder = new GameObject("WeaponHolder").transform;
            weaponHolder.SetParent(hand != null ? hand : root.transform);
            weaponHolder.localPosition = Vector3.zero;
            weaponHolder.localRotation = Quaternion.Euler(90, 0, 0);
        }
        ctrl.WeaponHolder = weaponHolder;
        ctrl.SwordRoot    = BuildWeapon(weaponHolder, false, bodyColor);

        root.AddComponent<EnvironmentInteractor>();
        var health = root.AddComponent<PlayerHealth>();
        health.PlayerIndex = playerIndex;

        Debug.Log($"[GameBootstrapper] Built animated ninja {playerName} using Mixamo T-pose model");
        return root;
    }
    #endif

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
        if (Application.isPlaying) Destroy(head.GetComponent<Collider>()); else DestroyImmediate(head.GetComponent<Collider>());
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
        if (Application.isPlaying) Destroy(band.GetComponent<Collider>()); else DestroyImmediate(band.GetComponent<Collider>());
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
        if (Application.isPlaying) Destroy(scarf.GetComponent<Collider>()); else DestroyImmediate(scarf.GetComponent<Collider>());
        var scarfMat = new Material(GetHDRPLitShader());
        scarfMat.color = bodyColor;
        SetHDRPEmission(scarfMat, bodyColor, 2f);
        scarf.GetComponent<Renderer>().material = scarfMat;

        // ── Arms (capsules) ──
    CreateLimb(root.transform, "ArmL", new Vector3(-0.45f, 1.35f, 0), new Vector3(0.18f, 0.45f, 0.18f), bodyMat);
    CreateLimb(root.transform, "ArmR", new Vector3( 0.45f, 1.35f, 0), new Vector3(0.18f, 0.45f, 0.18f), bodyMat);

    // ── Legs (capsules) ──
    CreateLimb(root.transform, "LegL", new Vector3(-0.22f, 0.45f, 0), new Vector3(0.22f, 0.5f, 0.22f), bodyMat);
    CreateLimb(root.transform, "LegR", new Vector3( 0.22f, 0.45f, 0), new Vector3(0.22f, 0.5f, 0.22f), bodyMat);

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
        root.AddComponent<EnvironmentInteractor>();
        ctrl.PlayerIndex      = playerIndex;
        ctrl.IsGhost          = isGhost;
        ctrl.BodyColor        = bodyColor;
        ctrl.WeaponHolder     = weaponHolder.transform;
        ctrl.SwordRoot        = sword;

        // ── Health ──
        var health = root.AddComponent<PlayerHealth>();
        health.PlayerIndex = playerIndex;

        // ── Aura VFX (particle system) ──
        // BuildAuraVFX(root.transform, bodyColor, isGhost);

        return root;
    }

    void CreateEye(Transform parent, Color color, Vector3 localPos)
    {
        var eye = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        eye.name = "Eye";
        eye.transform.SetParent(parent);
        eye.transform.localPosition = localPos;
        eye.transform.localScale    = new Vector3(0.07f, 0.07f, 0.07f);
        if (Application.isPlaying) Destroy(eye.GetComponent<Collider>()); else DestroyImmediate(eye.GetComponent<Collider>());
        var mat = new Material(GetHDRPLitShader());
        mat.color = Color.white;
        SetHDRPEmission(mat, color, 12f);
        eye.GetComponent<Renderer>().material = mat;
    }

    void CreateLimb(Transform parent, string name, Vector3 localPos, Vector3 scale, Material mat)
    {
        var limb = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        limb.name = name;
        limb.transform.SetParent(parent);
        limb.transform.localPosition = localPos;
        limb.transform.localScale    = scale;
        if (Application.isPlaying) Destroy(limb.GetComponent<Collider>()); else DestroyImmediate(limb.GetComponent<Collider>());
        limb.GetComponent<Renderer>().material = mat;
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
            if (Application.isPlaying) Destroy(pole.GetComponent<Collider>()); else DestroyImmediate(pole.GetComponent<Collider>());
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
            blade.transform.localPosition = new Vector3(0, 0.5f, 0);
            blade.transform.localScale    = new Vector3(0.045f, 0.63f, 0.0072f);
            if (Application.isPlaying) Destroy(blade.GetComponent<Collider>()); else DestroyImmediate(blade.GetComponent<Collider>());
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
            if (Application.isPlaying) Destroy(guard.GetComponent<Collider>()); else DestroyImmediate(guard.GetComponent<Collider>());
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
            if (Application.isPlaying) Destroy(handle.GetComponent<Collider>()); else DestroyImmediate(handle.GetComponent<Collider>());
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
        if (Application.isPlaying) Destroy(orb.GetComponent<Collider>()); else DestroyImmediate(orb.GetComponent<Collider>());
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
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
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

        if (IsTwoPlayerMode)
        {
            // Split-screen: Player 1 camera – left half, Player 2 camera – right half
            var cam1Obj  = new GameObject("Camera_P1");
            cam1Obj.transform.SetParent(_cameraRig.transform);
            var cam1 = cam1Obj.AddComponent<Camera>();
            cam1.rect = new Rect(0f, 0f, 0.5f, 1f);
            cam1Obj.transform.position = new Vector3(-8f, 20f, -28f);
            cam1Obj.transform.LookAt(Vector3.zero);
            var camCtrl1 = cam1Obj.AddComponent<SplitScreenCamera>();
            camCtrl1.TargetTransform = _player1 != null ? _player1.transform : null;
            camCtrl1.Distance        = -GameSettings.CameraOffset.z;
            camCtrl1.HeightOffset    = GameSettings.CameraOffset.y;
            camCtrl1.SideOffset      = GameSettings.CameraOffset.x;
            cam1Obj.AddComponent<HDAdditionalCameraData>();
            cam1.farClipPlane = 600f;

            var cam2Obj  = new GameObject("Camera_P2");
            cam2Obj.transform.SetParent(_cameraRig.transform);
            var cam2 = cam2Obj.AddComponent<Camera>();
            cam2.rect = new Rect(0.5f, 0f, 0.5f, 1f);
            cam2Obj.transform.position = new Vector3(8f, 20f, -28f);
            cam2Obj.transform.LookAt(Vector3.zero);
            var camCtrl2 = cam2Obj.AddComponent<SplitScreenCamera>();
            camCtrl2.TargetTransform = _player2Ghost != null ? _player2Ghost.transform : null;
            camCtrl2.Distance        = -GameSettings.CameraOffset.z;
            camCtrl2.HeightOffset    = GameSettings.CameraOffset.y;
            camCtrl2.SideOffset      = GameSettings.CameraOffset.x;
            cam2Obj.AddComponent<HDAdditionalCameraData>();
            cam2.farClipPlane = 600f;
        }
        else
        {
            // Single-screen: Player 1 camera – full screen, positioned between P1 and companion
            var cam1Obj  = new GameObject("Camera_P1");
            cam1Obj.transform.SetParent(_cameraRig.transform);
            var cam1 = cam1Obj.AddComponent<Camera>();
            cam1.rect = new Rect(0f, 0f, 1f, 1f);  // Full screen
            cam1Obj.transform.position = new Vector3(0f, 20f, -28f);  // Center between both players
            cam1Obj.transform.LookAt(Vector3.zero);
            var camCtrl1 = cam1Obj.AddComponent<SplitScreenCamera>();
            camCtrl1.TargetTransform = _player1 != null ? _player1.transform : null;
            camCtrl1.Distance        = -GameSettings.CameraOffset.z;
            camCtrl1.HeightOffset    = GameSettings.CameraOffset.y;
            camCtrl1.SideOffset      = 0f;  // Center camera, no side offset
            cam1Obj.AddComponent<HDAdditionalCameraData>();
            cam1.farClipPlane = 600f;
        }
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
        var hud = _uiRoot.AddComponent<GameHUD>();
        hud.gameObject.SetActive(false); // Hide HUD initially, StartGame() will enable it
        _uiRoot.AddComponent<RuntimeTweakUI>();
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

    public static void SpawnAlert(Transform parent, bool isDanger = false)
    {
        var alert = new GameObject("AlertExclamation");
        alert.transform.SetParent(parent);
        alert.transform.localPosition = new Vector3(0, 3f, 0);
        
        // "!" — two spheres (top long, bottom small)
        var top = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        top.transform.SetParent(alert.transform);
        top.transform.localPosition = new Vector3(0, 0.4f, 0);
        top.transform.localScale = new Vector3(0.15f, 0.35f, 0.15f);
        Destroy(top.GetComponent<Collider>());
        
        var dot = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        dot.transform.SetParent(alert.transform);
        dot.transform.localPosition = new Vector3(0, -0.2f, 0);
        dot.transform.localScale = new Vector3(0.18f, 0.18f, 0.18f);
        Destroy(dot.GetComponent<Collider>());

        var mat = new Material(GetHDRPLitShader());
        Color alertColor = isDanger ? PaletteCrimson : PaletteGold;
        mat.color = alertColor;
        SetHDRPEmission(mat, alertColor, 15f);
        top.GetComponent<Renderer>().sharedMaterial = mat;
        dot.GetComponent<Renderer>().sharedMaterial = mat;

        // Animated "pop"
        alert.transform.localScale = Vector3.zero;
        if (Instance != null) Instance.StartCoroutine(AnimateAlert(alert));
    }

    private static System.Collections.IEnumerator AnimateAlert(GameObject obj)
    {
        float t = 0;
        while (t < 1f) {
            t += Time.deltaTime * 5f;
            obj.transform.localScale = Vector3.one * Mathf.Lerp(0, 1.2f, t);
            yield return null;
        }
        yield return new WaitForSeconds(1.5f);
        t = 0;
        while (t < 1f) {
            t += Time.deltaTime * 5f;
            obj.transform.localScale = Vector3.one * Mathf.Lerp(1.2f, 0, t);
            yield return null;
        }
        Destroy(obj);
    }

    // --- Menu Actions ---
    public void StartGame()
    {
        var menu = FindAnyObjectByType<MenuManager>();
        if (menu != null) menu.HideMenu();
        
        // Show HUD
        var hud = FindAnyObjectByType<GameHUD>(FindObjectsInactive.Include);
        if (hud != null) 
        {
            hud.gameObject.SetActive(true);
            hud.ShowIntroText();
        }
        
        // Optional: lock cursor and reset time scale just to be safe
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
        Time.timeScale = 1f;

        Debug.Log("[GameBootstrapper] Game Started!");
    }

    public void QuitGame()
    {
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }
}
