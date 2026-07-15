# Key files — sandbox map polish

Paths relative to repo root (`REPO`). Read ≤3 files per evaluation pass.

## Chunk grid & generation

| File | Role |
|------|------|
| `SharpOpenGl.Engine/Grid/SandboxChunkGrid.cs` | Lazy chunk load/unload, merged `GridSystem` |
| `SharpOpenGl.Engine/Grid/SandboxChunkCoords.cs` | Chunk coordinate math |
| `SharpOpenGl.Engine/Grid/MapGenerator.cs` | `GenerateChunk` — procedural terrain per chunk |
| `SharpOpenGl.Engine/Grid/MapFeatureSpawner.cs` | `SpawnChunkEconomy` — resources/features per chunk |
| `SharpOpenGl.Engine/Grid/GridSystem.cs` | Base grid; world/grid coordinate conversion |
| `SharpOpenGl.Engine/Grid/GridCell.cs` | Per-cell terrain data |

### Chunk load flow

```
UpdateSandboxChunks()
    → SandboxChunkGrid.EnsureChunksAround(camera, chunkLoadRadius, seed)
    → MapGenerator.GenerateChunk (new chunks)
    → SpawnSandboxChunkEconomy(loaded chunks)
    → GridSystem grows; fog grid resizes
```

## Fog of war (two-layer)

| File | Role |
|------|------|
| `SharpOpenGl.Engine/Grid/FogState.cs` | `Unexplored`, `Explored`, `Visible` enum |
| `SharpOpenGl.Engine/Grid/FogOfWar.cs` | Per-player per-cell fog state storage |
| `SharpOpenGl.Engine/Grid/FogOfWarSystem.cs` | Updates visibility from unit sight |
| `SharpOpenGl.Engine/Rendering/FogNebulaOverlay.cs` | `ResolveOverlayState`, `Config`, `Sync`, `MaxActiveChunks` |
| `SharpOpenGl.Engine/Rendering/FogVisualPalette.cs` | Nebula color palette per fog state |
| `SharpOpenGl.Engine/Rendering/ParticleEffects.cs` | `CreateFogNebulaChunk` emitter factory |
| `SharpOpenGl.Engine/Grid/CombatFogGate.cs` | Combat LOS respects fog |

### Fog overlay flow

```
FogOfWarSystem (sight updates)
    → FogNebulaOverlay.Sync(fog, grid, playerId, chunkWorldSize, cameraBounds)
    → ResolveOverlayState per chunk → null | Unexplored | Explored
    → ParticleEffects.CreateFogNebulaChunk (two variants only)
    → RenderFogOverlay (particle draw pass)
```

**Contract doc:** `references/fog-two-layer.md`

## Engine window integration

| File | Role |
|------|------|
| `SharpOpenGl/EngineWindow.Map.cs` | `InitializeMapSystems`, `UpdateSandboxChunks`, `RenderFogOverlay`, `RevealAreaAt`, `SpawnSandboxChunkEconomy` |
| `SharpOpenGl/EngineWindow.Gameplay.cs` | `SetupSandboxWorld` — sandbox bootstrap |
| `SharpOpenGl/EngineWindow.cs` | Render loop calls `UpdateSandboxChunks`, `RenderFogOverlay` |
| `SharpOpenGl/EngineWindow.MapFeatures.cs` | `BuildMapFeatureMeshes` for chunk features |

## Config

| File | Role |
|------|------|
| `GameData/Config/sandbox.json` | `chunkLoadRadius`, `initialRevealRadius`, `startingResources`, `spawnHostileAi` |

Default values:

```json
{
  "initialRevealRadius": 24,
  "chunkLoadRadius": 2
}
```

## UI / minimap

| File | Role |
|------|------|
| `SharpOpenGl.Engine/UI/Widgets/Minimap.cs` | Fog state colors on minimap |

## Tests

| Test class | Evidence for |
|------------|--------------|
| `SharpOpenGl.Tests/Grid/SandboxChunkGridTests.cs` | Chunk load, grid merge, coords |
| `SharpOpenGl.Tests/Rendering/FogNebulaOverlayTests.cs` | **Fog two-layer contract** — `ResolveOverlayState` |
| `SharpOpenGl.Tests/Rendering/FogVisualPaletteTests.cs` | Palette distinctness per state |
| `SharpOpenGl.Tests/Gameplay/SandboxBootstrapTests.cs` | End-to-end sandbox setup |

### Test command

```powershell
dotnet test SharpOpenGl.Tests --filter "FullyQualifiedName~SandboxChunkGrid|FogNebulaOverlay|FogVisualPalette|SandboxBootstrap"
```

## Edit priority by low rubric category

| Low category | Edit first | Edit second | Rarely |
|--------------|------------|-------------|--------|
| ChunkVariety | `MapGenerator.cs` | `SandboxChunkGrid.cs` | `GridCell.cs` |
| EconomyScatter | `MapFeatureSpawner.cs` | `EngineWindow.Map.cs` | `sandbox.json` |
| FogTwoLayer | `FogNebulaOverlay.cs` | `FogVisualPalette.cs`, `ParticleEffects.cs` | `FogOfWar.cs` |
| ExplorationFeel | `FogOfWarSystem.cs`, `sandbox.json` | `EngineWindow.Map.cs` | `Minimap.cs` |
| PerfBudget | `sandbox.json`, `FogNebulaOverlay.cs` | `SandboxChunkGrid.cs` | `EngineWindow.Map.cs` |

## Cross-skill references

| Skill | Shared pattern |
|-------|----------------|
| `combat-visual-polish` | `ParticleEffects.cs` pooling, overlay draw order |
| `mesh-improvement-loop` | Map feature meshes in `MapFeatureSpawner` / `ProceduralMeshes` |