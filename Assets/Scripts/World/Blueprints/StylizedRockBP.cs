using UnityEngine;

/// <summary>
/// StylizedRockBP – Generates "blocky" basalt-style rock formations.
/// </summary>
public class StylizedRockBP : BaseBlueprint
{
    [Header("Rock Settings")]
    public int BlockCount = 3;
    public float Scale = 2f;
    
    public override void Generate()
    {
        Clear();
        
        var root = new GameObject("Blocks").transform;
        root.SetParent(transform, false);
        
        for (int i = 0; i < BlockCount; i++)
        {
            var block = GameObject.CreatePrimitive(PrimitiveType.Cube);
            block.transform.SetParent(root, false);
            
            // Random offset but kept in a cluster
            block.transform.localPosition = new Vector3(
                Random.Range(-0.5f, 0.5f),
                Random.Range(0f, 0.5f),
                Random.Range(-0.5f, 0.5f)
            ) * Scale;
            
            // Random blocky scales
            block.transform.localScale = new Vector3(
                Random.Range(0.8f, 1.2f),
                Random.Range(1.5f, 3.0f),
                Random.Range(0.8f, 1.2f)
            ) * Scale;
            
            // Slight tilt
            block.transform.rotation = Quaternion.Euler(
                Random.Range(-10f, 10f),
                Random.Range(0, 360f),
                Random.Range(-10f, 10f)
            );
            
            // Stylized reddish-gray rock
            var mat = GetStylizedMaterial(new Color(0.15f, 0.1f, 0.25f)); // Dark purplish-gray
            block.GetComponent<Renderer>().material = mat;
            
            // If it's the first block, keep collider, else destroy it to optimize
            if (i > 0) DestroyImmediate(block.GetComponent<Collider>());
        }
    }
}
