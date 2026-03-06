# 🥷 NinjaStrike: Island Gods — Game Design Document
> **Version:** 0.4 · **Updated:** 2026-02-23  
> **Engine:** Unity 6 (HDRP 17.3) · **Pipeline:** High Definition Render Pipeline  
> **Project path:** `/home/alexg/Dev/unity/projects/game`

---

## 🧭 At a Glance (TL;DR for AI)
This is a **2-player split-screen co-op island-defence brawler** played with gamepads.  
Players are **god-sized ninjas** on a procedural island — like **Gulliver's Travels meets Dragon Ball Z**.  
A tiny **tribal village** sits at the island centre; villagers **worship** the god-players.  
Enemies invade from the **ocean's edge** in **waves (3 → 5 → 8)** but fight **one-at-a-time** (Zelda-style queue).  
Players **fly, shoot lasers, swing swords/staffs** to **defend the village** (which has its own HP bar).  
Looking up at the players fighting looks like **star-fights** to the tiny villagers.  
If no second controller is connected, **Player 2 becomes a ghost AI companion**.  
Art style: **cinematic golden-hour island** — warm sunset + Blade Runner accents.  
The **entire world is built procedurally at runtime from C# scripts** — no manual Unity editor work needed.

---

## 🎮 Core Pillars

| Pillar | Inspiration | What it means in game |
|--------|------------|------------------------|
| **God-scale power fantasy** | Gulliver's Travels | Players are massive; villagers are tiny ants that worship them |
| **Island world** | Open-world feel | Perlin-noise terrain, ocean, trees, rocks, beach |
| **Village defence** | Tower-defence horde | If enemies reach the village, it takes damage; village has HP |
| **Fluid combat feel** | UFC 5 | Responsive input, momentum-based movement, no input lag |
| **Aerial freedom** | Dragon Ball Z | Players can fly, charge, dash mid-air, hover |
| **Energy attacks** | Dragon Ball Z | Chargeable laser/ki beam, splash damage |
| **Weapon duality** | Ninja lore | Sword ↔ Staff transform on button hold |
| **Queued enemy duels** | Legend of Zelda | One enemy engages at a time; others circle and wait |
| **Cinematic visuals** | Akira, Blade Runner | HDRP emissive colours, bloom, fog, cinematic palettes |
| **Horde pressure** | Horde games | Waves escalate; between waves players can breathe |

---

## 🏝️ World Design — The Island

### Terrain
- **150m radius** procedural island using Perlin-noise heightmap mesh (128 × 128 resolution)
- Multi-octave noise with circular falloff (smooth shoreline edges)
- Village area (centre) is flattened for building placement
- Peak height: 18m — hills and rocky outcrops around the edges
- Vertex-coloured: **grass** (mid), **sand** (beach/low), **rock** (peaks)
- Mesh collider for physics

### Ocean
- Massive 800m × 800m ocean plane at y = 0.3
- Reflective HDRP material (metallic, high smoothness)
- Subtle foam particle shimmer on the surface

### Nature
- **120 trees** — trunk cylinder + 2–3 canopy spheres, random scale/colour variation
- **50 rocks** — cubes/spheres with random scale/rotation
- **80 grass patches** — low flat cubes, varied green shading
- **Beach ring** — 36 sand-coloured segments around the shoreline

### Atmosphere
- **12 distant mountain silhouettes** around the ocean (deep navy, no collision)
- Golden-hour lighting (warm sunset sun + cool blue fill)
- Fog: exponential squared, density 0.003, warm haze

---

## ⛺ Village System (NEW)

### Overview
A tiny tribal settlement sits at the island's centre. To the god-sized players, it looks like a miniature diorama. The village has:
- **12 huts** — wooden walls + thatch roofs, arranged in a ring (~0.5m tall)
- **1 worship totem** — 3m tall, carved faces, glowing gold eyes, crystal gem crown
- **4 campfires** — log rings + fire particles + warm point lights
- **24 fence posts** — low wooden fence perimeter
- **20 villagers** — tiny tribal people (~0.15m tall)

### Village HP
- **Max HP:** 500
- **Damage:** enemies that breach the village perimeter (25m radius) deal 5 damage per tick
- **HUD:** Town HP bar at centre-top of screen; flashes red when below 30%
- **Village destroyed:** huts despawn, totem remains as ruins → game loss condition (planned)

