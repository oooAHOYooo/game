using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// VillageFireSystem — manages physical fires on village huts.
/// Fires spread over time, deal damage to the village, and can be extinguished by players.
/// </summary>
public class VillageFireSystem : MonoBehaviour
{
    public static VillageFireSystem Instance;

    private Dictionary<GameObject, GameObject> _activeFires = new Dictionary<GameObject, GameObject>();
    private float _spreadTimer;
    private float _damageTimer;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Update()
    {
        if (_activeFires.Count == 0 || Village.Instance == null || Village.Instance.IsDestroyed) return;

        // Spread logic
        _spreadTimer += Time.deltaTime;
        if (_spreadTimer >= GameSettings.FireSpreadInterval)
        {
            _spreadTimer = 0f;
            SpreadFire();
        }

        // Damage logic
        _damageTimer += Time.deltaTime;
        if (_damageTimer >= 1f)
        {
            _damageTimer = 0f;
            Village.Instance.TakeDamage(GameSettings.FireDamagePerTick * _activeFires.Count);
        }
    }

    public void IgniteNearestHut(Vector3 position)
    {
        if (Village.Instance == null || Village.Instance.Huts == null) return;

        GameObject nearest = null;
        float minDist = float.MaxValue;

        foreach (var hut in Village.Instance.Huts)
        {
            if (hut == null || _activeFires.ContainsKey(hut)) continue;

            float d = Vector3.Distance(hut.transform.position, position);
            if (d < minDist)
            {
                minDist = d;
                nearest = hut;
            }
        }

        if (nearest != null) IgniteHut(nearest);
    }

    private void IgniteHut(GameObject hut)
    {
        if (_activeFires.ContainsKey(hut)) return;

        var fireRoot = new GameObject("HutFire");
        fireRoot.transform.SetParent(hut.transform);
        fireRoot.transform.localPosition = Vector3.up * 0.5f;

        // Visuals
        var ps = fireRoot.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.loop = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.4f, 0.8f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(1f, 3f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.2f, 0.5f);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.45f, 0.05f), new Color(1f, 0.8f, 0.2f));
        main.maxParticles = 50;

        var em = ps.emission;
        em.rateOverTime = 30f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.4f;

        // Light
        var pointLight = fireRoot.AddComponent<Light>();
        pointLight.type = LightType.Point;
        pointLight.color = new Color(1f, 0.4f, 0f);
        pointLight.intensity = 2000f;
        var hd = pointLight.gameObject.AddComponent<UnityEngine.Rendering.HighDefinition.HDAdditionalLightData>();
        hd.range = 8f;

        // Charred material
        foreach (var r in hut.GetComponentsInChildren<Renderer>())
        {
            r.material.color = Color.Lerp(r.material.color, Color.black, 0.5f);
        }

        ps.Play();
        _activeFires.Add(hut, fireRoot);
    }

    private void SpreadFire()
    {
        if (Village.Instance == null) return;

        List<GameObject> toIgnite = new List<GameObject>();
        foreach (var burningHut in _activeFires.Keys)
        {
            if (burningHut == null) continue;

            foreach (var hut in Village.Instance.Huts)
            {
                if (hut == null || hut == burningHut || _activeFires.ContainsKey(hut)) continue;

                if (Vector3.Distance(burningHut.transform.position, hut.transform.position) < GameSettings.FireSpreadRadius)
                {
                    toIgnite.Add(hut);
                    break; // one spread per hut per tick
                }
            }
        }

        foreach (var newFire in toIgnite) IgniteHut(newFire);
    }

    public void ExtinguishFires(Vector3 center, float radius)
    {
        List<GameObject> toRemove = new List<GameObject>();

        foreach (var kvp in _activeFires)
        {
            if (kvp.Key == null || kvp.Value == null)
            {
                toRemove.Add(kvp.Key);
                continue;
            }

            if (Vector3.Distance(kvp.Key.transform.position, center) <= radius)
            {
                // Smoke puff
                SpawnSmoke(kvp.Value.transform.position);
                Destroy(kvp.Value);
                toRemove.Add(kvp.Key);
            }
        }

        foreach (var k in toRemove) _activeFires.Remove(k);
    }

    private void SpawnSmoke(Vector3 position)
    {
        var smoke = new GameObject("SmokePuff");
        smoke.transform.position = position;
        var ps = smoke.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.duration = 1f;
        main.loop = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(1f, 2f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(1f, 2f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.5f, 1.5f);
        main.startColor = new Color(0.3f, 0.3f, 0.3f, 0.6f);
        
        var em = ps.emission;
        em.SetBursts(new[] { new ParticleSystem.Burst(0f, 15) });
        em.rateOverTime = 0;

        ps.Play();
        Destroy(smoke, 2.5f);
    }
}
