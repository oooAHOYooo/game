# CLAUDE.md

## Project
Unity game — see GDD.md and README.md for design context.
All UI is procedural (no prefabs). Uses Unity new Input System.

## Target Audience
**1st–5th graders (ages 6–11).** They need: loud satisfying sounds, big visual celebration, simple clear goals, and constant positive feedback. Silent = broken to kids.

## Priority TODO (for kids to love it)
1. ~~**Sound effects**~~ ✅ **DONE** — `SoundManager.cs` added. Procedural sounds hooked up: enemy death, laser charge+fire, wave start alert, wave clear fanfare, village damage alarm, player death ditty, respawn twinkle, villager cheer. Hit sounds already in `ImpactFeedback.cs`.
2. ~~**Win/Lose screen**~~ ✅ **DONE** — "YOU SAVED THE VILLAGE!" and "THE VILLAGE FELL" with score, buttons to restart/quit, game paused during screens.
3. **Controller rumble** — `Gamepad.current.SetMotorSpeeds(...)` on every hit. Kids love physical feedback.
4. **Bigger hit reactions** — screen flash on kill, enemies ragdoll into ocean. Makes difference between "ok" and "AGAIN AGAIN AGAIN".
5. **Totem healing** — stand on totem to heal between waves; villagers cheer. Gives kids a clear goal between fights.
6. **Wave clear celebration** — BIG screen banner, color flash, villagers doing backflips. Tell kids they're amazing.
7. **Combo counter** — big "3 HIT COMBO!!" text on screen. Kids love score feedback.
8. **Enemy idle personalities** — one enemy type sleeping/scratching head before aggro. Kids will replay just to see it.
9. ~~**Day/night cycle**~~ ✅ **DONE** — `DayNightCycle.cs` cycles through 6 color palettes on each wave clear.
10. **Wave 1 tutorial prompts** — 1–2 on-screen hints ("PUNCH THEM → J / RB") for 1st graders.

## Recent Work
**Win/Lose screens + 5-wave victory condition (2026-03-16)**
- `WaveManager.cs` — Added `MaxWaves = 5` constant; `OnVictory` event fires after wave 5 cleared
- `SoundManager.cs` — Added `PlayGameOver()` (slow descending G4→F4→D4→G3 dirge) and `PlayVictory()` (ascending C4→E4→G4→C5 fanfare)
- `GameHUD.cs` — Fixed `ShowGameOver()` to pause time, show buttons for "PLAY AGAIN"/"QUIT"; added `ShowVictory()` coroutine with gold pulsing headline, victory sound + villager cheer
- `MenuManager.cs` — Changed title from "NINJA ISLAND" to "NINJA STRIKE"
- `NinjaController.cs` — Fixed `IsIntroDive` defaulting to `true` (was causing wind streaks on startup); now defaults to `false`

**Mixamo animation pipeline + fighting game inputs (2026-03-13)**
- Downloaded 9 Mixamo FBX animations + X Bot character model; tracked in git under `Assets/Art/`
- `sort_mixamo.sh` — bash script: drop Mixamo downloads in `~/Downloads`, run script, files land in correct project folders with correct names
- `Assets/Scripts/Editor/CharacterSetup.cs` — menu **NinjaStrike ▶ Setup Animated Characters**: builds `PlayerAnimated` prefab in `Resources/` from Mixamo FBX; must run in editor after git pull on each machine
- `Assets/Scripts/Core/AnimationLibrary.cs` — loads animation clips with multi-term fuzzy fallback matching
- `Assets/Scripts/Core/AnimatorControllerBuilder.cs` — full state machine: IsGrounded/JumpTrigger/AttackType(1-4)/IsAttacking/IsChargingKi; proper Jump→Fall→Land aerial chain
- `NinjaController.cs` — attack combo cycling (AttackType 1→2→3→4), JumpTrigger fired on jump, IsGrounded sent every frame
- `EnemyBase.cs` — runtime `#else` block loads `Resources/Characters/PlayerAnimated` prefab (enemies share same Mixamo model)
- `EnemyAI.cs` — `UpdateAnimator()` drives IsGrounded (raycast), AttackType (cycling), IsAttacking

**Sky-drop fireball enemies + day themes (2026-03-13)**
- `EnemyAI.cs` — SkyBomb enemy type: spawns at Y=220, falls with extra gravity, spawns fire trail, lands → fire zone + camera shake
- `FireZoneDamage.cs` (new) — persistent trigger sphere; deals 8 dmg/tick every 0.6s to players caught in fire
- `WaveManager.cs` — SkyBombs added to waves 3+; intermission calls `DayNightCycle.SetDayTheme(waveIndex)` + celebration light pillars
- `DayNightCycle.cs` — 6 color palettes cycling per wave; `GetDayColor(int)` static helper for VFX

**Island bounds enforcement (2026-03-13)**
- `NinjaController.cs` `EnforceIslandBounds()` — players pushed back with increasing force + hard clamped at `GameSettings.IslandRadius`
- `EnemyAI.cs` `EnforceIslandBounds()` — same for all enemies

**Windows installer (2026-03-13)**
- `installer.iss` — Inno Setup 6 wizard: installs to `{autopf}\Ninja Strike`, Start Menu + optional desktop shortcut

**Pause menu controls display**
- Three-column layout: P1 Keyboard (gold) | Gamepad (grey) | P2 Keyboard (cyan)
- All 10 actions per column; mappings sourced from `GameHUD.cs`

## Workflow: Adding New Animations
1. Download from mixamo.com (FBX for Unity, any FPS, with or without skin)
2. Run `bash sort_mixamo.sh` from project root
3. In Unity: **NinjaStrike ▶ Setup Animated Characters** (rebuilds prefab + controller)
4. Commit new FBX + generated `.controller` asset

## Missing Animations (download when possible)
- `Fall A Loop.fbx` — falling idle loop (search "Falling Idle" or "Fall A Loop" on Mixamo)
- `Standing Sprint Forward.fbx` — sprint animation

## Key Files
- `Assets/Scripts/UI/PauseMenuManager.cs` — pause menu (procedural)
- `Assets/Scripts/UI/GameHUD.cs` — in-game HUD; has authoritative input mapping strings
- `Assets/Scripts/Player/NinjaController.cs` — player input logic
- `Assets/Scripts/Core/GameBootstrapper.cs` — scene bootstrap; defines palette colors
- `Assets/Scripts/Core/AnimationLibrary.cs` — loads Mixamo clips by name with fuzzy fallback
- `Assets/Scripts/Core/AnimatorControllerBuilder.cs` — builds Animator Controller state machine in editor
- `Assets/Scripts/Editor/CharacterSetup.cs` — **run this after every git pull on a new machine**
- `Assets/Scripts/Enemy/FireZoneDamage.cs` — fire zone burn tick
- `sort_mixamo.sh` — auto-places Mixamo downloads into correct project folders
