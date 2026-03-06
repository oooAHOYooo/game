using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

/// <summary>
/// ImpactFeedback — centralizes audio, controller rumble, camera shake, and hit VFX for combat.
/// Triggered manually via Play(DamageInfo, Vector3).
/// </summary>
public class ImpactFeedback : MonoBehaviour
{
    public static ImpactFeedback Instance;

    private AudioSource _audioSource;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        
        _audioSource = gameObject.AddComponent<AudioSource>();
        _audioSource.spatialBlend = 1f; // 3D sound
        _audioSource.maxDistance = 50f;
        _audioSource.playOnAwake = false;
    }

    /// <summary>
    /// Play full suite of impact feedback.
    /// </summary>
    public static void Play(float damageAmount, Vector3 position, int playerIndex = -1)
    {
        if (Instance == null) return;
        Instance.StartCoroutine(Instance.PlayFeedbackRoutine(damageAmount, position, playerIndex));
    }

    public static void Play(DamageInfo info, Vector3 position, int playerIndex = -1)
    {
        Play(info.Amount, position, playerIndex);
    }

    private IEnumerator PlayFeedbackRoutine(float damageAmount, Vector3 position, int playerIndex)
    {
        float intensity = Mathf.Clamp01(damageAmount / 45f); // Scale 0-1 based on max heavy damage

        // 1. Procedural Audio
        PlayImpactSound(position, intensity);

        // 2. Controller Rumble (if player)
        if (playerIndex >= 0 && playerIndex < Gamepad.all.Count)
        {
            var pad = Gamepad.all[playerIndex];
            if (pad != null)
            {
                float lf = GameSettings.RumbleIntensityBase * (intensity > 0.5f ? 1f : 0.2f);
                float hf = GameSettings.RumbleIntensityBase * (intensity <= 0.5f ? 1f : 0.5f);
                pad.SetMotorSpeeds(lf, hf);
            }
        }

        // 3. Camera Shake
        if (playerIndex >= 0)
        {
            SplitScreenCamera.ShakeCamera(playerIndex, 0.1f + 0.3f * intensity, 0.15f + 0.15f * intensity);
        }

        // 4. Particle Hit Burst
        SpawnHitVFX(position, intensity);

        // 5. Cinematic "Hit Stop" Effect (Freezes time briefly on heavy hits)
        if (intensity > 0.5f)
        {
            float hitStopDuration = 0.05f + 0.05f * intensity; 
            Time.timeScale = 0.1f;
            yield return new WaitForSecondsRealtime(hitStopDuration);
            Time.timeScale = 1f;
        }

        // Reset rumble
        yield return new WaitForSecondsRealtime(GameSettings.RumbleDurationBase + 0.1f * intensity);
        if (playerIndex >= 0 && playerIndex < Gamepad.all.Count)
        {
            Gamepad.all[playerIndex]?.SetMotorSpeeds(0f, 0f);
        }
    }

    private void PlayImpactSound(Vector3 position, float intensity)
    {
        // Procedurally generate a quick "thwack" sound
        int sampleRate = 44100;
        float duration = 0.15f + 0.1f * intensity;
        int sampleCount = Mathf.FloorToInt(sampleRate * duration);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleRate;
            // Noise burst with quick fade
            samples[i] = Random.Range(-1f, 1f) * Mathf.Exp(-t * 20f); 
            // Add a sub-bass thump for heavy hits
            if (intensity > 0.5f)
            {
                samples[i] += Mathf.Sin(t * 150f * Mathf.PI * 2f) * Mathf.Exp(-t * 10f) * 0.5f;
            }
        }

        var clip = AudioClip.Create("Impact", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);

        _audioSource.transform.position = position;
        _audioSource.pitch = Mathf.Lerp(GameSettings.ImpactAudioMaxPitch, GameSettings.ImpactAudioMinPitch, intensity);
        _audioSource.PlayOneShot(clip, 0.6f + 0.4f * intensity);
        
        Destroy(clip, duration + 0.1f);
    }

    private void SpawnHitVFX(Vector3 position, float intensity)
    {
        var obj = new GameObject("HitVFX");
        obj.transform.position = position;
        var ps = obj.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.playOnAwake = false;
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        main.duration = 0.2f;
        main.loop = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.1f, 0.3f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(5f, 15f * (1f + intensity));
        main.startSize = new ParticleSystem.MinMaxCurve(0.1f, 0.3f + 0.2f * intensity);
        main.startColor = new ParticleSystem.MinMaxGradient(Color.white, GameBootstrapper.PaletteGold);
        main.maxParticles = 30;

        var em = ps.emission;
        em.SetBursts(new[] { new ParticleSystem.Burst(0f, 10 + (short)(20 * intensity)) });
        em.rateOverTime = 0;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.2f;

        ps.Play();
        Destroy(obj, 1f);
    }

    void OnDisable()
    {
        // Ensure rumble is stopped when disabled
        foreach (var pad in Gamepad.all)
        {
            pad.SetMotorSpeeds(0f, 0f);
        }
    }
}
