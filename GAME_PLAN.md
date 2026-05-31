# SharpOpenGL — Space RTS Game Development Plan

## Executive Summary

A top-down (with adjustable vertical perspective) real-time strategy game set in space. Built on the SharpOpenGL engine (C# + OpenTK desktop / WebGL2 browser). Features hero ships, squadrons, base building, 4 resource types, grid-based maps, mission design, and future multiplayer. Targets both desktop and mobile with touch-friendly controls.

---

## Table of Contents

1. [Game Vision](#1-game-vision)
2. [Architecture Overview](#2-architecture-overview)
3. [Directory Structure](#3-directory-structure)
4. [Phase 0 — Engine Foundation](#4-phase-0--engine-foundation)
5. [Phase 1 — Core Game Systems](#5-phase-1--core-game-systems)
6. [Phase 2 — Entities & Units](#6-phase-2--entities--units)
7. [Phase 3 — Map & Grid System](#7-phase-3--map--grid-system)
8. [Phase 4 — Resources & Economy](#8-phase-4--resources--economy)
9. [Phase 5 — Combat & Abilities](#9-phase-5--combat--abilities)
10. [Phase 6 — UI & HUD](#10-phase-6--ui--hud)
11. [Phase 7 — Mission System](#11-phase-7--mission-system)
12. [Phase 8 — Visual Design Pipeline](#12-phase-8--visual-design-pipeline)
13. [Phase 9 — Mobile & Input](#13-phase-9--mobile--input)
14. [Phase 10 — Audio](#14-phase-10--audio)
15. [Phase 11 — Multiplayer Foundation](#15-phase-11--multiplayer-foundation)
16. [Phase 12 — Polish & Shipping](#16-phase-12--polish--shipping)
17. [Data-Driven Design Principles](#17-data-driven-design-principles)
18. [Agent Collaboration Guidelines](#18-agent-collaboration-guidelines)
19. [Master Checklist](#19-master-checklist)

---

## 1. Game Vision

| Attribute | Detail |
|-----------|--------|
| Genre | Real-Time Strategy (RTS) |
| Setting | Outer space — nebulae, asteroid fields, space stations |
| Perspective | Top-down with adjustable camera height (pseudo-3D tilt) |
| Platforms | Desktop (Windows/Linux/macOS via OpenTK), Browser (WebGL2), Mobile (touch) |
| Core Loop | Explore → Gather resources → Build fleet → Complete missions → Expand |
| Hero Unit | A single upgradeable hero spaceship |
| Squads | Groups of units that move/attack in formation |
| Resources | 4 types (see Phase 4) |
| Expandability | Data-driven entities, JSON/YAML definitions, modular systems |

---

## 2. Architecture Overview

```
┌─────────────────────────────────────────────────────┐
│                    Game Application                  │
├──────────┬──────────┬──────────┬────────────────────┤
│  Scenes  │    UI    │  Audio   │   Networking       │
├──────────┴──────────┴──────────┴────────────────────┤
│                  Game Systems Layer                  │
│  (ECS, Resources, Combat, AI, Missions, Squads)     │
├─────────────────────────────────────────────────────┤
│                  Engine Core Layer                   │
│  (Renderer, Camera, Input, Grid, Asset Pipeline)    │
├─────────────────────────────────────────────────────┤
│           Platform Abstraction Layer                 │
│  (OpenTK Desktop | WebGL2 Browser | Touch Input)    │
└─────────────────────────────────────────────────────┘
```

### Key Design Patterns

- **Entity-Component-System (ECS)**: All game objects are entities with attached components. Systems process components each frame.
- **Data-Driven Definitions**: Ships, units, bases, missions defined in JSON files — no code changes needed to add content.
- **Default Fallback Object**: Any undefined entity renders as a stock default placeholder (colored wireframe cube) so the game never crashes on missing assets.
- **Scene Graph**: Hierarchical scene management for menus, gameplay, loading screens.
- **Event Bus**: Decoupled communication between systems via typed events.

---

## 3. Directory Structure

```
SharpOpenGl/
├── SharpOpenGl.sln
├── GAME_PLAN.md                    # This file
├── docs/                           # WebGL2 browser build
│   ├── index.html
│   └── engine.js
├── SharpOpenGl/                    # Desktop engine (existing)
│   ├── Program.cs
│   ├── EngineWindow.cs
│   ├── Camera.cs
│   ├── InputHandler.cs
│   └── Environment/
├── SharpOpenGl.Engine/             # NEW — Shared engine core library
│   ├── ECS/
│   │   ├── Entity.cs
│   │   ├── Component.cs
│   │   ├── System.cs
│   │   └── World.cs
│   ├── Rendering/
│   │   ├── Renderer.cs
│   │   ├── ShaderManager.cs
│   │   ├── MeshBuilder.cs
│   │   ├── SpriteRenderer.cs
│   │   └── DefaultObject.cs
│   ├── Input/
│   │   ├── IInputProvider.cs
│   │   ├── KeyboardInput.cs
│   │   ├── TouchInput.cs
│   │   └── InputAction.cs
│   ├── Grid/
│   │   ├── GridSystem.cs
│   │   ├── GridCell.cs
│   │   └── Pathfinding.cs
│   ├── Camera/
│   │   ├── RTSCamera.cs
│   │   └── CameraController.cs
│   ├── Assets/
│   │   ├── AssetManager.cs
│   │   ├── JsonLoader.cs
│   │   └── DefaultAssets.cs
│   ├── Events/
│   │   ├── EventBus.cs
│   │   └── GameEvents.cs
│   └── Math/
│       └── GridMath.cs
├── SharpOpenGl.Game/               # NEW — Game logic library
│   ├── Entities/
│   │   ├── Components/
│   │   │   ├── TransformComponent.cs
│   │   │   ├── RenderComponent.cs
│   │   │   ├── HealthComponent.cs
│   │   │   ├── MovementComponent.cs
│   │   │   ├── WeaponComponent.cs
│   │   │   ├── ResourceStorageComponent.cs
│   │   │   ├── SquadMemberComponent.cs
│   │   │   └── HeroComponent.cs
│   │   ├── Systems/
│   │   │   ├── MovementSystem.cs
│   │   │   ├── CombatSystem.cs
│   │   │   ├── ResourceSystem.cs
│   │   │   ├── SquadSystem.cs
│   │   │   ├── AISystem.cs
│   │   │   └── SelectionSystem.cs
│   │   └── Factories/
│   │       ├── ShipFactory.cs
│   │       ├── BaseFactory.cs
│   │       └── UnitFactory.cs
│   ├── Resources/
│   │   ├── ResourceManager.cs
│   │   ├── ResourceType.cs
│   │   └── ResourceNode.cs
│   ├── Combat/
│   │   ├── DamageCalculator.cs
│   │   ├── ProjectileSystem.cs
│   │   └── AbilitySystem.cs
│   ├── Missions/
│   │   ├── MissionLoader.cs
│   │   ├── MissionState.cs
│   │   ├── ObjectiveSystem.cs
│   │   └── TriggerSystem.cs
│   ├── Squads/
│   │   ├── SquadManager.cs
│   │   ├── Formation.cs
│   │   └── SquadAI.cs
│   ├── Maps/
│   │   ├── MapLoader.cs
│   │   ├── MapGenerator.cs
│   │   └── FogOfWar.cs
│   └── Economy/
│       ├── BuildQueue.cs
│       ├── TechTree.cs
│       └── UpgradeSystem.cs
├── SharpOpenGl.UI/                 # NEW — UI system
│   ├── UIManager.cs
│   ├── Widgets/
│   │   ├── Button.cs
│   │   ├── Panel.cs
│   │   ├── ResourceBar.cs
│   │   ├── Minimap.cs
│   │   └── UnitInfoPanel.cs
│   ├── Screens/
│   │   ├── MainMenuScreen.cs
│   │   ├── GameplayHUD.cs
│   │   ├── PauseScreen.cs
│   │   ├── MissionSelectScreen.cs
│   │   └── ShipDesignerScreen.cs
│   └── Touch/
│       ├── VirtualJoystick.cs
│       ├── GestureRecognizer.cs
│       └── TouchHUD.cs
├── SharpOpenGl.Tests/              # NEW — Unit tests
│   ├── ECS/
│   ├── Grid/
│   ├── Combat/
│   └── Resources/
└── GameData/                       # NEW — Data-driven content
    ├── Ships/
    │   ├── hero_default.json
    │   ├── fighter_basic.json
    │   └── _template.json
    ├── Bases/
    │   ├── command_center.json
    │   └── _template.json
    ├── Units/
    │   ├── drone_worker.json
    │   └── _template.json
    ├── Missions/
    │   ├── tutorial_01.json
    │   └── _template.json
    ├── Maps/
    │   ├── sector_alpha.json
    │   └── _template.json
    └── Config/
        ├── resources.json
        ├── balance.json
        └── controls.json
```

---

## 4. Phase 0 — Engine Foundation ✅

**Completed.** Modular engine core library (`SharpOpenGl.Engine`) with `ShaderManager`, `IRenderer`, `MeshBuilder`, `DefaultObject`, `AssetManager`, `EventBus`, `IInputProvider`, and xUnit test project.

---

## 5. Phase 1 — Core Game Systems ✅

**Completed.** ECS framework: `Entity` (ID + generation), `ComponentPool<T>`, `World`, `GameSystem`, `TransformComponent`, `RenderComponent`, `RenderSystem`, `SceneManager`, game state machine.

---

## 6. Phase 2 — Entities & Units ✅

**Completed.** Entity types with JSON-driven definitions: `HeroComponent`, `MovementComponent`, `HealthComponent`, `WeaponComponent`, `SquadMemberComponent`, `BuildingComponent`. Factories: `ShipFactory`, `BaseFactory`, `UnitFactory`. Default fallback rendering for missing assets.

---

## 7. Phase 3 — Map & Grid System ✅

**Completed.** Spatial grid (`GridCell`, `GridSystem`), coordinate conversion, A* and flow-field pathfinding, `MapLoader`, `MapGenerator`, `FogOfWar`, height layers, `RTSCamera` with adjustable tilt/zoom.

---

## 8. Phase 4 — Resources & Economy ✅

**Completed.** 4 resource types (Energy, Minerals, Data, Crew), `ResourceManager`, `ResourceNode`, `ResourceCollectorComponent`, `ResourceSystem`, `BuildQueue`, node depletion, `resources.json` config.

---

## 9. Phase 5 — Combat & Abilities ✅

**Completed.** `CombatSystem`, `DamageCalculator` (armor/shield formula), `ProjectileSystem` (instant/linear/homing/AoE), unit death handling, `AbilitySystem` with cooldowns, targeting AI, squad combat behaviors, `balance.json`.

---

## 10. Phase 6 — UI & HUD ✅

**Completed.** `UIManager`, widget hierarchy (`Button`, `Panel`), screens (`MainMenuScreen`, `GameplayHUD`, `ResourceBar`, `Minimap`, `UnitInfoPanel`, `PauseScreen`, `MissionSelectScreen`, `ShipDesignerScreen`), UI scaling and anchoring.

---

## 11. Phase 7 — Mission System ✅

**Completed.** `MissionState` (phase tracking, objective progress, trigger progress, entity tag registry), `ObjectiveSystem` (destroy_target, survive_time, reach_area, collect, condition), `TriggerSystem` (timer, area_enter, kill_count, resource_threshold; scripted actions: spawn_units, dialog, camera_pan), `MissionController` (start, rewards, replay), `BriefingScreen` UI.

### Mission Structure

- Pre-mission briefing (narrative text, objectives preview)
- Objectives: primary (must complete) and secondary (optional, bonus rewards)
- Triggers: spatial (enter area), temporal (timer), conditional (kill count, resource threshold)
- Scripted events: spawn waves, dialog, camera moves, reinforcements
- Victory/defeat conditions
- Rewards: resources, XP, ship unlocks

### Tasks

- [x] Implement `MissionLoader` — parse mission JSON into runtime state
- [ ] Implement `MissionState` — tracks current objectives, progress, completion
- [ ] Implement `ObjectiveSystem` — evaluates objective conditions each frame
- [ ] Implement objective types (destroy target, escort, survive time, collect, reach area)
- [ ] Implement `TriggerSystem` — evaluates trigger conditions, fires events
- [ ] Implement trigger types (area enter, timer, kill count, resource threshold, custom)
- [ ] Implement scripted event execution (spawn units, show dialog, camera pan)
- [ ] Implement mission briefing screen integration
- [ ] Implement mission rewards distribution
- [ ] Implement mission replay (restart with same conditions)
- [ ] Create `tutorial_01.json` — first tutorial mission
- [ ] Create `_template.json` mission template with all fields documented
- [ ] Write mission state machine tests

### Mission JSON Template

```json
{
  "id": "tutorial_01",
  "displayName": "First Contact",
  "description": "Learn the basics of fleet command.",
  "map": "sector_alpha",
  "briefing": {
    "text": "Commander, sensors have detected...",
    "objectives_preview": ["Destroy the enemy scout", "Protect your base"]
  },
  "startConditions": {
    "playerSpawn": [5, 5],
    "startingUnits": ["hero_default"],
    "startingResources": { "energy": 500, "minerals": 300, "data": 0, "crew": 10 }
  },
  "objectives": {
    "primary": [
      {
        "id": "destroy_scout",
        "type": "destroy_target",
        "target": "enemy_scout_1",
        "description": "Destroy the enemy scout ship"
      }
    ],
    "secondary": [
      {
        "id": "no_damage",
        "type": "condition",
        "condition": "hero.health == hero.maxHealth",
        "description": "Complete without taking damage"
      }
    ]
  },
  "triggers": [
    {
      "id": "spawn_wave_1",
      "condition": { "type": "timer", "seconds": 30 },
      "actions": [
        { "type": "spawn_units", "units": ["fighter_basic", "fighter_basic"], "position": [50, 50] },
        { "type": "dialog", "speaker": "AI", "text": "Incoming hostiles detected!" }
      ]
    }
  ],
  "victory": { "type": "all_primary_complete" },
  "defeat": { "type": "hero_destroyed" },
  "rewards": {
    "resources": { "energy": 200, "data": 50 },
    "xp": 100,
    "unlocks": ["fighter_advanced"]
  }
}
```

### Acceptance Criteria

- Missions load and objectives track in real-time
- Triggers fire at correct conditions
- Victory/defeat correctly detected
- Mission rewards applied on completion
- Missions replayable

---

## 12. Phase 8 — Visual Design Pipeline ✅

**Goal**: Enable custom ship/base/unit visual design with a workflow that supports iterative art.

### Design Approach

- Procedural geometry for prototyping (parameterized shapes)
- Simple mesh format support (.obj) for custom models
- Color/material system (base color, emissive for engines/weapons)
- Particle effects for engines, explosions, shields
- Ship silhouettes for minimap/icons auto-generated from mesh
- All visuals swappable via JSON — change `"mesh"` field, reload

### Tasks

- [x] Implement `.obj` mesh loader (vertices, normals, basic materials)
- [x] Implement procedural ship generator (hull from parameters: length, width, wing angle)
- [x] Implement material system (diffuse color, emissive color, opacity)
- [x] Implement particle system (emitter, lifetime, velocity, color over time)
- [x] Implement engine trail particles
- [x] Implement explosion effect
- [x] Implement shield bubble effect (translucent sphere)
- [x] Implement weapon fire visuals (laser line, missile trail)
- [x] Implement `ShipDesignerScreen` rendering (rotate model, change colors)
- [x] Implement mesh LOD system (detailed close, simple far, icon strategic)
- [x] Implement sprite/billboard fallback for very far zoom
- [x] Create default meshes: default_ship, default_base, default_projectile
- [x] Document visual design workflow in `docs/VISUAL_DESIGN.md`

### Acceptance Criteria

- Custom .obj files render correctly
- Procedural ships generate reasonable shapes from parameters
- Particles render for engines and explosions
- Ship designer allows rotating and recoloring models
- Default objects display when custom mesh is missing

---

## 13. Phase 9 — Mobile & Input ✅

**Goal**: Full mobile support with touch controls, responsive UI, and performance optimization.

### Input Mapping

| Action | Desktop | Mobile |
|--------|---------|--------|
| Pan camera | Middle-drag / Edge scroll / WASD | Two-finger drag |
| Zoom | Scroll wheel | Pinch |
| Select unit | Left click | Tap |
| Multi-select | Left drag box | Tap + hold + drag |
| Move command | Right click | Double-tap location |
| Attack command | A + right click | Long press enemy |
| Ability | Number keys / click HUD | Tap ability button |
| Build menu | B key / click base | Tap base → build panel |
| Camera height | Z/X keys | Slider on HUD |

### Delivered

- `TouchPoint`, `GestureType`, `GestureEvent` — core touch data types
- `GestureRecognizer` — tap, double-tap, long-press, drag, two-finger drag, pinch
- `TouchInput` — `IInputProvider` implementation backed by gesture recognition
- `VirtualJoystick` — on-screen touch joystick widget
- `AdaptiveLayout` — viewport classification (Desktop / Tablet / Phone) with minimum 44 px touch targets
- `PerformanceProfile` + `PerformanceTier` — quality scaling (High / Medium / Low) for particles, LOD, draw distance, and shader complexity
- `docs/CONTROLS.md` — full control scheme documentation for all platforms

---

## 14. Phase 10 — Audio ✅

**Completed.** `IAudioManager` interface, `AudioSettings` (clamped volume properties, effective-gain helpers), `AudioEventType` enum (13 event types), `NullAudioManager` (no-op, safe for tests/headless), `OpenAlAudioManager` (pooled AL sources, positional playback, looping music with crossfade, lazy placeholder-buffer generation), `PlaceholderSoundGenerator` (PCM tone/sweep/noise generators), audio events in `EventBus` (`SoundRequestedEvent`, `MusicRequestedEvent`, `MusicStopRequestedEvent`, `VolumeChangedEvent`), and `WebAudioManager` JS class for the browser build.

### Tasks

- [x] Design audio system interface (`IAudioManager`)
- [x] Implement WebAudio backend for browser
- [x] Implement OpenAL backend for desktop (or NAudio)
- [x] Implement sound effect playback (positional, pooled)
- [x] Implement music playback (looping, crossfade)
- [x] Define audio events in event bus (weapon fire, explosion, UI click)
- [x] Create placeholder sound effects (procedural beeps/boops)
- [x] Implement volume controls in settings

### Acceptance Criteria

- Audio plays without stuttering
- Positional audio works relative to camera
- Volume controls functional
- Works on both desktop and mobile browser

---

## 15. Phase 11 — Multiplayer Foundation ✅

**Completed.** Command pattern (`IGameCommand`, `MoveCommand`, `AttackCommand`, `BuildCommand`, `StopCommand`, `UseAbilityCommand`, `CommandSerializer`, `CommandQueue`), `DeterministicClock` (fixed-timestep, drift-free), `ReplayRecorder`/`ReplayPlayer` (full session recording and playback), `NetworkMessage` protocol envelope with `NetworkMessageType`, `LobbyRoom`/`LobbyPlayer` (room lifecycle, readiness, game-start handshake), and `LocalGameSession` (two-player local split-test harness). Architecture documented in `docs/MULTIPLAYER.md`.

---

## 16. Phase 12 — Polish & Shipping

**Goal**: Final polish, performance, and release preparation.

### Tasks

- [ ] Performance profiling and optimization pass
- [ ] Memory leak audit
- [ ] Loading screen with progress bar
- [ ] Save/load game state (serialize world to JSON)
- [ ] Settings persistence (localStorage for web, file for desktop)
- [ ] Error handling and crash recovery
- [ ] Analytics hooks (optional, privacy-respecting)
- [ ] Accessibility features (colorblind modes, font scaling)
- [ ] Final balance pass on all units/resources
- [ ] Create 3–5 complete missions
- [ ] Write player-facing documentation / tutorial
- [ ] CI/CD pipeline for automated builds and deployment
- [ ] WebGL2 production build with minification
- [ ] Desktop builds for Windows/Linux/macOS

### Acceptance Criteria

- No crashes in normal gameplay
- Load times under 3 seconds
- Saves and loads correctly
- All missions completable
- Deployed and accessible

---

## 17. Data-Driven Design Principles

All game content is defined in JSON files under `GameData/`. This enables:

1. **Non-programmers** can add/modify content by editing JSON
2. **Future agents** can generate content without touching engine code
3. **Hot-reload** during development (reload JSON without restart)
4. **Modding support** (players can add custom content later)

### Rules

- Every entity type has a `_template.json` documenting all fields
- Missing fields use sensible defaults from code
- Unknown fields are ignored (forward compatibility)
- All file references (meshes, textures) fall back to `DefaultObject` if not found
- Validation tool checks JSON against schema before game loads

---

## 18. Agent Collaboration Guidelines

This project will be built across many agentic sessions. Follow these rules:

### For Any Agent Working on This Project

1. **Read this file first** — understand the full plan before making changes
2. **Check the Master Checklist** (Section 19) — see what's done and what's next
3. **Work on one phase/section at a time** — don't try to do everything
4. **Update the checklist** when you complete items (mark `[x]`)
5. **Keep files small** — no file over 300 lines; split into focused modules
6. **Follow the directory structure** — put code where it belongs
7. **Write tests** for any logic you implement
8. **Document public APIs** with XML comments (C#) or JSDoc (JS)
9. **Use the interfaces** — implement against interfaces, not concrete classes
10. **Don't break existing functionality** — run tests before and after changes
11. **Commit with descriptive messages** — future agents read git history

### Token Efficiency Guidelines

- Each file should have a single clear responsibility
- Keep class files under 200 lines where possible
- Use descriptive file names that indicate purpose
- Group related small files in dedicated folders
- Prefer composition over deep inheritance (less context needed)
- JSON configs are self-documenting (field names are the docs)

### Priority Order for New Agents

If unsure what to work on, follow this priority:
1. Unfinished items in the earliest incomplete phase
2. Bug fixes in completed phases
3. Tests for completed code
4. Documentation updates

---

## 19. Master Checklist

### Phase 0 — Engine Foundation
- [x] Create `SharpOpenGl.Engine` class library project
- [x] Extract shader management into `ShaderManager`
- [x] Create `IRenderer` interface
- [x] Implement `MeshBuilder`
- [x] Implement `DefaultObject`
- [x] Implement `AssetManager` with JSON loading
- [x] Create `EventBus`
- [x] Add `IInputProvider` abstraction
- [x] Set up test project
- [x] Verify desktop app works through new library
- [x] Document WebGL2 parity plan

### Phase 1 — Core Game Systems (ECS)
- [x] Implement `Entity` ID system
- [x] Implement `ComponentPool<T>`
- [x] Implement `World` container
- [x] Implement `GameSystem` base class
- [x] Create `TransformComponent`
- [x] Create `RenderComponent`
- [x] Implement `RenderSystem`
- [x] Implement `SceneManager`
- [x] Implement game state machine
- [x] Write ECS unit tests

### Phase 2 — Entities & Units
- [x] Define `HeroComponent`
- [x] Define `MovementComponent`
- [x] Define `HealthComponent`
- [x] Define `WeaponComponent`
- [x] Define `SquadMemberComponent`
- [x] Define `BuildingComponent`
- [x] Implement `ShipFactory`
- [x] Implement `BaseFactory`
- [x] Implement `UnitFactory`
- [x] Create entity JSON templates
- [x] Implement Default Fallback rendering
- [x] Write factory unit tests

### Phase 3 — Map & Grid
- [x] Implement `GridCell`
- [x] Implement `GridSystem`
- [x] Implement coordinate conversion
- [x] Implement A* pathfinding
- [x] Implement flow-field pathfinding
- [x] Implement `MapLoader`
- [x] Implement `MapGenerator`
- [x] Implement `FogOfWar`
- [x] Implement height layers
- [x] Implement `RTSCamera`
- [x] Implement camera height adjustment
- [x] Create sample map JSON
- [x] Write pathfinding tests
- [x] Write grid coordinate conversion tests

### Phase 4 — Resources & Economy
- [x] Define `ResourceType` enum
- [x] Implement `ResourceManager`
- [x] Implement `ResourceNode` component
- [x] Implement `ResourceCollectorComponent`
- [x] Implement `ResourceSystem`
- [x] Implement `BuildQueue`
- [x] Implement resource UI data
- [x] Create `resources.json` config
- [x] Implement node depletion
- [x] Write economy tests

### Phase 5 — Combat & Abilities
- [x] Implement `CombatSystem`
- [x] Implement `DamageCalculator`
- [x] Implement `ProjectileSystem`
- [x] Implement projectile types
- [x] Implement unit death handling
- [x] Implement `AbilitySystem`
- [x] Implement ability types
- [x] Implement targeting AI
- [x] Implement squad combat behavior
- [x] Create `balance.json`
- [x] Write combat tests

### Phase 6 — UI & HUD
- [x] Implement `UIManager`
- [x] Implement widget base class
- [x] Implement `Button` widget
- [x] Implement `Panel` widget
- [x] Implement `MainMenuScreen`
- [x] Implement `GameplayHUD`
- [x] Implement `ResourceBar`
- [x] Implement `Minimap`
- [x] Implement `UnitInfoPanel`
- [x] Implement `PauseScreen`
- [x] Implement `MissionSelectScreen`
- [x] Implement `ShipDesignerScreen`
- [x] Implement UI scaling
- [x] Implement UI anchoring
- [x] Write UI tests

### Phase 7 — Mission System
- [x] Implement `MissionLoader`
- [x] Implement `MissionState`
- [x] Implement `ObjectiveSystem`
- [x] Implement objective types
- [x] Implement `TriggerSystem`
- [x] Implement trigger types
- [x] Implement scripted events
- [x] Implement briefing screen integration
- [x] Implement rewards distribution
- [x] Implement mission replay
- [x] Create tutorial mission JSON
- [x] Create mission template
- [x] Write mission tests

### Phase 8 — Visual Design
- [x] Implement `.obj` mesh loader
- [x] Implement procedural ship generator
- [x] Implement material system
- [x] Implement particle system
- [x] Implement engine trails
- [x] Implement explosions
- [x] Implement shield effects
- [x] Implement weapon visuals
- [x] Implement ship designer rendering
- [x] Implement LOD system
- [x] Implement sprite fallback
- [x] Create default meshes
- [x] Document visual workflow

### Phase 9 — Mobile & Input
- [x] Implement `TouchInput`
- [x] Implement `GestureRecognizer`
- [x] Implement `VirtualJoystick`
- [x] Implement touch-to-action mapping
- [x] Implement adaptive UI layouts
- [x] Implement minimum touch targets
- [x] Implement touch-friendly buttons
- [x] Implement mobile camera controls
- [x] Implement performance scaling
- [x] Implement WebGL2 mobile optimizations
- [x] Test on multiple screen sizes
- [x] Document control schemes

### Phase 10 — Audio
- [x] Design `IAudioManager` interface
- [x] Implement WebAudio backend
- [x] Implement desktop audio backend
- [x] Implement sound effects
- [x] Implement music playback
- [x] Define audio events
- [x] Create placeholder sounds
- [x] Implement volume controls

### Phase 11 — Multiplayer Foundation
- [x] Design network protocol
- [x] Implement command pattern
- [x] Implement deterministic game loop
- [x] Implement replay system
- [x] Design lobby architecture
- [x] Document multiplayer architecture
- [x] Implement local test mode
- [x] Eliminate non-determinism

### Phase 12 — Polish & Shipping
- [ ] Performance profiling
- [ ] Memory leak audit
- [ ] Loading screens
- [ ] Save/load system
- [ ] Settings persistence
- [ ] Error handling
- [ ] Accessibility features
- [ ] Final balance pass
- [ ] Create 3–5 missions
- [ ] Player documentation
- [ ] CI/CD pipeline
- [ ] WebGL2 production build
- [ ] Desktop platform builds

---

## Appendix: Technology Stack

| Layer | Desktop | Browser |
|-------|---------|---------|
| Language | C# (.NET 8) | JavaScript (ES2020+) |
| Graphics | OpenTK 4.8 / OpenGL 3.3+ | WebGL2 |
| Window | OpenTK GameWindow | Canvas |
| Input | OpenTK Keyboard/Mouse | DOM Events + Touch API |
| Audio | OpenAL / NAudio | Web Audio API |
| Networking | TCP/UDP sockets | WebSocket / WebRTC |
| Data | System.Text.Json | Native JSON |
| Testing | xUnit | Jest (future) |
| CI/CD | GitHub Actions | GitHub Pages |

---

*Last Updated: 2026-05-31*
*Status: Phase 11 (Multiplayer Foundation) Complete — Ready for Phase 12 (Polish & Shipping)*
