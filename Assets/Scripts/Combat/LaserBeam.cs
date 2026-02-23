using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// LaserBeam — Dragon Ball Z ki beam.
/// Spawns a cylindrical beam projectile that travels until it hits an enemy or max range.
/// Full-ki fires an "Ultra Beam" (wider, longer, screen effect).
/// </summary>
public class LaserBeam : MonoBehaviour
{
    public NinjaController Controller;
    public Color           BeamColor     = Color.cyan;
    public float           DamagePerTick = 18f;
    public float           BeamSpeed     = 35f;
    public float           MaxRange      = 40f;
    public float           BeamRadius    = 0.15f;

    private static readonly int _ultraBeamKiThreshold = 80;

    // ─────────────────────────────────────────────────────────────────────
    public void Fire(float kiAtFire)
    {
        bool isUltra = kiAtFire >= _ultraBeamKiThreshold;
        StartCoroutine(SpawnBeam(isUltra, kiAtFire));
    }

    IEnumerator SpawnBeam(bool isUltra, float kiAtFire)
    {
        // Origin: in front of the player, at chest height
        Vector3 origin    = transform.position + transform.forward * 0.6f + Vector3.up * 1.2f;
        Vector3 direction = transform.forward;

        // If locked on, aim at target
        var enemies = FindObjectsByType<EnemyBase>(FindObjectsSortMode.None);
        EnemyBase nearest = null;
        float nearDist    = float.MaxValue;
        foreach (var e in enemies)
        {
            if (!e.IsActiveEnemy) continue;
            float d = Vector3.Distance(origin, e.transform.position);
            if (d < nearDist) { nearDist = d; nearest = e; }
        }
        if (nearest != null && nearDist < MaxRange)
            direction = (nearest.transform.position + Vector3.up - origin).normalized;

        // Build beam GO
        float radius   = isUltra ? BeamRadius * 3f : BeamRadius;
        float length   = 0.5f;
        Color bColor   = isUltra ? GameBootstrapper.PaletteCrimson : BeamColor;
        float intensity = isUltra ? 20f : 10f;

        var beamObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        beamObj.name = isUltra ? "UltraBeam" : "LaserBeam";
        beamObj.transform.position = origin;
        beamObj.transform.up       = direction;
        beamObj.transform.localScale = new Vector3(radius * 2f, length * 0.5f, radius * 2f);

        Destroy(beamObj.GetComponent<Collider>());
        var mat = new Material(GameBootstrapper.GetHDRPLitShader());
        mat.color = bColor;
        GameBootstrapper.SetHDRPEmission(mat, bColor, intensity);
        beamObj.GetComponent<Renderer>().material = mat;

        // Muzzle flash
        SpawnMuzzleFlash(origin, bColor, isUltra);

        // Screen shake
        SplitScreenCamera.ShakeCamera(Controller.PlayerIndex, isUltra ? 0.5f : 0.2f, isUltra ? 0.5f : 0.25f);

        // Travel beam forward
        float       travelled  = 0f;
        bool        hitTarget  = false;
        HashSet<int> hitIds    = new HashSet<int>();

        while (travelled < MaxRange)
        {
            float step = BeamSpeed * Time.deltaTime;
            beamObj.transform.position += direction * step;

            // Grow length visually while travelling
            float visualLen = Mathf.Clamp(travelled / 5f, 0.05f, 3f);
            beamObj.transform.localScale = new Vector3(radius * 2f, visualLen, radius * 2f);

            // Overlap sphere check for hits
            Collider[] hits = Physics.OverlapSphere(beamObj.transform.position, radius * 1.5f);
            foreach (var c in hits)
            {
                var enemy = c.GetComponentInParent<EnemyBase>();
                if (enemy != null && !hitIds.Contains(enemy.GetInstanceID()))
                {
                    hitIds.Add(enemy.GetInstanceID());
                    float dmg = DamagePerTick * (isUltra ? 3f : 1f);
                    enemy.TakeDamage(dmg, transform);

                    // Knockback from beam
                    var rb = enemy.GetComponent<Rigidbody>();
                    if (rb != null) rb.AddForce(direction * dmg * 0.8f + Vector3.up * 5f, ForceMode.Impulse);

                    if (!isUltra) { hitTarget = true; break; }
                }
            }

            // Check arena boundary
            if (hitTarget || beamObj.transform.position.y < -2f) break;

            travelled += step;
            yield return null;
        }

        // Impact explosion
        SpawnImpactVFX(beamObj.transform.position, bColor, isUltra);

        Destroy(beamObj);

        // Ultra beam additional VFX — chromatic aberration spike (simulated by brief colour flash)
        if (isUltra)
            StartCoroutine(UltraBeamScreenEffect());
    }

    void SpawnMuzzleFlash(Vector3 pos, Color color, bool isUltra)
    {
        var obj  = new GameObject("MuzzleFlash");
        obj.transform.position = pos;
        var ps   = obj.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.duration      = 0.15f;
        main.loop          = false;
        main.startLifetime = 0.15f;
        main.startSpeed    = new ParticleSystem.MinMaxCurve(5f, 12f);
        main.startSize     = isUltra ? 1.2f : 0.5f;
        main.startColor    = new ParticleSystem.MinMaxGradient(color, Color.white);
        main.maxParticles  = isUltra ? 60 : 20;

        var emit = ps.emission;
        emit.SetBursts(new[] { new ParticleSystem.Burst(0f, main.maxParticles.constant) });
        emit.rateOverTime = 0;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius    = isUltra ? 0.5f : 0.2f;

        ps.Play();
        Destroy(obj, 0.5f);
    }

    void SpawnImpactVFX(Vector3 pos, Color color, bool isUltra)
    {
        var obj  = new GameObject("BeamImpact");
        obj.transform.position = pos;
        var ps   = obj.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.duration      = 0.3f;
        main.loop          = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.2f, 0.5f);
        main.startSpeed    = new ParticleSystem.MinMaxCurve(3f, isUltra ? 20f : 10f);
        main.startSize     = isUltra ? 0.8f : 0.3f;
        main.startColor    = new ParticleSystem.MinMaxGradient(color, Color.white);
        main.maxParticles  = isUltra ? 150 : 60;

        var emit = ps.emission;
        emit.SetBursts(new[] { new ParticleSystem.Burst(0f, main.maxParticles.constant) });
        emit.rateOverTime = 0;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius    = isUltra ? 1.5f : 0.5f;

        ps.Play();
        Destroy(obj, 1f);
    }

    IEnumerator UltraBeamScreenEffect()
    {
        // Simple colour overlay flash using a screen-space canvas quad
        // (Full post-processing volume chromatic aberration requires Volume access)
        var obj        = new GameObject("UltraBeamFlash");
        DontDestroyOnLoad(obj);
        var canvas     = obj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 99;

        var imgObj = new GameObject("FlashImg");
        imgObj.transform.SetParent(obj.transform);
        var img = imgObj.AddComponent<UnityEngine.UI.Image>();
        img.color = new Color(BeamColor.r, BeamColor.g, BeamColor.b, 0.45f);
        var rt  = imgObj.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        // Fade out
        for (float t = 0; t < 0.4f; t += Time.deltaTime)
        {
            img.color = new Color(img.color.r, img.color.g, img.color.b, Mathf.Lerp(0.45f, 0f, t / 0.4f));
            yield return null;
        }

        Destroy(obj);
    }
}
