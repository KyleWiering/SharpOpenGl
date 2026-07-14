# Sandbox map rubric

SharpOpenGl scores the sandbox chunked procedural map on a **0–100** scale across five categories. Use when interpreting `score-NN.json` from `/sandbox-map-polish`.

**Fog authority:** `fog-two-layer.md` — violations auto-fail **FogTwoLayer** regardless of aesthetics.

## Scope

Covers sandbox chunked mode only (`_sandboxChunkedMode`):

- Procedural chunk generation and lazy loading
- Per-chunk economy scatter (resources, map features)
- Two-layer fog overlay (Unexplored / Explored; Visible clear)
- Exploration pacing and reveal behavior
- Performance budget for large maps

## Categories (100 total)

| Category | Max | Measures |
|----------|-----|----------|
| **ChunkVariety** | 20 | Terrain and feature diversity across loaded chunks |
| **EconomyScatter** | 20 | Resource nodes and features distributed meaningfully |
| **FogTwoLayer** | 20 | Correct two-layer fog; Visible never veiled |
| **ExplorationFeel** | 20 | Tension at fog edge, reveal satisfaction, minimap truth |
| **PerfBudget** | 20 | Chunk radius, emitter cap, camera cull |

**Pass threshold:** overall ≥ 80; each category ≥ 16.  
**Fog gate:** `fogTwoLayer` < 16 → block sign-off.

---

## ChunkVariety (20)

Maps to `SandboxChunkGrid`, `MapGenerator.GenerateChunk`, `SandboxChunkCoords`.

| Score band | Criteria |
|------------|----------|
| 18–20 | Adjacent chunks visibly differ in terrain/features; seed-stable |
| 14–17 | Some repetition; biome transitions still noticeable |
| 10–13 | Large homogeneous regions; chunks feel copy-pasted |
| 0–9 | Single terrain type dominates; no procedural identity |

### Checklist

- [ ] `GenerateChunk` uses chunk coords + world seed for deterministic variety
- [ ] Height/scatter variation readable at RTS zoom
- [ ] Chunk boundaries don't show harsh seams (grid merge in `SandboxChunkGrid`)
- [ ] New chunks load with distinct feature placement vs neighbors
- [ ] `EnsureChunksAround` loads ring without duplicate spawn

### Deductions

| Issue | Points | Notes keyword |
|-------|--------|---------------|
| Identical economy layout per chunk | −5 | `chunk-clone` |
| Visible grid seam at chunk border | −4 | `chunk-seam` |
| Non-deterministic regen on reload | −6 | `seed-unstable` |
| Empty chunks (no terrain variation) | −4 | `flat-chunk` |

---

## EconomyScatter (20)

Maps to `MapFeatureSpawner`, `SpawnSandboxChunkEconomy`, `EngineWindow.Map.cs`.

| Score band | Criteria |
|------------|----------|
| 18–20 | Resources/features scale with chunk; exploration rewards movement |
| 14–17 | Good density; occasional empty or overcrowded chunks |
| 10–13 | Clumped spawns; large dead zones |
| 0–9 | Economy absent or only at map origin |

### Checklist

- [ ] `SpawnChunkEconomy` called for each newly loaded chunk
- [ ] Resource nodes not stacked on identical grid cells
- [ ] Feature meshes match `BuildMapFeatureMeshes` registry
- [ ] `sandbox.json` `chunkLoadRadius` balances content vs perf
- [ ] Starting area has viable economy after `initialRevealRadius`

### Deductions

| Issue | Points | Notes keyword |
|-------|--------|---------------|
| Double-spawn on chunk revisit | −6 | `economy-dupe` |
| Zero nodes in loaded radius | −5 | `economy-empty` |
| All nodes in one chunk corner | −4 | `economy-clump` |
| Features spawn outside grid bounds | −5 | `spawn-_oob` |

---

## FogTwoLayer (20) — GATE CATEGORY

Maps to `FogState`, `FogOfWar`, `FogNebulaOverlay`, `FogVisualPalette`.

| Score band | Criteria |
|------------|----------|
| 18–20 | Unexplored dense, Explored light, Visible crystal clear; tests green |
| 14–17 | Two layers distinct; minor edge case at chunk boundaries |
| 10–13 | Explored/Unexplored too similar OR slight Visible haze |
| 0–9 | Visible veiled, third overlay tier, or fog logic broken |

### Hard contract (see `fog-two-layer.md`)

```
any cell Visible in chunk → ResolveOverlayState returns null (NO overlay)
else anyExplored → FogState.Explored (light memory fog)
else → FogState.Unexplored (dense nebula)
```

