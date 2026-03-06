using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// ComboSystem — tracks hit streaks per player.
/// Combos increase Ki regeneration and trigger villager worship at high levels.
/// </summary>
public class ComboSystem : MonoBehaviour
{
    public static ComboSystem Instance;

    private Dictionary<int, int> _comboCounts = new Dictionary<int, int>();
    private Dictionary<int, float> _lastHitTimes = new Dictionary<int, float>();

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void RegisterHit(int playerIndex, float damage)
    {
        if (playerIndex < 0) return;

        if (!_comboCounts.ContainsKey(playerIndex))
        {
            _comboCounts[playerIndex] = 0;
            _lastHitTimes[playerIndex] = 0f;
        }

        _comboCounts[playerIndex]++;
        _lastHitTimes[playerIndex] = Time.time;

        // Check for villager worship at high combos
        if (_comboCounts[playerIndex] >= GameSettings.ComboWorshipThreshold && _comboCounts[playerIndex] % 5 == 0)
        {
            TriggerVillagerWorship(playerIndex);
        }
    }

    public int GetComboCount(int playerIndex)
    {
        if (!_comboCounts.ContainsKey(playerIndex)) return 0;
        
        // Check for decay
        if (Time.time - _lastHitTimes[playerIndex] > GameSettings.ComboDecayTime)
        {
            _comboCounts[playerIndex] = 0;
            return 0;
        }

        return _comboCounts[playerIndex];
    }

    public float GetMultiplier(int playerIndex)
    {
        int combo = GetComboCount(playerIndex);
        
        if (combo >= GameSettings.ComboTier3Threshold) return GameSettings.ComboTier3Multiplier;
        if (combo >= GameSettings.ComboTier2Threshold) return GameSettings.ComboTier2Multiplier;
        if (combo >= GameSettings.ComboTier1Threshold) return GameSettings.ComboTier1Multiplier;
        
        return 1.0f; // Base
    }

    private void TriggerVillagerWorship(int playerIndex)
    {
        // Find players to get position
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
            {
                v.Celebrate(1.5f); // Existing method in Villager
            }
        }
    }
}
