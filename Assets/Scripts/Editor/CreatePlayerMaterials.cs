#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering.HighDefinition;

/// <summary>
/// Editor utility to create branded HDRP materials for Players
/// Run this once to generate mat_P1_Gold and mat_P2_Cyan in Assets/Art/Materials/
/// </summary>
public class CreatePlayerMaterials
{
    [MenuItem("NinjaStrike/Create Player Materials")]
    public static void CreateMaterials()
    {
        string materialPath = "Assets/Art/Materials/";

        // Ensure directory exists
        if (!System.IO.Directory.Exists(materialPath))
            System.IO.Directory.CreateDirectory(materialPath);

        // Get HDRP Lit shader
        Shader hdrpLit = Shader.Find("HDRP/Lit");
        if (hdrpLit == null)
        {
            Debug.LogError("HDRP/Lit shader not found! Make sure HDRP is installed.");
            return;
        }

        // ─────────────────────────────────────────────────────────────────
        // PLAYER 1 — GOLD (#FFC819)
        // ─────────────────────────────────────────────────────────────────
        Material matP1 = new Material(hdrpLit);
        matP1.name = "mat_P1_Gold";

        // Core gold color
        Color goldColor = new Color(1.00f, 0.78f, 0.10f);  // #FFC819
        matP1.SetColor("_BaseColor", goldColor);

        // HDRP-specific properties for that holographic glow
        matP1.SetFloat("_Metallic", 0.8f);           // High metallic for reflective surface
        matP1.SetFloat("_Smoothness", 0.9f);         // High gloss for sleek look
        matP1.SetColor("_EmissiveColor", goldColor);
        matP1.SetFloat("_EmissiveIntensity", 8f);    // Bright glow

        AssetDatabase.CreateAsset(matP1, materialPath + "mat_P1_Gold.mat");
        Debug.Log($"✓ Created {materialPath}mat_P1_Gold.mat");

        // ─────────────────────────────────────────────────────────────────
        // PLAYER 2 — CYAN (#1AE5FF)
        // ─────────────────────────────────────────────────────────────────
        Material matP2 = new Material(hdrpLit);
        matP2.name = "mat_P2_Cyan";

        // Core cyan color
        Color cyanColor = new Color(0.10f, 0.90f, 1.00f);  // #1AE5FF
        matP2.SetColor("_BaseColor", cyanColor);

        // HDRP-specific properties
        matP2.SetFloat("_Metallic", 0.8f);
        matP2.SetFloat("_Smoothness", 0.9f);
        matP2.SetColor("_EmissiveColor", cyanColor);
        matP2.SetFloat("_EmissiveIntensity", 8f);

        AssetDatabase.CreateAsset(matP2, materialPath + "mat_P2_Cyan.mat");
        Debug.Log($"✓ Created {materialPath}mat_P2_Cyan.mat");

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("✨ Player materials created! Drag these onto your Mannequin mesh.");
    }
}
#endif
