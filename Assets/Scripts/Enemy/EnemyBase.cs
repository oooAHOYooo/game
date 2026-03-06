using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// EnemyBase — shared HP, state machine, visual identity, and floating heart UI for all enemy types.
/// Extended by EnemyAI for behaviour.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class EnemyBase : MonoBehaviour
{
    // ── Config (set by EnemySpawner) ──────────────────────────────────────
    public float MaxHP        = 40f;
    public Color AccentColor  = Color.red;
    public int   WaveIndex    = 0;         // which wave spawned this

    // ── State ─────────────────────────────────────────────────────────────
    [HideInInspector] public float CurrentHP;
    [HideInInspector] public bool  IsActiveEnemy = false;  // true = currently engaging
    [HideInInspector] public bool  IsAlive       = true;
    [HideInInspector] public bool  IsFlying      = false;

    // ── References ────────────────────────────────────────────────────────
    protected Rigidbody       _rb;
    protected EnemyAI         _ai;
    protected RagdollHandler  _ragdoll;
    protected Renderer[]      _renderers;
    private   float        _flashTimer;
    private   bool         _isFlashing;

    // ── Floating Heart UI ─────────────────────────────────────────────────
    private const int   ENEMY_HEART_COUNT = 3;
    private const float HEART_SIZE        = 0.18f;
    private List<Image> _heartImages = new List<Image>();
    private Canvas      _heartCanvas;

    private static readonly Color EnemyHeartFull  = new Color(0.85f, 0.1f, 0.15f);
    private static readonly Color EnemyHeartHalf  = new Color(1f, 0.45f, 0.45f);
    private static readonly Color EnemyHeartEmpty = new Color(0.25f, 0.1f, 0.1f, 0.4f);

    // ─────────────────────────────────────────────────────────────────────
    protected virtual void Awake()
    {
        _rb        = GetComponent<Rigidbody>();
        _ai        = GetComponent<EnemyAI>();
        _ragdoll   = GetComponent<RagdollHandler>();
        if (_ragdoll == null) _ragdoll = gameObject.AddComponent<RagdollHandler>();

        _renderers = GetComponentsInChildren<Renderer>();
        CurrentHP  = MaxHP;

        _rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        _rb.linearDamping        = 3f;

        _ragdoll.Setup();
    }

    protected virtual void Start()
    {
        BuildFloatingHearts();
    }

    protected virtual void Update()
    {
        if (_isFlashing) FlashTick();
        UpdateFloatingHearts();
    }

    // ─────────────────────────────────────────────────────────────────────
    // FLOATING HEART UI
    // ─────────────────────────────────────────────────────────────────────
    void BuildFloatingHearts()
    {
        var canvasObj = new GameObject("EnemyHeartCanvas");
        canvasObj.transform.SetParent(transform);
        canvasObj.transform.localPosition = Vector3.up * 2.5f;

        _heartCanvas = canvasObj.AddComponent<Canvas>();
        _heartCanvas.renderMode = RenderMode.WorldSpace;

        var scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 10f;

        var canvasRT = canvasObj.GetComponent<RectTransform>();
        canvasRT.sizeDelta = new Vector2(ENEMY_HEART_COUNT * 1.2f, 1f);
        canvasObj.transform.localScale = Vector3.one * 0.5f;

        float totalWidth = ENEMY_HEART_COUNT * HEART_SIZE + (ENEMY_HEART_COUNT - 1) * 0.04f;
        float startX = -totalWidth * 0.5f;

        for (int i = 0; i < ENEMY_HEART_COUNT; i++)
        {
            var heartObj = new GameObject("EnemyHeart_" + i);
            heartObj.transform.SetParent(canvasObj.transform, false);
            var img = heartObj.AddComponent<Image>();
            img.color = EnemyHeartFull;

            var rt = heartObj.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(startX + i * (HEART_SIZE + 0.04f) + HEART_SIZE * 0.5f, 0f);
            rt.sizeDelta = new Vector2(HEART_SIZE, HEART_SIZE);

            _heartImages.Add(img);
        }
    }

    void UpdateFloatingHearts()
    {
        if (_heartCanvas == null || _heartImages.Count == 0) return;

        // Billboard: face main camera
        var cam = Camera.main;
        if (cam != null)
            _heartCanvas.transform.rotation = Quaternion.LookRotation(_heartCanvas.transform.position - cam.transform.position);

        // Update heart colors
        float hpPerHeart = MaxHP / ENEMY_HEART_COUNT;
        for (int i = 0; i < ENEMY_HEART_COUNT; i++)
        {
            if (_heartImages[i] == null) continue;

            float heartStart = i * hpPerHeart;
            float heartEnd   = (i + 1) * hpPerHeart;
            float heartMid   = heartStart + hpPerHeart * 0.5f;

            if (CurrentHP >= heartEnd)
            {
                _heartImages[i].color = EnemyHeartFull;
                _heartImages[i].transform.localScale = Vector3.one;
            }
            else if (CurrentHP >= heartMid)
            {
                _heartImages[i].color = EnemyHeartHalf;
                _heartImages[i].transform.localScale = Vector3.one * 0.85f;
            }
            else if (CurrentHP > heartStart)
            {
                _heartImages[i].color = Color.Lerp(EnemyHeartEmpty, EnemyHeartHalf, 0.4f);
                _heartImages[i].transform.localScale = Vector3.one * 0.7f;
            }
            else
            {
                _heartImages[i].color = EnemyHeartEmpty;
                _heartImages[i].transform.localScale = Vector3.one * 0.55f;
            }
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    // DAMAGE
    // ─────────────────────────────────────────────────────────────────────
    public void TakeDamage(float amount, Transform attacker)
    {
        if (!IsAlive) return;

        CurrentHP -= amount;
        CurrentHP = Mathf.Max(0, CurrentHP);

        ImpactFeedback.Play(amount, transform.position);

        // Damage flash
        StartCoroutine(DamageFlash());

        if (CurrentHP <= 0)
        {
            Vector3 pushDir = (transform.position - attacker.position).normalized;
            StartCoroutine(Die(pushDir * amount * 0.5f));
        }
    }

    IEnumerator DamageFlash()
    {
        SetEmission(Color.white, 20f);
        yield return new WaitForSeconds(0.08f);
        SetEmission(AccentColor, 4f);
    }

    void SetEmission(Color color, float intensity)
    {
        foreach (var r in _renderers)
            if (r != null && r.material != null)
                GameBootstrapper.SetHDRPEmission(r.material, color, intensity);
    }

    void FlashTick()
    {
        _flashTimer -= Time.deltaTime;
        if (_flashTimer <= 0) { _isFlashing = false; SetEmission(AccentColor, 4f); }
    }

    IEnumerator Die(Vector3 impactForce)
    {
        IsAlive = false;
        IsActiveEnemy = false;

        // Notify wave manager
        var wm = FindAnyObjectByType<WaveManager>();
        if (wm != null) wm.OnEnemyDied(this);

        // Hide hearts
        if (_heartCanvas != null)
            _heartCanvas.gameObject.SetActive(false);

        // Enable Ragdoll
        _rb.constraints = RigidbodyConstraints.None;
        _rb.useGravity = true;
        _ragdoll.EnableRagdoll(true);
        
        // Fling into the sky (Ragdoll firework)
        Vector3 skywardForce = Vector3.up * 40f + (impactForce.normalized + Random.insideUnitSphere).normalized * 15f;
        _rb.AddForce(skywardForce, ForceMode.Impulse);
        _rb.angularVelocity = Random.insideUnitSphere * 25f;
        _ragdoll.ApplyImpact(skywardForce * 0.5f);

        // Fly as a ragdoll for 1.2 seconds, then EXPLODE like a firework!
        yield return new WaitForSeconds(1.2f);

        SpawnFireworkVFX();
        Destroy(gameObject);
    }

    void SpawnFireworkVFX()
    {
        var obj  = new GameObject("FireworkVFX");
        // Spawn it slightly higher than their center
        obj.transform.position = transform.position + Vector3.up; 

        // Main colored explosion
        var ps = obj.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.duration = 1.0f;
        main.loop = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.8f, 1.5f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(10f, 35f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.2f, 0.7f);
        
        // Use a mix of their accent color and gold/white for a celebratory feel
        main.startColor = new ParticleSystem.MinMaxGradient(AccentColor, GameBootstrapper.PaletteGold);
        main.maxParticles = 300;
        main.gravityModifier = 0.6f;

        var emit = ps.emission;
        emit.SetBursts(new[] { new ParticleSystem.Burst(0f, 200) });
        emit.rateOverTime = 0;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 1f;

        // Add trails for that beautiful firework falling effect
        var trails = ps.trails;
        trails.enabled = true;
        trails.ratio = 0.5f; // 50% of particles have trails
        trails.lifetimeMultiplier = 0.4f;
        
        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.trailMaterial = new Material(GameBootstrapper.GetHDRPLitShader());
        renderer.trailMaterial.color = AccentColor;
        GameBootstrapper.SetHDRPEmission(renderer.trailMaterial, AccentColor, 10f);

        ps.Play();
        
        // Quick white flash to simulate the "pop" of the explosion
        var flash = new GameObject("FireworkFlash");
        flash.transform.position = transform.position;
        var light = flash.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = Color.white;
        light.intensity = 500000f; // Brief, massive intensity for HDRP
        light.range = 25f;
        Destroy(flash, 0.1f);

        Destroy(obj, 2.0f);
    }

    // ─────────────────────────────────────────────────────────────────────
    // VISUAL BUILDERS (called by EnemySpawner)
    // ─────────────────────────────────────────────────────────────────────
    public static GameObject BuildFootSoldier(Vector3 spawnPos, int waveIndex)
    {
        return BuildEnemyBase("Grunt", spawnPos, 40f,
            GameBootstrapper.PaletteCrimson, waveIndex, 1.0f);
    }

    public static GameObject BuildShadowArcher(Vector3 spawnPos, int waveIndex)
    {
        return BuildEnemyBase("Elite", spawnPos, 30f,
            GameBootstrapper.PalettePurple, waveIndex, 0.95f);
    }

    public static GameObject BuildBerserker(Vector3 spawnPos, int waveIndex)
    {
        return BuildEnemyBase("Brute", spawnPos, 100f,
            GameBootstrapper.PaletteGold, waveIndex, 1.35f);
    }

    public static GameObject BuildMiniBoss(Vector3 spawnPos, int waveIndex)
    {
        return BuildEnemyBase("Boss", spawnPos, 200f,
            new Color(1f, 0.2f, 0f), waveIndex, 1.6f);
    }

    public static GameObject BuildArsonist(Vector3 spawnPos, int waveIndex)
    {
        return BuildEnemyBase("Arsonist", spawnPos, 50f,
            new Color(1f, 0.4f, 0.05f), waveIndex, 1.05f);
    }

    static GameObject BuildEnemyBase(string typeName, Vector3 pos, float hp, Color accent, int wave, float scale)
    {
        GameObject root;
        #if UNITY_EDITOR
        var modelPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Models/Character/Mannequin_Base.fbx");
        if (modelPrefab != null)
        {
            root = Instantiate(modelPrefab, pos, Quaternion.identity);
            root.name = "Enemy_" + typeName;
            root.transform.localScale = Vector3.one * scale;

            // Apply material color
            var renderers = root.GetComponentsInChildren<Renderer>();
            foreach (var r in renderers)
            {
                var mat = new Material(GameBootstrapper.GetHDRPLitShader());
                mat.color = new Color(0.05f, 0.05f, 0.08f);
                GameBootstrapper.SetHDRPEmission(mat, accent, 1.5f);
                r.material = mat;
            }

            // Animator setup
            var anim = root.GetComponent<Animator>();
            if (anim == null) anim = root.AddComponent<Animator>();
            var animSet = AnimationLibrary.LoadAnimations();
            anim.runtimeAnimatorController = AnimatorControllerBuilder.GetOrCreateController(animSet);
            anim.applyRootMotion = false; // Prevents "pulsing" jitter
        }
        else
        {
            // Fallback to primitive if model missing
            root = new GameObject("Enemy_" + typeName);
            root.transform.position = pos;
            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.transform.SetParent(root.transform);
            body.transform.localPosition = Vector3.up;
            body.transform.localScale = new Vector3(0.6f, 0.8f, 0.6f) * scale;
        }
        #else
        root = new GameObject("Enemy_" + typeName);
        root.transform.position = pos;
        #endif

        // Physics
        var rb = root.GetComponent<Rigidbody>();
        if (rb == null) rb = root.AddComponent<Rigidbody>();
        rb.mass = 60f * scale;
        rb.linearDamping = 3f;
        rb.angularDamping = 5f;
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

        var col = root.GetComponent<CapsuleCollider>();
        if (col == null) col = root.AddComponent<CapsuleCollider>();
        col.height = 2f;
        col.radius = 0.4f;
        col.center = Vector3.up;

        // Components
        var eb = root.GetComponent<EnemyBase>();
        if (eb == null) eb = root.AddComponent<EnemyBase>();
        eb.MaxHP = hp * (1f + wave * 0.2f);
        eb.AccentColor = accent;
        eb.WaveIndex = wave;
        eb.CurrentHP = eb.MaxHP;

        var ai = root.GetComponent<EnemyAI>();
        if (ai == null) ai = root.AddComponent<EnemyAI>();
        ai.EnemyType = typeName;
        ai.Scale = scale;

        // Aura
        BuildEnemyAura(root.transform, accent);

        return root;
    }

    static void CreateEnemyEye(Transform parent, Color color, Vector3 localPos)
    {
        var eye = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        eye.name = "EnemyEye";
        eye.transform.SetParent(parent);
        eye.transform.localPosition = localPos;
        eye.transform.localScale    = Vector3.one * 0.06f;
        Destroy(eye.GetComponent<Collider>());
        var mat = new Material(GameBootstrapper.GetHDRPLitShader());
        mat.color = Color.white;
        GameBootstrapper.SetHDRPEmission(mat, color, 15f);
        eye.GetComponent<Renderer>().material = mat;
    }

    static void CreateEnergyArm(Transform parent, Color accent, Vector3 localPos, float scale)
    {
        var arm = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        arm.name = "EnergyArm";
        arm.transform.SetParent(parent);
        arm.transform.localPosition = localPos;
        arm.transform.localScale    = new Vector3(0.08f * scale, 0.35f * scale, 0.08f * scale);
        arm.transform.localRotation = Quaternion.Euler(0, 0, 90f);
        Destroy(arm.GetComponent<Collider>());
        var mat = new Material(GameBootstrapper.GetHDRPLitShader());
        mat.color = new Color(0.05f, 0.05f, 0.08f);
        GameBootstrapper.SetHDRPEmission(mat, accent, 5f);
        arm.GetComponent<Renderer>().material = mat;
    }

    static void CreateEnemyWeapon(Transform parent, Color accent, float scale, bool bigWeapon)
    {
        var wep = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wep.name = "EnemyWeapon";
        wep.transform.SetParent(parent);
        wep.transform.localPosition = new Vector3(0.7f * scale, 1.2f * scale, 0);
        wep.transform.localScale    = bigWeapon
            ? new Vector3(0.12f * scale, 0.9f * scale, 0.08f * scale)
            : new Vector3(0.06f * scale, 0.7f * scale, 0.05f * scale);
        Destroy(wep.GetComponent<Collider>());
        var mat = new Material(GameBootstrapper.GetHDRPLitShader());
        mat.color = new Color(0.5f, 0.5f, 0.6f);
        GameBootstrapper.SetHDRPEmission(mat, accent, 6f);
        wep.GetComponent<Renderer>().material = mat;
    }

    static void BuildEnemyAura(Transform root, Color color)
    {
        var aura = new GameObject("EnemyAura");
        aura.transform.SetParent(root);
        aura.transform.localPosition = Vector3.up;
        var ps   = aura.AddComponent<ParticleSystem>();
        var main = ps.main;
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        main.loop             = true;
        main.duration         = 1f;
        main.startLifetime    = 0.5f;
        main.startSpeed       = 1.5f;
        main.startSize        = 0.18f;
        main.startColor       = new ParticleSystem.MinMaxGradient(color, Color.black);
        main.simulationSpace  = ParticleSystemSimulationSpace.World;
        main.maxParticles     = 40;

        var em = ps.emission;
        em.rateOverTime = 20f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius    = 0.5f;

        ps.Play();
    }
}
