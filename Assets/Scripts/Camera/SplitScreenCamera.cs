using UnityEngine;

/// <summary>
/// SplitScreenCamera — follows its assigned player target with smooth lag.
/// Zoom adapts dynamically to the player's movement speed.
/// Screen shake is triggered via the static ShakeCamera() method.
/// </summary>
public class SplitScreenCamera : MonoBehaviour
{
    // ── Configuration ─────────────────────────────────────────────────────
    [Header("Follow")]
    public Transform TargetTransform;
    public Vector3   Offset        = new Vector3(0f, 4.5f, -8.5f);
    public float     SmoothTime    = 0.18f;

    [Header("Zoom")]
    public float BaseFOV           = 50f;
    public float MaxFOV            = 75f;   // zoomed out when sprinting / flying
    public float FOVLerpSpeed      = 5f;

    [Header("Shake")]
    public float ShakeMagnitude    = 0.3f;
    public float ShakeDuration     = 0.2f;

    // ── Static shake table (indexed by player) ────────────────────────────
    private static SplitScreenCamera[] _instances = new SplitScreenCamera[2];

    // ── Private state ─────────────────────────────────────────────────────
    private Camera   _cam;
    private Vector3  _velocity         = Vector3.zero;
    private float    _shakeTimer       = 0f;
    private float    _currentShakeMag  = 0f;
    private float    _shakeDuration    = 0f;
    private Vector3  _shakeOffset      = Vector3.zero;
    private int      _playerIndex      = -1;

    // ─────────────────────────────────────────────────────────────────────
    void Awake()
    {
        _cam = GetComponent<Camera>();

        // Register in static table by name convention
        if (gameObject.name.Contains("P1")) { _playerIndex = 0; _instances[0] = this; }
        if (gameObject.name.Contains("P2")) { _playerIndex = 1; _instances[1] = this; }
    }

    void LateUpdate()
    {
        if (TargetTransform == null) return;

        FollowTarget();
        UpdateFOV();
        ApplyShake();
    }

    // ─────────────────────────────────────────────────────────────────────
    // FOLLOW
    // ─────────────────────────────────────────────────────────────────────
    void FollowTarget()
    {
        Vector3 desiredPos = TargetTransform.position + Offset;

        // Slight lean forward in direction of movement
        var rb = TargetTransform.GetComponent<Rigidbody>();
        if (rb != null)
        {
            Vector3 horiz = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
            desiredPos += horiz * 0.4f;
        }

        transform.position = Vector3.SmoothDamp(transform.position, desiredPos, ref _velocity, SmoothTime);
        transform.LookAt(TargetTransform.position + Vector3.up * 1.2f);
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
        if (nc != null && nc.IsFiringLaser)
            targetFOV = Mathf.Min(targetFOV + 8f, GameSettings.CameraMaxFOV + 8f); // push FOV on laser fire

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
