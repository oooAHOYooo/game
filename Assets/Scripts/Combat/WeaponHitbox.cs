using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// WeaponHitbox — trigger-based melee hit detection with hit-stop for UFC5 feel.
/// </summary>
public class WeaponHitbox : MonoBehaviour
{
    private float     _damage;
    private float     _range;
    private float     _arc;          // degrees of swing arc
    private bool      _active;
    private Transform _preferredTarget;
    private Rigidbody _ownerRb;
    private HashSet<Collider> _hitThisSwing = new HashSet<Collider>();

    // Returns IEnumerator so NinjaController can yield on it
    public IEnumerator Activate(float damage, float range, float arc, float duration, Transform preferredTarget)
    {
        _range            = range;
        _arc              = arc;
        _active           = true;
        _preferredTarget  = preferredTarget;
        _ownerRb          = transform.root.GetComponent<Rigidbody>();
        _hitThisSwing.Clear();

        yield return new WaitForSeconds(duration);

        _active = false;
        _hitThisSwing.Clear();
    }

    void OnTriggerEnter(Collider other)
    {
        if (!_active) return;
        if (_hitThisSwing.Contains(other)) return;

        // Check arc (dot product against owner's forward)
        Vector3 toOther = (other.transform.position - transform.position).normalized;
        float   angle   = Vector3.Angle(transform.parent?.parent?.forward ?? Vector3.forward, toOther);
        if (angle > _arc * 0.5f) return;

        // Ignore own player colliders
        if (other.GetComponentInParent<NinjaController>() != null) return;

        var enemy = other.GetComponentInParent<EnemyBase>();
        if (enemy != null)
        {
            _hitThisSwing.Add(other);
            
            // PHYSICS-BASED DAMAGE: Scale damage by Rigidbody velocity
            float impactVel = _ownerRb != null ? _ownerRb.linearVelocity.magnitude : 5f;
            float physDamage = impactVel * 2.5f; // Physics multiplier
            
            enemy.TakeDamage(physDamage, transform.root);

            var nc = transform.root.GetComponentInChildren<NinjaController>();
            if (nc != null && ComboSystem.Instance != null)
            {
                ComboSystem.Instance.RegisterHit(nc.PlayerIndex, physDamage);
            }

            // Hit-stop: scale duration with impact force
            float stopDur = Mathf.Clamp(physDamage * 0.002f, 0.04f, 0.12f);
            StartCoroutine(HitStop(stopDur));

            // Directional knockback — player velocity at impact drives the launch vector.
            // Moving forward = enemy goes where you're going (Skate-style momentum transfer).
            // Diving = spike; rising = uppercut; neutral = standard send.
            var enemyRb = enemy.GetComponent<Rigidbody>();
            if (enemyRb != null)
            {
                Vector3 playerVel = _ownerRb != null ? _ownerRb.linearVelocity : Vector3.zero;
                Vector3 horizVel  = new Vector3(playerVel.x, 0f, playerVel.z);
                Vector3 awayDir   = (enemy.transform.position - transform.root.position);
                awayDir.y = 0f;
                if (awayDir.sqrMagnitude < 0.01f) awayDir = transform.root.forward;
                awayDir.Normalize();

                Vector3 knockDir;
                if (horizVel.magnitude > 3f)
                {
                    // Blend player travel direction into the send direction
                    knockDir = (horizVel.normalized * 0.6f + awayDir * 0.4f).normalized;
                    float upBias = playerVel.y < -6f ? 0.10f   // dive → spike outward
                                 : playerVel.y >  4f ? 1.40f   // rising → uppercut skyward
                                 : 0.45f;                       // neutral → standard
                    knockDir = (knockDir + Vector3.up * upBias).normalized;
                }
                else
                {
                    knockDir = (awayDir + Vector3.up * 0.4f).normalized;
                }

                enemyRb.linearVelocity = Vector3.zero; // clean slate — force is predictable
                enemyRb.AddForce(knockDir * (physDamage * 0.45f), ForceMode.Impulse);
            }

            // Momentum burst: chaining hits slightly accelerates the attacker
            if (nc != null) nc.ApplySpeedBoost(1.0f, 1.15f);

            // Spawn hit sparks scaled by impact
            SpawnScalingHitVFX(other.ClosestPoint(transform.position), impactVel);
        }
    }

    IEnumerator HitStop(float duration)
    {
        Time.timeScale = 0.05f;
        yield return new WaitForSecondsRealtime(duration);
        Time.timeScale = 1f;
    }

    void SpawnScalingHitVFX(Vector3 position, float velocity)
    {
        var vfx  = new GameObject("PhysicsSpark");
        vfx.transform.position = position;
        var ps   = vfx.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.duration      = 0.25f;
        main.loop          = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.1f, 0.3f);
        main.startSpeed    = new ParticleSystem.MinMaxCurve(velocity * 0.5f, velocity);
        main.startSize     = new ParticleSystem.MinMaxCurve(0.05f, 0.1f + velocity * 0.01f);
        main.startColor    = new ParticleSystem.MinMaxGradient(Color.white, GameBootstrapper.PaletteGold);
        main.maxParticles  = (int)(20 + velocity * 2);

        var emit = ps.emission;
        emit.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)main.maxParticles) });
        emit.rateOverTime = 0;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius    = 0.05f;

        ps.Play();
        Destroy(vfx, 0.6f);
    }
}
