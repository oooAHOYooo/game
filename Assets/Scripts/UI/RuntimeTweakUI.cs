using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// A developer-only frontend to tweak GameSettings at runtime.
/// Press TAB to toggle.
/// </summary>
public class RuntimeTweakUI : MonoBehaviour
{
    private bool _isVisible = false;
    private Vector2 _scrollPos;

    void Update()
    {
        // Toggle visibility with Tab key (New Input System)
        if (Keyboard.current != null && Keyboard.current.tabKey.wasPressedThisFrame)
        {
            _isVisible = !_isVisible;
        }
    }

    void OnGUI()
    {
        if (!_isVisible)
        {
            GUI.Box(new Rect(10, 10, 200, 25), "Press [TAB] for Tweak UI");
            return;
        }

        float width = 350;
        float height = Screen.height - 100;
        
        GUI.Box(new Rect(10, 10, width, height + 40), "🥷 NinjaStrike - Developer Tweak UI");

        _scrollPos = GUI.BeginScrollView(new Rect(20, 40, width - 20, height), _scrollPos, new Rect(0, 0, width - 40, 1100));

        float y = 10;
        float labelWidth = 150;
        float sliderWidth = 140;

        // --- CAMERA ---
        GUI.Label(new Rect(0, y, 200, 25), "<b>[ CAMERA ]</b>"); y += 25;
        
        GUI.Label(new Rect(0, y, labelWidth, 20), "X Offset");
        GameSettings.CameraOffset.x = GUI.HorizontalSlider(new Rect(labelWidth, y, sliderWidth, 20), GameSettings.CameraOffset.x, -20f, 20f); y += 25;
        
        GUI.Label(new Rect(0, y, labelWidth, 20), "Y Offset");
        GameSettings.CameraOffset.y = GUI.HorizontalSlider(new Rect(labelWidth, y, sliderWidth, 20), GameSettings.CameraOffset.y, 1f, 30f); y += 25;
        
        GUI.Label(new Rect(0, y, labelWidth, 20), "Z Offset");
        GameSettings.CameraOffset.z = GUI.HorizontalSlider(new Rect(labelWidth, y, sliderWidth, 20), GameSettings.CameraOffset.z, -40f, -5f); y += 25;

        GUI.Label(new Rect(0, y, labelWidth, 20), "Base FOV");
        GameSettings.CameraBaseFOV = GUI.HorizontalSlider(new Rect(labelWidth, y, sliderWidth, 20), GameSettings.CameraBaseFOV, 20f, 100f); y += 25;

        // --- PLAYER ---
        y += 10;
        GUI.Label(new Rect(0, y, 200, 25), "<b>[ PLAYER ]</b>"); y += 25;

        GUI.Label(new Rect(0, y, labelWidth, 20), "Ground Speed");
        GameSettings.PlayerGroundSpeed = GUI.HorizontalSlider(new Rect(labelWidth, y, sliderWidth, 20), GameSettings.PlayerGroundSpeed, 1f, 30f); y += 25;

        GUI.Label(new Rect(0, y, labelWidth, 20), "Air Speed");
        GameSettings.PlayerAirSpeed = GUI.HorizontalSlider(new Rect(labelWidth, y, sliderWidth, 20), GameSettings.PlayerAirSpeed, 1f, 50f); y += 25;

        GUI.Label(new Rect(0, y, labelWidth, 20), "Max HP");
        GameSettings.PlayerMaxHP = GUI.HorizontalSlider(new Rect(labelWidth, y, sliderWidth, 20), GameSettings.PlayerMaxHP, 10f, 500f); y += 25;

        GUI.Label(new Rect(0, y, labelWidth, 20), "Ninja Scale");
        GameSettings.NinjaScale = GUI.HorizontalSlider(new Rect(labelWidth, y, sliderWidth, 20), GameSettings.NinjaScale, 0.5f, 5.0f); y += 25;

        GUI.Label(new Rect(0, y, labelWidth, 20), "Gravity Mult");
        GameSettings.GravityMultiplier = GUI.HorizontalSlider(new Rect(labelWidth, y, sliderWidth, 20), GameSettings.GravityMultiplier, 0.5f, 4.0f); y += 25;

        // --- COMBAT ---
        y += 10;
        GUI.Label(new Rect(0, y, 200, 25), "<b>[ COMBAT ]</b>"); y += 25;

        GUI.Label(new Rect(0, y, labelWidth, 20), "Laser Damage");
        GameSettings.LaserDamagePerTick = GUI.HorizontalSlider(new Rect(labelWidth, y, sliderWidth, 20), GameSettings.LaserDamagePerTick, 1f, 100f); y += 25;

        GUI.Label(new Rect(0, y, labelWidth, 20), "Ki Recharge");
        GameSettings.KiRechargeRate = GUI.HorizontalSlider(new Rect(labelWidth, y, sliderWidth, 20), GameSettings.KiRechargeRate, 1f, 50f); y += 25;

        GUI.Label(new Rect(0, y, labelWidth, 20), "Stomp Force");
        GameSettings.StompForce = GUI.HorizontalSlider(new Rect(labelWidth, y, sliderWidth, 20), GameSettings.StompForce, 5f, 40f); y += 25;

        GUI.Label(new Rect(0, y, labelWidth, 20), "Stomp Radius");
        GameSettings.StompRadius = GUI.HorizontalSlider(new Rect(labelWidth, y, sliderWidth, 20), GameSettings.StompRadius, 5f, 30f); y += 25;

        // --- WORLD ---
        y += 10;
        GUI.Label(new Rect(0, y, 200, 25), "<b>[ WORLD ]</b>"); y += 25;

        GUI.Label(new Rect(0, y, labelWidth, 20), "Village HP");
        GameSettings.VillageMaxHP = GUI.HorizontalSlider(new Rect(labelWidth, y, sliderWidth, 20), GameSettings.VillageMaxHP, 100f, 2000f); y += 25;
        
        GUI.Label(new Rect(0, y, labelWidth, 20), "Intermission");
        GameSettings.IntermissionDuration = GUI.HorizontalSlider(new Rect(labelWidth, y, sliderWidth, 20), GameSettings.IntermissionDuration, 1f, 30f); y += 25;

        GUI.Label(new Rect(0, y, labelWidth, 20), "Throw Force");
        GameSettings.ThrowForce = GUI.HorizontalSlider(new Rect(labelWidth, y, sliderWidth, 20), GameSettings.ThrowForce, 10f, 100f); y += 25;

        if (GUI.Button(new Rect(0, y, 100, 30), "Reset Camera"))
        {
            GameSettings.CameraOffset = new Vector3(0.5f, 2.5f, -3.5f);
        }

        GUI.EndScrollView();
    }
}
