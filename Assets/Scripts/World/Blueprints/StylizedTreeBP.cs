using UnityEngine;

/// <summary>
/// StylizedTreeBP – Generates a "Ghibli-style" bent tree with tiered foliage.
/// </summary>
public class StylizedTreeBP : BaseBlueprint
{
    [Header("Tree Stats")]
    public float Height = 6f;
    public float Curvature = 1.5f;
    public int CanopyTiers = 3;
    
    public override void Generate()
    {
        Clear();
        
        // --- Trunk (Bent) ---
        var trunkRoot = new GameObject("Trunk").transform;
        trunkRoot.SetParent(transform, false);
        
        int segments = 6;
        Vector3 lastPos = Vector3.zero;
        for (int i = 0; i < segments; i++)
        {
            float t = i / (float)segments;
            float sectionHeight = (Height / segments);
            Vector3 pos = new Vector3(Mathf.Sin(t * Curvature), t * Height, 0);
            
            var part = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            part.transform.SetParent(trunkRoot, false);
            part.transform.localPosition = Vector3.Lerp(lastPos, pos, 0.5f);
            part.transform.localScale = new Vector3(0.4f * (1f - t*0.5f), sectionHeight * 0.55f, 0.4f * (1f - t*0.5f));
            part.transform.up = (pos - lastPos).normalized == Vector3.zero ? Vector3.up : (pos - lastPos).normalized;
            
            var mat = GetStylizedMaterial(new Color(0.25f, 0.15f, 0.10f)); // warm brown
            part.GetComponent<Renderer>().material = mat;
            DestroyImmediate(part.GetComponent<Collider>());
            
            lastPos = pos;
        }

        // --- Canopy (Tiered) ---
        var canopyRoot = new GameObject("Canopy").transform;
        canopyRoot.SetParent(transform, false);
        
        for (int i = 0; i < CanopyTiers; i++)
        {
            float t = (i + 1) / (float)CanopyTiers;
            var tier = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            tier.transform.SetParent(canopyRoot, false);
            tier.transform.localPosition = lastPos + Vector3.up * (i * 1.2f - 1f);
            float scale = 3.5f * (1.2f - t * 0.5f);
            tier.transform.localScale = new Vector3(scale, scale * 0.7f, scale);
            
            // Vibrant green with slight glow
            var leafColor = Color.Lerp(new Color(0.1f, 0.5f, 0.1f), new Color(0.3f, 0.8f, 0.2f), i / (float)CanopyTiers);
            var mat = GetStylizedMaterial(leafColor, 0.3f);
            tier.GetComponent<Renderer>().material = mat;
            DestroyImmediate(tier.GetComponent<Collider>());
        }
        
        // Final collider
        var col = gameObject.GetComponent<CapsuleCollider>();
        if (col == null) col = gameObject.AddComponent<CapsuleCollider>();
        col.height = Height + 2f;
        col.radius = 1f;
        col.center = new Vector3(0, Height * 0.5f, 0);
    }
}
