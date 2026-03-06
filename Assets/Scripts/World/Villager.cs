using UnityEngine;

/// <summary>
/// Villager — tiny tribal person (Gulliver-scale ~0.15m tall).
/// Worships the god-players, flees from enemies, celebrates wave clears.
/// </summary>
public class Villager : MonoBehaviour
{
    // ── Config ────────────────────────────────────────────────────────────
    public float MoveSpeed      = 0.4f;
    public float WorshipRadius  = 8f;     // how close before they worship
    public float FleeRadius     = 6f;     // how close before they flee from enemies
    public float WanderRadius   = 12f;

    // ── State ─────────────────────────────────────────────────────────────
    private enum VillagerState { Wandering, Worshipping, Fleeing, Celebrating }
    private VillagerState _state = VillagerState.Wandering;

    private Transform _targetPlayer;
    private Vector3   _wanderTarget;
    private float     _stateTimer;
    private bool      _isPanicking;
    private float     _celebrateTimer;

    // Worship Synergy
    public bool IsWorshipping => _state == VillagerState.Worshipping && !_isPanicking;

    // ── Colours ───────────────────────────────────────────────────────────
    static readonly Color SkinTone1 = new Color(0.55f, 0.38f, 0.22f);
    static readonly Color SkinTone2 = new Color(0.72f, 0.52f, 0.32f);
    static readonly Color SkinTone3 = new Color(0.40f, 0.28f, 0.16f);
    static readonly Color ClothRed  = new Color(0.75f, 0.15f, 0.10f);
    static readonly Color ClothBlue = new Color(0.12f, 0.30f, 0.60f);
    static readonly Color ClothGreen = new Color(0.18f, 0.50f, 0.15f);

    // ─────────────────────────────────────────────────────────────────────
    public static Villager Create(Vector3 position, Transform parent)
    {
        var root = new GameObject("Villager");
        root.transform.SetParent(parent);
        root.transform.position = position;

        // TINY — these are like ants to the player
        float scale = Random.Range(0.12f, 0.18f);

        var skin = Random.value < 0.33f ? SkinTone1 : Random.value < 0.5f ? SkinTone2 : SkinTone3;
        var cloth = Random.value < 0.33f ? ClothRed : Random.value < 0.5f ? ClothBlue : ClothGreen;

        // Body (capsule)
        var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        body.name = "VillagerBody";
        body.transform.SetParent(root.transform);
        body.transform.localPosition = Vector3.up * scale * 0.5f;
        body.transform.localScale    = new Vector3(scale * 0.4f, scale * 0.5f, scale * 0.4f);
        Object.Destroy(body.GetComponent<Collider>());
        var bodyMat = new Material(GameBootstrapper.GetHDRPLitShader());
        bodyMat.color = cloth;
        body.GetComponent<Renderer>().material = bodyMat;

        // Head (sphere)
        var head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        head.name = "VillagerHead";
        head.transform.SetParent(root.transform);
        head.transform.localPosition = Vector3.up * scale * 1.1f;
        head.transform.localScale    = Vector3.one * scale * 0.32f;
        Object.Destroy(head.GetComponent<Collider>());
        var headMat = new Material(GameBootstrapper.GetHDRPLitShader());
        headMat.color = skin;
        head.GetComponent<Renderer>().material = headMat;

        // Tiny spear/tool (some villagers)
        if (Random.value > 0.5f)
        {
            var spear = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            spear.name = "VillagerSpear";
            spear.transform.SetParent(root.transform);
            spear.transform.localPosition = new Vector3(scale * 0.3f, scale * 0.6f, 0);
            spear.transform.localScale    = new Vector3(scale * 0.04f, scale * 0.5f, scale * 0.04f);
            spear.transform.localRotation = Quaternion.Euler(0, 0, 15f);
            Object.Destroy(spear.GetComponent<Collider>());
            var sMat = new Material(GameBootstrapper.GetHDRPLitShader());
            sMat.color = new Color(0.32f, 0.20f, 0.06f);
            spear.GetComponent<Renderer>().material = sMat;
        }

        // Headband/feather (some villagers)
        if (Random.value > 0.4f)
        {
            var feather = GameObject.CreatePrimitive(PrimitiveType.Cube);
            feather.name = "Headdress";
            feather.transform.SetParent(root.transform);
            feather.transform.localPosition = new Vector3(0, scale * 1.28f, 0);
            feather.transform.localScale    = new Vector3(scale * 0.3f, scale * 0.1f, scale * 0.03f);
            Object.Destroy(feather.GetComponent<Collider>());
            var fMat = new Material(GameBootstrapper.GetHDRPLitShader());
            fMat.color = Random.value > 0.5f ? ClothRed : GameBootstrapper.PaletteGold;
            feather.GetComponent<Renderer>().material = fMat;
        }

        var villager = root.AddComponent<Villager>();
        villager._wanderTarget = position;

        return villager;
    }

