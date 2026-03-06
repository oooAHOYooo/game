using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// WaveManager — spawns enemies in escalating waves and manages
/// the Zelda-style 1v1 engagement queue. One enemy fights at a time; others circle.
/// The more enemies you defeat, the more come! Includes arsonist enemies.
/// </summary>
public class WaveManager : MonoBehaviour
{
    public Transform ArenaRoot;
    public Village    IslandVillage;  // village HP & celebration

    // ── Wave config ───────────────────────────────────────────────────────
    private const float SpawnEdgeRadius       = 160f;  // ocean edge of island
    private const float IntermissionDuration  = 5f;
    private const float WaveClearPause        = 3f;
    private const float VillageDamageRadius   = 25f;   // if enemy gets this close, village takes damage
    private const float REINFORCEMENT_DELAY   = 4f;    // seconds between mid-wave reinforcement checks

    // ── State ─────────────────────────────────────────────────────────────
    public  int    CurrentWave    = 0;
    private int    _aliveCount    = 0;
    private int    _totalKills    = 0;
    private int    _waveMaxSpawn  = 0;   // total enemies for current wave
    private int    _waveSpawned   = 0;   // how many have spawned so far this wave
    private bool   _waveActive    = false;

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
            if (DayNightCycle.Instance != null) DayNightCycle.Instance.SetPhase(DayNightCycle.Phase.Sunset);
            yield return new WaitForSeconds(2f);

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
        _waveActive  = true;

        if (DayNightCycle.Instance != null) DayNightCycle.Instance.SetPhase(DayNightCycle.Phase.Night);

        int count         = GetWaveSize(waveIndex);
        _waveMaxSpawn     = count + Mathf.FloorToInt(count * 0.5f); // allow 50% bonus reinforcements
        int initialSpawn  = count;
        _waveSpawned      = 0;

        var enemyList = BuildWaveComposition(waveIndex, initialSpawn);

        // Notify HUD
        var hud = FindAnyObjectByType<GameHUD>();
        if (hud != null) hud.ShowWaveBanner(waveIndex + 1);

        // Nintendo-style alert for players
        var controllers = FindObjectsByType<NinjaController>(FindObjectsSortMode.None);
        foreach (var ctrl in controllers) GameBootstrapper.SpawnAlert(ctrl.transform);

        // Spawn enemies one-by-one with a tiny delay each
        for (int i = 0; i < enemyList.Count; i++)
        {
            SpawnSingleEnemy(enemyList[i], i, enemyList.Count, waveIndex);
            yield return new WaitForSeconds(0.3f);
        }

        // Kick off the first engagement
        PromoteNextEnemy();

