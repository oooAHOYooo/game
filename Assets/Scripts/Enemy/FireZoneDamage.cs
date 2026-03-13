using UnityEngine;

/// <summary>
/// Attached to fire-zone trigger spheres left by SkyBomb enemies.
/// Deals periodic fire damage to players who stand in the zone.
/// </summary>
public class FireZoneDamage : MonoBehaviour
{
    private const float DAMAGE_PER_TICK = 8f;
    private const float TICK_INTERVAL   = 0.6f;

    private float _tickTimer = 0f;

    void Update()
    {
        _tickTimer -= Time.deltaTime;
        if (_tickTimer > 0f) return;
        _tickTimer = TICK_INTERVAL;

        // Damage every player inside the sphere
        var sc = GetComponent<SphereCollider>();
        if (sc == null) return;

        Collider[] hits = Physics.OverlapSphere(transform.position, sc.radius);
        foreach (var h in hits)
        {
            var ph = h.GetComponentInParent<PlayerHealth>();
            if (ph != null && ph.IsAlive)
                ph.TakeDamage(DAMAGE_PER_TICK);
        }
    }
}
