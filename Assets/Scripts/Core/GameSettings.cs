using UnityEngine;

/// <summary>
/// Centralized GameSettings to allow real-time tweaking of gameplay parameters.
/// </summary>
public static class GameSettings
{
    // ── Camera Settings ──────────────────────────────────────────────────
    public static Vector3 CameraOffset = new Vector3(0.5f, 2.5f, -3.5f); // Over-the-shoulder zoomed in
    public static float CameraSmoothTime = 0.18f;
    public static float CameraBaseFOV = 50f;
    public static float CameraMaxFOV = 75f;
    public static float CameraFOVLerpSpeed = 5f;

    // ── Player Stats ─────────────────────────────────────────────────────
    public static float PlayerMaxHP = 100f;
    public static float PlayerMaxKi = 100f;
    public static float PlayerGroundSpeed = 12f;
    public static float PlayerAirSpeed = 22f;
    public static float PlayerMaxFlightAltitude = 25f;
    public static float PlayerDodgeForce = 25f;

    // ── Combat ───────────────────────────────────────────────────────────
    public static float LaserDamagePerTick = 18f;
    public static float KiRechargeRate = 12f;
    public static float SwordLightDamage = 22f;
    public static float SwordHeavyDamage = 45f;
    public static float StaffLightDamage = 15f;
    public static float StaffHeavyDamage = 35f;
    public static float StaffKiChargeMult = 1.4f;

    // ── World Generation ────────────────────────────────────────────────
    public static float IslandRadius = 150f;
    public static float TerrainMaxHeight = 18f;
    public static float WaterLevel = 0.3f;
    public static int TreeCount = 120;
    public static int RockCount = 50;

    // ── Wave & Enemies ──────────────────────────────────────────────────
    public static float VillageMaxHP = 500f;
    public static float VillageDamageRadius = 25f;
    public static float VillageDamagePerTick = 5f;
    public static float IntermissionDuration = 5f;
    public static float WaveClearPause = 3f;

    // ── Ninja Feel & God Powers ──────────────────────────────────────────
    public static float NinjaScale = 1.0f;
    public static float AuraIntensity = 3.0f;
    public static float GravityMultiplier = 1.0f;
    public static float StompRadius = 12f;
    public static float StompForce = 15f;
    public static float StompHeightThreshold = 5f; // min height for stomp effect
    public static float LiftingPower = 100f;
    public static float ThrowForce = 40f;

    // ── Enemy Stats ─────────────────────────────────────────────────────
    public static float EnemyFootSoldierHP = 40f;
    public static float EnemyShadowArcherHP = 30f;
    public static float EnemyBerserkerHP = 80f;
    public static float EnemyMiniBossHP = 150f;

    // ── Impact Feedback ───────────────────────────────────────────────────
    public static float HitStopBaseDuration = 0.04f;
    public static float RumbleIntensityBase = 0.4f;
    public static float RumbleDurationBase = 0.15f;
    public static float ImpactAudioMinPitch = 0.8f;
    public static float ImpactAudioMaxPitch = 1.2f;

    // ── Combo System (time-based: seconds of sustained combat) ────────────
    public static float ComboDecayTime = 3.0f;       // seconds without a hit before streak resets
    public static float ComboTier1Threshold = 3f;    // 3s  → 1.5× multiplier
    public static float ComboTier1Multiplier = 1.5f;
    public static float ComboTier2Threshold = 8f;    // 8s  → 2.0×
    public static float ComboTier2Multiplier = 2.0f;
    public static float ComboTier3Threshold = 15f;   // 15s → 3.0×
    public static float ComboTier3Multiplier = 3.0f;
    public static float ComboWorshipThreshold = 10f; // 10s sustained → villagers celebrate
    public static float ComboWorshipRadius = 25f;

    // ── Village Fire System ───────────────────────────────────────────────
    public static float FireSpreadInterval = 8.0f;
    public static float FireSpreadRadius = 6.0f;
    public static float FireDamagePerTick = 5.0f;
    // Note: FireDamagePerTick replaced VillageDamagePerTick logic, 
    // we already have VillageDamagePerTick = 5f at top, so we'll re-use it or define a precise one
    public static float TotemHealRadius = 4.5f;
    public static float TotemHealRate = 12f;
    
    // ── Faith & Totem Upgrades ───────────────────────────────────────────
    public static float FaithPerKillNearVillage = 5f;
    public static float FaithPerSecondWorshipped = 1f;
    public static float TotemLevel2Threshold = 100f;
    public static float TotemLevel3Threshold = 250f;
    public static float TotemShieldRadius = 30f;
    public static float TotemShieldSlowAmount = 0.5f;
    public static float TotemExtraKiGain = 2f;
}
