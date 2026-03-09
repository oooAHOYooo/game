using UnityEngine;
using System.Collections;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

/// <summary>
/// MythologicalBall — A sacred orb that everyone must fight for.
/// It can be grabbed, carried, and thrown into the goal to save the world!
/// </summary>
public class MythologicalBall : MonoBehaviour
{
    public static MythologicalBall Instance;

    [Header("Settings")]
    public float GrabRadius = 2.5f;
    public float ThrowForce = 25f;
    public Color BallColor = new Color(0.1f, 0.9f, 1.0f); // Divine Cyan
    public float ResetTime = 5f;

    [Header("State")]
    public Transform Carrier;
    private Rigidbody _rb;
    private NinjaController _ninjaCarrier;
    private EnemyBase _enemyCarrier;
    private Vector3 _startPos;
    private bool _isBeingReset = false;
    private Light _pointLight;
    private ParticleSystem _trail;

    void Awake()
    {
        Instance = this;
        _rb = GetComponent<Rigidbody>();
        if (_rb == null) _rb = gameObject.AddComponent<Rigidbody>();
        
        _rb.mass = 2f;
        _rb.interpolation = RigidbodyInterpolation.Interpolate;
        _rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        
        _startPos = transform.position;
        BuildVisuals();
    }

    void BuildVisuals()
    {
        // Primitive Sphere
        var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.transform.SetParent(transform);
        sphere.transform.localPosition = Vector3.zero;
        sphere.transform.localScale = Vector3.one * 0.8f;
        Destroy(sphere.GetComponent<Collider>()); // Use parent collier

        var mat = new Material(GameBootstrapper.GetHDRPLitShader());
        mat.color = Color.white;
        GameBootstrapper.SetHDRPEmission(mat, BallColor, 15f);
        sphere.GetComponent<Renderer>().material = mat;

        // Light
        var lObj = new GameObject("BallLight");
        lObj.transform.SetParent(transform);
        lObj.transform.localPosition = Vector3.zero;
        _pointLight = lObj.AddComponent<Light>();
        _pointLight.type = LightType.Point;
        _pointLight.color = BallColor;
        _pointLight.intensity = 5000f;
        var hd = lObj.AddComponent<HDAdditionalLightData>();
        hd.range = 8f;

        // Trail – mythic sparks
        var tObj = new GameObject("BallTrail");
        tObj.transform.SetParent(transform);
        tObj.transform.localPosition = Vector3.zero;
        _trail = tObj.AddComponent<ParticleSystem>();
        var main = _trail.main;
        main.startColor = BallColor;
        main.startSize = 0.3f;
        var trailEm = _trail.emission;
        trailEm.rateOverTime = 50f;
    }

    void Update()
    {
        if (Carrier != null)
        {
            // Follow carrier with a slight hover bob
            float bob = Mathf.Sin(Time.time * 3f) * 0.2f;
            transform.position = Vector3.Lerp(transform.position, Carrier.position + Vector3.up * (2.2f + bob), Time.deltaTime * 15f);
            
            _rb.linearVelocity = Vector3.zero;
            _rb.isKinematic = true;
            
            // Check for damage to drop it!
            if (_ninjaCarrier != null)
            {
                // PlayerHealth can be checked
                var health = _ninjaCarrier.GetComponent<PlayerHealth>();
                if (health != null && health.IsTakingDamage) Drop(Vector3.up * 5f);
            }
            else if (_enemyCarrier != null)
            {
                if (_enemyCarrier.IsTakingDamage) Drop(Vector3.up * 5f);
            }
        }
        else
        {
            _rb.isKinematic = false;
        }

        if (Carrier == null && !_isBeingReset)
        {
            CheckForGrab();
        }
    }

    void CheckForGrab()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, GrabRadius);
        foreach (var h in hits)
        {
            var ninja = h.GetComponent<NinjaController>();
            if (ninja != null) { Grab(ninja.transform, true); break; }
            
            var eb = h.GetComponent<EnemyBase>();
            if (eb != null && eb.IsAlive) { Grab(eb.transform, false); break; }
        }
    }

    public void Grab(Transform target, bool isPlayer)
    {
        if (Carrier != null) Carrier = null;
        Carrier = target;
        _rb.isKinematic = true;
        
        if (isPlayer) {
            _ninjaCarrier = target.GetComponent<NinjaController>();
            _enemyCarrier = null;
        } else {
            _enemyCarrier = target.GetComponent<EnemyBase>();
            _ninjaCarrier = null;
        }

        Debug.Log($"[Ball] Grabbed by {(isPlayer ? "Player" : "Enemy")} {target.name}");
        GameBootstrapper.SpawnAlert(target, !isPlayer);
    }

    public void Drop(Vector3 force)
    {
        if (Carrier == null) return;
        
        Carrier = null;
        _ninjaCarrier = null;
        _enemyCarrier = null;
        _rb.isKinematic = false;
        _rb.AddForce(force, ForceMode.Impulse);
    }


    public void ResetBall()
    {
        if (_isBeingReset) return;
        StartCoroutine(ResetRoutine());
    }

    IEnumerator ResetRoutine()
    {
        _isBeingReset = true;
        Carrier = null;
        _rb.linearVelocity = Vector3.zero;
        _rb.isKinematic = true;
        
        // Flash or something
        yield return new WaitForSeconds(ResetTime);
        
        transform.position = _startPos;
        _rb.isKinematic = false;
        _isBeingReset = false;
    }
}
