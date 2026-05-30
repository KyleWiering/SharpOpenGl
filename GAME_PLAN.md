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

## 4. Phase 0 — Engine Foundation

**Goal**: Refactor the existing engine into a modular, reusable core library that both desktop and WebGL targets consume.

### Tasks

- [x] Create `SharpOpenGl.Engine` class library project
- [x] Extract shader management into `ShaderManager` class
- [x] Create `IRenderer` interface for platform-agnostic rendering
- [x] Implement `MeshBuilder` for procedural geometry (quads, grids, lines)
- [x] Create `DefaultObject` — a wireframe cube/diamond rendered when no asset is defined
- [x] Implement `AssetManager` with JSON loading
- [x] Create `EventBus` for decoupled system communication
- [x] Add `IInputProvider` abstraction (keyboard, touch, gamepad)
- [x] Set up `SharpOpenGl.Tests` project with xUnit
- [x] Ensure existing desktop app still works via the new library
- [x] Ensure WebGL2 `docs/engine.js` parity plan is documented

### Acceptance Criteria

- Desktop app renders same scene through new engine library
- Unit tests pass for math, grid, and ECS utilities
- `DefaultObject` renders correctly when no mesh is specified

---

## 5. Phase 1 — Core Game Systems

**Goal**: Implement the Entity-Component-System framework and foundational game loop.

### ECS Design

```
Entity = uint ID
Component = struct/class with data only
System = processes all entities with matching component sets each frame
World = container for all entities, components, and systems
```

### Tasks

- [x] Implement `Entity` (lightweight ID + generation counter)
- [x] Implement `ComponentPool<T>` (sparse set or dictionary per type)
- [x] Implement `World` (entity creation/destruction, system registration)
- [x] Implement base `GameSystem` class with `Update(float deltaTime)`
- [x] Create `TransformComponent` (Position, Rotation, Scale in 3D)
- [x] Create `RenderComponent` (MeshId, Color, Visible flag)
- [x] Implement `RenderSystem` that draws all entities with RenderComponent
- [x] Implement `SceneManager` (load/unload scenes, transition handling)
- [x] Implement game state machine (Menu → Loading → Playing → Paused)
- [x] Write unit tests for ECS operations

### Acceptance Criteria

- Can create 1000 entities with transforms, update them at 60fps
- Scene transitions work (menu → game → pause → menu)
- ECS tests cover create/destroy/query operations

---

## 6. Phase 2 — Entities & Units

**Goal**: Define the entity types for the game — hero ship, squadron units, bases, and resource nodes.

### Hero Ship

- Single player-controlled capital ship
- Upgradeable (weapons, shields, speed, abilities)
- Persists across missions
- Has unique abilities (special attacks, buffs)

### Squadron Units

- Groups of 3–12 smaller ships
- Formation-based movement (V, line, circle, custom)
- Shared health pool or individual HP (configurable)
- Auto-attack with basic AI

### Bases / Structures

- Stationary buildings on grid cells
- Production queues (build units over time)
- Resource collection radius
- Defensive capabilities (turrets)

### Tasks

- [ ] Define `HeroComponent` (level, XP, ability slots, upgrade tree reference)
- [ ] Define `MovementComponent` (speed, acceleration, turnRate, pathTarget)
- [ ] Define `HealthComponent` (currentHP, maxHP, shields, armor)
- [ ] Define `WeaponComponent` (damage, range, fireRate, projectileType)
- [ ] Define `SquadMemberComponent` (squadId, formationSlot, formationOffset)
- [ ] Define `BuildingComponent` (buildingType, buildQueue, productionRate)
- [ ] Implement `ShipFactory` — reads JSON definition, creates entity with all components
- [ ] Implement `BaseFactory` — reads JSON, places building on grid
- [ ] Implement `UnitFactory` — generic factory for any unit type
- [ ] Create `hero_default.json` template with all configurable fields
- [ ] Create `fighter_basic.json` template
- [ ] Create `command_center.json` template
- [ ] Implement **Default Fallback**: if JSON references undefined mesh/texture, use `DefaultObject`
- [ ] Write unit tests for factories