        // Start reinforcement coroutine
        StartCoroutine(ReinforcementLoop(waveIndex));
    }

    void SpawnSingleEnemy(string type, int index, int total, int waveIndex)
    {
        Vector3 spawnPos = GetSpawnPosition(index, total);

        GameObject enemyGO = type switch
        {
            "Elite"    => EnemyBase.BuildShadowArcher(spawnPos, waveIndex),
            "Brute"    => EnemyBase.BuildBerserker(spawnPos, waveIndex),
            "Boss"     => EnemyBase.BuildMiniBoss(spawnPos, waveIndex),
            "Arsonist" => EnemyBase.BuildArsonist(spawnPos, waveIndex),
            _          => EnemyBase.BuildFootSoldier(spawnPos, waveIndex)
        };

        var eb = enemyGO.GetComponent<EnemyBase>();
        _allEnemies.Add(eb);
        _engageQueue.Enqueue(eb);
        _aliveCount++;
        _waveSpawned++;

        // Visual arrival effect
        SpawnEnemyArrivalFX(spawnPos);
    }

    void SpawnEnemyArrivalFX(Vector3 pos)
    {
        var obj = new GameObject("EnemyArrivalVFX");
        obj.transform.position = pos;
        
        var ps = obj.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main;
        main.duration = 1f;
        main.loop = false;
        main.startLifetime = 1f;
        main.startSpeed = 20f;
        main.startSize = 0.5f;
        main.startColor = GameBootstrapper.PaletteCyan;
        main.gravityModifier = -0.5f;

        var em = ps.emission;
        em.SetBursts(new[] { new ParticleSystem.Burst(0f, 100) });
        
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 15f;
        
        ps.Play();
        
        // Light pillar
        var lightObj = new GameObject("ArrivalLight");
        lightObj.transform.SetParent(obj.transform);
        lightObj.transform.localPosition = Vector3.zero;
        var l = lightObj.AddComponent<Light>();
        l.type = LightType.Spot;
        l.innerSpotAngle = 5f;
        l.spotAngle = 10f;
        l.range = 100f;
        l.color = GameBootstrapper.PaletteCyan;
        l.intensity = 500000f;
        lightObj.transform.rotation = Quaternion.Euler(-90, 0, 0);

        Destroy(obj, 2f);
    }

    /// <summary>
    /// Mid-wave reinforcement: as enemies die, spawn more to keep the pressure up.
    /// "The more enemies you defeat, the more come!"
    /// </summary>
    IEnumerator ReinforcementLoop(int waveIndex)
    {
        while (_waveActive)
        {
            yield return new WaitForSeconds(REINFORCEMENT_DELAY);

            if (!_waveActive) break;

            // Only reinforce if we haven't hit the wave's max and there are fewer alive than desired
            int desiredAlive = Mathf.Max(2, GetWaveSize(waveIndex) / 2);
            if (_aliveCount < desiredAlive && _waveSpawned < _waveMaxSpawn)
            {
                int toSpawn = Mathf.Min(2, _waveMaxSpawn - _waveSpawned);
                var reinforcements = BuildWaveComposition(waveIndex, toSpawn);
                for (int i = 0; i < reinforcements.Count; i++)
                {
                    SpawnSingleEnemy(reinforcements[i], i, reinforcements.Count, waveIndex);
                }
                // Ensure we have an active engagement
                if (_activeEnemy == null || !_activeEnemy.IsAlive)
                    PromoteNextEnemy();
            }
        }
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
        _totalKills++;

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

        _waveActive = false;

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

        yield return new WaitForSeconds(GameSettings.WaveClearPause);
    }

    IEnumerator Intermission()
    {
        if (DayNightCycle.Instance != null) DayNightCycle.Instance.SetPhase(DayNightCycle.Phase.Day);

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
        // Endless escalation: starts at 3, grows each wave
        return 3 + waveIndex * 2;
    }

    List<string> BuildWaveComposition(int waveIndex, int count)
    {
        var list = new List<string>();

        if (waveIndex == 0)
        {
            // Wave 1: all grunts
            for (int i = 0; i < count; i++) list.Add("Grunt");
        }
        else if (waveIndex == 1)
        {
            // Wave 2: grunts + 1 elite
            for (int i = 0; i < count - 1; i++) list.Add("Grunt");
            list.Add("Elite");
        }
        else if (waveIndex == 2)
        {
            // Wave 3: grunts + elites + 1 arsonist
            list.Add("Arsonist");
            list.Add("Elite");
            for (int i = 2; i < count; i++) list.Add("Grunt");
        }
        else if (waveIndex == 3)
        {
            // Wave 4: brute + elites + arsonists
            list.Add("Brute");
            list.Add("Arsonist");
            list.Add("Elite");
            list.Add("Elite");
            for (int i = 4; i < count; i++) list.Add("Grunt");
        }
        else
        {
            // Wave 5+: boss + escalating complexity + always arsonists
            list.Add("Boss");
            int arsonists = Mathf.Min(waveIndex - 2, count / 3); // more arsonists over time
            for (int i = 0; i < arsonists; i++) list.Add("Arsonist");
            int remaining = count - 1 - arsonists;
            int elites    = remaining / 3;
            int brutes    = remaining / 5;
            for (int i = 0; i < brutes; i++)  list.Add("Brute");
            for (int i = 0; i < elites; i++)  list.Add("Elite");
            int grunts = remaining - elites - brutes;
            for (int i = 0; i < grunts; i++)  list.Add("Grunt");
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
                // Skip arsonists — they manage their own village damage
                var ai = e.GetComponent<EnemyAI>();
                if (ai != null && ai.EnemyType == "Arsonist") continue;

                float dist = Vector3.Distance(e.transform.position, IslandVillage.Centre);
                if (dist < GameSettings.VillageDamageRadius)
                {
                    IslandVillage.TakeDamage(GameSettings.VillageDamagePerTick);  // chip damage per enemy per tick
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
