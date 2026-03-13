using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// DayNightCycle — controls cinematic lighting transitions tied to wave states.
/// </summary>
public class DayNightCycle : MonoBehaviour
{
    public static DayNightCycle Instance;

    public enum Phase { Day, Sunset, Night }
    public Phase CurrentPhase { get; private set; } = Phase.Day;

    private Light _sunLight;
    private HDAdditionalLightData _sunHD;
    private Light _fillLight;
    private HDAdditionalLightData _fillHD;

    private List<Light> _campfires = new List<Light>();

    // Target values
    private Color _targetSunColor;
    private float _targetSunIntensity;
    private Quaternion _targetSunRot;

    private Color _targetFillColor;
    private float _targetFillIntensity;

    private Color _targetAmbient;
    private Color _targetFog;

    // Speeds
    private float _lerpSpeed = 1f;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        // Find lights created by Bootstrapper
        var sunObj = GameObject.Find("DirectionalLight_Sun");
        if (sunObj != null)
        {
            _sunLight = sunObj.GetComponent<Light>();
            _sunHD = sunObj.GetComponent<HDAdditionalLightData>();
        }

        var fillObj = GameObject.Find("DirectionalLight_Fill");
        if (fillObj != null)
        {
            _fillLight = fillObj.GetComponent<Light>();
            _fillHD = fillObj.GetComponent<HDAdditionalLightData>();
        }

