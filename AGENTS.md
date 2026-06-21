# SharpOpenGl — AI Agent Context

> **Read this file first.** It is the canonical project map for AI assistants.
> Update it whenever you change architecture, workflows, or key behavior.
> See `.cursor/rules/ai-documentation.mdc` for maintenance rules.

**Repo:** [KyleWiering/SharpOpenGl](https://github.com/KyleWiering/SharpOpenGl)  
**Stack:** .NET 8 · OpenTK 4.8 · xUnit · WebGL2 (GitHub Pages)  
**Default window:** 1024×768 · **UI reference resolution:** 1920×1080

---

## Quick Start (no local C# required)

All build/test/release runs through **GitHub Actions** on push to `master`:

| Workflow | Trigger | What it does |
|----------|---------|--------------|
| `ci.yml` | push/PR → `master` | `dotnet test`, cross-platform publish, WebGL2 minify, GitHub Release |
| `build-and-screenshot.yml` | push → `master` | Headless screenshot + gameplay demo MP4 via `--demo-recording` |
| `deploy-pages.yml` | push → `master` | Deploy `docs/` → [Live Demo](https://kylewiering.github.io/SharpOpenGl/) |

**To verify changes:** push to a branch, open a PR against `master`, check Actions tab.

```bash
# If .NET 8 SDK is available locally:
dotnet restore && dotnet build && dotnet test
dotnet run --project SharpOpenGl
```

---

## Solution Layout

```
SharpOpenGl.sln
├── SharpOpenGl/              # Desktop exe — GameWindow, rendering, input
├── SharpOpenGl.Engine/       # Shared library — ECS, UI, missions, economy
├── SharpOpenGl.Tests/        # xUnit (~776 tests)
├── GameData/                 # JSON content (ships, missions, maps, config)
├── docs/                     # WebGL2 browser build (index.html + engine.js)
├── .github/workflows/        # CI/CD
├── GAME_PLAN.md              # 12-phase master development plan
├── IMPLEMENTATION_PLAN.md    # Feature checklist
└── AGENTS.md                 # This file
```

### Key entry points

| File | Role |
|------|------|
| `SharpOpenGl/Program.cs` | Creates `EngineWindow` 1024×768; `--screenshot` for CI |
| `SharpOpenGl/EngineWindow.cs` | Game loop, scenes, ECS init, UI routing, gameplay input |
| `SharpOpenGl/GLUIRenderer.cs` | OpenGL 2D UI (ortho quads + segment font) |
| `docs/index.html` + `docs/engine.js` | Browser WebGL2 preview (no menus; direct sim) |

---

## Architecture

### Scene flow

```
MainMenu → MissionSelect (galactic star map) → Briefing → Gameplay
         → MultiplayerSetup → Gameplay
Gameplay + PauseScreen overlay
```

`EngineWindow` owns `SceneManager` + `UIManager`. UI clicks are routed **before** gameplay input in `OnMouseDown`.

### ECS (`SharpOpenGl.Engine.ECS`)

- **World** — entity slots, sparse-set component pools, system registry
- **27 components** — Transform, Movement, Render, Health, Race, Weapon, Building, etc.
- **20 systems** — Movement, Combat, ShieldRegen, Build, Resource, AI, FogOfWar, Supply, Squad, etc.
- **Squads** — `SquadSystem` + `SquadMemberComponent`; formations (line/wedge/box/column); G key / ShipControlBar
- **Move routes** — `RouteCommands` → `WaypointQueueComponent` → `AutoMoveSystem` → `DestinationComponent` → `PathFollowingSystem` → `MovementSystem`; shift+right-click appends waypoints
- **Map:** 200×200 grid @ 10f cell = 2000-unit world (`GridColumns/Rows` in `EngineWindow`)

### UI system (`SharpOpenGl.Engine.UI`)

```
OnMouseDown → UIManager.HandlePointerTapped (physical → logical coords)
           → UIScreen → Widget tree (depth-first, children first)
           → Button.Clicked
```

| Concept | Details |
|---------|---------|
| Reference resolution | **1920×1080** — all widget positions authored here |
| `UIScaler` | Maps logical ↔ physical pixels |
| `ScaledUIRenderer` | Wraps `IUIRenderer`; scales draw calls |
| `UIManager` | Screen stack; only topmost screen receives input |
| Screens | MainMenu, MissionSelect, MultiplayerSetup, Briefing, Loading, GameplayHUD, Pause, SaveGame, LoadGame, ShipDesigner |
| Widgets | Button, Panel, ResourceBar, Minimap, UnitInfoPanel, BuildPanel, ShipControlBar, ProgressBar, VirtualJoystick |

**Important:** UI layout always resolves against `UIScaler.ReferenceSize`. `UIManager` converts mouse coords via `UnscalePosition` before hit-testing.

### GameData (JSON)

Loaded via `JsonLoader` (case-insensitive, comments allowed).

| Path | Schema / DTO |
|------|--------------|
| `GameData/Ships/*.json` | `EntityDefinition` |
| `GameData/Bases/*.json` | `EntityDefinition` (building) |
| `GameData/Units/*.json` | `EntityDefinition` |
| `GameData/Missions/*.json` | `MissionDefinition` |
| `GameData/Maps/*.json` | `MapDefinition` |
| `GameData/Config/*.json` | balance, controls, resources, race_visuals, race_shields, race_ultimates |

Ship types: `fighter_basic`, `bomber_heavy`, `destroyer_assault`, `scout_light`, `carrier_command`, `cruiser_heavy`, `hero_default`, `miner_basic`, `transport_cargo`.

### Economy

4 resources: Energy, Minerals, Data, Crew. `ResourceManager` ticks each frame; `GameplayHUD.ResourceBar` bound in `BindResourceHUD()`.

### Missions

`MissionLoader` reads JSON → `ObjectiveSystem` + `TriggerSystem`. Campaign missions include star-map fields (`planetName`, `starMapPosition`, `planetColor`, `prerequisiteMissionId`). `MissionSelectScreen` renders the galactic star map (`StarMapCanvas`); Start Mission / double-click → `BriefingScreen` → gameplay.

---

## Known Issues & Conventions

| Topic | Status |
|-------|--------|
| Menu buttons at non-1080p | **Fixed** — `UIScaler` wired in `UIManager` (branch `copilot/fix-menu-button-issues`) |
| Start Mission disabled | Auto-selects first **unlocked** mission in `SetMissions()` |
| `UIScaler` in tests | Pass **physical** coords to `UIManager.HandlePointerTapped`; pass **logical** coords to `UIScreen`/`Widget` directly |
| Hover feedback | **Fixed** — `EngineWindow.UpdateUiPointerState` calls `UIManager.HandlePointerMove` each frame; hover SFX on button enter |
| WebGL2 | No menu screens; hardcoded scenario; see `docs/WEBGL2_PARITY.md` |
| Screenshot mode | `dotnet run --project SharpOpenGl -- --screenshot --screenshot-path out.png` |
| Demo recording | `dotnet run --project SharpOpenGl -- --demo-recording --mission example_scenario` → `docs/gameplay-demo.mp4` + poster PNG |

---

## Test Coverage Map

| Area | Location | ~Tests |
|------|----------|--------|
| UI / widgets | `SharpOpenGl.Tests/UI/` | anchors, buttons, UIManager, scaler, mission select |
| ECS | `SharpOpenGl.Tests/ECS/` | entities, movement, stances, squads, routes |
| Combat | `SharpOpenGl.Tests/Combat/` | projectiles, abilities, race ultimates |
| Grid | `SharpOpenGl.Tests/Grid/` | pathfinding, fog, map gen |
| Economy | `SharpOpenGl.Tests/Economy/` | resources, build queue |
| Missions | `SharpOpenGl.Tests/Missions/` | loader, objectives, triggers, star map, playthrough agent |
| Config / shields | `SharpOpenGl.Tests/Config/RaceShieldDoctrineTests.cs` | race doctrine, spawn, regen |
| Config / ultimates | `SharpOpenGl.Tests/Config/RaceUltimateTests.cs` | per-race ultimate lookup, hero slot 2 |
| Multiplayer | `SharpOpenGl.Tests/Multiplayer/` | lobby, replay, commands |
| Multiplayer setup | `SharpOpenGl.Tests/UI/MultiplayerSetupLogicTests.cs`, `MultiplayerSetupScreenTests.cs` | slot validation, race cycling, map picker, 8-player config |
| Skirmish maps | `SharpOpenGl.Tests/Grid/SkirmishMapLogicTests.cs` | baseArea bounds, spawn count, JSON schema |
| Persistence | `SharpOpenGl.Tests/Persistence/` | SaveManager, WorldSaveLoad round-trip |

---

## Active Branch Context

**Backlog item #15 (done):** Mobile-friendly gameplay demo video on GitHub Pages.

**Delivered:**
1. Canonical assets — `docs/gameplay-demo.mp4` (H.264, &lt;15 MB) + `docs/gameplay-demo-poster.png`
2. `place_building` demo step + updated `example_scenario` script (select, move, combat, base build, HUD)
3. CI — `build-and-screenshot.yml` and `deploy-pages.yml` record via xvfb + ffmpeg on push to `master`
4. Embeds — HTML5 `<video controls playsinline poster=…>` in `readme.md`, `docs/index.html`, Blazor `wwwroot/index.html`

**Backlog item #14 (done):** Mission playthrough agent for demo recordings.

**Delivered:**
1. `DemoScriptStepDefinition` + `demoScript` on `MissionDefinition` (all campaign missions)
2. `MissionPlaythroughAgent` — timed steps: select_units, move_to, attack_move, attack_target, wait, wait_objective, camera_pan, build_unit, place_building
3. `GameCommandExecutor` / `GameCommandContext` — applies `IGameCommand` to ECS world
4. `--demo-recording --mission <id>` — headless capture → `docs/gameplay-demo.mp4` + poster PNG (ffmpeg when available)
5. Tests — `MissionPlaythroughTests` (parsing, sequencing, no-op safety)

**Backlog item #13 (done):** Full save/load with slot UI.

**Delivered:**
1. `WorldSaveService` / `WorldLoadService` — entities, resources, mission progress, fog, camera
2. `SaveSlotNames` — 5 manual slots (`Slot1`–`Slot5`) + `Autosave` quick-save
3. `SaveGameScreen` — pause overlay with slot picker + overwrite confirmation
4. `LoadGameScreen` — main-menu list (slot, mission, elapsed, timestamp)
5. `EngineWindow.SaveLoad.cs` — `Continue` / Load Game use `LoadLatest` + full world restore
6. Tests — `WorldSaveLoadTests`, `SaveLoadScreenTests`, updated `SaveManagerTests`

**Backlog item #12 (done):** Standard multiplayer skirmish maps with base areas.

---

## Documentation Index

| File | Audience |
|------|----------|
| `readme.md` | Users — build, run, controls |
| `AGENTS.md` | AI agents — architecture, conventions (this file) |
| `GAME_PLAN.md` | Developers — 12-phase roadmap |
| `IMPLEMENTATION_PLAN.md` | Developers — feature checklist |
| `docs/PLAYER_GUIDE.md` | Players |
| `docs/CONTROLS.md` | Players — key bindings |
| `docs/WEBGL2_PARITY.md` | Developers — desktop vs browser |
| `docs/MULTIPLAYER.md` | Developers — netcode design |
| `docs/VISUAL_DESIGN.md` | Artists / developers |

---

## Agent Maintenance Checklist

When making changes, update **AGENTS.md** if you touch:

- [ ] Project structure or new top-level directories
- [ ] Entry points, scene flow, or UI routing
- [ ] ECS components/systems (add/remove/rename)
- [ ] GameData schemas or loading paths
- [ ] CI workflows or build commands
- [ ] Known issues / fixed bugs
- [ ] Test locations for new subsystems
- [ ] Active branch goals or completion status

Keep entries concise. Prefer tables and file paths over prose.