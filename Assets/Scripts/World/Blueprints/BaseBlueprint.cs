using UnityEngine;

/// <summary>
/// BaseBlueprint – A base class for procedural objects that build themselves.
/// Similar to UE5 Blueprints, these are self-contained logic-driven assets.
/// </summary>
public abstract class BaseBlueprint : MonoBehaviour
{
    [Header("Generation Settings")]
    public uint Seed = 0;
    public bool AutoGenerateOnStart = true;

    protected virtual void Start()
    {
        if (AutoGenerateOnStart)
        {
            Generate();
        }
    }

    [ContextMenu("Generate")]
    public abstract void Generate();

    [ContextMenu("Clear")]
    public virtual void Clear()
    {
        // Clear all children
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            if (Application.isPlaying)
                Destroy(transform.GetChild(i).gameObject);
            else
                DestroyImmediate(transform.GetChild(i).gameObject);
        }
    }

    protected Material GetStylizedMaterial(Color color, float emission = 0f)
    {
        Material mat = new Material(GameBootstrapper.GetHDRPLitShader());
        mat.color = color;
        mat.SetFloat("_Smoothness", 0.1f);
        mat.SetFloat("_Metallic", 0.0f);
        
        if (emission > 0)
        {
            GameBootstrapper.SetHDRPEmission(mat, color, emission);
        }
        
        return mat;
    }
}
