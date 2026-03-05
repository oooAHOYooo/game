using UnityEngine;

/// <summary>
/// HazardFallingRock — Spawns rocks from above that drop down and deal AoE damage on impact.
/// </summary>
public class HazardFallingRock : MonoBehaviour
{
    public float SpawnInterval = 5f;
    public float SpawnRadius = 10f;
    public float Damage = 30f;
    public float ExplosionRadius = 4f;

    private float _timer;

    void Update()
    {
        _timer += Time.deltaTime;
        if (_timer >= SpawnInterval)
        {
            _timer = 0f;
            SpawnRock();
        }
    }

    void SpawnRock()
    {
        Vector2 randCircle = Random.insideUnitCircle * SpawnRadius;
        Vector3 spawnPos = transform.position + new Vector3(randCircle.x, 30f, randCircle.y);

        var rock = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        rock.name = "FallingRockProjectile";
        rock.transform.position = spawnPos;
        rock.transform.localScale = Vector3.one * Random.Range(1.5f, 3f);
        
        var mat = new Material(GameBootstrapper.GetHDRPLitShader());
        mat.color = new Color(0.3f, 0.25f, 0.2f);
        rock.GetComponent<Renderer>().material = mat;

        var rb = rock.AddComponent<Rigidbody>();
        rb.mass = 50f;

        var proj = rock.AddComponent<FallingRockProjectile>();
        proj.Damage = Damage;
        proj.ExplosionRadius = ExplosionRadius;

        Destroy(rock, 10f); // Fallback cleanup
    }
}

public class FallingRockProjectile : MonoBehaviour
{
    public float Damage = 30f;
    public float ExplosionRadius = 4f;
    private bool _exploded = false;

    void OnCollisionEnter(Collision collision)
    {
        if (_exploded) return;
        _exploded = true;

        // Explode
        Collider[] hits = Physics.OverlapSphere(transform.position, ExplosionRadius);
        foreach (var hit in hits)
        {
            var p = hit.GetComponentInParent<PlayerHealth>();
            if (p != null) p.TakeDamage(Damage);

            var e = hit.GetComponentInParent<EnemyBase>();
            if (e != null) e.TakeDamage(Damage, transform);
        }

        // VFX & SFX
        if (ImpactFeedback.Instance != null)
        {
            var dmgInfo = new DamageInfo { Amount = Damage, Critical = true };
            ImpactFeedback.Instance.Play(dmgInfo, transform.position);
        }

        // Create a dust cloud
        var vfx = new GameObject("RockDust");
        vfx.transform.position = transform.position;
        var ps = vfx.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.duration = 1f;
        main.loop = false;
        main.startLifetime = 1.5f;
        main.startSpeed = new ParticleSystem.MinMaxCurve(2f, 5f);
        main.startSize = new ParticleSystem.MinMaxCurve(1f, 3f);
        main.startColor = new Color(0.4f, 0.35f, 0.3f, 0.8f);
        var em = ps.emission;
        em.SetBursts(new[] { new ParticleSystem.Burst(0f, 30) });
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Hemisphere;
        shape.radius = 2f;
        ps.Play();
        Destroy(vfx, 2f);

        Destroy(gameObject); // Destroy the rock
    }
}