### Script: `Assets/Scripts/World/Village.cs`

---

## 🧍 Villagers (Gulliver-scale People)

Tiny (~0.12–0.18m) tribal people with:
- Randomised skin tones and cloth colours (red/blue/green)
- Some carry spears, some wear feather headdresses
- No collision — they're decorative + ambient life

### Villager Behaviour States
| State | Trigger | Behaviour |
|-------|---------|-----------|
| **Wandering** | Default | Meanders randomly near home |
| **Worshipping** | Player within 8m | Faces the god-player, bows (bobbing motion) |
| **Cheering** | Player attacks nearby | Tiny excited jumps |
| **Fleeing** | Enemy within 6m | Runs away from nearest enemy at 2× speed |
| **Celebrating** | Wave clear | Jumps & spins for 4 seconds |

### Script: `Assets/Scripts/World/Villager.cs`

---

## 🕹️ Input & Controls

**Target devices:** Xbox / PS / Switch Pro controller. No motion controls.

| Action | Button |
|--------|--------|
| Move (ground) | Left Stick |
| Fly / Ascend | Right Stick Up (hold) |
| Descend | Right Stick Down (hold) |
| Jump | South (A/Cross/B) |
| Dodge / Air-dash | East (B/Circle/A) — double-tap direction |
| Light attack | Right Bumper (RB/R1/R) |
| Heavy attack | Right Trigger (RT/R2/ZR) — hold to charge |
| Ki/Laser charge | Left Trigger (LT/L2/ZL) hold → release to fire |
| Block | Left Bumper (LB/L1/L) |
| Transform weapon | Hold West (X/Square/Y) for 1 second |
| Lock-on enemy | Right Stick click (RS) toggle |
| Pause | Start / Menu |

**UFC5-like feel:** inputs have a small, forgiving window (~4 frames) so combos feel deliberate but not clunky. Rigidbody physics drive momentum — you slide, you build speed when chaining attacks.

**Keyboard fallback (P1):** WASD move · E/Q ascend/descend · Space jump · Shift dodge · J light · K heavy · L ki · I block · U weapon swap · F lock-on

---

## 👤 Players (God-sized Ninjas)

### Scale
- Players are standard Unity unit scale (~2m tall)
- Villagers are ~0.15m → players appear **~13× larger** — Godzilla/Gulliver ratio
- When players fight enemies in the sky, it looks like **star-fights** from the village below

### Shared Stats (per ninja)
| Stat | Value |
|------|-------|
| Max HP | 100 |
| Max Ki (energy) | 100 |
| Ground speed | 8 m/s |
| Air speed | 14 m/s |
| Max flight altitude | 25 m |
| Laser damage | 18 / charge tick |
| Sword damage | 22 light / 45 heavy |
| Staff damage | 15 light / 35 heavy (hits wider arc) |

### Player 1
- **Color identity:** Gold (`#FFC819`) aura + crimson headband  
- **Controller:** First detected gamepad (index 0), fallback keyboard

### Player 2
- **Color identity:** Cyan-ghost blue  
- **Controller:** Second detected gamepad (index 1)  
- **If no controller:** Becomes a **Ghost AI** — translucent, semi-autonomous, mimics Player 1's position with slight delay, still fires lasers and attacks, cannot die (respawns in 3s)

---

## ⚔️ Weapon System

### Sword Mode (default)
- Fast slashes, short range  
- Light attack: horizontal slash arc (box trigger sweep)  
- Heavy: downward plunge, small shockwave on land  
- Can deflect enemy projectiles on perfect-block

### Staff Mode (hold West 1s)
- Slower but wider sweep, longer reach  
- Light: spinning staff sweep (360° arc hits all enemies in range)  
- Heavy: slam ground → AoE shockwave up to 4m radius  
- Channels Ki faster while staff is active (laser charge rate ×1.4)

**Transform VFX:** glowing particles spiral outward, weapon lerp-morphs between shapes over 0.4s.

---

## 🔥 Ki / Laser System (Dragon Ball Z)
- **Hold LT/L2/ZL:** Ki meter drains, a glowing energy ball forms at hand  
- **Release:** fires a cylindrical laser beam forward  
- Ki recharges at 12/s when not firing  
- **Full charge (100% Ki):** `Ultra Beam` — massive ray, 3× damage, knockback  
- Visual: bloom spike, screen chromatic aberration on Ultra Beam  
- Enemies can be hit mid-air and ragdoll-fly backward

