using UnityEngine;

/// <summary>
/// SplitScreenCamera — follows its assigned player target with smooth lag.
/// Zoom adapts dynamically to the player's movement speed.
/// Screen shake is triggered via the static ShakeCamera() method.
/// </summary>
[ExecuteAlways]
public class SplitScreenCamera : MonoBehaviour
{
    public Transform TargetTransform;

    [Header("Smart Camera")]
    public float     Distance      = 3.5f;
    public float     HeightOffset  = 1.4f;
    public float     SideOffset    = 0.6f;
    public float     RotationSpeed = 3.5f;

    // ── Static shake table (indexed by player) ────────────────────────────
    private static SplitScreenCamera[] _instances = new SplitScreenCamera[2];

    // ── Private state ─────────────────────────────────────────────────────
    private Camera   _cam;
    private Vector3  _currentPos;
    private float    _currentYaw       = 0f;
    private float    _targetYaw        = 0f;
    private float    _moveTime         = 0f;
    private float    _shakeTimer       = 0f;
    private float    _currentShakeMag  = 0f;
    private float    _shakeDuration    = 0f;
    private Vector3  _shakeOffset      = Vector3.zero;
    private int      _playerIndex      = -1;

    void Awake()
    {
        _cam = GetComponent<Camera>();
        if (gameObject.name.Contains("P1")) { _playerIndex = 0; _instances[0] = this; }
        if (gameObject.name.Contains("P2")) { _playerIndex = 1; _instances[1] = this; }
        _currentPos = transform.position;
    }

    void LateUpdate()
    {
        if (TargetTransform == null) return;
        HandleSmartFollow();
        UpdateFOV();
        ApplyShake();
    }

    void HandleSmartFollow()
    {
        var rb = TargetTransform.GetComponent<Rigidbody>();
        if (rb == null) return;

        Vector3 horizVel = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
        float speed = horizVel.magnitude;

        // Auto-Swing Logic:
        // If moving, slowly rotate camera to face the direction of travel
        if (speed > 1.5f)
        {
            _moveTime += Time.deltaTime;
            if (_moveTime > 0.4f) // Only start following after a short move
            {
                float moveYaw = Mathf.Atan2(horizVel.x, horizVel.z) * Mathf.Rad2Deg;
                _targetYaw = Mathf.LerpAngle(_targetYaw, moveYaw, Time.deltaTime * RotationSpeed);
            }
        }
        else
        {
            _moveTime = 0f;
        }

        _currentYaw = Mathf.LerpAngle(_currentYaw, _targetYaw, Time.deltaTime * RotationSpeed);

        // Position camera behind player based on _currentYaw
        Quaternion rotation = Quaternion.Euler(12f, _currentYaw, 0);
        Vector3 backDir = rotation * Vector3.back;
        Vector3 rightDir = rotation * Vector3.right;

        Vector3 desiredPos = TargetTransform.position 
                             + Vector3.up * HeightOffset 
                             + backDir * Distance 
                             + rightDir * SideOffset;

        // Intro dive override - looking down dramatically
        var ctrl = TargetTransform.GetComponent<NinjaController>();
        if (ctrl != null && ctrl.IsIntroDive)
        {
            desiredPos = TargetTransform.position + Vector3.up * 6f + backDir * 4f;
            transform.LookAt(TargetTransform.position + Vector3.down * 4f);
        }
        else
        {
            float smooth = Application.isPlaying ? 0.15f : 0f;
            transform.position = Vector3.Lerp(transform.position, desiredPos, Time.deltaTime * 8f);
            transform.LookAt(TargetTransform.position + Vector3.up * HeightOffset + (rotation * Vector3.forward * 5f));
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    // DYNAMIC FOV
    // ─────────────────────────────────────────────────────────────────────
    void UpdateFOV()
    {
        if (_cam == null) return;

        float targetFOV = GameSettings.CameraBaseFOV;

        var rb = TargetTransform != null ? TargetTransform.GetComponent<Rigidbody>() : null;
        if (rb != null)
        {
            float speed = rb.linearVelocity.magnitude;
            targetFOV = Mathf.Lerp(GameSettings.CameraBaseFOV, GameSettings.CameraMaxFOV, Mathf.Clamp01(speed / 20f));
        }

        var nc = TargetTransform != null ? TargetTransform.GetComponent<NinjaController>() : null;
        if (nc != null)
        {
            if (nc.IsIntroDive) targetFOV = 85f; // Super wide sky-dive
            else if (nc.IsFiringLaser) targetFOV = Mathf.Min(targetFOV + 8f, GameSettings.CameraMaxFOV + 8f);
        }

        _cam.fieldOfView = Mathf.Lerp(_cam.fieldOfView, targetFOV, GameSettings.CameraFOVLerpSpeed * Time.deltaTime);
    }

    // ─────────────────────────────────────────────────────────────────────
    // SCREEN SHAKE
    // ─────────────────────────────────────────────────────────────────────
    public static void ShakeCamera(int playerIndex, float magnitude, float duration)
    {
        if (playerIndex < 0 || playerIndex >= _instances.Length) return;
        var cam = _instances[playerIndex];
        if (cam == null) return;
        cam._currentShakeMag = magnitude;
        cam._shakeDuration   = duration;
        cam._shakeTimer      = duration;
    }

    void ApplyShake()
    {
        if (_shakeTimer > 0f)
        {
            _shakeTimer -= Time.deltaTime;
            float progress = _shakeTimer / _shakeDuration;
            float mag      = _currentShakeMag * progress;

            // Perlin noise shake — different seed per axis so it's not uniform
            float px = (Mathf.PerlinNoise(Time.time * 40f, _playerIndex * 100f) - 0.5f) * 2f;
            float py = (Mathf.PerlinNoise(_playerIndex * 100f, Time.time * 40f) - 0.5f) * 2f;
            _shakeOffset = new Vector3(px, py, 0f) * mag;

            transform.position += transform.right   * _shakeOffset.x;
            transform.position += transform.up      * _shakeOffset.y;
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    // PUBLIC — let GameBootstrapper assign target after this component exists
    // ─────────────────────────────────────────────────────────────────────
    public void SetTarget(Transform t) => TargetTransform = t;
}
