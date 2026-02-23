using UnityEngine;

/// <summary>
/// EnemyProjectile — fired by ShadowArcher and other ranged enemies.
/// Moves in a straight line, damages the first player it hits, then explodes.
/// </summary>
public class EnemyProjectile : MonoBehaviour
{
    public Vector3 Direction = Vector3.forward;
    public float   Speed     = 18f;
    public float   Damage    = 8f;
    public Color   Color     = Color.magenta;
    public float   MaxLife   = 3.5f;   // auto-destroy after this many seconds

    private float _life;
    private bool  _spent;

    void Start()
    {
        _life = MaxLife;
        // Point the primitive along direction of travel
        if (Direction.sqrMagnitude > 0.01f)
        {
            transform.rotation = Quaternion.LookRotation(Direction);
            // Stretch into a bolt shape
            transform.localScale = new Vector3(0.18f, 0.18f, 0.6f);
        }
    }

    void Update()
    {
        if (_spent) return;

        transform.position += Direction * Speed * Time.deltaTime;

        // Slow spin for visual interest
        transform.Rotate(0, 0, 180f * Time.deltaTime, Space.Self);

        _life -= Time.deltaTime;
        if (_life <= 0f) Destroy(gameObject);
    }

    void OnTriggerEnter(Collider other)
    {
        if (_spent) return;

        // Ignore other enemy projectiles and enemies
        if (other.GetComponentInParent<EnemyBase>() != null) return;
        if (other.GetComponentInParent<EnemyProjectile>() != null) return;

        var ph = other.GetComponentInParent<PlayerHealth>();
        if (ph != null && ph.IsAlive)
        {
            var nc = other.GetComponentInParent<NinjaController>();

            // Block check — player holding LB/L1
            if (nc != null && nc.IsBlocking)
            {
                // Deflected! Reverse the projectile back toward the enemy
                Direction = -Direction;
                Damage    *= 1.5f;  // bonus damage on deflect
                Speed     *= 1.2f;
                SplitScreenCamera.ShakeCamera(nc.PlayerIndex, 0.08f, 0.15f);
                return;
            }

            ph.TakeDamage(Damage);
            SpawnImpact(transform.position);
            _spent = true;
            Destroy(gameObject);
        }
        else if (other.GetComponentInParent<EnemyBase>() == null)
        {
            // Hit terrain / wall / platform
            SpawnImpact(transform.position);
            _spent = true;
            Destroy(gameObject);
        }
    }

    void SpawnImpact(Vector3 pos)
    {
        var obj  = new GameObject("ProjectileImpact");
        obj.transform.position = pos;
        var ps   = obj.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.duration      = 0.2f;
        main.loop          = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.15f, 0.35f);
        main.startSpeed    = new ParticleSystem.MinMaxCurve(2f, 6f);
        main.startSize     = new ParticleSystem.MinMaxCurve(0.05f, 0.18f);
        main.startColor    = new ParticleSystem.MinMaxGradient(Color, Color.white);
        main.maxParticles  = 25;

        var emit = ps.emission;
        emit.SetBursts(new[] { new ParticleSystem.Burst(0f, 25) });
        emit.rateOverTime = 0;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius    = 0.1f;

        ps.Play();
        Destroy(obj, 0.8f);
    }
}
