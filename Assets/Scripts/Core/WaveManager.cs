using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// WaveManager — spawns enemies in waves (3 → 5 → 8 → 8+scaling) and manages
/// the Zelda-style 1v1 engagement queue. One enemy fights at a time; others circle.
/// </summary>
public class WaveManager : MonoBehaviour
{
    public Transform ArenaRoot;
    public Village    IslandVillage;  // village HP & celebration

    // ── Wave config ───────────────────────────────────────────────────────
    private static readonly int[] WaveSizes   = { 3, 5, 8 };
    private const float SpawnEdgeRadius       = 160f;  // ocean edge of island
    private const float IntermissionDuration  = 5f;
    private const float WaveClearPause        = 3f;
    private const float VillageDamageRadius   = 25f;   // if enemy gets this close, village takes damage

    // ── State ─────────────────────────────────────────────────────────────
    public  int    CurrentWave    = 0;
    private int    _aliveCount    = 0;

    // Engagement queue
    private Queue<EnemyBase>  _engageQueue  = new Queue<EnemyBase>();
    private EnemyBase         _activeEnemy  = null;
    private List<EnemyBase>   _allEnemies   = new List<EnemyBase>();

    // ─────────────────────────────────────────────────────────────────────
    void Start()
    {
        StartCoroutine(GameLoop());
        StartCoroutine(VillageDamageCheck());
    }