### JSON Entity Template Example

```json
{
  "id": "hero_default",
  "displayName": "Vanguard",
  "category": "hero",
  "mesh": "meshes/hero_vanguard.obj",
  "fallbackMesh": "default",
  "components": {
    "health": { "maxHP": 1000, "shields": 500, "armor": 50 },
    "movement": { "speed": 120, "acceleration": 60, "turnRate": 90 },
    "weapons": [
      { "slot": 0, "type": "laser", "damage": 25, "range": 300, "fireRate": 4.0 },
      { "slot": 1, "type": "missile", "damage": 100, "range": 600, "fireRate": 0.5 }
    ],
    "abilities": [
      { "slot": 0, "id": "shield_boost", "cooldown": 30 },
      { "slot": 1, "id": "emp_burst", "cooldown": 60 }
    ]
  },
  "upgrades": {
    "tree": "tech_trees/hero_vanguard.json"
  }
}
```

### Acceptance Criteria

- Hero ship spawns from JSON, renders with correct components
- Undefined meshes fall back to default object
- Squadrons form up in configured formation
- Bases place on grid correctly

---

## 7. Phase 3 — Map & Grid System

**Goal**: Implement the spatial grid, maps, pathfinding, and fog of war.

### Grid Design

- Hexagonal or square grid (configurable per map — start with square)
- Each cell has: terrain type, occupancy, resource node, visibility state
- Multiple vertical layers (surface, orbital) for the 3D height aspect
- Map sizes: Small (32×32), Medium (64×64), Large (128×128)

### Camera Height / 3D Perspective

- Camera positioned above the plane looking down at an angle (30–60° tilt)
- Player can zoom in/out (adjusting height + angle)
- At closest zoom: near-isometric view showing ship detail
- At farthest zoom: strategic overview, units become icons
- Height layers allow units to be "above" or "below" the main plane

### Tasks

- [ ] Implement `GridCell` (position, terrain, occupant, layer, fogState)
- [ ] Implement `GridSystem` (create grid, get neighbors, coordinate conversion)
- [ ] Implement world-to-grid and grid-to-world coordinate mapping
- [ ] Implement A* pathfinding on grid (with terrain costs)
- [ ] Implement flow-field pathfinding for large groups
- [ ] Implement `MapLoader` — load map from JSON (terrain layout, spawn points, resources)
- [ ] Implement `MapGenerator` — procedural generation with seed
- [ ] Implement `FogOfWar` — per-player visibility based on unit sight ranges
- [ ] Implement height layers (orbital layer above surface layer)
- [ ] Implement `RTSCamera` — top-down with tilt, zoom, pan, edge-scroll
- [ ] Implement camera height adjustment (Z-axis movement for perspective shift)
- [ ] Create `sector_alpha.json` sample map
- [ ] Write pathfinding unit tests
- [ ] Write grid coordinate conversion tests

### Map JSON Template

```json
{
  "id": "sector_alpha",
  "displayName": "Sector Alpha",
  "gridSize": [64, 64],
  "layers": ["surface", "orbital"],
  "terrain": {
    "default": "space",
    "regions": [
      { "type": "asteroid_field", "cells": [[10,10], [10,11], [11,10]] },
      { "type": "nebula", "rect": [20, 20, 30, 30] }
    ]
  },
  "spawnPoints": [
    { "player": 1, "position": [5, 5], "layer": "surface" },
    { "player": 2, "position": [58, 58], "layer": "surface" }
  ],
  "resourceNodes": [
    { "type": "energy", "position": [15, 15], "amount": 5000 },
    { "type": "minerals", "position": [45, 20], "amount": 3000 }
  ]
}
```

### Acceptance Criteria