---

## 👹 Enemies

### Spawning
Enemies emerge from the **ocean's edge** (160m from centre, at water level).  
They march inland toward the village. If not intercepted, they damage the village.

### Wave Structure
```
Wave 1:  3 enemies   — Basic Foot Soldiers
Wave 2:  5 enemies   — Mix: Soldiers + 1 Archer type
Wave 3:  8 enemies   — Mix: Soldiers + Archers + 1 Berserker
Wave 4+: 8 enemies   — Mix with MiniBoss, stats scale ×1.2 per cycle
```

### Engagement Queue (Zelda-style)
- Only **1 enemy actively attacks** at a time (the "active combatant")  
- All others **circle the player** at ~5m distance, occasionally throwing projectiles  
- When the active enemy is defeated or staggers, the next in queue steps forward  
- This creates a fair, readable challenge that rewards skill over chaos

### Enemy Types & "Nintendo" AI Philosophy
To achieve that signature "Nintendo polish," enemy AI relies on **highly readable, state-based behaviours** rather than complex unpredictability. 
- **Extreme Telegraphing:** Every attack has a distinct wind-up animation and audio cue.
- **Personality Quirks:** Enemies exhibit idle behaviours (sleeping, scratching heads) before spotting the player.
- **Clear States:** Unaware (wandering) → Suspicious (looking) → Aggro (attacking) → Defeated (fleeing/falling).

| Name (Archetype) | HP | Speed | Behaviour | Personality & AI State Notes |
|------------------|----|-------|-----------|------------------------------|
| **Foot Soldier** (Rookie) | 40 | 5 m/s | Melee rush, basic 3-hit combo. | Nervous, swings wildly, easily startled, backs down when hit. |
| **Shadow Archer** (Skirmisher) | 30 | 3 m/s | Ranged energy arrows, side-steps. | Skittish, hyperactive, tries to stay mid-range. Backpedals when approached. |
| **Berserker** (Bruiser) | 80 | 6 m/s | Two-handed heavy, unblockable charged slam. | Arrogant, slow, highly telegraphed attacks. Cannot be staggered during wind-up. |
| **Wave Mini-boss** (Commander) | 150 | 4 m/s | Attacks rarely, buffs allies' speed/power, summons 1 clone. | Rigid, disciplined. Points to command others. Defeating them demoralises the squad. |

### Enemy Visual Identity
- Dark body suit with single accent colour (crimson for soldiers, purple for archers, orange for berserkers).
- Glowing eyes matching accent colour.
- Emissive energy lines along arms when attacking.
- Distinct silhouettes and posture (e.g. hunched Skirmisher vs. puff-chested Bruiser) to telegraph their role instantly.

### Required Animation List (Mixamo/Assets)
*Note: Currently, no 3D humanoid animation assets (.fbx, .anim) exist in the `Assets` folder. The following list details the required animations to drive the AI State Machines once imported.*

**Core Locomotion**
- [ ] **Idle (Basic):** Nervous looking around or heavy chest-puffed breathing.
- [ ] **Walk/Run (Basic):** Scurrying or heavy stomping walk.
- [ ] **Walk/Run (Stealth):** Low-to-the-ground creeping (for Archers).

**Combat & Attacks**
- [ ] **Melee Wide Swing:** Heavily telegraphed 180-degree sweep (Bruiser).
- [ ] **Overhead Smash:** Raising weapon, pausing, slamming down.
- [ ] **Poke/Jab:** Fast forward thrust (Foot Soldier).
- [ ] **Throw Projectile:** Lobbing/shooting motion with wind-up (Archer).
- [ ] **Charge Wind-up:** Scraping ground/preparing to sprint.

**Reactions & States**
- [ ] **Hit Reaction (Light):** Quick flinch backward.
- [ ] **Hit Reaction (Heavy):** Staggering off balance.
- [ ] **Stunned/Dizzy:** Swaying, head-holding "vulnerable" state.
- [ ] **Notice/Surprise:** Dramatic flinch when spotting player.
- [ ] **Taunt/Command:** Pointing forward, shouting, or chest-pounding.
- [ ] **Standard Death:** Collapsing backwards.

---

## 🌊 Wave Manager Logic (Code Reference)
**Script:** `Assets/Scripts/Core/WaveManager.cs`

