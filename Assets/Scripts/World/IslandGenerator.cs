using UnityEngine;

/// <summary>
/// IslandGenerator — procedurally builds a large island with Perlin noise terrain,
/// ocean, beaches, forests, rocks, and cliffs. Players are god-sized (Gulliver scale).
/// </summary>
public class IslandGenerator : MonoBehaviour
{
    // ── Island config ─────────────────────────────────────────────────────
    public const float IslandRadius     = 150f;    // total radius of the island
    public const float TerrainSize      = 300f;    // terrain plane size
    public const float MaxHeight        = 18f;     // peak height
    public const float WaterLevel       = 0.3f;    // y-level of ocean surface
    public const int   TerrainRes       = 128;     // mesh resolution

    // ── Palette ───────────────────────────────────────────────────────────
    private static readonly Color GrassGreen   = new Color(0.15f, 0.38f, 0.12f);
    private static readonly Color GrassDark    = new Color(0.08f, 0.22f, 0.06f);
    private static readonly Color SandColor    = new Color(0.76f, 0.68f, 0.45f);
    private static readonly Color RockGray     = new Color(0.30f, 0.28f, 0.26f);
    private static readonly Color OceanDeep    = new Color(0.02f, 0.10f, 0.25f);
    private static readonly Color OceanSurf    = new Color(0.05f, 0.30f, 0.55f);
    private static readonly Color OceanFoam    = new Color(0.70f, 0.85f, 0.90f);

    public Transform IslandRoot { get; private set; }

