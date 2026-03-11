# CLAUDE.md

## Project
Unity game — see GDD.md and README.md for design context.
All UI is procedural (no prefabs). Uses Unity new Input System.

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
