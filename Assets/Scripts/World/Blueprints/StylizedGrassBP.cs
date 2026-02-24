using UnityEngine;

/// <summary>
/// StylizedGrassBP – Generates lush "cross-quad" grass patches.
/// </summary>
public class StylizedGrassBP : BaseBlueprint
{
    [Header("Grass Settings")]
    public int BladeCount = 12;
    public float Radius = 2f;
    
    public override void Generate()
    {
        Clear();
        
        var root = new GameObject("Blades").transform;
        root.SetParent(transform, false);
        
        for (int i = 0; i < BladeCount; i++)
        {
            Vector2 randCircle = Random.insideUnitCircle * Radius;
            Vector3 pos = new Vector3(randCircle.x, 0, randCircle.y);
            
            // A "blade" patch is two intersecting quads
            CreateBlade(root, pos, 0f);
            CreateBlade(root, pos, 45f);
        }
    }
    
    void CreateBlade(Transform parent, Vector3 pos, float rotationOffset)
    {
        var blade = GameObject.CreatePrimitive(PrimitiveType.Quad);
        blade.transform.SetParent(parent, false);
        blade.transform.localPosition = pos + Vector3.up * 0.5f;
        blade.transform.localScale = new Vector3(0.8f, 1.2f, 1f);
        blade.transform.rotation = Quaternion.Euler(0, rotationOffset + Random.Range(0, 180f), 0);
        
        var mat = GetStylizedMaterial(new Color(0.2f, 0.7f, 0.1f), 0.5f); // Vibrant glow green
        blade.GetComponent<Renderer>().material = mat;
        
        DestroyImmediate(blade.GetComponent<Collider>());
    }
}