- Grid renders visually with cell borders
- Pathfinding finds valid paths around obstacles
- Camera zoom changes perspective angle smoothly
- Fog of war hides unexplored areas
- Maps load from JSON successfully

---

## 8. Phase 4 — Resources & Economy

**Goal**: Implement the 4-resource economy system.

### Resource Types

| Resource | Lore Name | Gathered From | Use |
|----------|-----------|---------------|-----|
| **Energy** | Plasma Cores | Solar collectors, star proximity | Powers buildings, shields, abilities |
| **Minerals** | Astrium Ore | Asteroid mining | Ship/base construction |
| **Data** | Quantum Fragments | Derelict scans, research stations | Tech upgrades, special units |
| **Crew** | Personnel | Training facilities, recruitment | Manning ships/stations, hero abilities |

### Economy Design

- Resources stored per-player (global pool)
- Buildings/units cost resources to produce
- Some resources regenerate (energy), others are finite on map (minerals)
- Trade between resource types at a conversion rate (later multiplayer marketplace)
- Upkeep costs for large fleets

### Tasks

- [ ] Define `ResourceType` enum (Energy, Minerals, Data, Crew)
- [ ] Implement `ResourceManager` (per-player storage, income/expense tracking)
- [ ] Implement `ResourceNode` component (type, amount, harvestRate, depleted flag)
- [ ] Implement `ResourceCollectorComponent` (assigned node, carry capacity, deposit target)
- [ ] Implement `ResourceSystem` — processes collection, deposits, income ticks
- [ ] Implement `BuildQueue` — deducts resources on start, refunds on cancel
- [ ] Implement resource UI display data (current/max/income per second)
- [ ] Define `resources.json` config (starting amounts, max storage, conversion rates)
- [ ] Implement resource node depletion and respawn timers
- [ ] Write economy balance tests (ensure no infinite resource exploits)

### Acceptance Criteria

- All 4 resources track correctly
- Workers harvest and deposit
- Buildings cost correct resources
- Insufficient resources prevents build
- Resource nodes deplete and show visual feedback

---

## 9. Phase 5 — Combat & Abilities

**Goal**: Implement the combat system with projectiles, damage, and hero abilities.

### Combat Design

- Real-time combat with auto-attack within range
- Projectile types: instant (laser), travel-time (missile), area (explosion)
- Damage formula: `finalDamage = baseDamage * (100 / (100 + armor)) - shieldAbsorb`
- Unit death → removal from world, drop loot/XP
- Hero abilities with cooldowns and resource costs

### Tasks

- [ ] Implement `CombatSystem` — target acquisition, range checking, attack timing
- [ ] Implement `DamageCalculator` — apply damage formula with armor/shields
- [ ] Implement `ProjectileSystem` — spawn projectiles, movement, collision
- [ ] Implement projectile types (instant, linear, homing, AoE)
- [ ] Implement unit death handling (remove entity, spawn explosion effect, award XP)
- [ ] Implement `AbilitySystem` — hero abilities with cooldowns
- [ ] Implement ability types (buff, damage, heal, summon, area denial)
- [ ] Implement aggro/targeting AI (closest, lowest HP, priority target)
- [ ] Implement squad combat behavior (focus fire, spread, kite)
- [ ] Define `balance.json` with damage/armor/speed tables
- [ ] Write damage calculation tests
- [ ] Write targeting priority tests

### Acceptance Criteria

- Units auto-attack enemies in range
- Projectiles travel and deal damage
- Hero abilities activate with cooldowns
- Units die and are removed cleanly
- Squad focus fire works correctly

---

## 10. Phase 6 — UI & HUD

**Goal**: Full menu system and in-game HUD, mobile-responsive.

### Screen Flow

```
Main Menu
├── New Game → Mission Select → Loading → Gameplay
├── Continue → Loading → Gameplay
├── Ship Designer → (design ships/bases)
├── Settings → (controls, audio, display)
└── Quit

Gameplay HUD
├── Resource Bar (top)
├── Minimap (bottom-left)
├── Unit Info Panel (bottom-center)
├── Ability Bar (bottom-right, hero abilities)
├── Build Menu (context, on base selection)
└── Pause Menu (overlay)
```

