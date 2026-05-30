# SharpOpenGL Engine

[![Build and Screenshot](https://github.com/KyleWiering/SharpOpenGl/actions/workflows/build-and-screenshot.yml/badge.svg)](https://github.com/KyleWiering/SharpOpenGl/actions/workflows/build-and-screenshot.yml)

## Introduction

A C# OpenGL rendering engine built with [OpenTK 4.x](https://opentk.net/) on .NET 8. Originally a C++ university project, converted to C# with OpenGL 1.x, now modernized to use:

- **.NET 8** SDK-style project
- **OpenTK 4.8** with GameWindow (cross-platform)
- **Modern OpenGL 3.3+** (shaders, VAOs, VBOs)
- **GitHub Actions CI/CD** with automated screenshot verification

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

## Build & Run

```bash
dotnet restore
dotnet build
dotnet run --project SharpOpenGl
```

## Controls

| Key | Action |
|-----|--------|
| W/S | Move forward/backward |
| Q/E | Strafe left/right |
| Z/X | Move up/down |
| A/D | Rotate left/right |
| ESC | Exit |

## Screenshot Mode (for CI/CD)

Run headlessly and capture a rendered frame:

```bash
dotnet run --project SharpOpenGl -- --screenshot --screenshot-path output.png
```

## Render Example

![Rotating Triangle](https://raw.githubusercontent.com/KyleWiering/SharpOpenGl/master/render001.PNG "Rotating Triangle")
