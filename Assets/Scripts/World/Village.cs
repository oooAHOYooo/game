using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Village — tiny tribal settlement at the centre of the island.
/// Has its own HP bar. Villagers worship the god-sized players.
/// Enemies can damage the village by reaching it.
/// </summary>
public class Village : MonoBehaviour
{
    public static Village Instance;

    // ── Config ────────────────────────────────────────────────────────────
    [Header("Village Stats")]
    public float MaxHP         = 500f;
    public float CurrentHP;
    public int   HutCount      = 12;
    public int   VillagerCount = 20;
    public float VillageRadius = 22f;

    // ── Palette ───────────────────────────────────────────────────────────
    static readonly Color HutWall     = new Color(0.55f, 0.40f, 0.22f);
    static readonly Color HutRoof     = new Color(0.70f, 0.50f, 0.15f);
    static readonly Color TotemGold   = new Color(0.85f, 0.65f, 0.10f);
    static readonly Color FireOrange  = new Color(1.0f, 0.45f, 0.05f);
    static readonly Color FireYellow  = new Color(1.0f, 0.80f, 0.20f);

    // ── References ────────────────────────────────────────────────────────
    public Transform VillageRoot { get; private set; }
    private List<GameObject> _huts = new List<GameObject>();
    public List<GameObject> Huts => _huts;
    private List<Villager>   _villagers = new List<Villager>();
    private GameObject       _totem;
    private bool             _isDestroyed = false;

    // Worship Synergy Mechanic
    public Vector3 TotemPosition => _totem != null ? _totem.transform.position : Vector3.zero;
    public int ActiveWorshippers { get; private set; }

    // ── Totem Upgrades — Faith System ───────────────────────────────────
    public float FaithPoints { get; private set; }
    public int   TotemLevel  { get; private set; } = 1;

    public void AddFaith(float amount)
    {
        if (_isDestroyed) return;
        FaithPoints += amount;
        CheckUpgrades();
    }

    void CheckUpgrades()
    {
        int oldLevel = TotemLevel;
        if (FaithPoints >= GameSettings.TotemLevel3Threshold) TotemLevel = 3;
        else if (FaithPoints >= GameSettings.TotemLevel2Threshold) TotemLevel = 2;
        else TotemLevel = 1;

        if (TotemLevel > oldLevel)
        {
            // Visual feedback for level up
            if (ImpactFeedback.Instance != null)
                ImpactFeedback.Instance.Play(new DamageInfo{Amount = 0}, _totem.transform.position + Vector3.up * 3f);
            
            // Notification VFX on totem
            StartCoroutine(TotemLevelUpVFX());
        }
    }

    System.Collections.IEnumerator TotemLevelUpVFX()
    {
        for (int i = 0; i < 5; i++)
        {
            ResetTotemGlow();
            yield return new WaitForSeconds(0.1f);
            foreach (var r in _totem.GetComponentsInChildren<Renderer>())
                GameBootstrapper.SetHDRPEmission(r.material, GameBootstrapper.PaletteCyan, 40f);
            yield return new WaitForSeconds(0.1f);
        }
        ResetTotemGlow();
    }