### Tasks

- [ ] Implement `UIManager` (screen stack, input routing, render order)
- [ ] Implement UI widget base class (position, size, anchor, visibility)
- [ ] Implement `Button` widget (text, icon, click handler, hover state)
- [ ] Implement `Panel` widget (background, children layout)
- [ ] Implement `MainMenuScreen` (New Game, Continue, Ship Designer, Settings, Quit)
- [ ] Implement `GameplayHUD` — resource bar, minimap, unit info, ability bar
- [ ] Implement `ResourceBar` widget — shows all 4 resources with income rate
- [ ] Implement `Minimap` widget — renders fog-of-war map with unit dots
- [ ] Implement `UnitInfoPanel` — shows selected unit(s) stats
- [ ] Implement `PauseScreen` overlay
- [ ] Implement `MissionSelectScreen` — list missions, preview, start
- [ ] Implement `ShipDesignerScreen` — visual ship/base customization
- [ ] Implement UI scaling for different resolutions
- [ ] Implement UI anchoring system (corners, center, stretch)
- [ ] Write UI layout tests

### Acceptance Criteria

- All screens navigable with keyboard and touch
- HUD displays correct real-time data
- UI scales properly from 720p to 4K
- Touch targets minimum 44px for mobile

---

## 11. Phase 7 — Mission System

**Goal**: Designable missions loaded from data files with objectives, triggers, and scripted events.

### Mission Structure

- Pre-mission briefing (narrative text, objectives preview)
- Objectives: primary (must complete) and secondary (optional, bonus rewards)
- Triggers: spatial (enter area), temporal (timer), conditional (kill count, resource threshold)
- Scripted events: spawn waves, dialog, camera moves, reinforcements
- Victory/defeat conditions
- Rewards: resources, XP, ship unlocks

### Tasks

- [ ] Implement `MissionLoader` — parse mission JSON into runtime state
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

## 12. Phase 8 — Visual Design Pipeline

**Goal**: Enable custom ship/base/unit visual design with a workflow that supports iterative art.

### Design Approach

- Procedural geometry for prototyping (parameterized shapes)
- Simple mesh format support (.obj) for custom models
- Color/material system (base color, emissive for engines/weapons)
- Particle effects for engines, explosions, shields
- Ship silhouettes for minimap/icons auto-generated from mesh
- All visuals swappable via JSON — change `"mesh"` field, reload

### Tasks

- [ ] Implement `.obj` mesh loader (vertices, normals, basic materials)
- [ ] Implement procedural ship generator (hull from parameters: length, width, wing angle)
- [ ] Implement material system (diffuse color, emissive color, opacity)
- [ ] Implement particle system (emitter, lifetime, velocity, color over time)
- [ ] Implement engine trail particles
- [ ] Implement explosion effect
- [ ] Implement shield bubble effect (translucent sphere)
- [ ] Implement weapon fire visuals (laser line, missile trail)
- [ ] Implement `ShipDesignerScreen` rendering (rotate model, change colors)
- [ ] Implement mesh LOD system (detailed close, simple far, icon strategic)
- [ ] Implement sprite/billboard fallback for very far zoom
- [ ] Create default meshes: default_ship, default_base, default_projectile
- [ ] Document visual design workflow in `docs/VISUAL_DESIGN.md`

### Acceptance Criteria

- Custom .obj files render correctly
- Procedural ships generate reasonable shapes from parameters
- Particles render for engines and explosions
- Ship designer allows rotating and recoloring models
- Default objects display when custom mesh is missing

---

## 13. Phase 9 — Mobile & Input

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

### Tasks

