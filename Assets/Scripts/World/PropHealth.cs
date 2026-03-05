using UnityEngine;

/// <summary>
/// PropHealth — Allows environment objects (like Bamboo) to take damage and spawn particles when broken.
/// </summary>
public class PropHealth : MonoBehaviour
{
    public float MaxHP = 30f;
    private float _currentHP;

    void Start()
    {
        _currentHP = MaxHP;
    }

    public void TakeDamage(float amount)
    {
        _currentHP -= amount;
        
        // Spawn hit spark using ImpactFeedback if available
        if (ImpactFeedback.Instance != null && amount > 0)
        {
            var dmgInfo = new DamageInfo { Amount = amount, Critical = false };
            ImpactFeedback.Instance.Play(dmgInfo, transform.position + Vector3.up);
        }

        if (_currentHP <= 0)
        {
            Break();
        }
    }

    private void Break()
    {
        // 1. Spawn splinters
        var vfx = new GameObject("BambooSplinters");
        vfx.transform.position = transform.position + Vector3.up * 2f;
        var ps = vfx.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.duration = 1f;
        main.loop = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 1f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(3f, 8f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.1f, 0.3f);
        main.startColor = new Color(0.3f, 0.7f, 0.2f);
        var em = ps.emission;
        em.SetBursts(new[] { new ParticleSystem.Burst(0f, 20) });
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        ps.Play();
        Destroy(vfx, 2f);

        // 2. Play sound
        if (ImpactFeedback.Instance != null)
        {
            var audioData = new DamageInfo { Amount = 100f, Critical = true }; // Force heavy break sound
            ImpactFeedback.Instance.Play(audioData, transform.position);
        }

        // 3. Destroy prop
        Destroy(gameObject);
    }
}