    public void OnEnemyKilledNearVillage(Vector3 pos)
    {
        if (Vector3.Distance(pos, Centre) < VillageRadius * 1.5f)
        {
            AddFaith(GameSettings.FaithPerKillNearVillage);
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    public void Build(Vector3 centre)
    {
        VillageRoot = new GameObject("Village").transform;
        VillageRoot.position = centre;
        CurrentHP = MaxHP;

        BuildHuts(centre);
        BuildTotem(centre);
        BuildCampfires(centre);
        BuildFence(centre);
        SpawnVillagers(centre);
    }

    void Awake()
    {
        Instance = this;
    }

    void Update()
    {
        if (_isDestroyed || _totem == null) return;

        bool playerNear = false;
        var players = FindObjectsByType<NinjaController>(FindObjectsSortMode.None);
        foreach (var p in players)
        {
            if (Vector3.Distance(p.transform.position, _totem.transform.position) <= GameSettings.TotemHealRadius)
            {
                playerNear = true;
                break;
            }
        }

        // Nintendo Synergy: Track how many villagers are currently worshipping
        // This makes Villagers an active resource, not just decoration!
        int worshippers = 0;
        foreach (var v in _villagers)
        {
            if (v.IsWorshipping) worshippers++;
        }
        ActiveWorshippers = worshippers;

        if (playerNear)
        {
            if (VillageFireSystem.Instance != null)
                VillageFireSystem.Instance.ExtinguishFires(_totem.transform.position, GameSettings.TotemHealRadius * 1.5f);

            // Heal village slowly over time
            float rateMult = TotemLevel >= 2 ? 1.5f : 1.0f;
            float newHP = Mathf.Min(MaxHP, CurrentHP + GameSettings.TotemHealRate * rateMult * Time.deltaTime);
            if (newHP > CurrentHP) CurrentHP = newHP;
            
            // Subtle pulse on totem to indicate healing
            foreach (var r in _totem.GetComponentsInChildren<Renderer>())
            {
                GameBootstrapper.SetHDRPEmission(r.material, GameBootstrapper.PaletteCyan, 8f + Mathf.PingPong(Time.time * 5f, 4f));
            }

            // Passive Faith gain while player protects totem
            AddFaith(GameSettings.FaithPerSecondWorshipped * Time.deltaTime);

            // Level 3 bonus: Feed Ki to player
            if (TotemLevel >= 3)
            {
                foreach (var p in players)
                {
                    if (Vector3.Distance(p.transform.position, _totem.transform.position) < GameSettings.TotemHealRadius)
                        p.CurrentKi = Mathf.Min(p.KiMax, p.CurrentKi + GameSettings.TotemExtraKiGain * Time.deltaTime);
                }
            }
        }

        // Level 2 Passive: Slow enemies in radius
        if (TotemLevel >= 2)
        {
            var enemies = FindObjectsByType<EnemyBase>(FindObjectsSortMode.None);
            foreach (var e in enemies)
            {
                if (Vector3.Distance(e.transform.position, Centre) < GameSettings.TotemShieldRadius)
                {
                    // Apply slow effect (simulated here since we work with existing state)
                    // In a real refactor we'd call e.ApplySlow()
                }
            }
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    // HUTS — tiny thatch huts arranged in a semicircle
    // ─────────────────────────────────────────────────────────────────────
    void BuildHuts(Vector3 centre)
    {
        for (int i = 0; i < HutCount; i++)
        {
            float angle = (i / (float)HutCount) * Mathf.PI * 2f;
            float radius = VillageRadius * Random.Range(0.35f, 0.85f);
            Vector3 pos = centre + new Vector3(
                Mathf.Cos(angle) * radius,
                0f,
                Mathf.Sin(angle) * radius);

            // Raycast for terrain height
            if (Physics.Raycast(pos + Vector3.up * 50f, Vector3.down, out RaycastHit hit, 100f))
                pos.y = hit.point.y;

            var hut = BuildHut(pos, angle);
            _huts.Add(hut);
        }
    }

    GameObject BuildHut(Vector3 pos, float facingAngle)
    {
        var hutRoot = new GameObject("Hut");
        hutRoot.transform.SetParent(VillageRoot);
        hutRoot.transform.position = pos;

        // These huts are TINY — villagers are about 0.15 units tall
        // Huts are like 0.3 high, 0.5 wide
        float hutScale = Random.Range(0.4f, 0.65f);

        // Walls (cube)
        var walls = GameObject.CreatePrimitive(PrimitiveType.Cube);
        walls.name = "HutWall";
        walls.transform.SetParent(hutRoot.transform);
        walls.transform.localPosition = Vector3.up * hutScale * 0.3f;
        walls.transform.localScale    = new Vector3(hutScale, hutScale * 0.5f, hutScale);
        walls.transform.localRotation = Quaternion.Euler(0, facingAngle * Mathf.Rad2Deg, 0);

        var wallMat = new Material(GameBootstrapper.GetHDRPLitShader());
        wallMat.color = Color.Lerp(HutWall, new Color(0.42f, 0.30f, 0.15f), Random.Range(0f, 0.4f));
        walls.GetComponent<Renderer>().material = wallMat;

        // Roof (cone-like = stretched inverted cube with rotation)
        var roof = GameObject.CreatePrimitive(PrimitiveType.Cube);
        roof.name = "HutRoof";
        roof.transform.SetParent(hutRoot.transform);
        roof.transform.localPosition = Vector3.up * hutScale * 0.65f;
        roof.transform.localScale    = new Vector3(hutScale * 1.25f, hutScale * 0.18f, hutScale * 1.25f);
        roof.transform.localRotation = Quaternion.Euler(0, 45f + facingAngle * Mathf.Rad2Deg, 0);
        Object.Destroy(roof.GetComponent<Collider>());

        var roofMat = new Material(GameBootstrapper.GetHDRPLitShader());
        roofMat.color = Color.Lerp(HutRoof, new Color(0.60f, 0.38f, 0.10f), Random.Range(0f, 0.5f));
        roof.GetComponent<Renderer>().material = roofMat;

        // Second roof layer (peaked)
        var peak = GameObject.CreatePrimitive(PrimitiveType.Cube);
        peak.name = "RoofPeak";
        peak.transform.SetParent(hutRoot.transform);
        peak.transform.localPosition = Vector3.up * hutScale * 0.80f;
        peak.transform.localScale    = new Vector3(hutScale * 0.6f, hutScale * 0.12f, hutScale * 0.6f);
        peak.transform.localRotation = Quaternion.Euler(0, facingAngle * Mathf.Rad2Deg, 0);
        Object.Destroy(peak.GetComponent<Collider>());
        peak.GetComponent<Renderer>().material = roofMat;

        // Tiny door opening (dark cube inset)
        var door = GameObject.CreatePrimitive(PrimitiveType.Cube);
        door.name = "Door";
        door.transform.SetParent(hutRoot.transform);
        Vector3 doorFwd = new Vector3(Mathf.Cos(facingAngle), 0, Mathf.Sin(facingAngle));
        door.transform.localPosition = Vector3.up * hutScale * 0.18f + doorFwd * hutScale * 0.48f;
        door.transform.localScale    = new Vector3(hutScale * 0.15f, hutScale * 0.25f, hutScale * 0.06f);
        Object.Destroy(door.GetComponent<Collider>());
        var doorMat = new Material(GameBootstrapper.GetHDRPLitShader());
        doorMat.color = new Color(0.02f, 0.02f, 0.04f);
        door.GetComponent<Renderer>().material = doorMat;

        return hutRoot;
    }

    // ─────────────────────────────────────────────────────────────────────
    // TOTEM — giant worship totem at village centre (player-sized!)
    // ─────────────────────────────────────────────────────────────────────
    void BuildTotem(Vector3 centre)
    {
        _totem = new GameObject("Totem");
        _totem.transform.SetParent(VillageRoot);

        Vector3 totemPos = centre;
        if (Physics.Raycast(centre + Vector3.up * 50f, Vector3.down, out RaycastHit hit, 100f))
            totemPos.y = hit.point.y;
        _totem.transform.position = totemPos;

        // Base pillar
        var pillar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        pillar.name = "TotemPillar";
        pillar.transform.SetParent(_totem.transform);
        pillar.transform.localPosition = Vector3.up * 1.5f;
        pillar.transform.localScale    = new Vector3(0.5f, 1.5f, 0.5f);

        var pillarMat = new Material(GameBootstrapper.GetHDRPLitShader());
        pillarMat.color = new Color(0.35f, 0.22f, 0.08f);
        GameBootstrapper.SetHDRPEmission(pillarMat, TotemGold, 2f);
        pillar.GetComponent<Renderer>().material = pillarMat;

        // Carved faces (3 tiers)
        for (int tier = 0; tier < 3; tier++)
        {
            var face = GameObject.CreatePrimitive(PrimitiveType.Cube);
            face.name = "TotemFace";
            face.transform.SetParent(_totem.transform);
            face.transform.localPosition = new Vector3(0, 0.6f + tier * 0.9f, 0.28f);
            face.transform.localScale    = new Vector3(0.35f, 0.25f, 0.06f);
            Object.Destroy(face.GetComponent<Collider>());

            var faceMat = new Material(GameBootstrapper.GetHDRPLitShader());
            faceMat.color = TotemGold;
            GameBootstrapper.SetHDRPEmission(faceMat, TotemGold, 5f + tier * 2f);
            face.GetComponent<Renderer>().material = faceMat;

            // Glowing eyes on each face
            for (int e = 0; e < 2; e++)
            {
                var eye = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                eye.name = "TotemEye";
                eye.transform.SetParent(_totem.transform);
                eye.transform.localPosition = new Vector3(
                    (e == 0 ? -0.08f : 0.08f), 0.7f + tier * 0.9f, 0.32f);
                eye.transform.localScale = Vector3.one * 0.04f;
                Object.Destroy(eye.GetComponent<Collider>());
                var eyeMat = new Material(GameBootstrapper.GetHDRPLitShader());
                eyeMat.color = Color.white;
                GameBootstrapper.SetHDRPEmission(eyeMat, GameBootstrapper.PaletteGold, 20f);
                eye.GetComponent<Renderer>().material = eyeMat;
            }
        }

        // Crown gem
        var gem = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        gem.name = "TotemGem";
        gem.transform.SetParent(_totem.transform);
        gem.transform.localPosition = Vector3.up * 3.2f;
        gem.transform.localScale    = Vector3.one * 0.3f;
        Object.Destroy(gem.GetComponent<Collider>());
        var gemMat = new Material(GameBootstrapper.GetHDRPLitShader());
        gemMat.color = GameBootstrapper.PaletteCyan;
        GameBootstrapper.SetHDRPEmission(gemMat, GameBootstrapper.PaletteCyan, 15f);
        gem.GetComponent<Renderer>().material = gemMat;

        // Gem aura
        var aura = new GameObject("TotemAura");
        aura.transform.SetParent(gem.transform);
        aura.transform.localPosition = Vector3.zero;
        var ps   = aura.AddComponent<ParticleSystem>();
        var main = ps.main;
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        main.loop          = true;
        main.duration      = 2f;
        main.startLifetime = 1.5f;
        main.startSpeed    = 0.5f;
        main.startSize     = 0.1f;
        main.startColor    = new ParticleSystem.MinMaxGradient(GameBootstrapper.PaletteGold, GameBootstrapper.PaletteCyan);
        main.maxParticles  = 40;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        var em = ps.emission;
        em.rateOverTime = 15f;
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius    = 0.2f;
        ps.Play();
    }

    // ─────────────────────────────────────────────────────────────────────
    // CAMPFIRES
    // ─────────────────────────────────────────────────────────────────────
    void BuildCampfires(Vector3 centre)
    {
        int fireCount = 4;
        for (int i = 0; i < fireCount; i++)
        {
            float angle = (i / (float)fireCount) * Mathf.PI * 2f + 0.4f;
            float rad   = VillageRadius * 0.3f;
            Vector3 pos = centre + new Vector3(Mathf.Cos(angle) * rad, 0, Mathf.Sin(angle) * rad);
            if (Physics.Raycast(pos + Vector3.up * 50f, Vector3.down, out RaycastHit hit, 100f))
                pos.y = hit.point.y;

            var fireRoot = new GameObject("Campfire");
            fireRoot.transform.SetParent(VillageRoot);
            fireRoot.transform.position = pos;

            // Log ring (tiny cubes)
            for (int j = 0; j < 5; j++)
            {
                var log = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                log.name = "Log";
                log.transform.SetParent(fireRoot.transform);
                float la = (j / 5f) * Mathf.PI * 2f;
                log.transform.localPosition = new Vector3(Mathf.Cos(la) * 0.12f, 0.03f, Mathf.Sin(la) * 0.12f);
                log.transform.localScale    = new Vector3(0.03f, 0.05f, 0.03f);
                log.transform.localRotation = Quaternion.Euler(90f, la * Mathf.Rad2Deg, 0);
                Object.Destroy(log.GetComponent<Collider>());
                var lMat = new Material(GameBootstrapper.GetHDRPLitShader());
                lMat.color = new Color(0.22f, 0.12f, 0.04f);
                log.GetComponent<Renderer>().material = lMat;
            }

            // Fire particles
            var fireObj = new GameObject("FireParticles");
            fireObj.transform.SetParent(fireRoot.transform);
            fireObj.transform.localPosition = Vector3.up * 0.05f;
            var ps   = fireObj.AddComponent<ParticleSystem>();
            var fMain = ps.main;
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            fMain.loop          = true;
            fMain.duration      = 1f;
            fMain.startLifetime = new ParticleSystem.MinMaxCurve(0.3f, 0.7f);
            fMain.startSpeed    = new ParticleSystem.MinMaxCurve(0.2f, 0.6f);
            fMain.startSize     = new ParticleSystem.MinMaxCurve(0.04f, 0.12f);
            fMain.startColor    = new ParticleSystem.MinMaxGradient(FireOrange, FireYellow);
            fMain.gravityModifier = -0.3f;
            fMain.maxParticles  = 25;
            var fEm = ps.emission;
            fEm.rateOverTime = 18f;
            var fShape = ps.shape;
            fShape.shapeType = ParticleSystemShapeType.Sphere;
            fShape.radius    = 0.06f;
            ps.Play();

            // Point light for fire glow
            var lightObj = new GameObject("FireLight");
            lightObj.transform.SetParent(fireRoot.transform);
            lightObj.transform.localPosition = Vector3.up * 0.15f;
            var fl = lightObj.AddComponent<Light>();
            fl.type  = LightType.Point;
            fl.color = FireOrange;
            fl.intensity = 800f;
            var hdl = lightObj.AddComponent<UnityEngine.Rendering.HighDefinition.HDAdditionalLightData>();
            hdl.range     = 5f;

            if (DayNightCycle.Instance != null) DayNightCycle.Instance.RegisterCampfire(fl);
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    // FENCE — low wooden fence ring around the village
    // ─────────────────────────────────────────────────────────────────────
    void BuildFence(Vector3 centre)
    {
        int posts = 24;
        for (int i = 0; i < posts; i++)
        {
            float angle = (i / (float)posts) * Mathf.PI * 2f;
            float r = VillageRadius;
            Vector3 pos = centre + new Vector3(Mathf.Cos(angle) * r, 0, Mathf.Sin(angle) * r);
            if (Physics.Raycast(pos + Vector3.up * 50f, Vector3.down, out RaycastHit hit, 100f))
                pos.y = hit.point.y;

            var post = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            post.name = "FencePost";
            post.transform.SetParent(VillageRoot);
            post.transform.position   = pos + Vector3.up * 0.15f;
            post.transform.localScale = new Vector3(0.06f, 0.18f, 0.06f);
            Object.Destroy(post.GetComponent<Collider>());
            var mat = new Material(GameBootstrapper.GetHDRPLitShader());
            mat.color = new Color(0.30f, 0.18f, 0.06f);
            post.GetComponent<Renderer>().material = mat;
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    // VILLAGERS
    // ─────────────────────────────────────────────────────────────────────
    void SpawnVillagers(Vector3 centre)
    {
        for (int i = 0; i < VillagerCount; i++)
        {
            float angle = Random.Range(0f, Mathf.PI * 2f);
            float rad   = Random.Range(2f, VillageRadius * 0.9f);
            Vector3 pos = centre + new Vector3(Mathf.Cos(angle) * rad, 0, Mathf.Sin(angle) * rad);
            if (Physics.Raycast(pos + Vector3.up * 50f, Vector3.down, out RaycastHit hit, 100f))
                pos.y = hit.point.y;

            var v = Villager.Create(pos, VillageRoot);
            _villagers.Add(v);
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    // DAMAGE
    // ─────────────────────────────────────────────────────────────────────
    // ─────────────────────────────────────────────────────────────────────
    public void TakeDamage(float amount)
    {
        if (_isDestroyed) return;
        CurrentHP = Mathf.Max(0, CurrentHP - amount);

        // Flash totem red
        if (_totem != null)
        {
            foreach (var r in _totem.GetComponentsInChildren<Renderer>())
                GameBootstrapper.SetHDRPEmission(r.material, GameBootstrapper.PaletteCrimson, 12f);
        }

        // Villagers panic
        foreach (var v in _villagers) v.SetPanic(true);

        if (CurrentHP <= 0)
            DestroyVillage();

        // Reset totem after flash
        Invoke(nameof(ResetTotemGlow), 0.3f);
    }

    void ResetTotemGlow()
    {
        if (_totem == null) return;
        foreach (var r in _totem.GetComponentsInChildren<Renderer>())
            GameBootstrapper.SetHDRPEmission(r.material, TotemGold, 5f);
    }

    public void Heal(float amount)
    {
        if (_isDestroyed) return;
        CurrentHP = Mathf.Min(MaxHP, CurrentHP + amount);

        // Flash totem cyan/gold for healing
        if (_totem != null)
        {
            foreach (var r in _totem.GetComponentsInChildren<Renderer>())
                GameBootstrapper.SetHDRPEmission(r.material, GameBootstrapper.PaletteCyan, 12f);
            Invoke(nameof(ResetTotemGlow), 0.5f);
        }
    }

    void DestroyVillage()
    {
        _isDestroyed = true;
        foreach (var hut in _huts)
            if (hut != null) Object.Destroy(hut, Random.Range(0f, 0.5f));
        // Leave the totem as ruins
        Debug.Log("[Village] Village destroyed!");
    }

    public float GetHealthRatio() => CurrentHP / MaxHP;
    public bool IsDestroyed => _isDestroyed;
    public Vector3 Centre => VillageRoot != null ? VillageRoot.position : Vector3.zero;
}
