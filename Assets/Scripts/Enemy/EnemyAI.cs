using UnityEngine;
using System.Collections;

/// <summary>
/// EnemyAI — Zelda-style queued engagement.
/// When IsActiveEnemy = false → circle the player, occasionally throw ranged attacks.
/// When IsActiveEnemy = true  → step forward, engage with type-specific combo.
/// </summary>
[RequireComponent(typeof(EnemyBase))]
public class EnemyAI : MonoBehaviour
{
    public string EnemyType = "FootSoldier";
    public float  Scale     = 1f;

    // ── Attack timing ─────────────────────────────────────────────────────
    [Header("Attack Timing")]
    public float AttackRange       = 2.2f;
    public float CircleRadius      = 5.5f;
    public float MoveSpeed         = 5f;
    public float AttackCooldown    = 1.6f;  // base; varies by type
    public float RangedAttackChance = 0.2f; // while circling

    // ── State ─────────────────────────────────────────────────────────────
    private EnemyBase   _base;
    private Rigidbody   _rb;
    private Animator    _anim;
    private Transform   _target;          // nearest player
    private float       _attackTimer;
    private float       _rangedTimer;
    private float       _circleAngle;
    private float       _speedBlend;
    private bool        _isActing;
    private const float RANGED_COOLDOWN = 3f;

    // ─────────────────────────────────────────────────────────────────────
    void Awake()
    {
        _base = GetComponent<EnemyBase>();
        _rb   = GetComponent<Rigidbody>();
        _anim = GetComponent<Animator>();
    }

    void Start()
    {
        // Randomise circle starting angle so enemies don't stack
        _circleAngle = Random.Range(0f, 360f);

        // Type-specific stat overrides
        switch (EnemyType)
        {
            case "Elite": MoveSpeed = 3.5f; AttackCooldown = 2.5f; RangedAttackChance = 0.7f; break;
            case "Brute": MoveSpeed = 5.5f; AttackCooldown = 1.4f; AttackRange = 2.8f;        break;
            case "Boss":  MoveSpeed = 4.5f; AttackCooldown = 1.2f; AttackRange = 3.2f;        break;
            default:      MoveSpeed = 6.0f; AttackCooldown = 1.6f; break; // Grunt
        }
    }

    void Update()
    {
        if (!_base.IsAlive) return;
        FindTarget();
        if (_target == null) return;

        _attackTimer  -= Time.deltaTime;
        _rangedTimer  -= Time.deltaTime;

        if (_base.IsActiveEnemy)
            UpdateActiveEngagement();
        else
            UpdateCircling();

        UpdateAnimator();
    }

    void UpdateAnimator()
    {
        if (_anim == null) return;

        float horizontalSpeed = new Vector3(_rb.linearVelocity.x, 0, _rb.linearVelocity.z).magnitude;
        float targetSpeed = horizontalSpeed / MoveSpeed;
        _speedBlend = Mathf.Lerp(_speedBlend, targetSpeed > 0.1f ? targetSpeed : 0f, Time.deltaTime * 5f);

        _anim.SetFloat("Speed", _speedBlend);
        _anim.SetBool("IsFlying", _base.IsFlying);
        _anim.SetBool("IsAttacking", _isActing);
    }

