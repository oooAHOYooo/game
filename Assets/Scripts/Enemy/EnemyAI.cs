using UnityEngine;
using System.Collections;

/// <summary>
/// EnemyAI — Zelda-style queued engagement with player-mimic attacks.
/// When IsActiveEnemy = false → circle the player, occasionally throw ranged attacks.
/// When IsActiveEnemy = true  → step forward, engage with combos that mirror the player's moveset.
/// Arsonist type → ignores player, walks to village, sets fires.
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
    public float DodgeChance        = 0.35f;
    public float DodgeCooldown      = 1.5f;

    // ── State ─────────────────────────────────────────────────────────────
    private EnemyBase   _base;
    private Rigidbody   _rb;
    private Animator    _anim;
    private Transform   _target;          // nearest player
    private float       _attackTimer;
    private float       _rangedTimer;
    private float       _dodgeTimer;
    private float       _circleAngle;
    private float       _speedBlend;
    private bool        _isActing;
    private const float RANGED_COOLDOWN = 3f;
    private const float LOW_HEALTH_THRESHOLD = 0.35f;

    // ── Arsonist state ────────────────────────────────────────────────────
    private Village     _village;
    private bool        _isBurning = false;
    private float       _burnTimer = 0f;
    private const float BURN_TICK_INTERVAL = 0.8f;
    private const float ARSONIST_VILLAGE_RANGE = 8f;
    private GameObject  _fireVFX;

    // ── Mimic state ───────────────────────────────────────────────────────
    private int _mimicComboIndex = 0;

    // ── SkyBomb state ─────────────────────────────────────────────────────
    private bool        _isSkyDropping    = false;
    private bool        _skyDropFalling   = false;   // true once we've started descending fast
    private GameObject  _skyDropTrailVFX;
    public  const float SKY_SPAWN_HEIGHT  = 220f;
    private const float SKYBOMB_FIRE_RADIUS = 8f;
    private const float SKYBOMB_FIRE_DAMAGE = 30f;
    private const float FIRE_ZONE_DURATION  = 12f;

    // ── Drop-in entry (non-SkyBomb enemies falling from sky) ─────────────
    public  bool        SpawnFromSky      = false;   // set by WaveManager before Start()
    private bool        _isMischiefing    = false;

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
            case "Elite":    MoveSpeed = 3.5f; AttackCooldown = 2.5f; RangedAttackChance = 0.7f; break;
            case "Brute":    MoveSpeed = 5.5f; AttackCooldown = 1.4f; AttackRange = 2.8f;        break;
            case "Boss":     MoveSpeed = 4.5f; AttackCooldown = 1.2f; AttackRange = 3.2f;        break;
            case "Arsonist": MoveSpeed = 7.0f; AttackCooldown = 999f; break; // doesn't attack players
            default:         MoveSpeed = 6.0f; AttackCooldown = 1.6f; break; // Grunt
        }

        // Cache village for arsonists
        if (EnemyType == "Arsonist")
            _village = FindAnyObjectByType<Village>();

        // SkyBomb: begin sky-drop if spawned high up
        if (EnemyType == "SkyBomb")
        {
            MoveSpeed = 7f; AttackCooldown = 1.4f; AttackRange = 2.5f;
            _isSkyDropping = true;
            _rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            _rb.useGravity   = true;
            SpawnSkyDropTrail();
        }

        // All wave-start enemies drop from the sky — mischief on landing
        if (SpawnFromSky && EnemyType != "SkyBomb")
        {
            _isSkyDropping = true;
            _rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            _rb.useGravity   = true;
        }
    }

    void Update()
    {
        if (!_base.IsAlive) return;

        // Sky-dropping — skip normal AI until landed
        if (_isSkyDropping)
        {
            UpdateSkyDrop();
            return;
        }

        // Mischief on landing — taunt before engaging
        if (_isMischiefing) return;

        // All enemies are confined to the island
        EnforceIslandBounds();

        // Arsonists have their own behaviour
        if (EnemyType == "Arsonist")
        {
            UpdateArsonist();
            UpdateAnimator();
            return;
        }

        FindTarget();
        if (_target == null) return;

        _attackTimer  -= Time.deltaTime;
        _rangedTimer  -= Time.deltaTime;
        _dodgeTimer   -= Time.deltaTime;

        UpdateCleverAI();

        if (_base.IsActiveEnemy)
            UpdateActiveEngagement();
        else
            UpdateCircling();

        // Biome interactions
        if (EnemyType == "Brute") UpdateBruteBiome();

        UpdateAnimator();
    }

    void UpdateCleverAI()
    {
        if (_target == null || _isActing) return;

        float dist = Vector3.Distance(transform.position, _target.position);
        float hpPercent = _base.CurrentHP / _base.MaxHP;

        // 1. DODGING
        if (_dodgeTimer <= 0f && dist < 4f)
        {
            var targetController = _target.GetComponentInParent<NinjaController>();
            if (targetController != null && targetController.IsAttacking)
            {
                if (Random.value < DodgeChance)
                    StartCoroutine(PerformDodge());
            }
        }

        // 2. TACTICAL RETREAT / AGGRESSION
        if (hpPercent < LOW_HEALTH_THRESHOLD)
        {
            CircleRadius = 8f; // Stay further back
            MoveSpeed *= 1.1f; // Panic speed
        }
        else
        {
            CircleRadius = 5.5f;
        }
    }

    IEnumerator PerformDodge()
    {
        _dodgeTimer = DodgeCooldown;
        _isActing = true;

        Vector3 sideDir = Vector3.Cross((_target.position - transform.position).normalized, Vector3.up);
        if (Random.value < 0.5f) sideDir *= -1f;

        // Dash VFX (reusing emission)
        foreach (var r in GetComponentsInChildren<Renderer>())
            GameBootstrapper.SetHDRPEmission(r.material, Color.white, 10f);

        _rb.AddForce(sideDir * 15f, ForceMode.VelocityChange);
        
        yield return new WaitForSeconds(0.3f);

        foreach (var r in GetComponentsInChildren<Renderer>())
            GameBootstrapper.SetHDRPEmission(r.material, _base.AccentColor, 1.5f);

        _isActing = false;
    }

    void UpdateBruteBiome()
    {
        // Smashing through Bamboo Grove while moving
        if (_rb.linearVelocity.magnitude > 1f)
        {
            Collider[] hits = Physics.OverlapSphere(transform.position + Vector3.up, 1.5f);
            foreach (var h in hits)
            {
                if (h.gameObject.name.Contains("Bamboo"))
                {
                    var prop = h.GetComponent<PropHealth>();
                    if (prop != null) prop.TakeDamage(50f); // Instant break
                }
            }
        }
    }

    void UpdateAnimator()
    {
        if (_anim == null) return;

        float horizontalSpeed = new Vector3(_rb.linearVelocity.x, 0, _rb.linearVelocity.z).magnitude;
        float targetSpeed = horizontalSpeed / MoveSpeed;
        _speedBlend = Mathf.Lerp(_speedBlend, targetSpeed > 0.1f ? targetSpeed : 0f, Time.deltaTime * 5f);

<<<<<<< HEAD
        if (_anim.runtimeAnimatorController != null && _anim.isActiveAndEnabled)
        {
            _anim.SetFloat("Speed", _speedBlend);
            _anim.SetBool("IsFlying", _base.IsFlying);
            _anim.SetBool("IsAttacking", _isActing);
        }
=======
        bool grounded = Physics.Raycast(transform.position + Vector3.up * 0.1f, Vector3.down, 0.35f);

        _anim.SetFloat("Speed",      _speedBlend);
        _anim.SetBool("IsGrounded",  grounded);
        _anim.SetBool("IsFlying",    _base.IsFlying);
        _anim.SetBool("IsAttacking", _isActing);
        // Enemies cycle through punch/kick so their attacks look varied
        _anim.SetInteger("AttackType", _isActing ? (Mathf.FloorToInt(Time.time * 2f) % 4 + 1) : 0);
>>>>>>> 8e810e5690dab86ac8de7c21cfeecb4810dd8e25
    }

    // ─────────────────────────────────────────────────────────────────────
    // ARSONIST BEHAVIOUR
    // ─────────────────────────────────────────────────────────────────────
    void UpdateArsonist()
    {
        if (_village == null || _village.IsDestroyed)
        {
            // Village gone — rage at player instead
            FindTarget();
            if (_target != null)
            {
                FaceTarget();
                MoveTo(_target.position, MoveSpeed);
                _attackTimer -= Time.deltaTime;
                if (_attackTimer <= 0f && Vector3.Distance(transform.position, _target.position) < AttackRange)
                {
                    _attackTimer = 2f;
                    StartCoroutine(MimicCombo(2));
                }
            }
            return;
        }

        float distToVillage = Vector3.Distance(transform.position, _village.Centre);

        if (distToVillage > ARSONIST_VILLAGE_RANGE)
        {
            // Sprint to village
            Vector3 dir = (_village.Centre - transform.position);
            dir.y = 0;
            if (dir.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir.normalized), 6f * Time.deltaTime);

            MoveTo(_village.Centre, MoveSpeed);
            _isBurning = false;
            DestroyFireVFX();
        }
        else
        {
            // In range: set fires!
            if (!_isBurning)
            {
                _isBurning = true;
                SpawnFireVFX();
            }

            _burnTimer -= Time.deltaTime;
        if (_burnTimer <= 0f)
        {
            _burnTimer = BURN_TICK_INTERVAL;
            if (VillageFireSystem.Instance != null) VillageFireSystem.Instance.IgniteNearestHut(transform.position);
        }    }

            // Slowly wander around the village
            _circleAngle += 20f * Time.deltaTime;
            float rad = ARSONIST_VILLAGE_RANGE * 0.6f;
            Vector3 wanderPos = _village.Centre +
                new Vector3(Mathf.Cos(_circleAngle * Mathf.Deg2Rad) * rad, 0,
                            Mathf.Sin(_circleAngle * Mathf.Deg2Rad) * rad);
            MoveTo(wanderPos, MoveSpeed * 0.4f);

            Vector3 faceDir = (_village.Centre - transform.position);
            faceDir.y = 0;
            if (faceDir.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(faceDir.normalized), 4f * Time.deltaTime);
        }

    void SpawnFireVFX()
    {
        if (_fireVFX != null) return;

        _fireVFX = new GameObject("ArsonistFire");
        _fireVFX.transform.SetParent(transform);
        _fireVFX.transform.localPosition = Vector3.forward * 1.5f + Vector3.up * 0.3f;

        var ps   = _fireVFX.AddComponent<ParticleSystem>();
        var main = ps.main;
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        main.loop          = true;
        main.duration      = 1f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.3f, 0.8f);
        main.startSpeed    = new ParticleSystem.MinMaxCurve(1f, 3f);
        main.startSize     = new ParticleSystem.MinMaxCurve(0.15f, 0.4f);
        main.startColor    = new ParticleSystem.MinMaxGradient(
            new Color(1f, 0.4f, 0.05f), new Color(1f, 0.85f, 0.2f));
        main.gravityModifier = -0.5f;
        main.maxParticles    = 40;

        var em = ps.emission;
        em.rateOverTime = 30f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle     = 25f;
        shape.radius    = 0.15f;

        ps.Play();

        // Fire light
        var lightObj = new GameObject("FireLight");
        lightObj.transform.SetParent(_fireVFX.transform);
        lightObj.transform.localPosition = Vector3.zero;
        var fl = lightObj.AddComponent<Light>();
        fl.type      = LightType.Point;
        fl.color     = new Color(1f, 0.5f, 0.1f);
        fl.intensity = 400f;
        fl.range     = 4f;
    }

    void DestroyFireVFX()
    {
        if (_fireVFX != null)
        {
            Destroy(_fireVFX);
            _fireVFX = null;
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    // CIRCLING (waiting for queue turn)
    // ─────────────────────────────────────────────────────────────────────
    void UpdateCircling()
    {
        if (_target == null) return;

        // ShadowArcher biome logic -> Seek high ground (Volcanic/Cliff)
        if (EnemyType == "ShadowArcher")
        {
            _circleAngle += 15f * Time.deltaTime;
            float rad = CircleRadius + 4f; // Stay further back
            
            // Bias towards the highest point in the circle
            Vector3 bestPos = _target.position;
            float targetHeight = _target.position.y;
            float maxFoundH = -100f;

            for (int i=0; i<8; i++)
            {
                float a = _circleAngle + (i * 45f);
                Vector3 check = _target.position + new Vector3(Mathf.Cos(a * Mathf.Deg2Rad) * rad, 10f, Mathf.Sin(a * Mathf.Deg2Rad) * rad);
                if (Physics.Raycast(check, Vector3.down, out RaycastHit hit, 20f))
                {
                    if (hit.point.y > maxFoundH)
                    {
                        maxFoundH = hit.point.y;
                        bestPos = hit.point;
                    }
                }
            }

            MoveTo(bestPos, MoveSpeed * 0.5f);
            FaceTarget();
        }
        else
        {
            // Normal circling
            _circleAngle += 28f * Time.deltaTime;
            float rad = CircleRadius;
            Vector3 desiredPos = _target.position +
                new Vector3(Mathf.Cos(_circleAngle * Mathf.Deg2Rad) * rad,
                            0,
                            Mathf.Sin(_circleAngle * Mathf.Deg2Rad) * rad);

            MoveTo(desiredPos, MoveSpeed * 0.65f);
            FaceTarget();
        }

        // Occasional ranged harass
        if (_rangedTimer <= 0f && Random.value < RangedAttackChance)
        {
            _rangedTimer = RANGED_COOLDOWN;
            if (EnemyType == "ShadowArcher" || EnemyType == "Elite") // Elite also shoots sometimes
                StartCoroutine(FireArrow());
        }
    }

    IEnumerator FireArrow()
    {
        _isActing = true;
        
        if (_anim != null) _anim.SetInteger("AttackType", 1);
        
        // Brief charge effect
        foreach (var r in GetComponentsInChildren<Renderer>())
            GameBootstrapper.SetHDRPEmission(r.material, _base.AccentColor, 5f);
            
        yield return new WaitForSeconds(0.6f);
        
        if (_target != null)
        {
            Vector3 origin = transform.position + Vector3.up * 1f;
            Vector3 dir = (_target.position + Vector3.up - origin).normalized;
            SpawnProjectile(origin, dir, GameBootstrapper.PaletteMagenta, 15f, 25f);
        }

        yield return new WaitForSeconds(0.4f);
        
        if (_anim != null) _anim.SetInteger("AttackType", 0);
        
        foreach (var r in GetComponentsInChildren<Renderer>())
            GameBootstrapper.SetHDRPEmission(r.material, _base.AccentColor, 1.5f);
            
        _isActing = false;
    }

    // ─────────────────────────────────────────────────────────────────────
    // ACTIVE ENGAGEMENT (Zelda-style duel, now with mimic combos)
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

            // Clever "Leap" attack if close enough
            if (_attackTimer <= 0f && distToTarget < AttackRange * 2.5f && Random.value < 0.2f)
            {
                _attackTimer = AttackCooldown;
                StartCoroutine(LeapAttack());
            }
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

    IEnumerator LeapAttack()
    {
        _isActing = true;
        
        // Wind up
        Vector3 jumpDir = (_target.position - transform.position).normalized;
        GameBootstrapper.SpawnAlert(transform, true); // DANGER!
        _rb.AddForce(Vector3.up * 5f, ForceMode.VelocityChange);
        
        yield return new WaitForSeconds(0.2f);

        // Leap!
        _rb.AddForce(jumpDir * 15f + Vector3.up * 2f, ForceMode.VelocityChange);
        if (_anim != null) _anim.SetInteger("AttackType", 2);

        yield return new WaitForSeconds(0.4f);
        
        HitPlayersInRange(AttackRange * 1.5f, GetDamage() * 1.2f);
        SpawnSlamVFX();

        yield return new WaitForSeconds(0.3f);
        if (_anim != null) _anim.SetInteger("AttackType", 0);
        _isActing = false;
    }

    // ─────────────────────────────────────────────────────────────────────
    // ATTACK PATTERNS — now includes player-mimic combos
    // ─────────────────────────────────────────────────────────────────────
    IEnumerator PerformAttack()
    {
        _isActing = true;

        switch (EnemyType)
        {
            case "Grunt":  yield return StartCoroutine(MimicCombo(3));     break;
            case "Elite":  yield return StartCoroutine(MimicRanged());     break;
            case "Brute":  yield return StartCoroutine(MimicHeavySlam()); break;
            case "Boss":   yield return StartCoroutine(BossCombo());       break;
            default:       yield return StartCoroutine(MimicCombo(2));     break;
        }

        _isActing = false;
    }

    /// <summary>
    /// Mimic Combo — enemies perform the same punches and kicks as the player.
    /// Cycles through Light Punch (1), Heavy Punch (2), Light Kick (3), Heavy Kick (4).
    /// </summary>
    IEnumerator MimicCombo(int hits)
    {
        for (int i = 0; i < hits; i++)
        {
            _mimicComboIndex = (_mimicComboIndex % 4) + 1; // cycle 1-4
            if (_anim != null) _anim.SetInteger("AttackType", _mimicComboIndex);

            // Lunge forward like the player does
            if (_target != null)
            {
                Vector3 dir = (_target.position - transform.position).normalized;
                float lungeForce = (_mimicComboIndex <= 2) ? 6f : 8f; // kicks lunge harder
                _rb.AddForce(dir * lungeForce, ForceMode.VelocityChange);
            }

            float damage = GetMimicDamage(_mimicComboIndex);
            float range  = (_mimicComboIndex >= 3) ? AttackRange * 1.1f : AttackRange; // kicks have slightly more range
            HitPlayersInRange(range, damage);

            float duration = (_mimicComboIndex % 2 == 0) ? 0.55f : 0.30f; // heavy = slower
            yield return new WaitForSeconds(duration);
            if (_anim != null) _anim.SetInteger("AttackType", 0);
        }
    }

    /// <summary>
    /// Mimic the player's ranged ki ability — fires a projectile mimicking a laser burst.
    /// </summary>
    IEnumerator MimicRanged()
    {
        if (_target == null) yield break;

        if (_anim != null) _anim.SetInteger("AttackType", 1);

        // Warn with glow
        foreach (var r in GetComponentsInChildren<Renderer>())
            GameBootstrapper.SetHDRPEmission(r.material, _base.AccentColor, 8f);

        yield return new WaitForSeconds(0.3f);

        // Fire projectile
        Vector3 origin = transform.position + Vector3.up * 1.5f;
        Vector3 dir    = (_target.position + Vector3.up - origin).normalized;
        SpawnProjectile(origin, dir, _base.AccentColor, GetDamage() * 0.7f, 18f);

        // Second projectile (mimic double-tap)
        yield return new WaitForSeconds(0.25f);
        if (_target != null)
        {
            dir = (_target.position + Vector3.up - origin).normalized;
            SpawnProjectile(origin, dir, _base.AccentColor, GetDamage() * 0.5f, 22f);
        }

        yield return new WaitForSeconds(0.4f);
        if (_anim != null) _anim.SetInteger("AttackType", 0);

        foreach (var r in GetComponentsInChildren<Renderer>())
            GameBootstrapper.SetHDRPEmission(r.material, _base.AccentColor, 1.5f);
    }

    /// <summary>
    /// Mimic the player's heavy attack — a ground slam with AoE.
    /// </summary>
    IEnumerator MimicHeavySlam()
    {
        GameBootstrapper.SpawnAlert(transform, true); // DANGER!
        if (_anim != null) _anim.SetInteger("AttackType", 2);

        // Wind-up: stand still and flash (like the player charging a heavy)
        float windUp = 0.6f;
        for (float t = 0; t < windUp; t += Time.deltaTime)
        {
            foreach (var r in GetComponentsInChildren<Renderer>())
                GameBootstrapper.SetHDRPEmission(r.material, _base.AccentColor,
                    4f + Mathf.Sin(t * 30f) * 6f);
            yield return null;
        }

        // Slam — AoE 4m (mimics the player's heavy punch forward lunge)
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

    IEnumerator BossCombo()
    {
        // Boss mimics the full player combo: punch → kick → heavy slam
        yield return StartCoroutine(MimicCombo(2));
        yield return new WaitForSeconds(0.2f);
        yield return StartCoroutine(MimicHeavySlam());
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
            "Grunt"    => 12f,
            "Elite"    => 8f,
            "Brute"    => 22f,
            "Boss"     => 20f,
            "Arsonist" => 10f,
            _          => 12f
        };
        return baseDmg * (1f + _base.WaveIndex * 0.1f);
    }

    float GetMimicDamage(int attackType)
    {
        // Mirror the player's damage types
        float baseDmg = attackType switch
        {
            1 => 10f,  // Light Punch
            2 => 20f,  // Heavy Punch
            3 => 9f,   // Light Kick
            4 => 22f,  // Heavy Kick
            _ => 12f
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
        
        // Social separation: avoid overlapping other enemies
        Vector3 separation = Vector3.zero;
        var enemies = FindObjectsByType<EnemyBase>(FindObjectsSortMode.None);
        foreach (var e in enemies)
        {
            if (e == _base || !e.IsAlive) continue;
            float d = Vector3.Distance(transform.position, e.transform.position);
            if (d < 2.5f)
            {
                separation += (transform.position - e.transform.position).normalized * (2.5f - d);
            }
        }

        if (dir.sqrMagnitude < 0.1f && separation.sqrMagnitude < 0.01f) return;
        
        dir = (dir.normalized + separation * 0.5f).normalized;
        _rb.AddForce(dir * speed * 3.5f, ForceMode.Acceleration);
        
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

    // ─────────────────────────────────────────────────────────────────────
    // ISLAND BOUNDS — enemies cannot leave the island
    // ─────────────────────────────────────────────────────────────────────
    void EnforceIslandBounds()
    {
        const float BOUNDARY = 148f;
        Vector2 horiz = new Vector2(transform.position.x, transform.position.z);
        float dist = horiz.magnitude;
        if (dist <= BOUNDARY) return;

        Vector3 inward = new Vector3(-horiz.normalized.x, 0, -horiz.normalized.y);
        _rb.AddForce(inward * ((dist - BOUNDARY) * 30f + 50f), ForceMode.Force);

        if (dist > GameSettings.IslandRadius)
        {
            Vector2 clamped = horiz.normalized * GameSettings.IslandRadius;
            _rb.position = new Vector3(clamped.x, _rb.position.y, clamped.y);
            Vector3 vel = _rb.linearVelocity;
            float outward = Vector2.Dot(new Vector2(vel.x, vel.z), horiz.normalized);
            if (outward > 0f)
            {
                vel.x -= horiz.normalized.x * outward;
                vel.z -= horiz.normalized.y * outward;
                _rb.linearVelocity = vel;
            }
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    // SKY-BOMB FIREBALL DROP
    // ─────────────────────────────────────────────────────────────────────
    void FixedUpdate()
    {
        if (_isSkyDropping)
            _rb.AddForce(Vector3.down * 28f, ForceMode.Acceleration);
    }

    void UpdateSkyDrop()
    {
        float velY = _rb.linearVelocity.y;
        if (!_skyDropFalling && velY < -10f)
            _skyDropFalling = true;

        // Landed when falling fast then suddenly slowed (hit ground)
        if (_skyDropFalling && velY > -2f && transform.position.y < 25f)
        {
            _isSkyDropping = false;
            if (EnemyType == "SkyBomb")
                OnSkyBombLanded();
            else
                OnDropInLanded();
        }
    }

    void OnDropInLanded()
    {
        // Restore rotation constraints
        _rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

        // Small dust puff on landing
        var dust = new GameObject("LandingDust");
        dust.transform.position = transform.position;
        var ps   = dust.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.duration      = 0.3f;
        main.loop          = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.3f, 0.7f);
        main.startSpeed    = new ParticleSystem.MinMaxCurve(2f, 7f);
        main.startSize     = new ParticleSystem.MinMaxCurve(0.2f, 0.5f);
        main.startColor    = new ParticleSystem.MinMaxGradient(
            new Color(0.7f, 0.6f, 0.4f, 0.8f), new Color(0.9f, 0.85f, 0.7f, 0.6f));
        main.gravityModifier = 0.2f;
        main.maxParticles    = 40;
        var em = ps.emission;
        em.SetBursts(new[] { new ParticleSystem.Burst(0f, 35) });
        em.rateOverTime = 0;
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius    = 0.8f;
        ps.Play();
        Destroy(dust, 1.5f);

        StartCoroutine(LandingMischief());
    }

    IEnumerator LandingMischief()
    {
        _isMischiefing = true;
        FindTarget();

        // 1. Stick the landing — brief crouch (freeze in place)
        _rb.linearVelocity = Vector3.zero;
        if (_anim != null) _anim.SetFloat("Speed", 0f);
        yield return new WaitForSeconds(0.25f);

        // 2. Spin to face nearest player with exaggerated speed
        if (_target != null)
        {
            float spinTime = 0.35f;
            for (float t = 0; t < spinTime; t += Time.deltaTime)
            {
                Vector3 dir = (_target.position - transform.position);
                dir.y = 0;
                if (dir.sqrMagnitude > 0.01f)
                    transform.rotation = Quaternion.Slerp(transform.rotation,
                        Quaternion.LookRotation(dir.normalized), t / spinTime);
                yield return null;
            }
        }

        // 3. Little strut — take 2 cocky steps toward the player then stop
        if (_target != null && _anim != null)
        {
            _anim.SetFloat("Speed", 0.6f);
            float strutTime = 0.5f;
            Vector3 strutDir = (_target.position - transform.position).normalized;
            for (float t = 0; t < strutTime; t += Time.deltaTime)
            {
                _rb.AddForce(strutDir * MoveSpeed * 1.5f, ForceMode.Acceleration);
                yield return null;
            }
            _rb.linearVelocity = new Vector3(0, _rb.linearVelocity.y, 0);
            _anim.SetFloat("Speed", 0f);
        }

        // 4. Taunt flash — enemy glows their accent color and "laughs" (AttackType pose briefly)
        yield return new WaitForSeconds(0.15f);
        if (_anim != null) _anim.SetInteger("AttackType", 3); // idle kick pose
        foreach (var r in GetComponentsInChildren<Renderer>())
            GameBootstrapper.SetHDRPEmission(r.material, _base.AccentColor, 12f);

        yield return new WaitForSeconds(0.45f);

        // 5. Second look — glance away then snap back (double-take)
        transform.Rotate(0, 45f, 0);
        yield return new WaitForSeconds(0.2f);
        if (_target != null)
        {
            Vector3 snapDir = (_target.position - transform.position);
            snapDir.y = 0;
            if (snapDir.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.LookRotation(snapDir.normalized);
        }
        yield return new WaitForSeconds(0.1f);

        // 6. Done — reset glow and unleash
        if (_anim != null) _anim.SetInteger("AttackType", 0);
        foreach (var r in GetComponentsInChildren<Renderer>())
            GameBootstrapper.SetHDRPEmission(r.material, _base.AccentColor, 1.5f);

        _isMischiefing = false;
    }

    void OnSkyBombLanded()
    {
        if (_skyDropTrailVFX != null) { Destroy(_skyDropTrailVFX); _skyDropTrailVFX = null; }

        Vector3 pos = transform.position;

        // Camera shake & danger alert
        SplitScreenCamera.ShakeCamera(0, 0.8f, 0.6f);
        SplitScreenCamera.ShakeCamera(1, 0.8f, 0.6f);
        GameBootstrapper.SpawnAlert(transform, true);
        SoundManager.PlayWaveStart(); // reuse the dramatic BOOM sound

        // Impact shockwave VFX
        SpawnImpactVFX(pos);

        // Damage players in radius
        HitPlayersInRange(SKYBOMB_FIRE_RADIUS, SKYBOMB_FIRE_DAMAGE);

        // Leave a persistent fire zone on the ground
        SpawnFireZone(pos);

        // Now fight like a normal grunt
        EnemyType = "Grunt";
    }

    void SpawnImpactVFX(Vector3 pos)
    {
        var obj  = new GameObject("SkyBombImpact");
        obj.transform.position = pos;
        var ps   = obj.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.duration     = 0.5f;
        main.loop         = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 1.5f);
        main.startSpeed    = new ParticleSystem.MinMaxCurve(8f, 30f);
        main.startSize     = new ParticleSystem.MinMaxCurve(0.3f, 1.2f);
        main.startColor    = new ParticleSystem.MinMaxGradient(
            new Color(1f, 0.4f, 0.05f), new Color(1f, 0.9f, 0.1f));
        main.gravityModifier = 0.3f;
        main.maxParticles    = 200;
        var em = ps.emission;
        em.SetBursts(new[] { new ParticleSystem.Burst(0f, 180) });
        em.rateOverTime = 0;
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 2f;
        ps.Play();

        var flash = new GameObject("ImpactFlash");
        flash.transform.position = pos;
        var l = flash.AddComponent<Light>();
        l.type = LightType.Point; l.color = new Color(1f, 0.6f, 0.1f);
        l.intensity = 600000f; l.range = 30f;
        Destroy(flash, 0.12f);

        Destroy(obj, 3f);
    }

    void SpawnFireZone(Vector3 pos)
    {
        var zone = new GameObject("FireZone");
        pos.y = IslandGenerator.WaterLevel + 0.05f;
        zone.transform.position = pos;

        // Visible fire ring
        var ps   = zone.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.loop          = true;
        main.duration      = 1f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.6f, 1.4f);
        main.startSpeed    = new ParticleSystem.MinMaxCurve(1f, 4f);
        main.startSize     = new ParticleSystem.MinMaxCurve(0.3f, 0.9f);
        main.startColor    = new ParticleSystem.MinMaxGradient(
            new Color(1f, 0.35f, 0.05f), new Color(1f, 0.7f, 0.1f));
        main.gravityModifier = -0.3f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles    = 300;
        var em = ps.emission;
        em.rateOverTime = 120f;
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 30f; shape.radius = SKYBOMB_FIRE_RADIUS * 0.7f;
        ps.Play();

        // Damage trigger sphere
        var triggerObj = new GameObject("FireZoneTrigger");
        triggerObj.transform.SetParent(zone.transform);
        triggerObj.transform.localPosition = Vector3.zero;
        var sc = triggerObj.AddComponent<SphereCollider>();
        sc.isTrigger = true;
        sc.radius = SKYBOMB_FIRE_RADIUS;
        triggerObj.AddComponent<FireZoneDamage>();

        // Point light flicker
        var lightObj = new GameObject("ZoneLight");
        lightObj.transform.SetParent(zone.transform);
        lightObj.transform.localPosition = Vector3.zero;
        var fl = lightObj.AddComponent<Light>();
        fl.type = LightType.Point; fl.color = new Color(1f, 0.5f, 0.1f);
        fl.intensity = 800f; fl.range = SKYBOMB_FIRE_RADIUS * 2f;

        Destroy(zone, FIRE_ZONE_DURATION);
    }

    void SpawnSkyDropTrail()
    {
        _skyDropTrailVFX = new GameObject("SkyDropTrail");
        _skyDropTrailVFX.transform.SetParent(transform);
        _skyDropTrailVFX.transform.localPosition = Vector3.zero;

        var ps   = _skyDropTrailVFX.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.loop          = true;
        main.duration      = 1f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.3f, 0.7f);
        main.startSpeed    = new ParticleSystem.MinMaxCurve(2f, 6f);
        main.startSize     = new ParticleSystem.MinMaxCurve(0.4f, 1f);
        main.startColor    = new ParticleSystem.MinMaxGradient(
            new Color(1f, 0.4f, 0.05f), new Color(1f, 0.9f, 0.15f));
        main.gravityModifier = 0.4f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles    = 200;
        var em = ps.emission;
        em.rateOverTime = 80f;
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.5f;
        ps.Play();

        // Glow light on the falling fireball
        var lObj = new GameObject("DropLight");
        lObj.transform.SetParent(_skyDropTrailVFX.transform);
        lObj.transform.localPosition = Vector3.zero;
        var l = lObj.AddComponent<Light>();
        l.type = LightType.Point; l.color = new Color(1f, 0.55f, 0.1f);
        l.intensity = 300000f; l.range = 20f;
    }

    void FindTarget()
    {
        // 1. BALL FOCUS: If the ball is on the ground, move to it!
        if (MythologicalBall.Instance != null && MythologicalBall.Instance.Carrier == null)
        {
            _target = MythologicalBall.Instance.transform;
            return;
        }

        // 2. CARRIER FOCUS: If someone has the ball, target them!
        if (MythologicalBall.Instance != null && MythologicalBall.Instance.Carrier != null)
        {
            _target = MythologicalBall.Instance.Carrier.transform;
            return;
        }

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
