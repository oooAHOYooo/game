using UnityEngine;

/// <summary>
/// StylizedBambooBP – Generates tall, segmented green bamboo stalks.
/// Often found in the Bamboo Grove biome. Can be destroyed by players and Berserkers.
/// </summary>
public class StylizedBambooBP : BaseBlueprint
{
    [Header("Bamboo Settings")]
    public float Height = 8f;
    public float Radius = 0.2f;
    public int Segments = 6;
    
    public override void Generate()
    {
        Clear();
        
        var root = new GameObject("BambooStalk").transform;
        root.SetParent(transform, false);
        
        float segmentHeight = Height / Segments;
        float currentHeight = 0f;
        
        var mat = GetStylizedMaterial(new Color(0.2f, 0.7f, 0.2f)); // Vibrant green

        for (int i = 0; i < Segments; i++)
        {
            // The internode
            var internode = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            internode.transform.SetParent(root, false);
            internode.transform.localPosition = Vector3.up * (currentHeight + segmentHeight * 0.5f);
            internode.transform.localScale = new Vector3(Radius, segmentHeight * 0.5f, Radius);
            internode.GetComponent<Renderer>().material = mat;
            if (i > 0) DestroyImmediate(internode.GetComponent<Collider>());
            
            // The node (ring)
            var node = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            node.transform.SetParent(root, false);
            node.transform.localPosition = Vector3.up * (currentHeight + segmentHeight);
            node.transform.localScale = new Vector3(Radius * 1.1f, 0.05f, Radius * 1.1f);
            var nodeMat = GetStylizedMaterial(new Color(0.15f, 0.5f, 0.15f)); // Darker green ring
            node.GetComponent<Renderer>().material = nodeMat;
            DestroyImmediate(node.GetComponent<Collider>());
            
            currentHeight += segmentHeight;
        }

        // Final collider for the whole stalk
        var col = gameObject.GetComponent<CapsuleCollider>();
        if (col == null) col = gameObject.AddComponent<CapsuleCollider>();
        col.height = Height;
        col.radius = Radius;
        col.center = new Vector3(0, Height * 0.5f, 0);

        // Add health so it can be broken
        var rb = gameObject.GetComponent<Rigidbody>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody>();
        rb.isKinematic = true;

        var health = gameObject.AddComponent<PropHealth>();
        health.MaxHP = 30f;
    }
}
