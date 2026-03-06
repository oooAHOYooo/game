using UnityEngine;

public enum PowerUpType { HealthHeart, FullKi, SpeedBoost, DamageBoost }

/// <summary>
/// PowerUp — Interactive collectible that restores HP or Ki for the player.
/// </summary>
public class PowerUp : MonoBehaviour
{
    public PowerUpType Type;
    public float HealthAmount = 20f; // 1 Heart
    private float _bobOffset;

    void Start()
    {
        _bobOffset = Random.value * Mathf.PI * 2f;
        
        // Spin and glow visual setup
        var mat = new Material(GameBootstrapper.GetHDRPLitShader());
        if (Type == PowerUpType.HealthHeart)
        {
            mat.color = new Color(1f, 0.2f, 0.3f);
            GameBootstrapper.SetHDRPEmission(mat, new Color(1f, 0.2f, 0.3f), 4f);
        }
        else if (Type == PowerUpType.FullKi)
        {
            mat.color = GameBootstrapper.PaletteCyan;
            GameBootstrapper.SetHDRPEmission(mat, GameBootstrapper.PaletteCyan, 4f);
        }
        else if (Type == PowerUpType.SpeedBoost)
        {
            mat.color = GameBootstrapper.PaletteGold;
            GameBootstrapper.SetHDRPEmission(mat, GameBootstrapper.PaletteGold, 6f);
        }
        else // DamageBoost
        {
            mat.color = GameBootstrapper.PaletteMagenta;
            GameBootstrapper.SetHDRPEmission(mat, GameBootstrapper.PaletteMagenta, 6f);
        }
        
        var renderer = GetComponentInChildren<Renderer>();
        if (renderer != null) renderer.material = mat;
    }

    void Update()
    {
        // Animate
        transform.Rotate(Vector3.up * 90f * Time.deltaTime);
        transform.position += Vector3.up * Mathf.Sin(Time.time * 3f + _bobOffset) * 0.005f;
    }

    void OnTriggerEnter(Collider other)
    {
        var player = other.GetComponentInParent<NinjaController>();
        var health = other.GetComponentInParent<PlayerHealth>();

        if (player != null && health != null)
        {
            if (Type == PowerUpType.HealthHeart)
            {
                health.Heal(HealthAmount);
            }
            else if (Type == PowerUpType.FullKi)
            {
                player.CurrentKi = GameSettings.PlayerMaxKi;
            }
            else if (Type == PowerUpType.SpeedBoost)
            {
                player.ApplySpeedBoost(15f, 1.6f); // 15 seconds, 1.6x speed
            }
            else if (Type == PowerUpType.DamageBoost)
            {
                player.ApplyDamageBoost(15f, 2.0f); // 15 seconds, 2x damage
            }

            // VFX
            if (ImpactFeedback.Instance != null)
            {
                 // Small pulse feedback
                ImpactFeedback.Play(new DamageInfo{Amount = 10f}, transform.position);
            }

            Destroy(gameObject);
        }
    }
}
