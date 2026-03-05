using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// HazardLava — Attached to volcanic puddles. Deals damage over time to entities inside.
/// </summary>
public class HazardLava : MonoBehaviour
{
    public float DamagePerTick = 10f;
    public float TickInterval = 1f;
    
    // Map of collider to next tick time
    private Dictionary<Collider, float> _inside = new Dictionary<Collider, float>();

    void OnTriggerEnter(Collider other)
    {
        if (IsValidTarget(other))
        {
            _inside[other] = Time.time; // Immediate first tick? Or delay? Let's tick immediately in Stay
        }
    }

    void OnTriggerStay(Collider other)
    {
        if (_inside.TryGetValue(other, out float nextTickTime))
        {
            if (Time.time >= nextTickTime)
            {
                ApplyDamage(other);
                _inside[other] = Time.time + TickInterval;
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        _inside.Remove(other);
    }

    private bool IsValidTarget(Collider col)
    {
        return col.GetComponentInParent<PlayerHealth>() != null || col.GetComponentInParent<EnemyBase>() != null;
    }

    private void ApplyDamage(Collider col)
    {
        // Player
        var playerDef = col.GetComponentInParent<PlayerHealth>();
        if (playerDef != null)
        {
            playerDef.TakeDamage(DamagePerTick);
            return;
        }

        // Enemy
        var enemy = col.GetComponentInParent<EnemyBase>();
        if (enemy != null)
        {
            enemy.TakeDamage(DamagePerTick, transform);
        }
    }
}