```
State machine:
  IDLE → SPAWNING → WAVE_ACTIVE → WAVE_CLEAR → INTERMISSION → SPAWNING → ...
  
- SPAWNING: instantiates enemy GameObjects at ocean edge positions
- WAVE_ACTIVE: runs the engagement queue, tracks alive count, checks village damage
- WAVE_CLEAR: plays fanfare VFX, villagers celebrate, waits 3s
- INTERMISSION: 5s breather, HUD countdown shown
```

---

## 🎥 Camera System
**Script:** `Assets/Scripts/Camera/SplitScreenCamera.cs`

- True split-screen: left half = P1, right half = P2  
- Camera offset: `(0, 14, -22)` — higher and further back for the open island  
- Far clip plane: 600m — entire island and ocean visible  
- Each camera follows its target with a `SmoothDamp` lag (~0.25s)  
- Dynamic FOV: zooms out when player moves fast or fires laser  
- On big moves (laser fire, Ultra Beam) camera does a short **screen shake** (`perlin noise offset`)  

---

## 🎨 Art Style — Cinematic Colour Palette

### Core Palette
| Name | Hex | Use |
|------|-----|-----|
| Deep Navy | `#0D1230` | Environment shadows, distant mountains |
| Midnight Blue | `#142059` | Walls, dark geometry |
| Crimson | `#D91420` | Headbands, enemy soldiers, danger |
| Gold | `#FFC819` | Player 1 aura, totem, highlights |
| Cyan | `#1AE5FF` | Player 2 aura, totem gem |
| Purple | `#8C26E5` | Enemy archers, arcane |
| Ghost Blue | `#66B3FF73` | P2 ghost tint |
| Grass Green | `#266120` | Island terrain mid |
| Sand | `#C2AD73` | Beach, low terrain |
| Ocean Blue | `#0D4C8C` | Ocean surface |

### Cinematic Inspirations
- **Gulliver's Travels** — power scale, tiny civilisation looking up at gods
- **Princess Mononoke** — misty island, spiritual guardians
- **Dragon Ball Z** — ultra-saturated energy, bloom explosions, vivid character auras
- **Blade Runner 2049** — warm/cool contrast, golden atmosphere

