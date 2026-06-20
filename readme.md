# SharpOpenGL Engine

[![Build and Screenshot](https://github.com/KyleWiering/SharpOpenGl/actions/workflows/build-and-screenshot.yml/badge.svg)](https://github.com/KyleWiering/SharpOpenGl/actions/workflows/build-and-screenshot.yml)
[![Deploy to GitHub Pages](https://github.com/KyleWiering/SharpOpenGl/actions/workflows/deploy-pages.yml/badge.svg)](https://github.com/KyleWiering/SharpOpenGl/actions/workflows/deploy-pages.yml)

## 🎮 [Live Demo](https://kylewiering.github.io/SharpOpenGl/)

The GitHub Pages site runs the **same C# game** as the desktop build (`SharpOpenGl.Browser` — Blazor WebAssembly + `SharpOpenGl.Engine`). It uses the same GLSL shader pipeline (`GameShaders`), procedural meshes (`ProceduralMeshes`), RTS camera, and `IRenderer` draw path as desktop — WebGL2 in the browser instead of OpenTK OpenGL. Menus, mission select, briefing, ECS gameplay, and `GameData/` JSON all come from the shared engine library. CI publishes the WASM build to `docs/` on every push to `master`.

## Introduction

A C# space RTS game engine built with [OpenTK 4.x](https://opentk.net/) on .NET 8, featuring an Entity Component System (ECS) architecture, procedural map generation, A* pathfinding, fog of war, and data-driven ship/mission definitions.

> **AI assistants:** Read [`AGENTS.md`](AGENTS.md) first for architecture, conventions, and CI workflow. Keep it updated per [`.cursor/rules/ai-documentation.mdc`](.cursor/rules/ai-documentation.mdc).

- **.NET 8** SDK-style project
- **OpenTK 4.8** with GameWindow (cross-platform desktop)
- **Blazor WebAssembly** browser build sharing `SharpOpenGl.Engine`
- **Modern OpenGL 3.3+** desktop / **WebGL2** browser rendering
- **ECS Architecture** with component pools, systems, and event bus
- **Data-driven content** via JSON definitions in `GameData/`
- **GitHub Actions CI/CD** with automated screenshot verification

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (optional — CI runs on GitHub Actions)

## Build & Run

**Desktop:**
```bash
dotnet restore
dotnet build
dotnet run --project SharpOpenGl
```

**Browser (local):**
```bash
dotnet run --project SharpOpenGl.Browser
```
Then open the URL shown in the terminal (typically `https://localhost:7xxx`).

## Run Tests

```bash
dotnet test
```

## Project Structure

| Directory | Description |
|-----------|-------------|
| `SharpOpenGl/` | Desktop executable — OpenTK window, camera, input, rendering |
| `SharpOpenGl.Browser/` | Blazor WASM — same engine, WebGL2 + canvas UI |
| `SharpOpenGl.Engine/` | Core engine — ECS, Grid, UI, Missions, Persistence |
| `SharpOpenGl.Tests/` | xUnit test suite |
| `GameData/` | JSON content — ships, maps, missions, config |
| `docs/` | GitHub Pages deploy target (WASM publish output + guides) |
| `AGENTS.md` | AI agent context — architecture map and conventions |

## Game Features

- **Ships**: Data-driven ship types (scout, cruiser, transport) with distinct stats
- **Movement**: A* pathfinding with terrain costs, waypoint queues, patrol mode
- **Combat**: Stance system (Neutral/Defensive/Aggressive), auto-targeting, projectiles
- **Fog of War**: Per-player visibility with sight radius, explored/visible states
- **Maps**: Procedural generation with configurable terrain, resources, spawn points
- **Missions**: Objective system with triggers, briefings, and rewards
- **UI**: Control bar with command buttons, keyboard shortcuts

## Running the Example Scenario

The `example_scenario` mission demonstrates fleet movement, combat, and objectives:

1. Build and run the project
2. Select "First Contact" from the mission select screen
3. Move your fleet to the waypoint at sector center
4. Eliminate enemy scouts that spawn when you approach

## Controls

| Key / Mouse | Action |
|-------------|--------|
| Left drag | Box-select multiple ships |
| Left click | Select ship, resource node, planet, or enemy (HUD color-coded) |
| Right click | Move / attack / mine (context-sensitive) |
| Right drag | Pan the map |
| W/S | Camera forward/back (Shift overrides unit key conflicts) |
| A/D | Camera strafe left/right |
| Q/E | Extra strafe left/right |
| Z/X | Camera height up/down |
| M | Move command |
| S | Stop command (tap; hold pans camera unless units selected) |
| P | Patrol command |
| F | Attack-move command |
| Right-click miner on node | Assign harvest |
| Right-click armed ship on enemy | Attack target |
| ESC | Pause / exit |

## Entity Colors (HUD & Minimap)

Click any world object to inspect it in the unit info panel. Selection rings and labels use these colors:

| Color | Entity type | Interaction |
|-------|-------------|-------------|
| Green | Your ships | Move, attack, patrol commands |
| Red | Hostile units | Right-click selected ships to attack |
| Yellow | Neutral planets | Inspect only — no faction allegiance |
| Light blue | Resource nodes & harvestable planets | Right-click miner to harvest |
| White | Scenery (asteroids, nebulae, debris) | Inspect only |

Map content is authored in `GameData/Maps/*.json` via `resourceNodes` (diamond markers) and `mapFeatures` (`neutral_planet`, `harvestable_planet`, `scenery`). Sector Alpha includes a neutral hub at the center waypoint, harvestable worlds, and scenery at terrain regions.

## Screenshot Mode (for CI/CD)

Run headlessly and capture a rendered frame:

```bash
dotnet run --project SharpOpenGl -- --screenshot --screenshot-path output.png
```

## Render Example

![Rotating Triangle](https://raw.githubusercontent.com/KyleWiering/SharpOpenGl/master/render001.PNG "Rotating Triangle")