using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// ComboSystem — tracks sustained combat time per player.
/// Multiplier rises the longer you stay in continuous combat (hit within decay window).
/// Combos also trigger villager worship at high engagement times.
/// </summary>
public class ComboSystem : MonoBehaviour
{
    public static ComboSystem Instance;

    // Track when continuous combat began and last hit time per player
    private Dictionary<int, float> _combatStartTime = new Dictionary<int, float>();
    private Dictionary<int, float> _lastHitTime     = new Dictionary<int, float>();

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    /// <summary>
    /// Call when the player lands a hit. Starts or continues the combat clock.
    /// </summary>
    public void RegisterHit(int playerIndex, float damage)
    {
        if (playerIndex < 0) return;

        float now = Time.time;

        if (!_lastHitTime.ContainsKey(playerIndex))
        {
            // First hit ever — open a fresh combat window
            _combatStartTime[playerIndex] = now;
            _lastHitTime[playerIndex]     = now;
        }
        else if (now - _lastHitTime[playerIndex] > GameSettings.ComboDecayTime)
        {
            // Gap too long — reset the clock
            _combatStartTime[playerIndex] = now;
        }
        // Either way, refresh last-hit timestamp
        _lastHitTime[playerIndex] = now;

        // Trigger villager worship at sustained combat milestones
        float elapsed = GetCombatTime(playerIndex);
        if (elapsed >= GameSettings.ComboWorshipThreshold)
        {
            // Every ~5 seconds of sustained combat past the threshold
            if ((int)(elapsed) % 5 == 0)
                TriggerVillagerWorship(playerIndex);
        }
    }

    /// <summary>
    /// Seconds of continuous combat for this player. Returns 0 if the streak has decayed.
    /// </summary>
    public float GetCombatTime(int playerIndex)
    {
        if (!_lastHitTime.ContainsKey(playerIndex)) return 0f;

        // Streak has expired
        if (Time.time - _lastHitTime[playerIndex] > GameSettings.ComboDecayTime)
        {
            _combatStartTime[playerIndex] = 0f;
            _lastHitTime[playerIndex]     = 0f;
            return 0f;
        }

        return _lastHitTime[playerIndex] - _combatStartTime[playerIndex];
    }

    /// <summary>
    /// Legacy int accessor — returns floored combat seconds for display/worship checks.
    /// </summary>
    public int GetComboCount(int playerIndex) => Mathf.FloorToInt(GetCombatTime(playerIndex));

    /// <summary>
    /// Score/Ki multiplier based on how long the player has been in sustained combat.
    /// </summary>
    public float GetMultiplier(int playerIndex)
    {
        float t = GetCombatTime(playerIndex);

        if (t >= GameSettings.ComboTier3Threshold) return GameSettings.ComboTier3Multiplier;
        if (t >= GameSettings.ComboTier2Threshold) return GameSettings.ComboTier2Multiplier;
        if (t >= GameSettings.ComboTier1Threshold) return GameSettings.ComboTier1Multiplier;

        return 1.0f;
    }

    private void TriggerVillagerWorship(int playerIndex)
    {
        var players = FindObjectsByType<NinjaController>(FindObjectsSortMode.None);
        Vector3 playerPos = Vector3.zero;
        foreach (var p in players)
        {
            if (p.PlayerIndex == playerIndex)
            {
                playerPos = p.transform.position;
                break;
            }
        }
        if (playerPos == Vector3.zero) return;

        var villagers = FindObjectsByType<Villager>(FindObjectsSortMode.None);
        foreach (var v in villagers)
        {
            if (Vector3.Distance(v.transform.position, playerPos) < GameSettings.ComboWorshipRadius)
                v.Celebrate(1.5f);
        }
    }
}