- [ ] Implement `TouchInput` provider (tap, double-tap, long-press, drag, pinch)
- [ ] Implement `GestureRecognizer` (classify touch sequences into game actions)
- [ ] Implement `VirtualJoystick` for camera movement (optional)
- [ ] Implement touch-to-game-action mapping via `controls.json`
- [ ] Implement adaptive UI layouts (phone portrait, phone landscape, tablet, desktop)
- [ ] Implement minimum touch target sizes (44px)
- [ ] Implement touch-friendly build/ability buttons (larger, spaced)
- [ ] Implement edge-of-screen detection disabled on mobile (replaced by drag)
- [ ] Implement performance scaling (reduce particles, LOD, draw distance on mobile)
- [ ] Implement WebGL2 mobile optimizations (reduce shader complexity, batch draws)
- [ ] Test on various screen sizes and aspect ratios
- [ ] Document control schemes in `docs/CONTROLS.md`

### Acceptance Criteria

- Game fully playable on mobile browser via touch only
- No accidental inputs from fat-finger touches
- UI readable on 5" phone screens
- Maintains 30fps on mid-range mobile devices
- Controls feel natural and responsive

---

## 14. Phase 10 — Audio

**Goal**: Sound effects and music system (can be deferred, but architecture should be in place).

### Tasks

- [ ] Design audio system interface (`IAudioManager`)
- [ ] Implement WebAudio backend for browser
- [ ] Implement OpenAL backend for desktop (or NAudio)
- [ ] Implement sound effect playback (positional, pooled)
- [ ] Implement music playback (looping, crossfade)
- [ ] Define audio events in event bus (weapon fire, explosion, UI click)
- [ ] Create placeholder sound effects (procedural beeps/boops)
- [ ] Implement volume controls in settings

### Acceptance Criteria

- Audio plays without stuttering
- Positional audio works relative to camera
- Volume controls functional
- Works on both desktop and mobile browser

---

## 15. Phase 11 — Multiplayer Foundation

**Goal**: Architecture for future multiplayer — not full implementation, but the hooks and design.

### Design Decisions (for later implementation)

- Client-server model (authoritative server)
- Lock-step or state-sync (evaluate both, likely state-sync for RTS)
- Lobby system with matchmaking
- Spectator mode
- Replay system (capture all inputs)

### Tasks

- [ ] Design network message protocol (serialized commands)
- [ ] Implement command pattern for all game actions (move, attack, build)
- [ ] Implement deterministic game loop (fixed timestep, no floating point drift)
- [ ] Implement replay system (record/playback command stream)
- [ ] Design lobby/room system architecture
- [ ] Document multiplayer architecture in `docs/MULTIPLAYER.md`
- [ ] Implement local "split" test (two game instances, shared command queue)
- [ ] Identify and eliminate all sources of non-determinism

### Acceptance Criteria

- All game actions go through command system
- Replay correctly reproduces a game session
- Game loop is deterministic (same inputs → same outputs)
- Architecture document complete for future implementers

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
- [ ] Define `HeroComponent`
- [ ] Define `MovementComponent`
- [ ] Define `HealthComponent`
- [ ] Define `WeaponComponent`
- [ ] Define `SquadMemberComponent`
- [ ] Define `BuildingComponent`
- [ ] Implement `ShipFactory`
- [ ] Implement `BaseFactory`
- [ ] Implement `UnitFactory`
- [ ] Create entity JSON templates
- [ ] Implement Default Fallback rendering
- [ ] Write factory unit tests

### Phase 3 — Map & Grid
- [ ] Implement `GridCell`
- [ ] Implement `GridSystem`
- [ ] Implement coordinate conversion
- [ ] Implement A* pathfinding
- [ ] Implement flow-field pathfinding
- [ ] Implement `MapLoader`
- [ ] Implement `MapGenerator`
- [ ] Implement `FogOfWar`
- [ ] Implement height layers
- [ ] Implement `RTSCamera`
- [ ] Implement camera height adjustment
- [ ] Create sample map JSON
- [ ] Write pathfinding tests