    // ─────────────────────────────────────────────────────────────────────
    public void Generate()
    {
        IslandRoot = new GameObject("Island").transform;

        BuildTerrain();
        BuildOcean();
        BuildBeachRing();
        PlaceTrees(120);
        PlaceRocks(50);
        PlaceGrassPatches(80);
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
                float falloff = 1f - Mathf.Clamp01(distFromCentre / (IslandRadius * 0.85f));
                falloff = falloff * falloff; // smooth edges

                // Multi-octave Perlin noise
                float nx = (x / (float)res) * 4f + seed;
                float nz = (z / (float)res) * 4f + seed;

                float height = 0f;
                height += Mathf.PerlinNoise(nx * 1.0f, nz * 1.0f) * 1.0f;
                height += Mathf.PerlinNoise(nx * 2.3f, nz * 2.3f) * 0.5f;
                height += Mathf.PerlinNoise(nx * 5.1f, nz * 5.1f) * 0.15f;
                height *= MaxHeight * falloff;

                // Flatten the village area (centre) slightly
                float villageFlatten = Mathf.Clamp01(1f - distFromCentre / 30f);
                height = Mathf.Lerp(height, 1.5f, villageFlatten * 0.6f);

                verts[i] = new Vector3(xPos, height, zPos);
                uvs[i]   = new Vector2(x / (float)res, z / (float)res);

                // Vertex colour for terrain tinting
                if (height < WaterLevel + 0.5f)
                    colors[i] = SandColor;        // beach
                else if (height > MaxHeight * 0.65f)
                    colors[i] = RockGray;         // rocky peaks
                else
                    colors[i] = Color.Lerp(GrassGreen, GrassDark, Mathf.PerlinNoise(nx * 3f, nz * 3f));
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
    // OCEAN
    // ─────────────────────────────────────────────────────────────────────
    void BuildOcean()
    {
        // Main ocean plane
        var ocean = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ocean.name = "Ocean";
        ocean.transform.SetParent(IslandRoot);
        ocean.transform.position   = new Vector3(0, WaterLevel, 0);
        ocean.transform.localScale = new Vector3(80f, 1f, 80f); // 800m × 800m
        Destroy(ocean.GetComponent<Collider>()); // don't collide with players

        var mat = new Material(GameBootstrapper.GetHDRPLitShader());
        mat.color = OceanSurf;
        mat.SetFloat("_Smoothness", 0.95f);
        mat.SetFloat("_Metallic", 0.2f);
        GameBootstrapper.SetHDRPEmission(mat, OceanDeep, 0.5f);
        ocean.GetComponent<Renderer>().material = mat;

        // Animated ocean shimmer (subtle particle layer)
        var shimmer = new GameObject("OceanShimmer");
        shimmer.transform.SetParent(IslandRoot);
        shimmer.transform.position = new Vector3(0, WaterLevel + 0.1f, 0);
        var ps   = shimmer.AddComponent<ParticleSystem>();
        var main = ps.main;
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
    // BEACH RING — sand-coloured ring around the island edge
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
            GameBootstrapper.SetMaterialColor(sand, Color.Lerp(SandColor, GrassGreen, Random.Range(0f, 0.2f)));
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    // TREES
    // ─────────────────────────────────────────────────────────────────────
    void PlaceTrees(int count)
    {
        for (int i = 0; i < count; i++)
        {
            Vector3 pos = GetRandomIslandPosition(25f, IslandRadius * 0.75f);
            if (pos.y < WaterLevel + 0.5f) continue;

            BuildTree(pos);
        }
    }

    void BuildTree(Vector3 pos)
    {
        var tree = new GameObject("Tree");
        tree.transform.SetParent(IslandRoot);
        tree.transform.position = pos;
        float scale = Random.Range(0.8f, 1.4f);

        // Trunk
        var trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        trunk.name = "Trunk";
        trunk.transform.SetParent(tree.transform);
        trunk.transform.localPosition = Vector3.up * 1.5f * scale;
        trunk.transform.localScale    = new Vector3(0.4f * scale, 1.5f * scale, 0.4f * scale);
        Destroy(trunk.GetComponent<Collider>());
        var trunkMat = new Material(GameBootstrapper.GetHDRPLitShader());
        trunkMat.color = new Color(0.25f, 0.15f, 0.05f);
        trunk.GetComponent<Renderer>().material = trunkMat;

        // Canopy (2-3 spheres)
        int canopyLayers = Random.Range(2, 4);
        for (int j = 0; j < canopyLayers; j++)
        {
            var leaf = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            leaf.name = "Canopy";
            leaf.transform.SetParent(tree.transform);
            float leafScale = Random.Range(1.8f, 3.2f) * scale;
            leaf.transform.localPosition = new Vector3(
                Random.Range(-0.5f, 0.5f) * scale,
                (3f + j * 0.8f) * scale,
                Random.Range(-0.5f, 0.5f) * scale);
            leaf.transform.localScale = new Vector3(leafScale, leafScale * 0.7f, leafScale);
            Destroy(leaf.GetComponent<Collider>());
            var leafMat = new Material(GameBootstrapper.GetHDRPLitShader());
            leafMat.color = Color.Lerp(GrassGreen, GrassDark, Random.Range(0f, 1f));
            GameBootstrapper.SetHDRPEmission(leafMat, new Color(0.05f, 0.15f, 0.02f), 0.5f);
            leaf.GetComponent<Renderer>().material = leafMat;
        }

        // Collider on tree trunk for the world
        var col = tree.AddComponent<CapsuleCollider>();
        col.height = 4f * scale;
        col.radius = 0.5f * scale;
        col.center = Vector3.up * 2f * scale;
    }

    // ─────────────────────────────────────────────────────────────────────
    // ROCKS
    // ─────────────────────────────────────────────────────────────────────
    void PlaceRocks(int count)
    {
        for (int i = 0; i < count; i++)
        {
            Vector3 pos = GetRandomIslandPosition(10f, IslandRadius * 0.8f);
            if (pos.y < WaterLevel + 0.3f) continue;

            var rock = GameObject.CreatePrimitive(
                Random.value > 0.5f ? PrimitiveType.Cube : PrimitiveType.Sphere);
            rock.name = "Rock";
            rock.transform.SetParent(IslandRoot);
            rock.transform.position   = pos;
            float s = Random.Range(0.8f, 3.5f);
            rock.transform.localScale = new Vector3(
                s * Random.Range(0.7f, 1.3f),
                s * Random.Range(0.5f, 1.0f),
                s * Random.Range(0.7f, 1.3f));
            rock.transform.rotation = Quaternion.Euler(
                Random.Range(-20f, 20f), Random.Range(0, 360f), Random.Range(-10f, 10f));

            var mat = new Material(GameBootstrapper.GetHDRPLitShader());
            mat.color = Color.Lerp(RockGray, new Color(0.45f, 0.42f, 0.38f), Random.Range(0f, 0.5f));
            rock.GetComponent<Renderer>().material = mat;
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    // GRASS PATCHES
    // ─────────────────────────────────────────────────────────────────────
    void PlaceGrassPatches(int count)
    {
        for (int i = 0; i < count; i++)
        {
            Vector3 pos = GetRandomIslandPosition(5f, IslandRadius * 0.7f);
            if (pos.y < WaterLevel + 0.6f) continue;

            var patch = GameObject.CreatePrimitive(PrimitiveType.Cube);
            patch.name = "GrassPatch";
            patch.transform.SetParent(IslandRoot);
            patch.transform.position   = pos + Vector3.up * 0.15f;
            patch.transform.localScale = new Vector3(
                Random.Range(1f, 4f), 0.3f, Random.Range(1f, 4f));
            patch.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360f), 0);
            Destroy(patch.GetComponent<Collider>());

            var mat = new Material(GameBootstrapper.GetHDRPLitShader());
            mat.color = Color.Lerp(GrassGreen, new Color(0.35f, 0.55f, 0.15f), Random.Range(0f, 0.7f));
            patch.GetComponent<Renderer>().material = mat;
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    // SKYBOX / ATMOSPHERE
    // ─────────────────────────────────────────────────────────────────────
    void BuildSkybox()
    {
        // Distant mountains (silhouettes around the ocean edge)
        int mountainCount = 12;
        for (int i = 0; i < mountainCount; i++)
        {
            float angle = (i / (float)mountainCount) * Mathf.PI * 2f;
            float dist  = IslandRadius * 2.8f + Random.Range(-20f, 20f);
            Vector3 pos = new Vector3(Mathf.Cos(angle) * dist, -5f, Mathf.Sin(angle) * dist);

            var mountain = GameObject.CreatePrimitive(PrimitiveType.Cube);
            mountain.name = "DistantMountain";
            mountain.transform.SetParent(IslandRoot);
            mountain.transform.position   = pos;
            float w = Random.Range(30f, 80f);
            float h = Random.Range(20f, 60f);
            mountain.transform.localScale = new Vector3(w, h, w * Random.Range(0.3f, 0.7f));
            mountain.transform.rotation   = Quaternion.Euler(0, angle * Mathf.Rad2Deg + 90f, 0);
            Destroy(mountain.GetComponent<Collider>());

            var mat = new Material(GameBootstrapper.GetHDRPLitShader());
            mat.color = new Color(0.08f, 0.12f, 0.22f);
            GameBootstrapper.SetHDRPEmission(mat, new Color(0.03f, 0.05f, 0.10f), 0.3f);
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
