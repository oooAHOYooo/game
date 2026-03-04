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

    public void SetPhase(Phase newPhase, bool immediate = false)
    {
        CurrentPhase = newPhase;

        switch (newPhase)
        {
            case Phase.Day: // Intermission
                _targetSunRot = Quaternion.Euler(35f, -40f, 0);
                _targetSunColor = new Color(1.0f, 0.9f, 0.7f);
                _targetSunIntensity = 80000f;
                
                _targetFillColor = new Color(0.4f, 0.6f, 1.0f);
                _targetFillIntensity = 15000f;
                
                _targetAmbient = new Color(0.1f, 0.15f, 0.2f);
                _targetFog = new Color(0.15f, 0.25f, 0.35f);
                _lerpSpeed = 0.5f; // Slow transition to day
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
