using UnityEngine;

/// <summary>
/// PlayerHealth — tracks HP, handles damage intake, death and respawn.
/// </summary>
public class PlayerHealth : MonoBehaviour
{
    public int   PlayerIndex   = 0;
    public float MaxHP         = 100f;
    public float RespawnTime   = 3f;

    [HideInInspector] public float CurrentHP;
    [HideInInspector] public bool  IsAlive = true;

    private bool _isInvincible = false;

    void Start() => CurrentHP = GameSettings.PlayerMaxHP;

    public void TakeDamage(float amount)
    {
        if (_isInvincible || !IsAlive) return;

        CurrentHP -= amount;
        CurrentHP = Mathf.Max(0, CurrentHP);

        // Camera shake on hit
        SplitScreenCamera.ShakeCamera(PlayerIndex, 0.15f, 0.3f);

        if (CurrentHP <= 0 && IsAlive)
            StartCoroutine(Die());
    }

    public void SetInvincible(bool value) => _isInvincible = value;

    public void Heal(float amount)
    {
        CurrentHP = Mathf.Min(MaxHP, CurrentHP + amount);
    }

    System.Collections.IEnumerator Die()
    {
        IsAlive = false;

        // Flash & shrink on death
        var renderers = GetComponentsInChildren<Renderer>();
        for (float t = 0; t < 0.5f; t += Time.deltaTime)
        {
            float alpha = Mathf.PingPong(t * 20f, 1f);
            transform.localScale = Vector3.Lerp(Vector3.one, Vector3.zero, t / 0.5f);
            yield return null;
        }

        gameObject.SetActive(false);

        yield return new WaitForSeconds(RespawnTime);

        // Respawn at a safe position
        var wm = FindAnyObjectByType<WaveManager>();
        Vector3 safePos = wm != null ? wm.GetSafeSpawnPosition() : new Vector3(PlayerIndex == 0 ? -5f : 5f, 0.5f, 0f);
        transform.position   = safePos;
        transform.localScale = Vector3.one;
        CurrentHP            = MaxHP * 0.6f; // respawn at 60% HP
        IsAlive              = true;
        gameObject.SetActive(true);
    }
}
