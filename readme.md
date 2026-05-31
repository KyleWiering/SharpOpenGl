# SharpOpenGL Engine

[![Build and Screenshot](https://github.com/KyleWiering/SharpOpenGl/actions/workflows/build-and-screenshot.yml/badge.svg)](https://github.com/KyleWiering/SharpOpenGl/actions/workflows/build-and-screenshot.yml)
[![Deploy to GitHub Pages](https://github.com/KyleWiering/SharpOpenGl/actions/workflows/deploy-pages.yml/badge.svg)](https://github.com/KyleWiering/SharpOpenGl/actions/workflows/deploy-pages.yml)

## 🎮 [Live Demo](https://kylewiering.github.io/SharpOpenGl/)

## Introduction

A C# space RTS game engine built with [OpenTK 4.x](https://opentk.net/) on .NET 8, featuring an Entity Component System (ECS) architecture, procedural map generation, A* pathfinding, fog of war, and data-driven ship/mission definitions.

- **.NET 8** SDK-style project
- **OpenTK 4.8** with GameWindow (cross-platform)
- **Modern OpenGL 3.3+** (shaders, VAOs, VBOs)
- **ECS Architecture** with component pools, systems, and event bus
- **Data-driven content** via JSON definitions in `GameData/`
- **WebGL2** browser-based rendering via GitHub Pages
- **GitHub Actions CI/CD** with automated screenshot verification

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

## Build & Run

```bash
dotnet restore
dotnet build
dotnet run --project SharpOpenGl
```

## Run Tests

```bash
dotnet test
```

## Project Structure

| Directory | Description |
|-----------|-------------|
| `SharpOpenGl/` | Main executable — window, camera, input, rendering |
| `SharpOpenGl.Engine/` | Core engine — ECS, Grid, UI, Missions, Persistence |
| `SharpOpenGl.Tests/` | xUnit test suite |
| `GameData/` | JSON content — ships, maps, missions, config |

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

| Key | Action |
|-----|--------|
| W/S | Move forward/backward |
| Q/E | Strafe left/right |
| Z/X | Move up/down |
| A/D | Rotate left/right |
| M | Move command |
| S | Stop command |
| P | Patrol command |
| A | Attack-move command |
| ESC | Exit |

## Screenshot Mode (for CI/CD)

Run headlessly and capture a rendered frame:

```bash
dotnet run --project SharpOpenGl -- --screenshot --screenshot-path output.png
```

## Render Example

![Rotating Triangle](https://raw.githubusercontent.com/KyleWiering/SharpOpenGl/master/render001.PNG "Rotating Triangle")
