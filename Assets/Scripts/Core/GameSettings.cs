using UnityEngine;

/// <summary>
/// Centralized GameSettings to allow real-time tweaking of gameplay parameters.
/// </summary>
public static class GameSettings
{
    // ── Camera Settings ──────────────────────────────────────────────────
    public static Vector3 CameraOffset = new Vector3(0, 10f, -16f); // Zoomed in default
    public static float CameraSmoothTime = 0.18f;
    public static float CameraBaseFOV = 50f;
    public static float CameraMaxFOV = 75f;
    public static float CameraFOVLerpSpeed = 5f;

    // ── Player Stats ─────────────────────────────────────────────────────
    public static float PlayerMaxHP = 100f;
    public static float PlayerMaxKi = 100f;
    public static float PlayerGroundSpeed = 8f;
    public static float PlayerAirSpeed = 14f;
    public static float PlayerMaxFlightAltitude = 25f;
    public static float PlayerDodgeForce = 20f;

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
}
