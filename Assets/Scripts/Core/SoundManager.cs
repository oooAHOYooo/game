using UnityEngine;
using System.Collections;

/// <summary>
/// SoundManager — procedurally synthesised audio for all game events.
/// No audio assets required: every sound is built from sine waves, noise, and sweeps.
/// Call static methods from anywhere; the singleton routes to the correct AudioSource.
/// </summary>
public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance;

    private AudioSource _uiSource;    // 2D: fanfares, alerts, music stings
    private AudioSource _worldSource; // 3D: positional world sounds

    const int SR = 22050; // sample rate — low enough to be cheap, high enough for clarity

    // ─── Lifecycle ────────────────────────────────────────────────────────

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;

        _uiSource = gameObject.AddComponent<AudioSource>();
        _uiSource.spatialBlend = 0f;
        _uiSource.playOnAwake  = false;
        _uiSource.volume       = 0.8f;

        _worldSource = gameObject.AddComponent<AudioSource>();
        _worldSource.spatialBlend = 1f;
        _worldSource.maxDistance  = 80f;
        _worldSource.rolloffMode  = AudioRolloffMode.Linear;
        _worldSource.playOnAwake  = false;
    }

    // ═══════════════════════════════════════════════════════════════════════
    // PUBLIC STATIC API
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>Wave-clear fanfare: ascending C-E-G-C victory sting.</summary>
    public static void PlayWaveClear()
        => Instance?.StartCoroutine(Instance.WaveClearRoutine());

    /// <summary>Wave-start alert: two low warning horn blasts.</summary>
    public static void PlayWaveStart()
        => Instance?.StartCoroutine(Instance.WaveStartRoutine());

    /// <summary>Cartoony descending squash on enemy death. Boss = bigger + lower.</summary>
    public static void PlayEnemyDeath(bool isBoss, Vector3 pos)
        => Instance?.DoEnemyDeath(isBoss, pos);

    /// <summary>Laser fire: descending zap sweep. Ultra = bigger + wider.</summary>
    public static void PlayLaserFire(bool isUltra, Vector3 pos)
        => Instance?.DoLaserFire(isUltra, pos);

    /// <summary>Short rising charge tone when ki starts building.</summary>
    public static void PlayLaserCharge(Vector3 pos)
        => Instance?.DoLaserCharge(pos);

    /// <summary>Bell-like alarm when the village takes damage.</summary>
    public static void PlayVillageDamage(Vector3 pos)
        => Instance?.DoVillageDamage(pos);

    /// <summary>Descending death ditty when a player dies.</summary>
    public static void PlayPlayerDeath(Vector3 pos)
        => Instance?.StartCoroutine(Instance.PlayerDeathRoutine(pos));

    /// <summary>Ascending sparkle twinkle on player respawn.</summary>
    public static void PlayRespawn(Vector3 pos)
        => Instance?.StartCoroutine(Instance.RespawnRoutine(pos));

    /// <summary>Crowd cheer noise — villagers celebrating a wave clear.</summary>
    public static void PlayVillagerCheer()
        => Instance?.DoVillagerCheer();

    /// <summary>Game-over sting: slow descending four-note dirge (G4→F4→D4→G3).</summary>
    public static void PlayGameOver()
        => Instance?.StartCoroutine(Instance.GameOverRoutine());

    /// <summary>Victory fanfare: ascending triumph sting (C4→E4→G4→C5).</summary>
    public static void PlayVictory()
        => Instance?.StartCoroutine(Instance.VictoryRoutine());

    // ═══════════════════════════════════════════════════════════════════════
    // SOUND IMPLEMENTATIONS
    // ═══════════════════════════════════════════════════════════════════════

    // ── Wave Clear: C5 → E5 → G5 → C6 ───────────────────────────────────
    IEnumerator WaveClearRoutine()
    {
        float[] freqs = { 523f, 659f, 784f, 1047f };
        foreach (float f in freqs)
        {
            PlayTone2D(f, 0.14f, 0.75f);
            yield return new WaitForSecondsRealtime(0.16f);
        }
        // Final sparkle flourish
        PlayTone2D(1568f, 0.18f, 0.5f);
    }

    // ── Wave Start: two descending horn blasts ────────────────────────────
    IEnumerator WaveStartRoutine()
    {
        PlayHorn(380f, 260f, 0.28f, 0.7f);
        yield return new WaitForSecondsRealtime(0.38f);
        PlayHorn(320f, 200f, 0.32f, 0.85f);
    }

    // ── Enemy Death: cartoony descending squash ───────────────────────────
    void DoEnemyDeath(bool isBoss, Vector3 pos)
    {
        float baseF    = isBoss ? 180f  : 700f;
        float endF     = isBoss ? 40f   : 90f;
        float duration = isBoss ? 0.45f : 0.22f;
        float volume   = isBoss ? 1.0f  : 0.65f;

        int n = Mathf.FloorToInt(SR * duration);
        float[] s = new float[n];
        float phase = 0f;

        for (int i = 0; i < n; i++)
        {
            float t    = (float)i / SR;
            float tN   = t / duration;
            float freq = Mathf.Lerp(baseF, endF, tN * tN);
            phase     += 2f * Mathf.PI * freq / SR;
            float env  = Mathf.Exp(-t * (isBoss ? 5f : 10f));
            s[i] = Mathf.Sin(phase) * env;
            s[i] += Random.Range(-0.3f, 0.3f) * Mathf.Exp(-t * 30f) * 0.5f; // crack on attack
        }
        PlayAt(s, duration, pos, volume);
    }

    // ── Laser Fire: descending frequency zap ─────────────────────────────
    void DoLaserFire(bool isUltra, Vector3 pos)
    {
        float duration = isUltra ? 0.55f : 0.28f;
        float startF   = isUltra ? 3200f : 2000f;
        float endF     = isUltra ? 80f   : 280f;
        float volume   = isUltra ? 1.0f  : 0.75f;

        int n = Mathf.FloorToInt(SR * duration);
        float[] s = new float[n];
        float phase = 0f;

        for (int i = 0; i < n; i++)
        {
            float t    = (float)i / SR;
            float tN   = t / duration;
            float freq = Mathf.Lerp(startF, endF, Mathf.Pow(tN, 0.45f));
            phase     += 2f * Mathf.PI * freq / SR;
            float env  = Mathf.Exp(-t * (isUltra ? 2.5f : 5f));
            s[i] = Mathf.Sin(phase) * env * 0.65f;
            // Buzzy noise layer
            s[i] += Random.Range(-1f, 1f) * env * 0.25f;
        }
        PlayAt(s, duration, pos, volume);
    }

    // ── Laser Charge: rising tone ─────────────────────────────────────────
    void DoLaserCharge(Vector3 pos)
    {
        float duration = 0.28f;
        int n = Mathf.FloorToInt(SR * duration);
        float[] s = new float[n];
        float phase = 0f;

        for (int i = 0; i < n; i++)
        {
            float t    = (float)i / SR;
            float tN   = t / duration;
            float freq = Mathf.Lerp(180f, 1400f, tN * tN);
            phase     += 2f * Mathf.PI * freq / SR;
            float env  = tN; // fade in
            s[i] = Mathf.Sin(phase) * env * 0.5f;
        }
        PlayAt(s, duration, pos, 0.5f);
    }

    // ── Village Damage: metallic alarm bell ───────────────────────────────
    void DoVillageDamage(Vector3 pos)
    {
        float duration = 0.4f;
        int n = Mathf.FloorToInt(SR * duration);
        float[] s = new float[n];

        for (int i = 0; i < n; i++)
        {
            float t   = (float)i / SR;
            float f   = 460f;
            // Bell-like inharmonic partials (classic struck metal ratios)
            s[i]  = Mathf.Sin(2f * Mathf.PI * f        * t) * Mathf.Exp(-t * 7f)  * 0.50f;
            s[i] += Mathf.Sin(2f * Mathf.PI * f * 2.76f * t) * Mathf.Exp(-t * 12f) * 0.28f;
            s[i] += Mathf.Sin(2f * Mathf.PI * f * 5.40f * t) * Mathf.Exp(-t * 28f) * 0.12f;
        }
        PlayAt(s, duration, pos, 0.95f);
    }

    // ── Player Death: descending four-note ditty ──────────────────────────
    IEnumerator PlayerDeathRoutine(Vector3 pos)
    {
        float[] freqs = { 392f, 349f, 311f, 261f }; // G4 F4 Eb4 C4
        foreach (float f in freqs)
        {
            float noteDur = 0.13f;
            int n = Mathf.FloorToInt(SR * noteDur);
            float[] s = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t  = (float)i / SR;
                float tN = t / noteDur;
                s[i] = Mathf.Sin(2f * Mathf.PI * f * t) * (1f - tN) * 0.65f;
                // Slight warm harmonic
                s[i] += Mathf.Sin(2f * Mathf.PI * f * 2f * t) * (1f - tN) * 0.15f;
            }
            PlayAt(s, noteDur, pos, 0.8f);
            yield return new WaitForSecondsRealtime(noteDur + 0.02f);
        }
    }

    // ── Game Over: slow descending death dirge ─────────────────────────────
    IEnumerator GameOverRoutine()
    {
        float[] freqs = { 392f, 349f, 294f, 196f }; // G4 F4 D4 G3
        float[] spacings = { 0.18f, 0.18f, 0.18f };

        for (int i = 0; i < freqs.Length; i++)
        {
            float f = freqs[i];
            float noteDur = 0.4f;
            int n = Mathf.FloorToInt(SR * noteDur);
            float[] s = new float[n];

            for (int j = 0; j < n; j++)
            {
                float t  = (float)j / SR;
                float tN = t / noteDur;
                float env = Mathf.Exp(-t * 2.5f); // slow decay for dirge
                s[j] = Mathf.Sin(2f * Mathf.PI * f * t) * env * 0.75f;
                // Add minor harmonic for sadness
                s[j] += Mathf.Sin(2f * Mathf.PI * f * 1.5f * t) * env * 0.2f;
            }
            PlayTone2D(f, noteDur, 0.8f);

            if (i < spacings.Length)
                yield return new WaitForSecondsRealtime(spacings[i]);
        }
    }

    // ── Victory: ascending triumph sting ───────────────────────────────────
    IEnumerator VictoryRoutine()
    {
        float[] freqs = { 261f, 329f, 392f, 523f }; // C4 E4 G4 C5
        float spacing = 0.2f;

        foreach (float f in freqs)
        {
            PlayTone2D(f, 0.16f, 0.85f);
            yield return new WaitForSecondsRealtime(spacing);
        }

        // Final triumphant chord burst
        yield return new WaitForSecondsRealtime(0.1f);
        PlayTone2D(261f, 0.22f, 0.9f);
    }

    // ── Respawn: ascending sparkle twinkle ───────────────────────────────
    IEnumerator RespawnRoutine(Vector3 pos)
    {
        float[] freqs = { 523f, 659f, 784f, 1047f, 1568f }; // C5 E5 G5 C6 G6
        foreach (float f in freqs)
        {
            float noteDur = 0.07f;
            int n = Mathf.FloorToInt(SR * noteDur);
            float[] s = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t  = (float)i / SR;
                float tN = t / noteDur;
                float env = Mathf.Sin(tN * Mathf.PI); // bell envelope
                s[i] = Mathf.Sin(2f * Mathf.PI * f      * t) * env * 0.55f;
                s[i] += Mathf.Sin(2f * Mathf.PI * f * 2f * t) * env * 0.20f;
                s[i] += Mathf.Sin(2f * Mathf.PI * f * 3f * t) * env * 0.08f;
            }
            PlayAt(s, noteDur, pos, 0.65f);
            yield return new WaitForSecondsRealtime(noteDur + 0.01f);
        }
    }

    // ── Villager Cheer: crowd noise with whoosh ───────────────────────────
    void DoVillagerCheer()
    {
        float duration = 1.8f;
        int n = Mathf.FloorToInt(SR * duration);
        float[] s = new float[n];

        float prev = 0f;
        for (int i = 0; i < n; i++)
        {
            float t  = (float)i / SR;
            float tN = t / duration;

            // Amplitude envelope: burst in, sustain, fade out
            float amp = tN < 0.15f ? tN / 0.15f
                      : tN > 0.65f ? (1f - tN) / 0.35f
                      : 1f;
            amp = Mathf.Clamp01(amp);

            // Low-pass filtered noise (crowd body)
            float white = Random.Range(-1f, 1f);
            prev = prev * 0.97f + white * 0.03f;

            // Light pitch: tiny cheering sine at ~350 Hz to give voice quality
            float voice = Mathf.Sin(2f * Mathf.PI * 350f * t) * 0.06f
                        + Mathf.Sin(2f * Mathf.PI * 700f * t) * 0.03f;

            s[i] = (prev * 0.55f + white * 0.08f + voice) * amp * 0.55f;
        }

        var clip = AudioClip.Create("VillagerCheer", n, 1, SR, false);
        clip.SetData(s, 0);
        _uiSource.PlayOneShot(clip, 0.75f);
        Destroy(clip, duration + 0.3f);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // HELPERS
    // ═══════════════════════════════════════════════════════════════════════

    /// Play a pure sine tone through the 2D UI source.
    void PlayTone2D(float freq, float duration, float volume)
    {
        int n = Mathf.FloorToInt(SR * duration);
        float[] s = new float[n];
        for (int i = 0; i < n; i++)
        {
            float t  = (float)i / SR;
            float tN = t / duration;
            float env = tN < 0.08f ? tN / 0.08f : Mathf.Exp(-(tN - 0.08f) * 6f);
            s[i]  = Mathf.Sin(2f * Mathf.PI * freq       * t) * env * volume;
            s[i] += Mathf.Sin(2f * Mathf.PI * freq * 2f  * t) * env * volume * 0.25f;
            s[i] += Mathf.Sin(2f * Mathf.PI * freq * 3f  * t) * env * volume * 0.10f;
        }
        var clip = AudioClip.Create("Tone2D", n, 1, SR, false);
        clip.SetData(s, 0);
        _uiSource.PlayOneShot(clip, 1f);
        Destroy(clip, duration + 0.1f);
    }

    /// Play a frequency-sweep horn through the 2D UI source.
    void PlayHorn(float startF, float endF, float duration, float volume)
    {
        int n = Mathf.FloorToInt(SR * duration);
        float[] s = new float[n];
        float phase = 0f;

        for (int i = 0; i < n; i++)
        {
            float t    = (float)i / SR;
            float tN   = t / duration;
            float freq = Mathf.Lerp(startF, endF, tN);
            phase     += 2f * Mathf.PI * freq / SR;
            float env  = tN < 0.05f ? tN / 0.05f : Mathf.Exp(-(tN - 0.05f) * 5f);
            s[i]  = Mathf.Sin(phase)        * env * volume;
            s[i] += Mathf.Sin(phase * 2f)   * env * volume * 0.38f;
            s[i] += Mathf.Sin(phase * 3f)   * env * volume * 0.18f;
            s[i] += Mathf.Sin(phase * 4f)   * env * volume * 0.07f;
        }
        var clip = AudioClip.Create("Horn", n, 1, SR, false);
        clip.SetData(s, 0);
        _uiSource.PlayOneShot(clip, 1f);
        Destroy(clip, duration + 0.1f);
    }

    /// Spawn a temporary 3D AudioSource at a world position and play a sample buffer.
    void PlayAt(float[] samples, float duration, Vector3 pos, float volume)
    {
        var go  = new GameObject("SFX_3D");
        go.transform.position = pos;
        var src = go.AddComponent<AudioSource>();
        src.spatialBlend = 1f;
        src.maxDistance  = 60f;
        src.rolloffMode  = AudioRolloffMode.Linear;
        src.volume       = volume;

        var clip = AudioClip.Create("SFX", samples.Length, 1, SR, false);
        clip.SetData(samples, 0);
        src.clip = clip;
        src.Play();

        Destroy(go,   duration + 0.25f);
        Destroy(clip, duration + 0.35f);
    }
}