    // ─────────────────────────────────────────────────────────────────────
    void Update()
    {
        _stateTimer -= Time.deltaTime;
        if (_stateTimer <= 0) EvaluateState();

        switch (_state)
        {
            case VillagerState.Wandering:    DoWander();     break;
            case VillagerState.Worshipping:  DoWorship();    break;
            case VillagerState.Fleeing:      DoFlee();       break;
            case VillagerState.Celebrating:  DoCelebrate();  break;
        }
    }

    void EvaluateState()
    {
        _stateTimer = Random.Range(0.5f, 1.5f);

        if (_isPanicking)
        {
            _state = VillagerState.Fleeing;
            return;
        }

        if (_celebrateTimer > 0)
        {
            _state = VillagerState.Celebrating;
            return;
        }

        // Check proximity to players
        var players = FindObjectsByType<NinjaController>(FindObjectsSortMode.None);
        float nearestDist = float.MaxValue;
        Transform nearestPlayer = null;
        foreach (var p in players)
        {
            float d = Vector3.Distance(transform.position, p.transform.position);
            if (d < nearestDist) { nearestDist = d; nearestPlayer = p.transform; }
        }

        if (nearestPlayer != null && nearestDist < WorshipRadius)
        {
            _targetPlayer = nearestPlayer;
            _state = VillagerState.Worshipping;
        }
        else
        {
            _state = VillagerState.Wandering;
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    void DoWander()
    {
        if (Vector3.Distance(transform.position, _wanderTarget) < 0.3f)
        {
            // Pick new random wander target near home
            float angle = Random.Range(0f, Mathf.PI * 2f);
            _wanderTarget = transform.position + new Vector3(
                Mathf.Cos(angle) * Random.Range(1f, WanderRadius),
                0,
                Mathf.Sin(angle) * Random.Range(1f, WanderRadius));
        }

        MoveToward(_wanderTarget, MoveSpeed * 0.5f);
    }

    void DoWorship()
    {
        if (_targetPlayer == null) { _state = VillagerState.Wandering; return; }

        // Face the god-player
        Vector3 toPlayer = (_targetPlayer.position - transform.position);
        toPlayer.y = 0;
        if (toPlayer.sqrMagnitude > 0.01f)
            transform.rotation = Quaternion.Slerp(transform.rotation,
                Quaternion.LookRotation(toPlayer), 3f * Time.deltaTime);

        // "Bowing" animation — bob up and down
        float bob = Mathf.Sin(Time.time * 4f) * 0.02f;
        var pos = transform.position;
        pos.y += bob;
        transform.position = pos;

        // If player attacks, villagers cheer (jump)
        var ctrl = _targetPlayer.GetComponent<NinjaController>();
        if (ctrl != null && (ctrl.IsAttacking || ctrl.IsFiringLaser))
        {
            // Little jump of excitement
            var pos2 = transform.position;
            pos2.y += Mathf.Abs(Mathf.Sin(Time.time * 10f)) * 0.06f;
            transform.position = pos2;
        }
    }

    void DoFlee()
    {
        // Run away from nearest enemy
        var enemies = FindObjectsByType<EnemyBase>(FindObjectsSortMode.None);
        Vector3 fleeDir = Vector3.zero;
        foreach (var e in enemies)
        {
            if (!e.IsAlive) continue;
            float d = Vector3.Distance(transform.position, e.transform.position);
            if (d < FleeRadius)
            {
                fleeDir += (transform.position - e.transform.position).normalized;
            }
        }

        if (fleeDir.sqrMagnitude > 0.01f)
            MoveToward(transform.position + fleeDir.normalized * 3f, MoveSpeed * 2f);
        else
        {
            _isPanicking = false;
            _state = VillagerState.Wandering;
        }
    }

    void DoCelebrate()
    {
        _celebrateTimer -= Time.deltaTime;
        if (_celebrateTimer <= 0) { _state = VillagerState.Wandering; return; }

        // Jump and spin!
        float jump = Mathf.Abs(Mathf.Sin(Time.time * 8f)) * 0.08f;
        var pos = transform.position;
        pos.y += jump;
        transform.position = pos;
        transform.Rotate(0, 200f * Time.deltaTime, 0);
    }

    // ─────────────────────────────────────────────────────────────────────
    void MoveToward(Vector3 target, float speed)
    {
        Vector3 dir = (target - transform.position);
        dir.y = 0;
        if (dir.sqrMagnitude < 0.05f) return;
        dir.Normalize();

        transform.position += dir * speed * Time.deltaTime;

        // Snap to terrain height
        if (Physics.Raycast(transform.position + Vector3.up * 5f, Vector3.down, out RaycastHit hit, 10f))
            transform.position = new Vector3(transform.position.x, hit.point.y, transform.position.z);

        // Face move direction
        if (dir.sqrMagnitude > 0.01f)
            transform.rotation = Quaternion.Slerp(transform.rotation,
                Quaternion.LookRotation(dir), 5f * Time.deltaTime);
    }

    // ─────────────────────────────────────────────────────────────────────
    // PUBLIC API
    // ─────────────────────────────────────────────────────────────────────
    public void SetPanic(bool panic) => _isPanicking = panic;
    public void Celebrate(float duration) => _celebrateTimer = duration;

    public void CalmDown()
    {
        _isPanicking = false;
        _state = VillagerState.Wandering;
    }
}

