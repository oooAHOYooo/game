using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// EnemyBase — shared HP, state machine, and visual identity for all enemy types.
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
    protected Rigidbody    _rb;
    protected EnemyAI      _ai;
    protected Renderer[]   _renderers;
    private   float        _flashTimer;
    private   bool         _isFlashing;

    // ─────────────────────────────────────────────────────────────────────
    protected virtual void Awake()
    {
        _rb        = GetComponent<Rigidbody>();
        _ai        = GetComponent<EnemyAI>();
        _renderers = GetComponentsInChildren<Renderer>();
        CurrentHP  = MaxHP;

        _rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        _rb.linearDamping        = 3f;
    }

    protected virtual void Update()
    {
        if (_isFlashing) FlashTick();
    }

    // ─────────────────────────────────────────────────────────────────────
    // DAMAGE
    // ─────────────────────────────────────────────────────────────────────
    public void TakeDamage(float amount, Transform attacker)
    {
        if (!IsAlive) return;

        CurrentHP -= amount;
        CurrentHP = Mathf.Max(0, CurrentHP);

        // Damage flash
        StartCoroutine(DamageFlash());

        if (CurrentHP <= 0)
            StartCoroutine(Die());
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
            if (r.material != null)
                GameBootstrapper.SetHDRPEmission(r.material, color, intensity);
    }

    void FlashTick()
    {
        _flashTimer -= Time.deltaTime;
        if (_flashTimer <= 0) { _isFlashing = false; SetEmission(AccentColor, 4f); }
    }

    // ─────────────────────────────────────────────────────────────────────
    // DEATH
    // ─────────────────────────────────────────────────────────────────────
    IEnumerator Die()
    {
        IsAlive = false;
        IsActiveEnemy = false;

        // Notify wave manager
        var wm = FindAnyObjectByType<WaveManager>();
        if (wm != null) wm.OnEnemyDied(this);

        // Death burst VFX
        SpawnDeathVFX();

        // Spin & shrink
        float t = 0f;
        while (t < 0.5f)
        {
            t += Time.deltaTime;
            transform.localScale = Vector3.Lerp(Vector3.one, Vector3.zero, t / 0.5f);
            transform.Rotate(0, 720f * Time.deltaTime, 0);
            yield return null;
        }

        Destroy(gameObject);
    }

    void SpawnDeathVFX()
    {
        var obj  = new GameObject("DeathVFX");
        obj.transform.position = transform.position + Vector3.up;
        var ps   = obj.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.duration      = 0.4f;
        main.loop          = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.3f, 0.8f);
        main.startSpeed    = new ParticleSystem.MinMaxCurve(2f, 12f);
        main.startSize     = new ParticleSystem.MinMaxCurve(0.1f, 0.4f);
        main.startColor    = new ParticleSystem.MinMaxGradient(AccentColor, Color.white);
        main.maxParticles  = 80;
        main.gravityModifier = 0.4f;

        var emit = ps.emission;
        emit.SetBursts(new[] { new ParticleSystem.Burst(0f, 80) });
        emit.rateOverTime = 0;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius    = 0.5f;

        ps.Play();
        Destroy(obj, 1.5f);
    }

    // ─────────────────────────────────────────────────────────────────────
    // VISUAL BUILDERS (called by EnemySpawner)
    // ─────────────────────────────────────────────────────────────────────
    public static GameObject BuildFootSoldier(Vector3 spawnPos, int waveIndex)
    {
        return BuildEnemyBase("FootSoldier", spawnPos, 40f,
            GameBootstrapper.PaletteCrimson, waveIndex, 1f + waveIndex * 0.12f);
    }

    public static GameObject BuildShadowArcher(Vector3 spawnPos, int waveIndex)
    {
        return BuildEnemyBase("ShadowArcher", spawnPos, 30f,
            GameBootstrapper.PalettePurple, waveIndex, 0.9f + waveIndex * 0.1f);
    }

    public static GameObject BuildBerserker(Vector3 spawnPos, int waveIndex)
    {
        return BuildEnemyBase("Berserker", spawnPos, 80f,
            new Color(1f, 0.45f, 0.05f), waveIndex, 1.2f + waveIndex * 0.12f);
    }

    public static GameObject BuildMiniBoss(Vector3 spawnPos, int waveIndex)
    {
        return BuildEnemyBase("MiniBoss", spawnPos, 150f,
            GameBootstrapper.PaletteGold, waveIndex, 1.5f + waveIndex * 0.12f);
    }

    static GameObject BuildEnemyBase(string typeName, Vector3 pos, float hp, Color accent, int wave, float scale)
    {
        var root = new GameObject("Enemy_" + typeName);
        root.transform.position = pos;

        // Body
        var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        body.name = "Body";
        body.transform.SetParent(root.transform);
        body.transform.localPosition = Vector3.up * 1f;
        body.transform.localScale    = new Vector3(0.6f, 0.8f, 0.6f) * scale;

        var bodyMat = new Material(GameBootstrapper.GetHDRPLitShader());
        bodyMat.color = new Color(0.05f, 0.05f, 0.08f);
        GameBootstrapper.SetHDRPEmission(bodyMat, accent, 1.5f);
        body.GetComponent<Renderer>().material = bodyMat;

        // Head
        var head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        head.name = "Head";
        head.transform.SetParent(root.transform);
        head.transform.localPosition = Vector3.up * (2f * scale);
        head.transform.localScale    = Vector3.one * (0.5f * scale);
        Destroy(head.GetComponent<Collider>());
        head.GetComponent<Renderer>().material = bodyMat;

        // Glowing eyes
        CreateEnemyEye(root.transform, accent, new Vector3( 0.1f * scale, 2.05f * scale, 0.2f * scale));
        CreateEnemyEye(root.transform, accent, new Vector3(-0.1f * scale, 2.05f * scale, 0.2f * scale));

        // Energy arms (emissive lines on arms)
        CreateEnergyArm(root.transform, accent, new Vector3( 0.5f * scale, 1.2f * scale, 0), scale);
        CreateEnergyArm(root.transform, accent, new Vector3(-0.5f * scale, 1.2f * scale, 0), scale);

        // Enemy weapon (varies by type)
        if (typeName == "Berserker" || typeName == "MiniBoss")
            CreateEnemyWeapon(root.transform, accent, scale, true);
        else if (typeName != "ShadowArcher")
            CreateEnemyWeapon(root.transform, accent, scale, false);

        // Physics
        var rb         = root.AddComponent<Rigidbody>();
        rb.mass        = 60f * scale;
        rb.linearDamping        = 3f;
        rb.angularDamping = 5f;

        var col        = root.AddComponent<CapsuleCollider>();
        col.height     = 2f * scale;
        col.radius     = 0.4f * scale;
        col.center     = Vector3.up;

        // Components
        var eb    = root.AddComponent<EnemyBase>();
        eb.MaxHP       = hp * (1f + wave * 0.2f);
        eb.AccentColor = accent;
        eb.WaveIndex   = wave;
        eb.CurrentHP   = eb.MaxHP;

        var ai = root.AddComponent<EnemyAI>();
        ai.EnemyType = typeName;
        ai.Scale     = scale;

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