### Config expectations (`FogNebulaOverlay.Config`)

| Constant | Unexplored | Explored | Rule |
|----------|------------|----------|------|
| Emit rate | 34f | 13f | Unexplored > Explored |
| Start alpha | 0.85f | 0.42f | Unexplored > Explored |
| Particle lifetime | 5f | 3.5f | Unexplored ≥ Explored |

### Checklist

- [ ] `FogState.Visible` never creates `FogChunkEntry`
- [ ] `RenderFogOverlay` draws only nebula particles — not full-screen black for Visible
- [ ] `RevealAreaAt` transitions Unexplored → Visible correctly
- [ ] Explored cells darken but show terrain silhouette (memory fog)
- [ ] `FogNebulaOverlayTests` pass after changes
- [ ] Minimap fog colors match world fog states

### Deductions

| Issue | Points | Notes keyword |
|-------|--------|---------------|
| Visible chunk has emitter | −10 | `visible-veiled` (**auto-fail category**) |
| Third overlay tier added | −8 | `third-layer` |
| Explored ≈ Unexplored (alpha within 0.1) | −5 | `layer-indistinct` |
| `ResolveOverlayState` logic changed incorrectly | −8 | `resolve-broken` |
| Black cell overlay instead of nebula for Unexplored | −3 | `flat-fog` |

---

## ExplorationFeel (20)

Maps to `FogOfWarSystem`, `RevealAreaAt`, `SetupSandboxWorld`, minimap.

| Score band | Criteria |
|------------|----------|
| 18–20 | Moving units reveals satisfying frontier; explored memory aids navigation |
| 14–17 | Good reveal; edge tension weak or minimap slightly stale |
| 10–13 | Reveal radius too small/large; exploration feels arbitrary |
| 0–9 | Fog doesn't update; player lost in homogeneous veil |

### Checklist

- [ ] `initialRevealRadius` from `sandbox.json` gives playable start (default 24)
- [ ] Unit sight updates `FogOfWarSystem` each tick
- [ ] Explored territory shows where player has been (lighter fog)
- [ ] Unexplored frontier feels dangerous/opaque
- [ ] `CombatFogGate` respects fog for combat LOS
- [ ] Camera panning reveals new chunks with fog clearing at edge

### Deductions

| Issue | Points | Notes keyword |
|-------|--------|---------------|
| Reveal doesn't fire on unit move | −6 | `reveal-stale` |
| Entire map revealed at start | −5 | `reveal-too-large` |
| Explored doesn't persist after leaving | −5 | `memory-lost` |
| Minimap fog desync from world | −4 | `minimap-fog` |

---

## PerfBudget (20)

Maps to `SandboxChunkGrid`, `FogNebulaOverlay.MaxActiveChunks`, camera cull.

| Score band | Criteria |
|------------|----------|
| 18–20 | Smooth pan/zoom; emitter count bounded; lazy chunk load |
| 14–17 | Minor hitches at chunk edge; within caps |
| 10–13 | Frequent emitter evictions visible; large load radius |
| 0–9 | Uncapped fog emitters; full map load; frame drops |

### Checklist

- [ ] `chunkLoadRadius` in `sandbox.json` reasonable (default 2)
- [ ] `MaxActiveChunks` = 400 respected with farthest eviction
- [ ] Camera bounds cull active on large maps (`grid.Width > 200`)
- [ ] `UpdateSandboxChunks` only loads/unloads delta around camera
- [ ] Fog `Sync` skips chunks outside camera bounds on large maps
- [ ] No per-frame full-grid fog rebuild

### Deductions

| Issue | Points | Notes keyword |
|-------|--------|---------------|
| Load radius > 4 without perf justification | −4 | `radius-high` |
| Fog emitters exceed cap without eviction | −6 | `emitter-unbounded` |
| All chunks loaded at bootstrap | −5 | `eager-load` |
| Full-grid fog sync every frame | −4 | `fog-sync-full` |

---

## Suggested next actions by lowest category

| Lowest category | First files to open |
|-----------------|---------------------|
| ChunkVariety | `MapGenerator.cs`, `SandboxChunkGrid.cs` |
| EconomyScatter | `MapFeatureSpawner.cs`, `EngineWindow.Map.cs`, `sandbox.json` |
| FogTwoLayer | `FogNebulaOverlay.cs`, `fog-two-layer.md`, `FogNebulaOverlayTests.cs` |
| ExplorationFeel | `FogOfWarSystem.cs`, `EngineWindow.Map.cs`, `sandbox.json` |
| PerfBudget | `EngineWindow.Map.cs`, `FogNebulaOverlay.cs`, `sandbox.json` |