### Phase 4 — Resources & Economy
- [ ] Define `ResourceType` enum
- [ ] Implement `ResourceManager`
- [ ] Implement `ResourceNode` component
- [ ] Implement `ResourceCollectorComponent`
- [ ] Implement `ResourceSystem`
- [ ] Implement `BuildQueue`
- [ ] Implement resource UI data
- [ ] Create `resources.json` config
- [ ] Implement node depletion
- [ ] Write economy tests

### Phase 5 — Combat & Abilities
- [ ] Implement `CombatSystem`
- [ ] Implement `DamageCalculator`
- [ ] Implement `ProjectileSystem`
- [ ] Implement projectile types
- [ ] Implement unit death handling
- [ ] Implement `AbilitySystem`
- [ ] Implement ability types
- [ ] Implement targeting AI
- [ ] Implement squad combat behavior
- [ ] Create `balance.json`
- [ ] Write combat tests

### Phase 6 — UI & HUD
- [ ] Implement `UIManager`
- [ ] Implement widget base class
- [ ] Implement `Button` widget
- [ ] Implement `Panel` widget
- [ ] Implement `MainMenuScreen`
- [ ] Implement `GameplayHUD`
- [ ] Implement `ResourceBar`
- [ ] Implement `Minimap`
- [ ] Implement `UnitInfoPanel`
- [ ] Implement `PauseScreen`
- [ ] Implement `MissionSelectScreen`
- [ ] Implement `ShipDesignerScreen`
- [ ] Implement UI scaling
- [ ] Implement UI anchoring
- [ ] Write UI tests

### Phase 7 — Mission System
- [ ] Implement `MissionLoader`
- [ ] Implement `MissionState`
- [ ] Implement `ObjectiveSystem`
- [ ] Implement objective types
- [ ] Implement `TriggerSystem`
- [ ] Implement trigger types
- [ ] Implement scripted events
- [ ] Implement briefing screen integration
- [ ] Implement rewards distribution
- [ ] Implement mission replay
- [ ] Create tutorial mission JSON
- [ ] Create mission template
- [ ] Write mission tests

### Phase 8 — Visual Design
- [ ] Implement `.obj` mesh loader
- [ ] Implement procedural ship generator
- [ ] Implement material system
- [ ] Implement particle system
- [ ] Implement engine trails
- [ ] Implement explosions
- [ ] Implement shield effects
- [ ] Implement weapon visuals
- [ ] Implement ship designer rendering
- [ ] Implement LOD system
- [ ] Implement sprite fallback
- [ ] Create default meshes
- [ ] Document visual workflow

### Phase 9 — Mobile & Input
- [ ] Implement `TouchInput`
- [ ] Implement `GestureRecognizer`
- [ ] Implement `VirtualJoystick`
- [ ] Implement touch-to-action mapping
- [ ] Implement adaptive UI layouts
- [ ] Implement minimum touch targets
- [ ] Implement touch-friendly buttons
- [ ] Implement mobile camera controls
- [ ] Implement performance scaling
- [ ] Implement WebGL2 mobile optimizations
- [ ] Test on multiple screen sizes
- [ ] Document control schemes

### Phase 10 — Audio
- [ ] Design `IAudioManager` interface
- [ ] Implement WebAudio backend
- [ ] Implement desktop audio backend
- [ ] Implement sound effects
- [ ] Implement music playback
- [ ] Define audio events
- [ ] Create placeholder sounds
- [ ] Implement volume controls

### Phase 11 — Multiplayer Foundation
- [ ] Design network protocol
- [ ] Implement command pattern
- [ ] Implement deterministic game loop
- [ ] Implement replay system
- [ ] Design lobby architecture
- [ ] Document multiplayer architecture
- [ ] Implement local test mode
- [ ] Eliminate non-determinism

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

*Last Updated: 2026-05-30*
*Status: Planning Complete — Ready for Phase 0 Implementation*
