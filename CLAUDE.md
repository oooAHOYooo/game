# CLAUDE.md

## Project
Unity game — see GDD.md and README.md for design context.
All UI is procedural (no prefabs). Uses Unity new Input System.

## Target Audience
**1st–5th graders (ages 6–11).** They need: loud satisfying sounds, big visual celebration, simple clear goals, and constant positive feedback. Silent = broken to kids.

## Priority TODO (for kids to love it)
1. ~~**Sound effects**~~ ✅ **DONE** — `SoundManager.cs` added. Procedural sounds hooked up: enemy death, laser charge+fire, wave start alert, wave clear fanfare, village damage alarm, player death ditty, respawn twinkle, villager cheer. Hit sounds already in `ImpactFeedback.cs`.
2. **Win/Lose screen** — "YOU SAVED THE VILLAGE!" or "THE VILLAGE FELL" with score. Village destruction currently does nothing (no game-over).
3. **Controller rumble** — `Gamepad.current.SetMotorSpeeds(...)` on every hit. Kids love physical feedback.
4. **Bigger hit reactions** — screen flash on kill, enemies ragdoll into ocean. Makes difference between "ok" and "AGAIN AGAIN AGAIN".
5. **Totem healing** — stand on totem to heal between waves; villagers cheer. Gives kids a clear goal between fights.
6. **Wave clear celebration** — BIG screen banner, color flash, villagers doing backflips. Tell kids they're amazing.
7. **Combo counter** — big "3 HIT COMBO!!" text on screen. Kids love score feedback.
8. **Enemy idle personalities** — one enemy type sleeping/scratching head before aggro. Kids will replay just to see it.
9. **Day/night cycle** — visual variety per session.
10. **Wave 1 tutorial prompts** — 1–2 on-screen hints ("PUNCH THEM → J / RB") for 1st graders.

## Recent Work
**Pause menu controls display** (`Assets/Scripts/UI/PauseMenuManager.cs`)
- Replaced single text block with a three-column layout: Player 1 Keyboard | Gamepad | Player 2 Keyboard
- Each column is a dark card with a colored top accent bar and header
- P1 = gold, Gamepad = grey, P2 = cyan — matching HUD player colors
- All 10 actions listed per column: MOVE, JUMP, FLY, DODGE, ATTACK, HEAVY, KI BEAM, BLOCK, WEAPON, LOCK-ON
- Input mappings sourced from `GameHUD.cs` (authoritative reference for keyboard P1 and gamepad)

## Key Files
- `Assets/Scripts/UI/PauseMenuManager.cs` — pause menu (procedural)
- `Assets/Scripts/UI/GameHUD.cs` — in-game HUD; has authoritative input mapping strings
- `Assets/Scripts/Player/NinjaController.cs` — player input logic
- `Assets/Scripts/Core/GameBootstrapper.cs` — scene bootstrap; defines palette colors
