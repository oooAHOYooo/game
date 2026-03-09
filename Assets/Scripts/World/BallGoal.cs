using UnityEngine;
using System.Collections;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

/// <summary>
/// BallGoal — The target where the Mythological Ball must be placed.
/// It represents the island's health/soul and grants power-ups.
/// </summary>
public class BallGoal : MonoBehaviour
{
    public static BallGoal Instance;

    [Header("Settings")]
    public float GoalRadius = 5f;
    public Color GoalColor = new Color(1.0f, 0.78f, 0.10f); // Divine Gold
    public float PowerUpDuration = 10f;
    public float PowerUpMultiplier = 1.5f;

    [Header("State")]
    public int Score = 0;
    private ParticleSystem _glow;
    private Light _light;

    void Awake()
    {
        Instance = this;
        BuildVisuals();
    }

    void BuildVisuals()
    {
        // Simple ring or base
        var torus = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        torus.transform.SetParent(transform);
        torus.transform.localPosition = new Vector3(0, 0.1f, 0);
        torus.transform.localScale = new Vector3(GoalRadius, 0.2f, GoalRadius);
        Destroy(torus.GetComponent<Collider>());

        var mat = new Material(GameBootstrapper.GetHDRPLitShader());
        mat.color = Color.black;
        GameBootstrapper.SetHDRPEmission(mat, GoalColor, 8f);
        torus.GetComponent<Renderer>().material = mat;

        // Central pillar/aura
        var aura = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        aura.transform.SetParent(transform);
        aura.transform.localPosition = new Vector3(0, 3f, 0);
        aura.transform.localScale = new Vector3(GoalRadius * 0.8f, 3f, GoalRadius * 0.8f);
        Destroy(aura.GetComponent<Collider>());
        
        var auraMat = new Material(GameBootstrapper.GetHDRPLitShader());
        auraMat.SetFloat("_SurfaceType", 1); // transparent
        auraMat.color = new Color(GoalColor.r, GoalColor.g, GoalColor.b, 0.3f);
        GameBootstrapper.SetHDRPEmission(auraMat, GoalColor * 0.5f, 5f);
        aura.GetComponent<Renderer>().material = auraMat;

        // Light
        var lObj = new GameObject("GoalLight");
        lObj.transform.SetParent(transform);
        lObj.transform.localPosition = new Vector3(0, 5f, 0);
        _light = lObj.AddComponent<Light>();
        _light.type = LightType.Point;
        _light.color = GoalColor;
        _light.intensity = 10000f;
        var hd = lObj.AddComponent<HDAdditionalLightData>();
        hd.range = 15f;
        
        // Trigger area
        var trigger = gameObject.AddComponent<SphereCollider>();
        trigger.isTrigger = true;
        trigger.radius = GoalRadius / 2f;
    }

    void OnTriggerEnter(Collider other)
    {
        var ball = other.GetComponent<MythologicalBall>();
        if (ball == null) ball = other.GetComponentInParent<MythologicalBall>();
        
        if (ball != null)
        {
            HandleGoal(ball);
        }
    }

    void HandleGoal(MythologicalBall ball)
    {
        Score++;
        Debug.Log($"[Goal] SCORED! Total: {Score}");
        
        // Reward carrier
        if (ball.Carrier != null)
        {
            var p = ball.Carrier;
            GrantPowerUps(p);
            
            // Visual feedback
            GameBootstrapper.SpawnAlert(p.transform, false);
            SplitScreenCamera.ShakeCamera(p.PlayerIndex, 0.8f, 1.0f);
        }

        // Heal Island/Village
        if (Village.Instance != null)
        {
            Village.Instance.Heal(200f);
            Village.Instance.CelebrateAll(5f);
        }

        // Reset the ball
        ball.ResetBall();
        
        // Score effects
        SpawnScoreVFX();
    }

    void GrantPowerUps(NinjaController player)
    {
        // Randomly pick speed or damage
        if (Random.value > 0.5f)
        {
            player.ApplySpeedBoost(PowerUpDuration, PowerUpMultiplier);
            Debug.Log($"[Goal] Speed boost granted to {player.name}");
        }
        else
        {
            player.ApplyDamageBoost(PowerUpDuration, PowerUpMultiplier);
            Debug.Log($"[Goal] Damage boost granted to {player.name}");
        }
    }

    void SpawnScoreVFX()
    {
        var boom = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        boom.transform.position = transform.position + Vector3.up * 3f;
        boom.transform.localScale = Vector3.zero;
        Destroy(boom.GetComponent<Collider>());
        
        var mat = new Material(GameBootstrapper.GetHDRPLitShader());
        mat.color = GoalColor;
        GameBootstrapper.SetHDRPEmission(mat, GoalColor, 20f);
        boom.GetComponent<Renderer>().material = mat;

        StartCoroutine(AnimateScoreBoom(boom));
    }

    IEnumerator AnimateScoreBoom(GameObject boom)
    {
        float t = 0;
        while (t < 0.6f)
        {
            t += Time.deltaTime;
            float s = t * 40f;
            boom.transform.localScale = new Vector3(s, s, s);
            yield return null;
        }
        Destroy(boom);
    }
}
