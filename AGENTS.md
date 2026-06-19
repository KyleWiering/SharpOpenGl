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
| `build-and-screenshot.yml` | push → `master` | Headless screenshot via `--screenshot` |
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
├── SharpOpenGl.Tests/        # xUnit (~410 tests)
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
MainMenu → MissionSelect → Gameplay
         → MultiplayerSetup → Gameplay
Gameplay + PauseScreen overlay
```

`EngineWindow` owns `SceneManager` + `UIManager`. UI clicks are routed **before** gameplay input in `OnMouseDown`.

### ECS (`SharpOpenGl.Engine.ECS`)

- **World** — entity slots, sparse-set component pools, system registry
- **26 components** — Transform, Movement, Render, Health, Weapon, Building, etc.
- **18 systems** — Movement, Combat, Build, Resource, AI, FogOfWar, Supply, etc.
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
| Screens | MainMenu, MissionSelect, MultiplayerSetup, Briefing, Loading, GameplayHUD, Pause, ShipDesigner |
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
| `GameData/Config/*.json` | balance, controls, resources |

Ship types: `fighter_basic`, `bomber_heavy`, `destroyer_assault`, `scout_light`, `carrier_command`, `cruiser_heavy`, `hero_default`, `miner_basic`, `transport_cargo`.

### Economy

4 resources: Energy, Minerals, Data, Crew. `ResourceManager` ticks each frame; `GameplayHUD.ResourceBar` bound in `BindResourceHUD()`.

### Missions

`MissionLoader` reads JSON → `ObjectiveSystem` + `TriggerSystem`. `BriefingScreen` exists but is **not wired** in current `EngineWindow` flow (MissionSelect goes straight to Gameplay).

---

## Known Issues & Conventions

| Topic | Status |
|-------|--------|
| Menu buttons at non-1080p | **Fixed** — `UIScaler` wired in `UIManager` (branch `copilot/fix-menu-button-issues`) |
| Start Mission disabled | Auto-selects first mission in `SetMissions()` |
| `UIScaler` in tests | Pass **physical** coords to `UIManager.HandlePointerTapped`; pass **logical** coords to `UIScreen`/`Widget` directly |
| Hover feedback | `Button.UpdatePointerState` exists but not called from `EngineWindow` |
| WebGL2 | No menu screens; hardcoded scenario; see `docs/WEBGL2_PARITY.md` |
| Screenshot mode | `dotnet run --project SharpOpenGl -- --screenshot --screenshot-path out.png` |

---

## Test Coverage Map

| Area | Location | ~Tests |
|------|----------|--------|
| UI / widgets | `SharpOpenGl.Tests/UI/` | anchors, buttons, UIManager, scaler, mission select |
| ECS | `SharpOpenGl.Tests/ECS/` | entities, movement, stances |
| Combat | `SharpOpenGl.Tests/Combat/` | projectiles, abilities |
| Grid | `SharpOpenGl.Tests/Grid/` | pathfinding, fog, map gen |
| Economy | `SharpOpenGl.Tests/Economy/` | resources, build queue |
| Missions | `SharpOpenGl.Tests/Missions/` | loader, objectives, triggers |
| Multiplayer | `SharpOpenGl.Tests/Multiplayer/` | lobby, replay, commands |

---

## Active Branch Context

**Branch:** `copilot/fix-menu-button-issues`  
**Task:** Fix menu / Start Mission button not responding at default 1024×768 window.

**Root cause:** UI authored at 1920×1080 but `UIScreen.Draw` and hit-testing used raw viewport size without scaling.

**Fix applied:**
1. `ScaledUIRenderer` — scales logical draw calls to physical pixels
2. `UIManager.Draw` / `HandlePointerTapped` — use `UIScaler`
3. `UIScreen.Draw` — layout against `UIScaler.ReferenceSize`
4. `EngineWindow` — `Resize` on init and `OnResize`
5. `MissionSelectScreen.SetMissions` — auto-select first mission
6. Tests in `UIScalerIntegrationTests`, `MissionSelectScreenTests`

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