        SetPhase(Phase.Day, true);
    }

    public void RegisterCampfire(Light fireLight)
    {
        if (!_campfires.Contains(fireLight))
            _campfires.Add(fireLight);
    }

    // ── Day themes — each wave clear brings a visually distinct new day ──
    // Cycle: golden dawn → cyan tropics → violet dusk → emerald forest →
    //        crimson volcano → deep indigo → back to golden.
    private static readonly Color[] DayAmbients = {
        new Color(0.10f, 0.15f, 0.20f),  // Day 1: classic blue night→dawn
        new Color(0.05f, 0.18f, 0.18f),  // Day 2: teal tropics
        new Color(0.12f, 0.05f, 0.18f),  // Day 3: violet dusk
        new Color(0.04f, 0.14f, 0.06f),  // Day 4: emerald forest
        new Color(0.18f, 0.04f, 0.02f),  // Day 5: crimson volcano
        new Color(0.04f, 0.04f, 0.16f),  // Day 6: deep indigo
    };
    private static readonly Color[] DaySunColors = {
        new Color(1.0f, 0.90f, 0.70f),   // warm gold
        new Color(0.4f, 1.00f, 0.90f),   // cyan-teal
        new Color(0.8f, 0.50f, 1.00f),   // violet
        new Color(0.5f, 1.00f, 0.50f),   // green
        new Color(1.0f, 0.35f, 0.15f),   // volcanic orange
        new Color(0.5f, 0.60f, 1.00f),   // indigo blue
    };
    private static readonly Color[] DayFogColors = {
        new Color(0.15f, 0.25f, 0.35f),
        new Color(0.05f, 0.25f, 0.25f),
        new Color(0.18f, 0.08f, 0.28f),
        new Color(0.05f, 0.18f, 0.07f),
        new Color(0.25f, 0.06f, 0.03f),
        new Color(0.06f, 0.06f, 0.22f),
    };

    public void SetDayTheme(int waveJustCompleted)
    {
        int idx = waveJustCompleted % DayAmbients.Length;
        // Override the Day phase target colours so the transition looks fresh
        // (SetPhase(Day) is called right after, which will use these targets)
        _pendingDayAmbient = DayAmbients[idx];
        _pendingDaySun     = DaySunColors[idx];
        _pendingDayFog     = DayFogColors[idx];
    }

    /// <summary>Returns the accent color for a given day number (for VFX).</summary>
    public static Color GetDayColor(int dayNumber)
    {
        int idx = (dayNumber - 1) % DaySunColors.Length;
        return DaySunColors[idx < 0 ? 0 : idx];
    }

    private Color? _pendingDayAmbient;
    private Color? _pendingDaySun;
    private Color? _pendingDayFog;

    public void SetPhase(Phase newPhase, bool immediate = false)
    {
        CurrentPhase = newPhase;

        switch (newPhase)
        {
            case Phase.Day: // Intermission
                _targetSunRot = Quaternion.Euler(35f, -40f, 0);
                _targetSunColor = _pendingDaySun     ?? new Color(1.0f, 0.9f, 0.7f);
                _targetSunIntensity = 80000f;

                _targetFillColor = new Color(0.4f, 0.6f, 1.0f);
                _targetFillIntensity = 15000f;

                _targetAmbient = _pendingDayAmbient  ?? new Color(0.1f, 0.15f, 0.2f);
                _targetFog     = _pendingDayFog      ?? new Color(0.15f, 0.25f, 0.35f);
                _lerpSpeed = 0.5f;

                // Clear the overrides so they don't persist unexpectedly
                _pendingDaySun = null; _pendingDayAmbient = null; _pendingDayFog = null;
                break;

            case Phase.Sunset: // Wave warning
                _targetSunRot = Quaternion.Euler(15f, -60f, 0);
                _targetSunColor = GameBootstrapper.PaletteCrimson;
                _targetSunIntensity = 60000f;
                
                _targetFillColor = new Color(0.6f, 0.2f, 0.1f);
                _targetFillIntensity = 10000f;
                
                _targetAmbient = new Color(0.15f, 0.05f, 0.05f);
                _targetFog = new Color(0.2f, 0.05f, 0.05f);
                _lerpSpeed = 1.0f;
                break;

            case Phase.Night: // Wave active
                _targetSunRot = Quaternion.Euler(-10f, -80f, 0); // below horizon
                _targetSunColor = GameBootstrapper.PaletteMidnightBlue;
                _targetSunIntensity = 5000f;
                
                _targetFillColor = GameBootstrapper.PaletteDeepNavy;
                _targetFillIntensity = 8000f;
                
                _targetAmbient = new Color(0.02f, 0.02f, 0.05f);
                _targetFog = new Color(0.03f, 0.03f, 0.08f);
                _lerpSpeed = 0.3f; // slow fade into night
                break;
        }

        if (immediate)
        {
            ApplyLerp(1.0f);
        }
    }

    void Update()
    {
        ApplyLerp(Time.deltaTime * _lerpSpeed);
        UpdateCampfires();
    }

    void ApplyLerp(float t)
    {
        if (_sunLight != null)
        {
            _sunLight.transform.rotation = Quaternion.Slerp(_sunLight.transform.rotation, _targetSunRot, t);
            _sunLight.color = Color.Lerp(_sunLight.color, _targetSunColor, t);
            _sunLight.intensity = Mathf.Lerp(_sunLight.intensity, _targetSunIntensity, t);
        }

        if (_fillLight != null)
        {
            _fillLight.color = Color.Lerp(_fillLight.color, _targetFillColor, t);
            _fillLight.intensity = Mathf.Lerp(_fillLight.intensity, _targetFillIntensity, t);
        }

        RenderSettings.ambientLight = Color.Lerp(RenderSettings.ambientLight, _targetAmbient, t);
        RenderSettings.fogColor = Color.Lerp(RenderSettings.fogColor, _targetFog, t);
    }

    void UpdateCampfires()
    {
        float targetIntensity = CurrentPhase == Phase.Night ? 1500f :
                               CurrentPhase == Phase.Sunset ? 800f : 200f;

        for (int i = _campfires.Count - 1; i >= 0; i--)
        {
            if (_campfires[i] == null)
            {
                _campfires.RemoveAt(i);
                continue;
            }
            _campfires[i].intensity = Mathf.Lerp(_campfires[i].intensity, targetIntensity, Time.deltaTime * 2f);
        }
    }
}