### HDRP Settings
- Sun: warm golden (#FFD98C), 30,000 lux, 25° angle  
- Fill: cool blue sky bounce (#668FE5), 5,000 lux  
- Bloom: enabled, threshold 0.8, intensity 0.5  
- Chromatic Aberration: subtle baseline, spikes on Ultra Beam  
- Fog: exponential squared, density 0.003, warm haze `#33261A`  
- Ambient: flat, `#1A1408`

---

## 📁 Script Architecture

```
Assets/Scripts/
├── Core/
│   ├── GameBootstrapper.cs     ← Builds entire scene at runtime (entry point)
│   ├── SceneBootstrap.cs       ← Single GO component to kick off everything
│   └── WaveManager.cs          ← Wave spawning, Zelda-queue, village damage check
├── Player/
│   ├── NinjaController.cs      ← UFC5-feel movement, flight, dodge, attack
│   ├── PlayerHealth.cs         ← HP tracking, death & respawn
│   └── GhostAI.cs              ← P2 ghost behaviour when no controller
├── Combat/
│   ├── LaserBeam.cs            ← Ki charge, fire, Ultra Beam
│   ├── WeaponHitbox.cs         ← Trigger-based melee hit detection + hit-stop
│   └── DamageInfo.cs           ← Shared damage data struct
├── Enemy/
│   ├── EnemyBase.cs            ← Shared enemy HP, state machine, factory builders
│   ├── EnemyAI.cs              ← Pathfinding, queue awareness, attack patterns
│   └── EnemyProjectile.cs      ← Self-propelled projectile, deflectable
├── World/
│   ├── IslandGenerator.cs      ← Perlin-noise terrain, ocean, trees, rocks, beach
│   ├── Village.cs              ← Huts, totem, campfires, fence, village HP
│   └── Villager.cs             ← Tiny tribal people, worship/flee/celebrate AI
├── Camera/
│   └── SplitScreenCamera.cs    ← Per-player follow cam, zoom, shake
└── UI/
    └── GameHUD.cs              ← HP/Ki bars, village HP, wave banners, ghost badge
```

Total: **17 scripts** across **7 folders**

---

## ✅ Implementation Status

| System | Status | Notes |
|--------|--------|-------|
| Island terrain | ✅ Done | Perlin-noise heightmap, 128×128 mesh |
| Ocean + beach | ✅ Done | Ocean plane, shimmer particles, sand ring |
| Trees + rocks + grass | ✅ Done | 120 trees, 50 rocks, 80 grass patches |
| Distant mountains | ✅ Done | 12 silhouette cubes at ocean edge |
| Village huts | ✅ Done | 12 tiny huts with walls, roofs, doors |
| Totem | ✅ Done | 3-tier carved pillar, glowing gem |
| Campfires | ✅ Done | 4 fires with particle + point light |
| Villagers (20) | ✅ Done | Wander, worship, flee, celebrate |
| Village HP | ✅ Done | 500 HP, damage check, HUD bar |
| Cinematic lighting | ✅ Done | Warm sunset + cool fill + accent lights |
| Player visuals | ✅ Done | Capsule ninja + weapon + aura |
| Weapon transform | ✅ Done | VFX burst + weapon swap |
| NinjaController | ✅ Done | UFC5 movement, flight, dodge, attack, ki |
| GhostAI | ✅ Done | Autonomous P2 with reaction delay |
| LaserBeam | ✅ Done | Ki beam projectile + Ultra Beam VFX |
| WaveManager | ✅ Done | Ocean-edge spawning, Zelda queue, village damage |
| EnemyAI | ✅ Done | 4 enemy types, type-specific combos |
| EnemyProjectile | ✅ Done | Arrow/bolt — deflectable with block |
| Split-screen camera | ✅ Done | SmoothDamp follow, perlin shake, dynamic FOV |
| HUD | ✅ Done | HP/Ki/Village bars, wave banners, ghost badge |
| DamageInfo struct | ✅ Done | Shared combat data struct |
| SceneBootstrap | ✅ Done | Single manual step to start game |
| HitStop (in-class) | ✅ Done | Time.timeScale 0.05 for 60ms on hit |
| Sound design | 📋 Planned | Placeholder SFX next priority |
| Village destruction consequence | 📋 Planned | Game-over screen |
| Healing mechanic | 📋 Planned | Player heals village by standing on totem |

---

## 🗺️ Roadmap

### Phase 1 — Playable Prototype ✅ COMPLETE
- [x] Procedural island generation  
- [x] Village with tiny villagers  
- [x] Full player movement & combat  
- [x] All enemy types + AI  
- [x] Wave spawning from ocean  
- [x] Laser system  
- [x] Village defence mechanic  

### Phase 2 — Game Feel (next)
- [ ] Sound effects (Unity AudioClip runtime generation or import)  
- [ ] Controller rumble on hit  
- [ ] Village destruction game-over  
- [ ] Totem healing mechanic  
- [ ] Day/night cycle  

### Phase 3 — Content & Polish
- [ ] Wave progression scaling  
- [ ] Score / combo system  
- [ ] Menu screen  
- [ ] Consider outline post-processing for Spider-Verse feel  
- [ ] More enemy types (flying enemies, siege enemies)  
- [ ] Island exploration rewards (hidden caves, power-ups)  

---

## 🤖 Notes for AI Assistants
- **Always read this file first** before making changes — it's the source of truth  
- All gameplay numbers are in this doc; don't hardcode different values without updating here  
- The scene is **100% code-driven** — modifying `.unity` scene files directly will likely be overwritten by `GameBootstrapper.Start()`  
- Palette colours are defined as static fields on `GameBootstrapper` — reference those, don't use ad-hoc `Color` literals  
- When adding new scripts, drop them in the appropriate subfolder and register in the Status table above  
- HDRP shader is `"HDRP/Lit"` with fallback `"Standard"` — use `GameBootstrapper.GetHDRPLitShader()` helper  
- Input uses **Unity's new Input System** (`com.unity.inputsystem 1.18.0`) — use `Gamepad.current`, `Gamepad.all`, `InputAction`  
- **Island terrain is a MeshCollider** — raycasts work for height finding. Use `Physics.Raycast(pos + Vector3.up * 100, Vector3.down, ...)`  
- **Enemies spawn at 160m radius** (ocean edge) and walk inland. Intercept them before they reach the 25m village perimeter  
- **Villager scale ~0.15m** — don't accidentally make them player-sized  