    IEnumerator GameLoop()
    {
        yield return new WaitForSeconds(2f); // brief opening pause

        while (true)
        {
            yield return StartCoroutine(SpawnWave(CurrentWave));
            yield return StartCoroutine(WaitForWaveClear());
            yield return StartCoroutine(Intermission());
            CurrentWave++;
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    // SPAWNING
    // ─────────────────────────────────────────────────────────────────────
    IEnumerator SpawnWave(int waveIndex)
    {
        _allEnemies.Clear();
        _engageQueue.Clear();
        _activeEnemy = null;

        int count     = GetWaveSize(waveIndex);
        var enemyList = BuildWaveComposition(waveIndex, count);

        // Notify HUD
        var hud = FindAnyObjectByType<GameHUD>();
        if (hud != null) hud.ShowWaveBanner(waveIndex + 1);

        // Spawn enemies one-by-one with a tiny delay each
        for (int i = 0; i < enemyList.Count; i++)
        {
            Vector3 spawnPos = GetSpawnPosition(i, enemyList.Count);

            GameObject enemyGO = enemyList[i] switch
            {
                "ShadowArcher" => EnemyBase.BuildShadowArcher(spawnPos, waveIndex),
                "Berserker"    => EnemyBase.BuildBerserker(spawnPos, waveIndex),
                "MiniBoss"     => EnemyBase.BuildMiniBoss(spawnPos, waveIndex),
                _              => EnemyBase.BuildFootSoldier(spawnPos, waveIndex)
            };

            var eb = enemyGO.GetComponent<EnemyBase>();
            _allEnemies.Add(eb);
            _engageQueue.Enqueue(eb);
            _aliveCount++;

            yield return new WaitForSeconds(0.3f);
        }

        // Kick off the first engagement
        PromoteNextEnemy();
    }

    // ─────────────────────────────────────────────────────────────────────
    // ENGAGEMENT QUEUE
    // ─────────────────────────────────────────────────────────────────────
    void PromoteNextEnemy()
    {
        _activeEnemy = null;

        // Drain dead from queue
        while (_engageQueue.Count > 0)
        {
            var candidate = _engageQueue.Peek();
            if (candidate == null || !candidate.IsAlive)
            {
                _engageQueue.Dequeue();
                continue;
            }
            _engageQueue.Dequeue();
            _activeEnemy = candidate;
            _activeEnemy.IsActiveEnemy = true;
            break;
        }
    }

    public void OnEnemyDied(EnemyBase enemy)
    {
        _aliveCount = Mathf.Max(0, _aliveCount - 1);
        _allEnemies.Remove(enemy);

        if (_activeEnemy == enemy)
            PromoteNextEnemy();
    }

    /// Called by MiniBoss clone or other dynamic spawns
    public void RegisterExtraEnemy(EnemyBase enemy)
    {
        _allEnemies.Add(enemy);
        _engageQueue.Enqueue(enemy);
        _aliveCount++;
    }

    // ─────────────────────────────────────────────────────────────────────
    // WAVE CLEAR / INTERMISSION
    // ─────────────────────────────────────────────────────────────────────
    IEnumerator WaitForWaveClear()
    {
        while (_aliveCount > 0) yield return new WaitForSeconds(0.1f);


        // Celebration VFX
        SpawnClearVFX();

        // Villagers celebrate!
        if (IslandVillage != null)
        {
            var villagers = FindObjectsByType<Villager>(FindObjectsSortMode.None);
            foreach (var v in villagers) { v.SetPanic(false); v.Celebrate(4f); }
        }

        var hud = FindAnyObjectByType<GameHUD>();
        if (hud != null) hud.ShowWaveClear();

        yield return new WaitForSeconds(WaveClearPause);
    }

    IEnumerator Intermission()
    {

        var hud = FindAnyObjectByType<GameHUD>();
        for (int i = Mathf.RoundToInt(IntermissionDuration); i > 0; i--)
        {
            if (hud != null) hud.ShowIntermission(i);
            yield return new WaitForSeconds(1f);
        }

    }

    // ─────────────────────────────────────────────────────────────────────
    // HELPERS
    // ─────────────────────────────────────────────────────────────────────
    int GetWaveSize(int waveIndex)
    {
        if (waveIndex < WaveSizes.Length) return WaveSizes[waveIndex];
        // After predefined waves: repeat 8 with escalating stats
        return 8;
    }

    List<string> BuildWaveComposition(int waveIndex, int count)
    {
        var list = new List<string>();

        if (waveIndex == 0)
        {
            // Wave 1: all foot soldiers
            for (int i = 0; i < count; i++) list.Add("FootSoldier");
        }
        else if (waveIndex == 1)
        {
            // Wave 2: soldiers + 1 archer
            for (int i = 0; i < count - 1; i++) list.Add("FootSoldier");
            list.Add("ShadowArcher");
        }
        else if (waveIndex == 2)
        {
            // Wave 3: soldiers + 2 archers + 1 berserker
            list.Add("Berserker");
            list.Add("ShadowArcher");
            list.Add("ShadowArcher");
            for (int i = 3; i < count; i++) list.Add("FootSoldier");
        }
        else
        {
            // Wave 4+: increasing complexity
            list.Add("MiniBoss");
            int remaining = count - 1;
            int archers   = remaining / 3;
            for (int i = 0; i < archers; i++)   list.Add("ShadowArcher");
            for (int i = archers; i < remaining; i++) list.Add("FootSoldier");
        }

        // Shuffle
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }

        return list;
    }

    Vector3 GetSpawnPosition(int index, int total)
    {
        // Spawn from the ocean edge surrounding the island
        float angle = (index / (float)total) * 360f * Mathf.Deg2Rad;
        float jitter = Random.Range(-10f, 10f);
        Vector3 pos = new Vector3(
            Mathf.Cos(angle) * (SpawnEdgeRadius + jitter),
            IslandGenerator.WaterLevel + 1f,
            Mathf.Sin(angle) * (SpawnEdgeRadius + jitter)
        );
        return pos;
    }

    public Vector3 GetSafeSpawnPosition()
    {
        // Safe respawn: near village, on terrain
        Vector3 pos = new Vector3(Random.Range(-12f, 12f), 10f, Random.Range(-12f, 12f));
        if (Physics.Raycast(pos + Vector3.up * 100f, Vector3.down, out RaycastHit hit, 200f))
            pos.y = hit.point.y + 0.5f;
        else
            pos.y = 2f;
        return pos;
    }

    /// <summary>Periodically checks if any enemy has breached the village perimeter</summary>
    IEnumerator VillageDamageCheck()
    {
        while (true)
        {
            yield return new WaitForSeconds(0.5f);
            if (IslandVillage == null || IslandVillage.IsDestroyed) continue;

            foreach (var e in _allEnemies)
            {
                if (e == null || !e.IsAlive) continue;
                float dist = Vector3.Distance(e.transform.position, IslandVillage.Centre);
                if (dist < VillageDamageRadius)
                {
                    IslandVillage.TakeDamage(5f);  // chip damage per enemy per tick
                    break;  // only one tick per check
                }
            }
        }
    }

    void SpawnClearVFX()
    {
        for (int i = 0; i < 6; i++)
        {
            float angle = i / 6f * Mathf.PI * 2f;
            Vector3 pos = new Vector3(Mathf.Cos(angle) * 5f, 2f, Mathf.Sin(angle) * 5f);

            var obj  = new GameObject("ClearVFX");
            obj.transform.position = pos;
            var ps   = obj.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.duration      = 0.5f;
            main.loop          = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 1.2f);
            main.startSpeed    = new ParticleSystem.MinMaxCurve(5f, 15f);
            main.startSize     = new ParticleSystem.MinMaxCurve(0.2f, 0.5f);
            main.startColor    = new ParticleSystem.MinMaxGradient(GameBootstrapper.PaletteGold, GameBootstrapper.PaletteCyan);
            main.maxParticles  = 80;
            main.gravityModifier = -0.3f;

            var em = ps.emission;
            em.SetBursts(new[] { new ParticleSystem.Burst(0f, 80) });
            em.rateOverTime = 0;
            ps.Play();
            Destroy(obj, 2f);
        }
    }
}