    // ─────────────────────────────────────────────────────────────────────
    // CIRCLING (waiting for queue turn)
    // ─────────────────────────────────────────────────────────────────────
    void UpdateCircling()
    {
        if (_target == null) return;

        // Slowly orbit the player
        _circleAngle += 28f * Time.deltaTime;
        float rad = CircleRadius;
        Vector3 desiredPos = _target.position +
            new Vector3(Mathf.Cos(_circleAngle * Mathf.Deg2Rad) * rad,
                        0,
                        Mathf.Sin(_circleAngle * Mathf.Deg2Rad) * rad);

        MoveTo(desiredPos, MoveSpeed * 0.65f);
        FaceTarget();

        // Occasional ranged harass
        if (_rangedTimer <= 0f && Random.value < RangedAttackChance)
        {
            _rangedTimer = RANGED_COOLDOWN;
            if (EnemyType == "ShadowArcher")
                StartCoroutine(FireArrow());
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    // ACTIVE ENGAGEMENT (Zelda-style duel)
    // ─────────────────────────────────────────────────────────────────────
    void UpdateActiveEngagement()
    {
        if (_target == null || _isActing) return;

        float distToTarget = Vector3.Distance(transform.position, _target.position);
        FaceTarget();

        if (distToTarget > AttackRange)
        {
            // Approach
            MoveTo(_target.position, MoveSpeed);
        }
        else if (_attackTimer <= 0f)
        {
            _attackTimer = AttackCooldown;
            StartCoroutine(PerformAttack());
        }
        else
        {
            // In range but on cooldown — strafe
            Vector3 strafe = Vector3.Cross((_target.position - transform.position).normalized, Vector3.up);
            strafe *= (Mathf.Sin(Time.time * 2f) > 0 ? 1 : -1);
            _rb.AddForce(strafe * MoveSpeed * 0.7f, ForceMode.Acceleration);
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    // ATTACK PATTERNS
    // ─────────────────────────────────────────────────────────────────────
    IEnumerator PerformAttack()
    {
        _isActing = true;

        switch (EnemyType)
        {
            case "Grunt": yield return StartCoroutine(ComboSlash(3)); break;
            case "Elite": yield return StartCoroutine(FireArrow());  break;
            case "Brute": yield return StartCoroutine(BerserkerSlam()); break;
            case "Boss":  yield return StartCoroutine(BossCombo()); break;
            default:      yield return StartCoroutine(ComboSlash(2)); break;
        }

        _isActing = false;
    }

    IEnumerator ComboSlash(int hits)
    {
        for (int i = 0; i < hits; i++)
        {
            // Animation trigger
            if (_anim != null) _anim.SetInteger("AttackType", (i % 2 == 0) ? 1 : 3);

            // Lunge
            if (_target != null)
            {
                Vector3 dir = (_target.position - transform.position).normalized;
                _rb.AddForce(dir * 6f, ForceMode.VelocityChange);
            }

            // Hit check sphere
            HitPlayersInRange(AttackRange, GetDamage());
            yield return new WaitForSeconds(0.45f);
            if (_anim != null) _anim.SetInteger("AttackType", 0);
        }
    }

    IEnumerator BerserkerSlam()
    {
        if (_anim != null) _anim.SetInteger("AttackType", 2);

        // Wind-up: stand still and flash
        float windUp = 0.6f;
        for (float t = 0; t < windUp; t += Time.deltaTime)
        {
            // Pulse warning
            foreach (var r in GetComponentsInChildren<Renderer>())
                GameBootstrapper.SetHDRPEmission(r.material, _base.AccentColor,
                    4f + Mathf.Sin(t * 30f) * 6f);
            yield return null;
        }

        // Slam — AoE 4m
        if (_target != null)
        {
            Vector3 dir = (_target.position - transform.position).normalized;
            _rb.AddForce(dir * 15f, ForceMode.VelocityChange);
        }
        HitPlayersInRange(4f, GetDamage() * 1.5f);
        SpawnSlamVFX();
        yield return new WaitForSeconds(0.5f);
        if (_anim != null) _anim.SetInteger("AttackType", 0);
    }

    IEnumerator FireArrow()
    {
        if (_target == null) yield break;

        if (_anim != null) _anim.SetInteger("AttackType", 1);

        // Spawn projectile
        Vector3 origin = transform.position + Vector3.up * 1.5f;
        Vector3 dir    = (_target.position + Vector3.up - origin).normalized;
        SpawnProjectile(origin, dir, _base.AccentColor, GetDamage() * 0.7f, 18f);
        yield return new WaitForSeconds(0.4f);
        if (_anim != null) _anim.SetInteger("AttackType", 0);
    }

    IEnumerator BossCombo()
    {
        yield return StartCoroutine(ComboSlash(2));
        yield return new WaitForSeconds(0.2f);
        yield return StartCoroutine(BerserkerSlam());
        // Chance to spawn a clone
        if (Random.value < 0.3f)
            SpawnClone();
    }

    // ─────────────────────────────────────────────────────────────────────
    // HELPERS
    // ─────────────────────────────────────────────────────────────────────
    float GetDamage()
    {
        float baseDmg = EnemyType switch
        {
            "Grunt" => 12f,
            "Elite" => 8f,
            "Brute" => 22f,
            "Boss"  => 20f,
            _       => 12f
        };
        return baseDmg * (1f + _base.WaveIndex * 0.1f);
    }

    void HitPlayersInRange(float range, float damage)
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, range);
        foreach (var c in hits)
        {
            var ph = c.GetComponentInParent<PlayerHealth>();
            if (ph != null && ph.IsAlive)
            {
                // Check if blocking
                var nc = c.GetComponentInParent<NinjaController>();
                if (nc != null && nc.IsBlocking)
                {
                    // Blocked: half damage, push back enemy instead
                    _rb.AddForce(-transform.forward * 10f, ForceMode.VelocityChange);
                    damage *= 0.3f;
                }
                ph.TakeDamage(damage);
                HitStopFrames(0.04f);
            }
        }
    }

    void HitStopFrames(float duration)
    {
        StartCoroutine(DoHitStop(duration));
    }

    IEnumerator DoHitStop(float duration)
    {
        Time.timeScale = 0.05f;
        yield return new WaitForSecondsRealtime(duration);
        Time.timeScale = 1f;
    }

    void SpawnSlamVFX()
    {
        var obj = new GameObject("SlamVFX");
        obj.transform.position = transform.position;
        var ps   = obj.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.duration      = 0.3f;
        main.loop          = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.2f, 0.5f);
        main.startSpeed    = new ParticleSystem.MinMaxCurve(3f, 8f);
        main.startSize     = new ParticleSystem.MinMaxCurve(0.2f, 0.6f);
        main.startColor    = new ParticleSystem.MinMaxGradient(_base.AccentColor, Color.white);
        main.maxParticles  = 60;
        main.gravityModifier = -0.1f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius    = 4f;

        var emit = ps.emission;
        emit.SetBursts(new[] { new ParticleSystem.Burst(0f, 60) });
        emit.rateOverTime = 0;
        ps.Play();
        Destroy(obj, 1f);
    }

    void SpawnProjectile(Vector3 origin, Vector3 direction, Color color, float damage, float speed)
    {
        var proj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        proj.name = "EnemyProjectile";
        proj.transform.position   = origin;
        proj.transform.localScale = Vector3.one * 0.2f;

        var col = proj.GetComponent<Collider>();
        col.isTrigger = true;

        var mat = new Material(GameBootstrapper.GetHDRPLitShader());
        mat.color = color;
        GameBootstrapper.SetHDRPEmission(mat, color, 15f);
        proj.GetComponent<Renderer>().material = mat;

        var ep = proj.AddComponent<EnemyProjectile>();
        ep.Direction = direction;
        ep.Speed     = speed;
        ep.Damage    = damage;
        ep.Color     = color;
    }

    void SpawnClone()
    {
        Vector3 clonePos = transform.position + Random.insideUnitSphere * 3f;
        clonePos.y = 0.5f;
        var clone = EnemyBase.BuildFootSoldier(clonePos, _base.WaveIndex);
        var wm    = FindAnyObjectByType<WaveManager>();
        if (wm != null) wm.RegisterExtraEnemy(clone.GetComponent<EnemyBase>());
    }

    void MoveTo(Vector3 dest, float speed)
    {
        Vector3 dir = (dest - transform.position);
        dir.y = 0;
        if (dir.sqrMagnitude < 0.1f) return;
        dir.Normalize();
        _rb.AddForce(dir * speed * 3f, ForceMode.Acceleration);
        // Clamp velocity
        Vector3 horiz = new Vector3(_rb.linearVelocity.x, 0, _rb.linearVelocity.z);
        if (horiz.magnitude > speed)
        {
            horiz = horiz.normalized * speed;
            _rb.linearVelocity = new Vector3(horiz.x, _rb.linearVelocity.y, horiz.z);
        }
    }

    void FaceTarget()
    {
        if (_target == null) return;
        Vector3 dir = (_target.position - transform.position);
        dir.y = 0;
        if (dir.sqrMagnitude < 0.01f) return;
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), 6f * Time.deltaTime);
    }

    void FindTarget()
    {
        // Find nearest alive player
        var players = FindObjectsByType<PlayerHealth>(FindObjectsSortMode.None);
        float nearest = float.MaxValue;
        Transform best = null;
        foreach (var p in players)
        {
            if (!p.IsAlive) continue;
            float d = Vector3.Distance(transform.position, p.transform.position);
            if (d < nearest) { nearest = d; best = p.transform; }
        }
        _target = best;
    }
}
