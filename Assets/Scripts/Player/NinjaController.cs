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
    private Animator    _anim;
    
    // Procedural animation parts (if primitives are used)
    private Transform _armL, _armR, _legL, _legR, _body;
    private float _walkCycleTime = 0f;

    private Gamepad     _pad;
    private float       _dodgeTimer        = 0f;
    private float       _attackCooldown    = 0f;
    private float       _speedBlend        = 0f;
    private float       _heavyChargeTimer  = 0f;
    private bool        _heavyChargeActive = false;
    [HideInInspector] public bool  IsIntroDive       = true;
    [HideInInspector] public bool  IsFlipping        = false;

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
    private bool        _gravityPullHeld;
    private bool        _gravityPushPressed;

    // ── Attack arc sweep timings ──────────────────────────────────────────
    private const float LIGHT_ATTACK_DURATION  = 0.30f;
    private const float HEAVY_ATTACK_DURATION  = 0.55f;
    private const float LIGHT_ATTACK_COOLDOWN  = 0.35f;
    private const float HEAVY_ATTACK_COOLDOWN  = 0.70f;

    // ─────────────────────────────────────────────────────────────────────
    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        // We do NOT set _currentWeapon = SwordRoot here, so player starts empty
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

        // Initialize Animator
        _anim = GetComponentInChildren<Animator>();
        if (_anim == null) _anim = gameObject.AddComponent<Animator>();

        // Find primitive body parts for procedural animation
        _body = transform.Find("Body");
        _armL = transform.Find("ArmL");
        _armR = transform.Find("ArmR");
        _legL = transform.Find("LegL");
        _legR = transform.Find("LegR");

        if (SwordRoot != null) SwordRoot.SetActive(false); // Hide sword at start
        _currentWeapon = null;

        // Laser beam component (manages the beam VFX + damage)
        _laser = gameObject.AddComponent<LaserBeam>();
        _laser.Controller    = this;
        _laser.BeamColor     = BodyColor;
        _laser.DamagePerTick = LaserDamagePerTick;

        var animSet = AnimationLibrary.LoadAnimations();
        _anim.runtimeAnimatorController = AnimatorControllerBuilder.GetOrCreateController(animSet);
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
        HandleGravityPowers();
        HandleWeaponRotation();
        
        if (IsIntroDive) HandleIntroDiveEffects();
        
        HandleBackflipJuice();
        UpdateAnimator();
    }

    public void ApplySpeedBoost(float duration, float multiplier)
    {
        StartCoroutine(SpeedBoostRoutine(duration, multiplier));
    }

    private IEnumerator SpeedBoostRoutine(float duration, float multiplier)
    {
        float origGround = GroundSpeed;
        float origAir = AirSpeed;
        GroundSpeed *= multiplier;
        AirSpeed *= multiplier;
        GameBootstrapper.SpawnAlert(transform, false); // Visual feedback

        yield return new WaitForSeconds(duration);

        GroundSpeed = origGround;
        AirSpeed = origAir;
    }

    public void ApplyDamageBoost(float duration, float multiplier)
    {
        StartCoroutine(DamageBoostRoutine(duration, multiplier));
    }

    private IEnumerator DamageBoostRoutine(float duration, float multiplier)
    {
        float origLight = LightAttackDamage;
        float origHeavy = HeavyAttackDamage;
        LightAttackDamage *= multiplier;
        HeavyAttackDamage *= multiplier;
        GameBootstrapper.SpawnAlert(transform, true); // Visual feedback (Danger/Red)

        yield return new WaitForSeconds(duration);

        LightAttackDamage = origLight;
        HeavyAttackDamage = origHeavy;
    }

    void UpdateAnimator()
    {
        if (_anim == null) return;

        // Speed blending: Smoothed normalized speed (0 to 1)
        float horizontalSpeed = new Vector3(_rb.linearVelocity.x, 0, _rb.linearVelocity.z).magnitude;
        float targetSpeedScale = horizontalSpeed / GameSettings.PlayerGroundSpeed;
        
        // Sprint logic: shift scale if > 1
        if (horizontalSpeed > GameSettings.PlayerGroundSpeed + 1f)
            targetSpeedScale = 1.2f; 

        _speedBlend = Mathf.Lerp(_speedBlend, horizontalSpeed > 0.1f ? targetSpeedScale : 0f, Time.deltaTime * 6f);

        _anim.SetFloat("Speed", _speedBlend);
        _anim.SetBool("IsFlying", IsFlying);
        _anim.SetBool("IsAttacking", IsAttacking);
        _anim.SetInteger("AttackType", IsAttacking ? LastAttackType : 0);
        _anim.SetBool("IsChargingKi", IsChargingKi);

        // Procedural Animation for Primitives
        if (_body != null && _armL != null) 
        {
            if (horizontalSpeed > 0.1f && IsGrounded && !IsAttacking)
            {
                _walkCycleTime += Time.deltaTime * (horizontalSpeed * 1.5f);
                float swing = Mathf.Sin(_walkCycleTime) * 35f; // 35 degrees swing
                float walkBob = Mathf.Abs(Mathf.Sin(_walkCycleTime * 2f)) * 0.15f; 
                
                _armL.localRotation = Quaternion.Euler(swing, 0, 0);
                _armR.localRotation = Quaternion.Euler(-swing, 0, 0);
                _legL.localRotation = Quaternion.Euler(-swing, 0, 0);
                _legR.localRotation = Quaternion.Euler(swing, 0, 0);

                // Body bobbing up and down
                _body.localPosition = new Vector3(0, 1f + walkBob, 0);
            }
            else
            {
                // Reset to idle
                _walkCycleTime = 0f;
                _armL.localRotation = Quaternion.Lerp(_armL.localRotation, Quaternion.identity, Time.deltaTime * 10f);
                _armR.localRotation = Quaternion.Lerp(_armR.localRotation, Quaternion.identity, Time.deltaTime * 10f);
                _legL.localRotation = Quaternion.Lerp(_legL.localRotation, Quaternion.identity, Time.deltaTime * 10f);
                _legR.localRotation = Quaternion.Lerp(_legR.localRotation, Quaternion.identity, Time.deltaTime * 10f);
                _body.localPosition = Vector3.Lerp(_body.localPosition, new Vector3(0, 1f, 0), Time.deltaTime * 10f);
            }

            // Flying pose 
            if (IsFlying && !IsGrounded)
            {
                _armL.localRotation = Quaternion.Lerp(_armL.localRotation, Quaternion.Euler(-160f, 0, 20f), Time.deltaTime * 8f);
                _armR.localRotation = Quaternion.Lerp(_armR.localRotation, Quaternion.Euler(-160f, 0, -20f), Time.deltaTime * 8f);
                _legL.localRotation = Quaternion.Lerp(_legL.localRotation, Quaternion.Euler(-20f, 0, 0), Time.deltaTime * 8f);
                _legR.localRotation = Quaternion.Lerp(_legR.localRotation, Quaternion.Euler(-20f, 0, 0), Time.deltaTime * 8f);
                _body.localRotation = Quaternion.Lerp(_body.localRotation, Quaternion.Euler(20f, 0, 0), Time.deltaTime * 5f);
            }
            else 
            {
                _body.localRotation = Quaternion.Lerp(_body.localRotation, Quaternion.identity, Time.deltaTime * 10f);
            }
        }
    }

    void ApplySettings()
    {
        // Apply ninja scale
        if (transform.localScale.x != GameSettings.NinjaScale)
            transform.localScale = Vector3.one * GameSettings.NinjaScale;
    }

    void FixedUpdate()
    {
        CheckGrounded();
        
        if (IsIntroDive)
        {
            HandleIntroDive();
            return;
        }

        if (IsGhost) return;

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
        _gravityPushPressed = false;
        _verticalInput = 0f; // Reset vertical input for each frame
        _heavyPunchHeld = false; // Reset held states
        _heavyKickHeld = false;
        _kiHeld = false;
        _blockHeld = false;
        _gravityPullHeld = false;
        _weaponTransformHeld = false;


        if (IsGhost) return;

        Vector2 compositeMove = Vector2.zero;
        _moveInput = Vector3.zero;

        // 1. Keyboard P1: LEFT SIDE (WASD + Space + J/K/L/U/I + Mouse)
        if (PlayerIndex == 0)
        {
            var k = Keyboard.current;
            var m = Mouse.current;
            if (k != null)
            {
                if (k.wKey.isPressed) compositeMove.y += 1;
                if (k.sKey.isPressed) compositeMove.y -= 1;
                if (k.aKey.isPressed) compositeMove.x -= 1;
                if (k.dKey.isPressed) compositeMove.x += 1;
                if (k.eKey.isPressed) _verticalInput = 1f;
                if (k.qKey.isPressed) _verticalInput = -1f;
                if (k.spaceKey.wasPressedThisFrame) _jumpPressed = true;
                if (k.leftShiftKey.wasPressedThisFrame) _dodgePressed = true;
                if (k.jKey.wasPressedThisFrame) _lightPunchPressed = true;
                _heavyPunchHeld = k.kKey.isPressed;
                if (k.uKey.wasPressedThisFrame) _lightKickPressed = true;
                _heavyKickHeld = k.iKey.isPressed;
                _kiHeld = k.lKey.isPressed;
                _blockHeld = k.oKey.isPressed;
                if (k.fKey.wasPressedThisFrame) _lockOnPressed = true;
            }
            if (m != null)
            {
                if (m.leftButton.wasPressedThisFrame) _lightPunchPressed = true;
                if (m.rightButton.isPressed) _heavyPunchHeld = true;
            }
        }

        // 2. Keyboard P2: RIGHT SIDE (Arrows + R-Ctrl + Numpad / [ ] \ )
        if (PlayerIndex == 1)
        {
            var k = Keyboard.current;
            if (k != null)
            {
                if (k.upArrowKey.isPressed) compositeMove.y += 1;
                if (k.downArrowKey.isPressed) compositeMove.y -= 1;
                if (k.leftArrowKey.isPressed) compositeMove.x -= 1;
                if (k.rightArrowKey.isPressed) compositeMove.x += 1;
                
                // Flight / Vert
                if (k.numpad8Key.isPressed || k.rightBracketKey.isPressed) _verticalInput = 1f;
                if (k.numpad2Key.isPressed || k.leftBracketKey.isPressed) _verticalInput = -1f;

                if (k.rightCtrlKey.wasPressedThisFrame || k.numpad0Key.wasPressedThisFrame) _jumpPressed = true;
                if (k.rightShiftKey.wasPressedThisFrame || k.numpadPeriodKey.wasPressedThisFrame) _dodgePressed = true;

                // Numpad Attacks (or symbols if no numpad)
                if (k.numpad7Key.wasPressedThisFrame || k.pKey.wasPressedThisFrame) _lightPunchPressed = true;
                _heavyPunchHeld = k.numpad8Key.isPressed || k.backslashKey.isPressed;
                if (k.numpad4Key.wasPressedThisFrame || k.semicolonKey.wasPressedThisFrame) _lightKickPressed = true;
                _heavyKickHeld = k.numpad5Key.isPressed || k.quoteKey.isPressed;
                
                _kiHeld = k.numpadEnterKey.isPressed || k.enterKey.isPressed;
                _blockHeld = k.numpadDivideKey.isPressed || k.slashKey.isPressed;
            }
        }

        // 3. Gamepad — Shared
        RefreshGamepad();
        if (_pad != null)
        {
            var ls = _pad.leftStick.ReadValue();
            if (ls.sqrMagnitude > 0.1f) compositeMove = ls;
            
            // Altitude control
            float rsY = _pad.rightStick.ReadValue().y;
            if (Mathf.Abs(rsY) > 0.1f) _verticalInput = rsY;
            
            // D-Pad backup for altitude
            if (_pad.dpad.up.isPressed) _verticalInput = 1f;
            else if (_pad.dpad.down.isPressed) _verticalInput = -1f;

            if (_pad.buttonSouth.wasPressedThisFrame) _jumpPressed = true;
            
            bool rbModifier = _pad.rightShoulder.isPressed;
            if (rbModifier)
            {
                if (_pad.buttonWest.wasPressedThisFrame) _lightPunchPressed = true;
                _heavyPunchHeld = _pad.buttonWest.isPressed;
                if (_pad.buttonNorth.wasPressedThisFrame) _lightKickPressed = true;
                _heavyKickHeld = _pad.buttonNorth.isPressed;
                _kiHeld = _pad.buttonEast.isPressed;
            }
            else
            {
                if (_pad.buttonWest.wasPressedThisFrame) _lightPunchPressed = true;
                if (_pad.buttonNorth.wasPressedThisFrame) _lightKickPressed = true;
                _kiHeld = _pad.buttonEast.isPressed;
            }

            _gravityPullHeld = _pad.leftTrigger.ReadValue() > 0.5f;
            if (_pad.rightTrigger.wasPressedThisFrame) _gravityPushPressed = true;
            _blockHeld = _pad.leftShoulder.isPressed; 
            _weaponTransformHeld = _pad.rightStickButton.isPressed;
            if (_pad.leftStickButton.wasPressedThisFrame) _lockOnPressed = true;
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

            // 2. JUMP HANG - Reduce gravity at the peak for better "Nintendo" feel
            float yVel = _rb.linearVelocity.y;
            float gravityScale = 1f;
            if (Mathf.Abs(yVel) < 2f && !IsGrounded) gravityScale = 0.5f;

            // Apply extra gravity for "heavy" feel if not hovering/ascending, but respect the hang
            if (!IsGrounded)
            {
                _rb.AddForce(Physics.gravity * (GameSettings.GravityMultiplier * gravityScale - 1f), ForceMode.Acceleration);
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

        // Nintendo Synergy: Totem Resonator!
        // If the player stomps the Totem, it amplifies the blast significantly, heals the village, and calms all villagers.
        if (Village.Instance != null && Vector3.Distance(transform.position, Village.Instance.TotemPosition) <= GameSettings.TotemHealRadius * 1.5f)
        {
            force *= 3f;
            radius *= 2.5f;
            intensity *= 2f;
            
            // Mega shake
            SplitScreenCamera.ShakeCamera(PlayerIndex, 1.2f, 0.8f);

            // Heal a burst of HP to the village
            Village.Instance.Heal(100f);

            // Calm panicked villagers
            var allVillagers = FindObjectsByType<Villager>(FindObjectsSortMode.None);
            foreach (var v in allVillagers)
            {
                v.CalmDown();
                v.Celebrate(2f); // Make them cheer for the god!
            }
            
            GameBootstrapper.SpawnAlert(transform, true); // Visual feedback
        }

        if (VillageFireSystem.Instance != null)
            VillageFireSystem.Instance.ExtinguishFires(transform.position, radius);

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

    void HandleIntroDive()
    {
        // 1. Force extreme downward velocity for that DBZ/Skydiving feel
        _rb.linearVelocity = new Vector3(_rb.linearVelocity.x, -60f, _rb.linearVelocity.z);

        // 2. Allow slight aerial control
        Vector3 camForward = Camera.main.transform.forward;
        camForward.y = 0;
        Vector3 worldDir = Camera.main.transform.right * _moveInput.x + camForward * _moveInput.z;
        _rb.AddForce(worldDir * 40f, ForceMode.Acceleration);

        // 3. BACKFLIP!
        if (_jumpPressed && !IsFlipping)
        {
            StartCoroutine(PerformBackflip());
        }

        // 4. Grounding ends it
        if (IsGrounded || transform.position.y < 1.5f)
        {
            IsIntroDive = false;
            TriggerStomp(70f); // Massive landing
            SplitScreenCamera.ShakeCamera(PlayerIndex, 1.4f, 1f);
            SpawnSonicBoom();
        }
    }

    void SpawnSonicBoom()
    {
        var boom = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        boom.name = "SonicBoom";
        boom.transform.position = transform.position + Vector3.up * 0.1f;
        boom.transform.localScale = new Vector3(1f, 0.05f, 1f);
        Destroy(boom.GetComponent<Collider>());
        
        var mat = new Material(GameBootstrapper.GetHDRPLitShader());
        mat.color = new Color(1, 1, 1, 0.5f);
        GameBootstrapper.SetHDRPEmission(mat, Color.white, 8f);
        boom.GetComponent<Renderer>().material = mat;

        StartCoroutine(AnimateSonicBoom(boom));
    }

    IEnumerator AnimateSonicBoom(GameObject boom)
    {
        float t = 0;
        while (t < 0.5f)
        {
            t += Time.deltaTime;
            float s = t * 40f;
            boom.transform.localScale = new Vector3(s, 0.05f, s);
            yield return null;
        }
        Destroy(boom);
    }

    void HandleBackflipJuice()
    {
        // Occasional flip text if spinning fast
        if (IsFlipping && Random.value < 0.05f)
        {
            GameBootstrapper.SpawnAlert(transform, false);
        }
    }

    void HandleIntroDiveEffects()
    {
        // Wind streaks flying past the player to simulate high-speed fall
        if (Random.value < 0.8f) // Spawn very frequently
        {
            var spark = new GameObject("WindStreak");
            spark.transform.position = transform.position + Random.insideUnitSphere * 1.5f + Vector3.down * 1f;
            spark.transform.rotation = Quaternion.Euler(-90, 0, 0); // Point straight UP so particles fly past

            var ps = spark.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.startColor = new Color(1f, 1f, 1f, 0.4f); // Semi-transparent white
            main.startSize = Random.Range(0.02f, 0.08f);
            main.startLifetime = 0.4f;
            main.startSpeed = Random.Range(40f, 90f); // VERY fast
            main.maxParticles = 5;

            var renderer = spark.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Stretch;
            renderer.lengthScale = 15f; // Extremely long stretched streaks
            renderer.velocityScale = 0.05f;

            ps.Play();
            Destroy(spark, 0.5f);
        }
    }

    IEnumerator PerformBackflip()
    {
        IsFlipping = true;
        float elapsed = 0f;
        float duration = 0.28f; // very snappy flip
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            transform.Rotate(Vector3.right, 360f * (Time.deltaTime / duration));
            yield return null;
        }
        
        IsFlipping = false;
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
            float comboMulti = ComboSystem.Instance != null ? ComboSystem.Instance.GetMultiplier(PlayerIndex) : 1f;

            // Nintendo Synergy: Worshipper Power!
            // If the player is near the Totem, each active worshipper provides a 10% boost to Ki recharge speed.
            float worshipMultiplier = 1f;
            if (Village.Instance != null && Vector3.Distance(transform.position, Village.Instance.TotemPosition) <= GameSettings.TotemHealRadius * 2f)
            {
                worshipMultiplier += Village.Instance.ActiveWorshippers * 0.1f;
            }

            CurrentKi = Mathf.Min(GameSettings.PlayerMaxKi, CurrentKi + GameSettings.KiRechargeRate * comboMulti * worshipMultiplier * Time.deltaTime);
        }

        IsBlocking = _blockHeld;
    }

    void HandleGravityPowers()
    {
        if (_gravityPullHeld)
        {
            ApplyGravityPull();
        }
        
        if (_gravityPushPressed)
        {
            ApplyGravityPush();
        }
    }

    void ApplyGravityPull()
    {
        float radius = 15f * GameSettings.NinjaScale;
        float pullForce = 25f;
        Collider[] hits = Physics.OverlapSphere(transform.position, radius);
        
        foreach (var h in hits)
        {
            var rb = h.GetComponent<Rigidbody>();
            if (rb != null && rb != _rb)
            {
                Vector3 dir = (transform.position - h.transform.position).normalized;
                rb.AddForce(dir * pullForce, ForceMode.Acceleration);
            }
        }
        
        // Visual feedback
        if (Random.value < 0.1f) SpawnGravityVFX(transform.position, Color.cyan);
    }

    void ApplyGravityPush()
    {
        float radius = 12f * GameSettings.NinjaScale;
        float pushForce = 40f;
        Collider[] hits = Physics.OverlapSphere(transform.position, radius);
        
        SplitScreenCamera.ShakeCamera(PlayerIndex, 0.3f, 0.4f);
        SpawnGravityVFX(transform.position, Color.magenta);

        foreach (var h in hits)
        {
            var rb = h.GetComponent<Rigidbody>();
            if (rb != null && rb != _rb)
            {
                Vector3 dir = (h.transform.position - transform.position).normalized;
                dir.y += 0.3f; // lift slightly
                rb.AddForce(dir * pushForce, ForceMode.Impulse);
            }
            
            var eb = h.GetComponentInParent<EnemyBase>();
            if (eb != null) eb.TakeDamage(15f, transform);
        }
    }

    void SpawnGravityVFX(Vector3 pos, Color color)
    {
        var obj = new GameObject("GravityVFX");
        obj.transform.position = pos;
        var ps = obj.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.startLifetime = 0.5f;
        main.startSpeed = 10f;
        main.startSize = 0.3f;
        main.startColor = color;
        main.loop = false;
        
        var em = ps.emission;
        em.SetBursts(new[] { new ParticleSystem.Burst(0f, 30) });
        
        ps.Play();
        Destroy(obj, 1f);
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

        // Destroy old weapon if any
        if (_currentWeapon != null)
        {
            SpawnTransformVFX(_currentWeapon.transform.position);
            Destroy(_currentWeapon);
        }
        else if (SwordRoot != null)
        {
            // If it's the first time and we just have the bootstrapper's reference
            SpawnTransformVFX(transform.position + transform.forward);
            SwordRoot.SetActive(false); 
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
        var ps = vfxObj.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear); // Ensure it's not "playing" before config
        var main = ps.main;
        main.duration = 1f;
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
