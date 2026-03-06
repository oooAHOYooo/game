using UnityEngine;

/// <summary>
/// IslandGenerator — procedurally builds a tropical ancient island with Perlin noise terrain,
/// turquoise ocean, white sand beaches, lush green forests, mossy rocks, ancient ruins, and cliffs.
/// </summary>
public class IslandGenerator : MonoBehaviour
{
    // ── Island config ─────────────────────────────────────────────────────
    public const float IslandRadius     = 150f;    // total radius of the island
    public const float TerrainSize      = 300f;    // terrain plane size
    public const float MaxHeight        = 18f;     // peak height
    public const float WaterLevel       = 0.3f;    // y-level of ocean surface
    public const int   TerrainRes       = 128;     // mesh resolution

    // ── Tropical Palette ──────────────────────────────────────────────────
    private static readonly Color GrassGreen   = new Color(0.08f, 0.55f, 0.15f);   // vibrant emerald
    private static readonly Color GrassDark    = new Color(0.04f, 0.38f, 0.12f);   // deep jungle green
    private static readonly Color SandColor    = new Color(0.96f, 0.90f, 0.72f);   // warm white sand
    private static readonly Color RockGray     = new Color(0.45f, 0.42f, 0.38f);   // weathered stone
    private static readonly Color OceanDeep    = new Color(0.02f, 0.18f, 0.42f);   // deep tropical blue
    private static readonly Color OceanSurf    = new Color(0.05f, 0.70f, 0.78f);   // turquoise surf
    private static readonly Color OceanFoam    = new Color(0.75f, 0.98f, 1.0f);    // bright seafoam
    private static readonly Color MossColor    = new Color(0.15f, 0.50f, 0.10f);   // mossy green
    private static readonly Color RuinStone    = new Color(0.52f, 0.48f, 0.40f);   // ancient sandstone

    public Transform IslandRoot { get; private set; }

    // ─────────────────────────────────────────────────────────────────────
    public void Generate()
    {
        IslandRoot = new GameObject("Island").transform;

        BuildTerrain();
        BuildOcean();
        BuildBeachRing();
        PlaceTrees(GameSettings.TreeCount + 30);          // more lush vegetation
        PlaceRocks(GameSettings.RockCount + 15);
        PlaceBamboo(80);                                  // Bamboo Grove biome
        PlaceGrassPatches(120);                           // denser grass
        PlaceAncientRuins(18);                            // the new ancient ruins
        PlaceHazards(15);                                 // Biome-specific hazards
        PlacePowerUps(5);                                 // Exploration rewards
        BuildSkybox();
    }

    // ─────────────────────────────────────────────────────────────────────
    // TERRAIN — Perlin-noise heightmapped mesh
    // ─────────────────────────────────────────────────────────────────────
    void BuildTerrain()
    {
        var terrainObj = new GameObject("Terrain");
        terrainObj.transform.SetParent(IslandRoot);

        var mf   = terrainObj.AddComponent<MeshFilter>();
        var mr   = terrainObj.AddComponent<MeshRenderer>();
        var mc   = terrainObj.AddComponent<MeshCollider>();

        Mesh mesh = GenerateTerrainMesh();
        mf.sharedMesh  = mesh;
        mc.sharedMesh  = mesh;

        // Multi-toned terrain material
        var mat = new Material(GameBootstrapper.GetHDRPLitShader());
        mat.color = GrassGreen;
        mr.material = mat;
    }

