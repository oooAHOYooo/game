using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

/// <summary>
/// NinjaController — UFC5-inspired movement with Dragon Ball Z flight & ki beams.
/// Attach automatically by GameBootstrapper; requires Rigidbody on same GameObject.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class NinjaController : MonoBehaviour
{
    // ── Injected by GameBootstrapper ──────────────────────────────────────
    public int       PlayerIndex;      // 0 = P1, 1 = P2
    public bool      IsGhost;          // true = Ghost AI drives this
    public Color     BodyColor;
    public Transform WeaponHolder;
    public GameObject SwordRoot;

    // ── Movement stats ────────────────────────────────────────────────────
    [Header("Movement")]
    public float GroundSpeed     = 8f;
    public float AirSpeed        = 14f;
    public float JumpForce       = 9f;
    public float AscendForce     = 18f;
    public float DescendForce    = 12f;
    public float MaxAltitude     = 25f;
    public float DodgeForce      = 18f;
    public float DodgeCooldown   = 0.45f;
    public float AirDrag         = 1.2f;
    public float GroundDrag      = 4f;

    // ── Combat stats ──────────────────────────────────────────────────────
    [Header("Combat")]
    public float LightAttackDamage  = 22f;
    public float HeavyAttackDamage  = 45f;
    public float StaffLightDamage   = 15f;
    public float StaffHeavyDamage   = 35f;
    public float KiMax              = 100f;
    public float KiRegen            = 12f;       // per second
    public float KiDrainRate        = 25f;        // per second while charging
    public float LaserDamagePerTick = 18f;
    public float WeaponTransformHold = 1.0f;      // seconds to hold West button

    // ── State (public so GhostAI + HUD can read) ──────────────────────────
    [HideInInspector] public bool  IsFlying          = false;
    [HideInInspector] public bool  IsStaffMode       = false;
    [HideInInspector] public bool  IsChargingKi      = false;
    [HideInInspector] public bool  IsFiringLaser     = false;
    [HideInInspector] public bool  IsAttacking       = false;
    [HideInInspector] public bool  IsBlocking        = false;
    [HideInInspector] public float CurrentKi         = 100f;
    [HideInInspector] public float WeaponHoldTimer   = 0f;
    [HideInInspector] public bool  IsGrounded        = false;
    [HideInInspector] public int   LastAttackType    = 0;  // 1=light punch, 2=heavy punch, 3=light kick, 4=heavy kick

    // ── Private ───────────────────────────────────────────────────────────
    private Rigidbody   _rb;
    private Gamepad     _pad;
    private float       _dodgeTimer        = 0f;
    private float       _attackCooldown    = 0f;
    private float       _heavyChargeTimer  = 0f;
    private bool        _heavyChargeActive = false;
    private GameObject  _currentWeapon;
    private Transform   _lockedTarget;
    private GhostAI     _ghostAI;
    private LaserBeam   _laser;
    private Vector3     _moveInput;
    private float       _verticalInput;
    private bool        _jumpPressed;
    private bool        _dodgePressed;
    private bool        _lightPunchPressed;
    private bool        _heavyPunchHeld;
    private bool        _lightKickPressed;
    private bool        _heavyKickHeld;
    private bool        _kiHeld;
    private bool        _blockHeld;
    private bool        _lockOnPressed;
    private bool        _weaponTransformHeld;

    // ── Attack arc sweep timings ──────────────────────────────────────────
    private const float LIGHT_ATTACK_DURATION  = 0.30f;
    private const float HEAVY_ATTACK_DURATION  = 0.55f;
    private const float LIGHT_ATTACK_COOLDOWN  = 0.35f;
    private const float HEAVY_ATTACK_COOLDOWN  = 0.70f;

    // ─────────────────────────────────────────────────────────────────────
    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _currentWeapon = SwordRoot;
    }

    void Start()
    {
        // Bind gamepad by index
        if (Gamepad.all.Count > PlayerIndex)
            _pad = Gamepad.all[PlayerIndex];

        // Ghost AI
        if (IsGhost)
        {
            _ghostAI = gameObject.AddComponent<GhostAI>();
            _ghostAI.Controller = this;
        }

        // Laser beam component (manages the beam VFX + damage)
        _laser = gameObject.AddComponent<LaserBeam>();
        _laser.Controller    = this;
        _laser.BeamColor     = BodyColor;
        _laser.DamagePerTick = LaserDamagePerTick;
    }

    // ─────────────────────────────────────────────────────────────────────
    void Update()
    {
        if (IsGhost) return; // Ghost is driven by GhostAI

        PollInput();
        ApplySettings();
        HandleWeaponTransform();
        HandleDodgeCooldown();
        HandleKi();
        HandleLockOn();
        HandleAttackCooldown();
        HandleWeaponRotation();
    }

    void ApplySettings()
    {
        // Apply ninja scale
        if (transform.localScale.x != GameSettings.NinjaScale)
            transform.localScale = Vector3.one * GameSettings.NinjaScale;
    }

    void FixedUpdate()
    {
        if (IsGhost) return;

        CheckGrounded();
        HandleMovement();
        HandleJumpAndFlight();
        HandleDodge();
        HandleAttacks();
    }

    // ─────────────────────────────────────────────────────────────────────
    // INPUT POLLING
    // ─────────────────────────────────────────────────────────────────────
    // ─────────────────────────────────────────────────────────────────────
    void PollInput()
    {
        // Reset per-frame triggers
        _jumpPressed = false;
        _dodgePressed = false;
        _lightPunchPressed = false;
        _lightKickPressed = false;
        _lockOnPressed = false;

        if (IsGhost) return;

        // COMBINED INPUT: Keyboard (P1) + Gamepad (by PlayerIndex)
        Vector2 compositeMove = Vector2.zero;
        Vector2 compositeVertical = Vector2.zero;

        // 1. Keyboard (Player 1 Only) — UFC-style mapping
        if (PlayerIndex == 0 && Keyboard.current != null)
        {
            var k = Keyboard.current;
            if (k.wKey.isPressed) compositeMove.y += 1;
            if (k.sKey.isPressed) compositeMove.y -= 1;
            if (k.aKey.isPressed) compositeMove.x -= 1;
            if (k.dKey.isPressed) compositeMove.x += 1;

            if (k.eKey.isPressed) _verticalInput = 1f;
            else if (k.qKey.isPressed) _verticalInput = -1f;
            else _verticalInput = 0f;

            if (k.spaceKey.wasPressedThisFrame) _jumpPressed = true;
            if (k.leftShiftKey.wasPressedThisFrame) _dodgePressed = true;

            // UFC-style attack mapping
            if (k.jKey.wasPressedThisFrame) _lightPunchPressed = true;      // J = Light Punch
            _heavyPunchHeld = k.kKey.isPressed;                              // K = Heavy Punch
            if (k.uKey.wasPressedThisFrame) _lightKickPressed = true;         // U = Light Kick
            _heavyKickHeld = k.iKey.isPressed;                               // I = Heavy Kick

            _kiHeld = k.lKey.isPressed;
            _blockHeld = k.oKey.isPressed;
            _weaponTransformHeld = k.pKey.isPressed;
            if (k.fKey.wasPressedThisFrame) _lockOnPressed = true;
        }

        // 2. Gamepad — UFC-style button layout
        RefreshGamepad();
        if (_pad != null)
        {
            var ls = _pad.leftStick.ReadValue();
            var rs = _pad.rightStick.ReadValue();

            // Override keyboard if stick is moved
            if (ls.sqrMagnitude > 0.1f) compositeMove = ls;
            if (Mathf.Abs(rs.y) > 0.1f) _verticalInput = rs.y;

            // UFC Attack Mapping:
            // South (A on Xbox / Cross on PS4) = Jump
            // East (B on Xbox / Circle on PS4) = Dodge
            // Right Shoulder (RB/R1) = Light Punch
            // Right Trigger (RT/R2) = Heavy Punch
            // Left Shoulder (LB/L1) = Light Kick
            // Left Trigger (LT/L2) = Heavy Kick

            if (_pad.buttonSouth.wasPressedThisFrame) _jumpPressed = true;
            if (_pad.buttonEast.wasPressedThisFrame) _dodgePressed = true;
            if (_pad.rightShoulder.wasPressedThisFrame) _lightPunchPressed = true;
            _heavyPunchHeld = _pad.rightTrigger.ReadValue() > 0.5f;
            if (_pad.leftShoulder.wasPressedThisFrame) _lightKickPressed = true;
            _heavyKickHeld = _pad.leftTrigger.ReadValue() > 0.5f;

            _kiHeld = _pad.buttonNorth.isPressed;  // Y/Triangle for Ki
            _blockHeld = _pad.buttonWest.isPressed;  // X/Square for Block
            _weaponTransformHeld = _pad.rightStickButton.isPressed;  // Right stick click for weapon transform
            if (_pad.leftStickButton.wasPressedThisFrame) _lockOnPressed = true;  // Left stick click for lock-on
        }

        _moveInput = new Vector3(compositeMove.x, 0, compositeMove.y);
    }

    void RefreshGamepad()
    {
        if (_pad == null && Gamepad.all.Count > PlayerIndex)
            _pad = Gamepad.all[PlayerIndex];
    }

    // ─────────────────────────────────────────────────────────────────────
    // MOVEMENT
    // ─────────────────────────────────────────────────────────────────────
    void HandleMovement()
    {
        if (_moveInput.sqrMagnitude < 0.01f) return;

        // Camera-relative movement
        var cam = Camera.allCameras.Length > PlayerIndex ? Camera.allCameras[PlayerIndex] : Camera.main;
        Vector3 camForward = cam != null ? Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up).normalized : Vector3.forward;
        Vector3 camRight   = cam != null ? cam.transform.right : Vector3.right;

        Vector3 worldDir = camForward * _moveInput.z + camRight * _moveInput.x;
        worldDir.Normalize();

        float speed = IsFlying ? GameSettings.PlayerAirSpeed : GameSettings.PlayerGroundSpeed;
        _rb.linearDamping = IsFlying ? AirDrag : GroundDrag;

        Vector3 targetVel = worldDir * speed;
        Vector3 currentHoriz = new Vector3(_rb.linearVelocity.x, 0, _rb.linearVelocity.z);
        Vector3 velDiff = targetVel - currentHoriz;

        // Apply a force towards target velocity (UFC5 feel: snappy but momentum-aware)
        float accel = IsGrounded ? 40f : 22f;
        _rb.AddForce(velDiff * accel, ForceMode.Acceleration);

        // Face movement direction
        if (worldDir.sqrMagnitude > 0.01f)
        {
            if (_lockedTarget != null)
            {
                Vector3 toTarget = (_lockedTarget.position - transform.position).normalized;
                toTarget.y = 0;
                if (toTarget.sqrMagnitude > 0.01f)
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(toTarget), 12f * Time.fixedDeltaTime);
            }
            else
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(worldDir), 14f * Time.fixedDeltaTime);
            }
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    // JUMP & FLIGHT
    // ─────────────────────────────────────────────────────────────────────
    void HandleJumpAndFlight()
    {
        // Jump from ground
        if (_jumpPressed && IsGrounded && !IsFlying)
        {
            _rb.AddForce(Vector3.up * JumpForce, ForceMode.VelocityChange); // Still using JumpForce field, but could move if needed
            IsFlying = true;
        }

        // Flight: right stick Y controls altitude
        if (IsFlying || !IsGrounded)
        {
            IsFlying = true;

            if (_verticalInput > 0.3f && transform.position.y < GameSettings.PlayerMaxFlightAltitude)
            {
                _rb.AddForce(Vector3.up * AscendForce * _verticalInput, ForceMode.Acceleration);
            }
            else if (_verticalInput < -0.3f && transform.position.y > 0.5f)
            {
                _rb.AddForce(Vector3.down * DescendForce * Mathf.Abs(_verticalInput), ForceMode.Acceleration);
            }
            else if (IsFlying)
            {
                // Hover — counteract gravity partially
                float gravComp = -Physics.gravity.y * _rb.mass * 0.85f * GameSettings.GravityMultiplier;
                _rb.AddForce(Vector3.up * gravComp, ForceMode.Force);
            }

            // Apply extra gravity for "heavy" feel if not hovering/ascending
            if (!IsGrounded)
            {
                _rb.AddForce(Physics.gravity * (GameSettings.GravityMultiplier - 1f), ForceMode.Acceleration);
            }

            // Landing check handled in CheckGrounded
        }
    }

    void CheckGrounded()
    {
        bool wasGrounded = IsGrounded;
        float fallSpeed = -_rb.linearVelocity.y;

        IsGrounded = Physics.Raycast(transform.position + Vector3.up * 0.1f, Vector3.down, 0.3f, LayerMask.GetMask("Default"));

        if (IsGrounded && !wasGrounded)
        {
            IsFlying = false;

            // STOMP MECHANIC
            if (fallSpeed > GameSettings.StompHeightThreshold)
            {
                TriggerStomp(fallSpeed);
            }
        }
    }

    void TriggerStomp(float intensity)
    {
        // VFX
        SpawnStompVFX();

        // Shake camera
        SplitScreenCamera.ShakeCamera(PlayerIndex, 0.4f, 0.5f);

        // Physics Blast
        float force = GameSettings.StompForce * (intensity * 0.5f);
        float radius = GameSettings.StompRadius;

        Collider[] hits = Physics.OverlapSphere(transform.position, radius);
        foreach (var h in hits)
        {
            var rb = h.GetComponent<Rigidbody>();
            if (rb != null && rb != _rb)
            {
                Vector3 dir = (h.transform.position - transform.position).normalized;
                dir.y = 0.5f; // push up slightly
                rb.AddForce(dir * force, ForceMode.Impulse);
            }

            var eb = h.GetComponentInParent<EnemyBase>();
            if (eb != null) eb.TakeDamage(intensity * 2f, transform);
        }
    }

    void SpawnStompVFX()
    {
        var obj = new GameObject("StompVFX");
        obj.transform.position = transform.position;
        var ps = obj.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.startLifetime = 0.5f;
        main.startSpeed = 15f;
        main.startSize = 0.5f;
        main.startColor = new Color(0.5f, 0.4f, 0.3f, 0.8f);
        main.maxParticles = 50;
        main.loop = false;

        var em = ps.emission;
        em.SetBursts(new[] { new ParticleSystem.Burst(0f, 50) });
        em.rateOverTime = 0;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.5f;

        ps.Play();
        Destroy(obj, 1f);
    }

    // ─────────────────────────────────────────────────────────────────────
    // DODGE / AIR-DASH  (East button + direction)
    // ─────────────────────────────────────────────────────────────────────
    void HandleDodge()
    {
        if (!_dodgePressed || _dodgeTimer > 0) return;

        _dodgeTimer = DodgeCooldown;

        Vector3 dodgeDir;
        if (_moveInput.sqrMagnitude > 0.1f)
            dodgeDir = transform.TransformDirection(new Vector3(_moveInput.x, 0, _moveInput.z)).normalized;
        else
            dodgeDir = -transform.forward; // back-dash as default

        // Zero out opposing velocity on dodge axis for clean feel
        Vector3 vel = _rb.linearVelocity;
        _rb.linearVelocity = new Vector3(0, vel.y * 0.5f, 0);
        _rb.AddForce(dodgeDir * GameSettings.PlayerDodgeForce, ForceMode.VelocityChange);

        StartCoroutine(DodgeInvincibility(0.15f));
    }

    void HandleDodgeCooldown()
    {
        if (_dodgeTimer > 0) _dodgeTimer -= Time.deltaTime;
    }

    IEnumerator DodgeInvincibility(float duration)
    {
        var health = GetComponent<PlayerHealth>();
        if (health) health.SetInvincible(true);
        yield return new WaitForSeconds(duration);
        if (health) health.SetInvincible(false);
    }

    // ─────────────────────────────────────────────────────────────────────
    // ATTACKS
    // ─────────────────────────────────────────────────────────────────────
    void HandleAttacks()
    {
        if (_attackCooldown > 0 || IsChargingKi) return;

        // Light Punch (RB/R1)
        if (_lightPunchPressed)
        {
            LastAttackType = 1;
            StartCoroutine(PerformLightAttack());
        }

        // Heavy Punch (RT/R2 hold)
        if (_heavyPunchHeld)
        {
            _heavyChargeTimer += Time.fixedDeltaTime;
            _heavyChargeActive = true;
        }
        else if (_heavyChargeActive)
        {
            _heavyChargeActive = false;
            LastAttackType = 2;
            StartCoroutine(PerformHeavyAttack(_heavyChargeTimer));
            _heavyChargeTimer = 0f;
        }

        // Light Kick (LB/L1)
        if (_lightKickPressed)
        {
            LastAttackType = 3;
            StartCoroutine(PerformLightKick());
        }

        // Heavy Kick (LT/L2 hold)
        if (_heavyKickHeld)
        {
            _heavyChargeTimer += Time.fixedDeltaTime;
            _heavyChargeActive = true;
        }
        else if (_heavyChargeActive && _heavyKickHeld == false)
        {
            _heavyChargeActive = false;
            LastAttackType = 4;
            StartCoroutine(PerformHeavyKick(_heavyChargeTimer));
            _heavyChargeTimer = 0f;
        }
    }

    IEnumerator PerformLightAttack()
    {
        IsAttacking = true;
        _attackCooldown = LIGHT_ATTACK_COOLDOWN;

        // Lunge forward slightly
        _rb.AddForce(transform.forward * 5f, ForceMode.VelocityChange);

        float damage = IsStaffMode ? GameSettings.StaffLightDamage : GameSettings.SwordLightDamage;
        float range  = IsStaffMode ? 2.5f : 1.8f;
        float angle  = IsStaffMode ? 180f : 90f;

        ActivateWeaponHitbox(damage, range, angle, LIGHT_ATTACK_DURATION);

        // Subtle hit-stop effect on contact (handled in WeaponHitbox)
        yield return new WaitForSeconds(LIGHT_ATTACK_DURATION);
        IsAttacking = false;
    }

    IEnumerator PerformHeavyAttack(float chargeTime)
    {
        IsAttacking = true;
        _attackCooldown = HEAVY_ATTACK_COOLDOWN;

        chargeTime = Mathf.Clamp(chargeTime, 0.1f, 1.5f);
        float damageMultiplier = 1f + chargeTime;
        float damage = (IsStaffMode ? GameSettings.StaffHeavyDamage : GameSettings.SwordHeavyDamage) * damageMultiplier;

        // Strong downward slam if in air
        if (IsFlying)
        {
            _rb.linearVelocity = new Vector3(_rb.linearVelocity.x, -20f, _rb.linearVelocity.z);
        }
        else
        {
            _rb.AddForce(transform.forward * 8f, ForceMode.VelocityChange);
        }

        float range = IsStaffMode ? 4f : 2.5f;
        ActivateWeaponHitbox(damage, range, 360f, HEAVY_ATTACK_DURATION);

        yield return new WaitForSeconds(HEAVY_ATTACK_DURATION);
        IsAttacking = false;
    }

    IEnumerator PerformLightKick()
    {
        IsAttacking = true;
        _attackCooldown = LIGHT_ATTACK_COOLDOWN;

        // Slight forward momentum on kick
        _rb.AddForce(transform.forward * 6f, ForceMode.VelocityChange);

        float damage = GameSettings.SwordLightDamage * 0.9f;  // Kicks slightly weaker than punches
        float range = 2.0f;  // Slightly longer range than punches
        float angle = 120f;  // Wider kick arc

        ActivateWeaponHitbox(damage, range, angle, LIGHT_ATTACK_DURATION);

        yield return new WaitForSeconds(LIGHT_ATTACK_DURATION);
        IsAttacking = false;
    }

    IEnumerator PerformHeavyKick(float chargeTime)
    {
        IsAttacking = true;
        _attackCooldown = HEAVY_ATTACK_COOLDOWN;

        chargeTime = Mathf.Clamp(chargeTime, 0.1f, 1.5f);
        float damageMultiplier = 1f + chargeTime;
        float damage = GameSettings.SwordHeavyDamage * 1.1f * damageMultiplier;  // Heavy kicks pack more punch

        // Strong forward thrust on heavy kick
        _rb.AddForce(transform.forward * 12f, ForceMode.VelocityChange);

        float range = 3f;
        ActivateWeaponHitbox(damage, range, 180f, HEAVY_ATTACK_DURATION);

        yield return new WaitForSeconds(HEAVY_ATTACK_DURATION);
        IsAttacking = false;
    }

    void HandleAttackCooldown()
    {
        if (_attackCooldown > 0) _attackCooldown -= Time.deltaTime;
    }

    void ActivateWeaponHitbox(float damage, float range, float arc, float duration)
    {
        var hitboxes = GetComponentsInChildren<WeaponHitbox>();
        foreach (var hb in hitboxes)
            StartCoroutine(hb.Activate(damage, range, arc, duration, _lockedTarget));
    }

    // ─────────────────────────────────────────────────────────────────────
    // KI / LASER
    // ─────────────────────────────────────────────────────────────────────
    void HandleKi()
    {
        if (_kiHeld && CurrentKi > 0f)
        {
            IsChargingKi = true;
            CurrentKi = Mathf.Max(0, CurrentKi - KiDrainRate * Time.deltaTime);
        }
        else
        {
            if (IsChargingKi)
            {
                IsChargingKi = false;
                IsFiringLaser = true;
                _laser.Fire(CurrentKi);
                StartCoroutine(ResetLaser());
            }
            // Regen
            CurrentKi = Mathf.Min(GameSettings.PlayerMaxKi, CurrentKi + GameSettings.KiRechargeRate * Time.deltaTime);
        }

        IsBlocking = _blockHeld;
    }

    IEnumerator ResetLaser()
    {
        yield return new WaitForSeconds(0.5f);
        IsFiringLaser = false;
    }

    // ─────────────────────────────────────────────────────────────────────
    // WEAPON TRANSFORM  (hold West 1s)
    // ─────────────────────────────────────────────────────────────────────
    void HandleWeaponTransform()
    {
        if (_weaponTransformHeld)
        {
            WeaponHoldTimer += Time.deltaTime;
            if (WeaponHoldTimer >= WeaponTransformHold)
            {
                WeaponHoldTimer = 0f;
                StartCoroutine(TransformWeapon());
            }
        }
        else
        {
            WeaponHoldTimer = Mathf.Max(0, WeaponHoldTimer - Time.deltaTime * 2f);
        }
    }

    IEnumerator TransformWeapon()
    {
        IsAttacking = true; // lock attacks during transform

        // Destroy old weapon
        if (_currentWeapon != null)
        {
            // Burst particle effect
            SpawnTransformVFX(_currentWeapon.transform.position);
            Destroy(_currentWeapon);
        }

        IsStaffMode = !IsStaffMode;

        yield return new WaitForSeconds(0.1f);

        // Build new weapon via bootstrapper helper
        _currentWeapon = ReflectBuildWeapon(IsStaffMode);

        yield return new WaitForSeconds(0.3f);
        IsAttacking = false;
    }

    // Uses reflection-like approach to call the builder without duplicating code
    GameObject ReflectBuildWeapon(bool asStaff)
    {
        // We re-use the GameBootstrapper's public method via a local workaround
        // (since BuildWeapon is private — we replicate the essentials inline)
        var wRoot = new GameObject(asStaff ? "Staff" : "Sword");
        wRoot.transform.SetParent(WeaponHolder);
        wRoot.transform.localPosition = Vector3.zero;
        wRoot.transform.localRotation = Quaternion.identity;

        // Blade / pole
        var main = GameObject.CreatePrimitive(asStaff ? PrimitiveType.Cylinder : PrimitiveType.Cube);
        main.transform.SetParent(wRoot.transform);
        main.transform.localPosition = new Vector3(0, 0.55f, 0);
        main.transform.localScale    = asStaff ? new Vector3(0.06f, 0.75f, 0.06f) : new Vector3(0.05f, 0.7f, 0.008f);
        Destroy(main.GetComponent<Collider>());

        var mat = new Material(GameBootstrapper.GetHDRPLitShader());
        mat.color = asStaff ? new Color(0.3f, 0.15f, 0.05f) : new Color(0.85f, 0.92f, 1.00f);
        GameBootstrapper.SetHDRPEmission(mat, BodyColor, 6f);
        main.GetComponent<Renderer>().material = mat;

        // Hitbox
        var hb = new GameObject("WeaponHitbox");
        hb.transform.SetParent(wRoot.transform);
        hb.transform.localPosition = new Vector3(0, 0.5f, 0);
        var bc = hb.AddComponent<BoxCollider>();
        bc.isTrigger = true;
        bc.size = asStaff ? new Vector3(0.12f, 1.5f, 0.12f) : new Vector3(0.08f, 1.2f, 0.08f);
        hb.AddComponent<WeaponHitbox>();

        return wRoot;
    }

    void SpawnTransformVFX(Vector3 position)
    {
        var vfxObj = new GameObject("TransformVFX");
        vfxObj.transform.position = position;
        var ps   = vfxObj.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.duration     = 0.4f;
        main.loop         = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.3f, 0.6f);
        main.startSpeed   = new ParticleSystem.MinMaxCurve(4f, 10f);
        main.startSize    = new ParticleSystem.MinMaxCurve(0.1f, 0.25f);
        main.startColor   = new ParticleSystem.MinMaxGradient(BodyColor, Color.white);
        main.maxParticles = 60;

        var em = ps.emission;
        var burst = new ParticleSystem.Burst(0f, 60);
        em.SetBursts(new[] { burst });
        em.rateOverTime = 0;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius    = 0.3f;

        ps.Play();
        Destroy(vfxObj, 1f);
    }

    // ─────────────────────────────────────────────────────────────────────
    // LOCK-ON
    // ─────────────────────────────────────────────────────────────────────
    void HandleLockOn()
    {
        if (!_lockOnPressed) return;

        if (_lockedTarget != null)
        {
            _lockedTarget = null;
            return;
        }

        // Find nearest enemy
        var enemies = FindObjectsByType<EnemyBase>(FindObjectsSortMode.None);
        float nearest = float.MaxValue;
        foreach (var e in enemies)
        {
            if (!e.IsActiveEnemy) continue;
            float d = Vector3.Distance(transform.position, e.transform.position);
            if (d < nearest) { nearest = d; _lockedTarget = e.transform; }
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    // WEAPON ROTATION (face locked target)
    // ─────────────────────────────────────────────────────────────────────
    void HandleWeaponRotation()
    {
        if (WeaponHolder == null) return;
        if (_lockedTarget != null)
        {
            Vector3 toTarget = (_lockedTarget.position - WeaponHolder.position).normalized;
            WeaponHolder.rotation = Quaternion.Slerp(WeaponHolder.rotation,
                Quaternion.LookRotation(toTarget), 8f * Time.deltaTime);
        }
        else
        {
            WeaponHolder.localRotation = Quaternion.Slerp(WeaponHolder.localRotation, Quaternion.identity, 6f * Time.deltaTime);
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    // PUBLIC API (used by GhostAI)
    // ─────────────────────────────────────────────────────────────────────
    public void AIMove(Vector3 direction, bool fly)
    {
        _moveInput    = direction;
        _verticalInput = fly ? 1f : 0f;
    }

    public void AIAttackLight()      => StartCoroutine(PerformLightAttack());
    public void AIAttackHeavy()      => StartCoroutine(PerformHeavyAttack(0.5f));
    public void AIAttackLightKick()  => StartCoroutine(PerformLightKick());
    public void AIAttackHeavyKick()  => StartCoroutine(PerformHeavyKick(0.5f));

    public void AIFireLaser()
    {
        IsChargingKi  = false;
        IsFiringLaser = true;
        float charge  = CurrentKi * 0.4f;
        _laser.Fire(charge);
        StartCoroutine(ResetLaser());
    }

    public Gamepad RefreshGamepadForInteractor()
    {
        RefreshGamepad();
        return _pad;
    }
}
