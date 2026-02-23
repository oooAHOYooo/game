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
    private HashSet<Collider> _hitThisSwing = new HashSet<Collider>();

    // Returns IEnumerator so NinjaController can yield on it
    public IEnumerator Activate(float damage, float range, float arc, float duration, Transform preferredTarget)
    {
        _damage           = damage;
        _range            = range;
        _arc              = arc;
        _active           = true;
        _preferredTarget  = preferredTarget;
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
            enemy.TakeDamage(_damage, transform.root);

            // Hit-stop: pause both parties briefly for impact weight
            StartCoroutine(HitStop(0.06f));

            // Knockback
            var rb = enemy.GetComponent<Rigidbody>();
            if (rb != null)
            {
                Vector3 dir = (enemy.transform.position - transform.root.position).normalized + Vector3.up * 0.4f;
                rb.AddForce(dir * (_damage * 0.5f), ForceMode.Impulse);
            }

            // Spawn hit sparks
            SpawnHitVFX(other.ClosestPoint(transform.position));
        }
    }

    IEnumerator HitStop(float duration)
    {
        Time.timeScale = 0.05f;
        yield return new WaitForSecondsRealtime(duration);
        Time.timeScale = 1f;
    }

    void SpawnHitVFX(Vector3 position)
    {
        var vfx  = new GameObject("HitSpark");
        vfx.transform.position = position;
        var ps   = vfx.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.duration      = 0.2f;
        main.loop          = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.1f, 0.25f);
        main.startSpeed    = new ParticleSystem.MinMaxCurve(3f, 8f);
        main.startSize     = new ParticleSystem.MinMaxCurve(0.05f, 0.15f);
        main.startColor    = new ParticleSystem.MinMaxGradient(Color.white, GameBootstrapper.PaletteGold);
        main.maxParticles  = 30;

        var emit = ps.emission;
        emit.SetBursts(new[] { new ParticleSystem.Burst(0f, 30) });
        emit.rateOverTime = 0;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius    = 0.05f;

        ps.Play();
        Destroy(vfx, 0.5f);
    }
}