    Mesh GenerateTerrainMesh()
    {
        int res = TerrainRes;
        Vector3[] verts   = new Vector3[(res + 1) * (res + 1)];
        Vector2[] uvs     = new Vector2[verts.Length];
        Color[]   colors  = new Color[verts.Length];
        int[]     tris    = new int[res * res * 6];

        float halfSize = TerrainSize * 0.5f;
        float seed     = Random.Range(0f, 1000f);

        for (int z = 0; z <= res; z++)
        {
            for (int x = 0; x <= res; x++)
            {
                int i = z * (res + 1) + x;

                float xPos = (x / (float)res) * TerrainSize - halfSize;
                float zPos = (z / (float)res) * TerrainSize - halfSize;

                // Island falloff — circular distance from centre
                float distFromCentre = Mathf.Sqrt(xPos * xPos + zPos * zPos);
                float falloff = 1f - Mathf.Clamp01(distFromCentre / (GameSettings.IslandRadius * 0.85f));
                falloff = falloff * falloff; // smooth edges

                // Biome Angle (0 to 360)
                float angle = Mathf.Atan2(zPos, xPos) * Mathf.Rad2Deg;
                if (angle < 0) angle += 360f;

                // Multi-octave Perlin noise
                float nx = (x / (float)res) * 4f + seed;
                float nz = (z / (float)res) * 4f + seed;

                float height = 0f;
                height += Mathf.PerlinNoise(nx * 1.0f, nz * 1.0f) * 1.0f;
                height += Mathf.PerlinNoise(nx * 2.3f, nz * 2.3f) * 0.5f;
                height += Mathf.PerlinNoise(nx * 5.1f, nz * 5.1f) * 0.15f;
                
                // Biome modifiers
                float biomeHeightMult = 1f;
                Color biomeColor = GrassGreen;
                
                if (angle >= 0 && angle < 90) 
                {
                    // Volcanic Ridge: Higher, rockier, rougher
                    biomeHeightMult = 1.6f;
                    biomeColor = Color.Lerp(new Color(0.2f, 0.1f, 0.1f), RockGray, Mathf.PerlinNoise(nx * 2f, nz * 2f)); 
                }
                else if (angle >= 90 && angle < 180)
                {
                    // Bamboo Grove: Flatter, very green
                    biomeHeightMult = 0.8f;
                    biomeColor = Color.Lerp(GrassGreen, GrassDark, Mathf.PerlinNoise(nx * 3f, nz * 3f));
                }
                else if (angle >= 180 && angle < 270)
                {
                    // Waterfall Cliff: Steep drops
                    biomeHeightMult = 1.3f;
                    biomeColor = Color.Lerp(GameBootstrapper.PaletteCyan * 0.4f, RockGray, Mathf.PerlinNoise(nx * 2f, nz * 2f));
                }
                else
                {
                    // Beach: Low, sandy
                    biomeHeightMult = 0.4f;
                    biomeColor = Color.Lerp(SandColor, GrassGreen, Mathf.PerlinNoise(nx * 1f, nz * 1f));
                }

                height *= GameSettings.TerrainMaxHeight * falloff * biomeHeightMult;

                // Flatten the village area (centre) slightly
                float villageFlatten = Mathf.Clamp01(1f - distFromCentre / 35f);
                height = Mathf.Lerp(height, 1.5f, villageFlatten * 0.8f);

                verts[i] = new Vector3(xPos, height, zPos);
                uvs[i]   = new Vector2(x / (float)res, z / (float)res);

                // Vertex colour for tropical terrain tinting
                if (height < WaterLevel + 0.5f)
                    colors[i] = SandColor;        // white beach sand
                else if (villageFlatten > 0.5f)
                    colors[i] = GrassGreen; // Village is always grassy
                else
                    colors[i] = biomeColor;
            }
        }

        // Triangles
        int tri = 0;
        for (int z = 0; z < res; z++)
        {
            for (int x = 0; x < res; x++)
            {
                int a = z * (res + 1) + x;
                int b = a + 1;
                int c = a + (res + 1);
                int d = c + 1;

                tris[tri++] = a; tris[tri++] = c; tris[tri++] = b;
                tris[tri++] = b; tris[tri++] = c; tris[tri++] = d;
            }
        }

        var mesh = new Mesh();
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.vertices  = verts;
        mesh.uv        = uvs;
        mesh.colors    = colors;
        mesh.triangles = tris;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    // ─────────────────────────────────────────────────────────────────────
    // OCEAN — turquoise tropical water
    // ─────────────────────────────────────────────────────────────────────
    void BuildOcean()
    {
        // Main ocean plane
        var ocean = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ocean.name = "Ocean";
        ocean.transform.SetParent(IslandRoot);
        ocean.transform.position   = new Vector3(0, WaterLevel, 0);
        ocean.transform.localScale = new Vector3(80f, 1f, 80f);
        Destroy(ocean.GetComponent<Collider>());

        var mat = new Material(GameBootstrapper.GetHDRPLitShader());
        mat.color = OceanSurf;
        mat.SetFloat("_Smoothness", 0.95f);
        mat.SetFloat("_Metallic", 0.2f);
        GameBootstrapper.SetHDRPEmission(mat, OceanDeep, 0.5f);
        ocean.GetComponent<Renderer>().material = mat;

        // Animated ocean shimmer
        var shimmer = new GameObject("OceanShimmer");
        shimmer.transform.SetParent(IslandRoot);
        shimmer.transform.position = new Vector3(0, WaterLevel + 0.1f, 0);
        var ps   = shimmer.AddComponent<ParticleSystem>();
        var main = ps.main;
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        main.loop            = true;
        main.duration        = 5f;
        main.startLifetime   = 3f;
        main.startSpeed      = 0.3f;
        main.startSize       = new ParticleSystem.MinMaxCurve(0.3f, 1.2f);
        main.startColor      = new ParticleSystem.MinMaxGradient(OceanFoam, new Color(1f, 1f, 1f, 0.2f));
        main.maxParticles    = 200;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var em = ps.emission;
        em.rateOverTime = 40f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale     = new Vector3(400f, 0.1f, 400f);

        ps.Play();
    }

    // ─────────────────────────────────────────────────────────────────────
    // BEACH RING — warm sand ring around the island edge
    // ─────────────────────────────────────────────────────────────────────
    void BuildBeachRing()
    {
        int segments = 36;
        for (int i = 0; i < segments; i++)
        {
            float angle = (i / (float)segments) * Mathf.PI * 2f;
            float r = IslandRadius * 0.82f + Random.Range(-8f, 8f);
            Vector3 pos = new Vector3(Mathf.Cos(angle) * r, WaterLevel + 0.15f, Mathf.Sin(angle) * r);

            var sand = GameObject.CreatePrimitive(PrimitiveType.Cube);
            sand.name = "BeachSegment";
            sand.transform.SetParent(IslandRoot);
            sand.transform.position   = pos;
            sand.transform.localScale = new Vector3(
                Random.Range(8f, 16f), Random.Range(0.2f, 0.5f), Random.Range(8f, 16f));
            sand.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360f), 0);
            Destroy(sand.GetComponent<Collider>());
            GameBootstrapper.SetMaterialColor(sand, Color.Lerp(SandColor, new Color(0.92f, 0.85f, 0.65f), Random.Range(0f, 0.3f)));
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    // TREES — tropical palms and lush canopy
    // ─────────────────────────────────────────────────────────────────────
    void PlaceTrees(int count)
    {
        for (int i = 0; i < count; i++)
        {
            Vector3 pos = GetRandomIslandPosition(25f, IslandRadius * 0.75f);
            if (pos.y < WaterLevel + 0.5f) continue;

            float angle = Mathf.Atan2(pos.z, pos.x) * Mathf.Rad2Deg;
            if (angle < 0) angle += 360f;
            
            // Bias away from Volcanic and Bamboo (bamboo has its own generator)
            if (angle >= 0 && angle <= 90 && Random.value > 0.3f) continue; 
            if (angle >= 90 && angle <= 180 && Random.value > 0.2f) continue;

            BuildTree(pos);
        }
    }

    void BuildTree(Vector3 pos)
    {
        var treeObj = new GameObject("StylizedTree");
        treeObj.transform.SetParent(IslandRoot);
        treeObj.transform.position = pos;
        
        var bp = treeObj.AddComponent<StylizedTreeBP>();
        bp.Height = Random.Range(5f, 9f);
        bp.Curvature = Random.Range(1f, 2.5f);
        bp.Generate();
        
        treeObj.AddComponent<SnapToTerrain>();
    }

    // ─────────────────────────────────────────────────────────────────────
    // ROCKS — mossy boulders
    // ─────────────────────────────────────────────────────────────────────
    void PlaceRocks(int count)
    {
        for (int i = 0; i < count; i++)
        {
            Vector3 pos = GetRandomIslandPosition(10f, IslandRadius * 0.8f);
            if (pos.y < WaterLevel + 0.3f) continue;
            
            float angle = Mathf.Atan2(pos.z, pos.x) * Mathf.Rad2Deg;
            if (angle < 0) angle += 360f;
            
            // Bias away from Beach and Bamboo
            if (angle >= 90 && angle <= 180 && Random.value > 0.3f) continue; // Less rocks in bamboo
            if (angle >= 270 && Random.value > 0.4f) continue; // Less rocks on beach

            var rockObj = new GameObject("StylizedRock");
            rockObj.transform.SetParent(IslandRoot);
            rockObj.transform.position = pos;
            
            var bp = rockObj.AddComponent<StylizedRockBP>();
            bp.BlockCount = Random.Range(2, 5);
            bp.Scale = Random.Range(1.5f, 4f);
            bp.Generate();
            
            rockObj.AddComponent<SnapToTerrain>();
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    // BAMBOO — densely packed in the Bamboo Grove biome
    // ─────────────────────────────────────────────────────────────────────
    void PlaceBamboo(int count)
    {
        for (int i = 0; i < count; i++)
        {
            Vector3 pos = GetRandomIslandPosition(20f, IslandRadius * 0.8f);
            if (pos.y < WaterLevel + 0.5f) continue;

            float angle = Mathf.Atan2(pos.z, pos.x) * Mathf.Rad2Deg;
            if (angle < 0) angle += 360f;

            // Only spawn bamboo in the Bamboo Grove (90 to 180 degrees)
            // or very rarely outside it
            if ((angle < 80 || angle > 190) && Random.value > 0.1f) continue;

            BuildBamboo(pos);
        }
    }

    void BuildBamboo(Vector3 pos)
    {
        var bambooObj = new GameObject("StylizedBamboo");
        int envLayer = LayerMask.NameToLayer("Environment");
        if (envLayer != -1) bambooObj.layer = envLayer;
        bambooObj.transform.SetParent(IslandRoot);
        bambooObj.transform.position = pos;
        
        var bp = bambooObj.AddComponent<StylizedBambooBP>();
        bp.Height = Random.Range(6f, 12f);
        bp.Segments = Random.Range(5, 8);
        bp.Generate();
        
        // Add physics so Berserker can break it
        var col = bambooObj.AddComponent<CapsuleCollider>();
        col.radius = bp.Radius;
        col.height = bp.Height;
        col.center = new Vector3(0, bp.Height * 0.5f, 0);

        var ph = bambooObj.AddComponent<PropHealth>();
        ph.MaxHP = 20f;

        bambooObj.AddComponent<SnapToTerrain>();
    }

    // ─────────────────────────────────────────────────────────────────────
    // GRASS PATCHES — denser tropical groundcover
    // ─────────────────────────────────────────────────────────────────────
    void PlaceGrassPatches(int count)
    {
        for (int i = 0; i < count; i++)
        {
            Vector3 pos = GetRandomIslandPosition(5f, IslandRadius * 0.7f);
            if (pos.y < WaterLevel + 0.6f) continue;

            var grassObj = new GameObject("StylizedGrass");
            grassObj.transform.SetParent(IslandRoot);
            grassObj.transform.position = pos;
            
            var bp = grassObj.AddComponent<StylizedGrassBP>();
            bp.BladeCount = Random.Range(8, 16);
            bp.Radius = Random.Range(1f, 3f);
            bp.Generate();
            
            grassObj.AddComponent<SnapToTerrain>();
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    // ANCIENT RUINS — mossy stone pillars, broken walls, and altars
    // ─────────────────────────────────────────────────────────────────────
    void PlaceAncientRuins(int count)
    {
        for (int i = 0; i < count; i++)
        {
            Vector3 pos = GetRandomIslandPosition(30f, IslandRadius * 0.7f);
            if (pos.y < WaterLevel + 1f) continue;

            float ruinType = Random.value;

            if (ruinType < 0.4f)
                BuildRuinPillar(pos);
            else if (ruinType < 0.7f)
                BuildRuinWall(pos);
            else
                BuildRuinAltar(pos);
        }
    }

    void BuildRuinPillar(Vector3 pos)
    {
        var pillar = new GameObject("AncientPillar");
        pillar.transform.SetParent(IslandRoot);
        pillar.transform.position = pos;

        float height = Random.Range(2f, 5f);
        float radius = Random.Range(0.3f, 0.6f);

        // Main column
        var col = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        col.name = "PillarColumn";
        col.transform.SetParent(pillar.transform);
        col.transform.localPosition = Vector3.up * height * 0.5f;
        col.transform.localScale = new Vector3(radius, height * 0.5f, radius);
        // Slightly tilted for "ancient ruin" feel
        col.transform.localRotation = Quaternion.Euler(Random.Range(-8f, 8f), Random.Range(0f, 360f), Random.Range(-8f, 8f));

        var colMat = new Material(GameBootstrapper.GetHDRPLitShader());
        colMat.color = Color.Lerp(RuinStone, MossColor, Random.Range(0.1f, 0.4f));
        col.GetComponent<Renderer>().material = colMat;

        // Top capital (flat disc)
        var cap = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        cap.name = "PillarCap";
        cap.transform.SetParent(pillar.transform);
        cap.transform.localPosition = Vector3.up * (height + 0.1f);
        cap.transform.localScale = new Vector3(radius * 1.5f, 0.1f, radius * 1.5f);
        Destroy(cap.GetComponent<Collider>());
        cap.GetComponent<Renderer>().material = colMat;

        // Moss particles
        SpawnMossVFX(pillar.transform, height * 0.5f);

        pillar.AddComponent<SnapToTerrain>();
    }

    void BuildRuinWall(Vector3 pos)
    {
        var wallRoot = new GameObject("AncientWall");
        wallRoot.transform.SetParent(IslandRoot);
        wallRoot.transform.position = pos;
        wallRoot.transform.rotation = Quaternion.Euler(0, Random.Range(0f, 360f), 0);

        float wallLen = Random.Range(3f, 7f);
        float wallH   = Random.Range(1.5f, 3.5f);

        // Main wall slab
        var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name = "WallBlock";
        wall.transform.SetParent(wallRoot.transform);
        wall.transform.localPosition = Vector3.up * wallH * 0.5f;
        wall.transform.localScale = new Vector3(wallLen, wallH, 0.5f);

        var wallMat = new Material(GameBootstrapper.GetHDRPLitShader());
        wallMat.color = Color.Lerp(RuinStone, MossColor, Random.Range(0.05f, 0.35f));
        wall.GetComponent<Renderer>().material = wallMat;

        // Broken chunks on top (jagged edge)
        int chunks = Random.Range(2, 5);
        for (int i = 0; i < chunks; i++)
        {
            var chunk = GameObject.CreatePrimitive(PrimitiveType.Cube);
            chunk.name = "WallChunk";
            chunk.transform.SetParent(wallRoot.transform);
            float cx = Random.Range(-wallLen * 0.4f, wallLen * 0.4f);
            float ch = Random.Range(0.3f, 1.0f);
            chunk.transform.localPosition = new Vector3(cx, wallH + ch * 0.5f, 0);
            chunk.transform.localScale = new Vector3(Random.Range(0.3f, 1f), ch, 0.5f);
            chunk.transform.localRotation = Quaternion.Euler(Random.Range(-15f, 15f), 0, Random.Range(-10f, 10f));
            Destroy(chunk.GetComponent<Collider>());
            chunk.GetComponent<Renderer>().material = wallMat;
        }

        SpawnMossVFX(wallRoot.transform, wallH);

        wallRoot.AddComponent<SnapToTerrain>();
    }

    void BuildRuinAltar(Vector3 pos)
    {
        var altarRoot = new GameObject("AncientAltar");
        altarRoot.transform.SetParent(IslandRoot);
        altarRoot.transform.position = pos;
        altarRoot.transform.rotation = Quaternion.Euler(0, Random.Range(0f, 360f), 0);

        var altarMat = new Material(GameBootstrapper.GetHDRPLitShader());
        altarMat.color = Color.Lerp(RuinStone, MossColor, Random.Range(0.15f, 0.45f));

        // Platform base (3 steps)
        for (int step = 0; step < 3; step++)
        {
            float sz = 2.5f - step * 0.6f;
            var slab = GameObject.CreatePrimitive(PrimitiveType.Cube);
            slab.name = "AltarStep";
            slab.transform.SetParent(altarRoot.transform);
            slab.transform.localPosition = Vector3.up * (step * 0.3f + 0.15f);
            slab.transform.localScale = new Vector3(sz, 0.3f, sz);
            slab.GetComponent<Renderer>().material = altarMat;
            if (step > 0) Destroy(slab.GetComponent<Collider>());
        }

        // Central relic sphere (glowing)
        var relic = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        relic.name = "AltarRelic";
        relic.transform.SetParent(altarRoot.transform);
        relic.transform.localPosition = Vector3.up * 1.2f;
        relic.transform.localScale = Vector3.one * 0.3f;
        Destroy(relic.GetComponent<Collider>());

        var relicMat = new Material(GameBootstrapper.GetHDRPLitShader());
        relicMat.color = GameBootstrapper.PaletteCyan;
        GameBootstrapper.SetHDRPEmission(relicMat, GameBootstrapper.PaletteCyan, 10f);
        relic.GetComponent<Renderer>().material = relicMat;

        // Orbiting particle aura
        var aura = new GameObject("AltarAura");
        aura.transform.SetParent(relic.transform);
        aura.transform.localPosition = Vector3.zero;
        var ps   = aura.AddComponent<ParticleSystem>();
        var main = ps.main;
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        main.loop          = true;
        main.duration      = 3f;
        main.startLifetime = 2f;
        main.startSpeed    = 0.3f;
        main.startSize     = 0.08f;
        main.startColor    = new ParticleSystem.MinMaxGradient(GameBootstrapper.PaletteGold, GameBootstrapper.PaletteCyan);
        main.maxParticles  = 25;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        var em = ps.emission;
        em.rateOverTime = 10f;
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius    = 0.3f;
        ps.Play();

        altarRoot.AddComponent<SnapToTerrain>();
    }

    void SpawnMossVFX(Transform parent, float height)
    {
        var moss = new GameObject("MossVFX");
        moss.transform.SetParent(parent);
        moss.transform.localPosition = Vector3.up * height * 0.3f;
        var ps   = moss.AddComponent<ParticleSystem>();
        var main = ps.main;
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        main.loop          = true;
        main.duration      = 5f;
        main.startLifetime = 4f;
        main.startSpeed    = 0.05f;
        main.startSize     = new ParticleSystem.MinMaxCurve(0.05f, 0.15f);
        main.startColor    = new ParticleSystem.MinMaxGradient(MossColor, new Color(0.3f, 0.6f, 0.2f, 0.5f));
        main.maxParticles  = 15;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        var em = ps.emission;
        em.rateOverTime = 3f;
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(1f, height * 0.5f, 1f);
        ps.Play();
    }

    // ─────────────────────────────────────────────────────────────────────
    // HAZARDS & POWER-UPS
    // ─────────────────────────────────────────────────────────────────────
    void PlaceHazards(int count)
    {
        for (int i = 0; i < count; i++)
        {
            Vector3 pos = GetRandomIslandPosition(15f, IslandRadius * 0.85f);
            float angle = Mathf.Atan2(pos.z, pos.x) * Mathf.Rad2Deg;
            if (angle < 0) angle += 360f;

            if (angle >= 0 && angle < 90 && pos.y > WaterLevel + 2f)
            {
                // Volcanic Ridge -> Lava Puddle or Falling Rocks
                if (Random.value > 0.5f) BuildLavaPuddle(pos);
                else BuildFallingRockSpawner(pos);
            }
            else if (angle >= 180 && angle < 270)
            {
                // Waterfall Cliff -> Falling Rocks or Water Currents
                if (pos.y > WaterLevel + 5f) BuildFallingRockSpawner(pos);
                else if (pos.y < WaterLevel + 1f) BuildWaterCurrent(pos, angle);
            }
            else if (angle >= 270 && angle <= 360 && pos.y <= WaterLevel + 0.5f)
            {
                // Beach -> Water Currents (Undertow)
                BuildWaterCurrent(pos, angle);
            }
        }
    }

    void BuildLavaPuddle(Vector3 pos)
    {
        var puddle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        puddle.name = "Hazard_Lava";
        puddle.transform.SetParent(IslandRoot);
        // Flatten into a puddle
        puddle.transform.position = pos + Vector3.up * 0.1f;
        float radius = Random.Range(3f, 6f);
        puddle.transform.localScale = new Vector3(radius, 0.05f, radius);

        var mat = new Material(GameBootstrapper.GetHDRPLitShader());
        mat.color = new Color(0.8f, 0.2f, 0f);
        GameBootstrapper.SetHDRPEmission(mat, new Color(1f, 0.3f, 0f), 3f);
        puddle.GetComponent<Renderer>().material = mat;

        var col = puddle.GetComponent<Collider>();
        col.isTrigger = true;
        puddle.AddComponent<HazardLava>();
    }

    void BuildFallingRockSpawner(Vector3 pos)
    {
        var spawner = new GameObject("Hazard_FallingRocks");
        spawner.transform.SetParent(IslandRoot);
        spawner.transform.position = pos;
        var hazard = spawner.AddComponent<HazardFallingRock>();
        hazard.SpawnRadius = Random.Range(8f, 15f);
    }

    void BuildWaterCurrent(Vector3 pos, float angle)
    {
        var current = new GameObject("Hazard_WaterCurrent");
        current.transform.SetParent(IslandRoot);
        current.transform.position = pos;
        
        var col = current.AddComponent<BoxCollider>();
        col.isTrigger = true;
        col.size = new Vector3(15f, 5f, 15f);

        var hazard = current.AddComponent<HazardCurrent>();
        // Push outwards away from center
        hazard.ForceDirection = new Vector3(Mathf.Cos(angle * Mathf.Deg2Rad), 0, Mathf.Sin(angle * Mathf.Deg2Rad));
        hazard.ForceMagnitude = 800f; // High force for rigidbodies
    }

    void PlacePowerUps(int count)
    {
        for (int i = 0; i < count; i++)
        {
            Vector3 pos = GetRandomIslandPosition(20f, IslandRadius * 0.8f);
            if (pos.y < WaterLevel + 0.5f) continue;

            var pupObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pupObj.name = "PowerUp";
            pupObj.transform.SetParent(IslandRoot);
            pupObj.transform.position = pos + Vector3.up;
            pupObj.transform.localScale = Vector3.one * 0.6f;
            
            var col = pupObj.GetComponent<BoxCollider>();
            col.isTrigger = true;

            var pup = pupObj.AddComponent<PowerUp>();
            pup.Type = Random.value > 0.5f ? PowerUpType.HealthHeart : PowerUpType.FullKi;
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    // SKYBOX / ATMOSPHERE — tropical distant islands
    // ─────────────────────────────────────────────────────────────────────
    void BuildSkybox()
    {
        int mountainCount = 12;
        for (int i = 0; i < mountainCount; i++)
        {
            float angle = (i / (float)mountainCount) * Mathf.PI * 2f;
            float dist  = IslandRadius * 2.8f + Random.Range(-20f, 20f);
            Vector3 pos = new Vector3(Mathf.Cos(angle) * dist, -5f, Mathf.Sin(angle) * dist);

            var mountain = GameObject.CreatePrimitive(PrimitiveType.Cube);
            mountain.name = "DistantIsland";
            mountain.transform.SetParent(IslandRoot);
            mountain.transform.position   = pos;
            float w = Random.Range(30f, 80f);
            float h = Random.Range(20f, 60f);
            mountain.transform.localScale = new Vector3(w, h, w * Random.Range(0.3f, 0.7f));
            mountain.transform.rotation   = Quaternion.Euler(0, angle * Mathf.Rad2Deg + 90f, 0);
            Destroy(mountain.GetComponent<Collider>());

            // Tropical distant haze — slight green tint instead of cold blue
            var mat = new Material(GameBootstrapper.GetHDRPLitShader());
            mat.color = new Color(0.12f, 0.20f, 0.18f);
            GameBootstrapper.SetHDRPEmission(mat, new Color(0.05f, 0.12f, 0.10f), 0.3f);
            mountain.GetComponent<Renderer>().material = mat;
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    // UTILITY
    // ─────────────────────────────────────────────────────────────────────
    Vector3 GetRandomIslandPosition(float minDist, float maxDist)
    {
        float angle = Random.Range(0f, Mathf.PI * 2f);
        float dist  = Random.Range(minDist, maxDist);
        float x = Mathf.Cos(angle) * dist;
        float z = Mathf.Sin(angle) * dist;

        // Raycast down for terrain height
        float y = 0f;
        if (Physics.Raycast(new Vector3(x, 100f, z), Vector3.down, out RaycastHit hit, 200f))
            y = hit.point.y;

        return new Vector3(x, y, z);
    }

    /// <summary>Gets a position at the ocean edge (for enemy spawning)</summary>
    public static Vector3 GetOceanEdgeSpawnPosition()
    {
        float angle = Random.Range(0f, Mathf.PI * 2f);
        float dist  = IslandRadius + 10f;
        return new Vector3(Mathf.Cos(angle) * dist, WaterLevel + 0.5f, Mathf.Sin(angle) * dist);
    }
}
