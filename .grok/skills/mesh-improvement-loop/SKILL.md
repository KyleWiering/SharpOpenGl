---
name: mesh-improvement-loop
description: >
  Run a 10-loop iterative mesh improvement pipeline for SharpOpenGl ships,
  stations, and objects. Each loop: edit geometry/textures from do-better.md,
  capture a 3-view preview, score the asset (plus optional race fleet audit),
  and write suggestions back. Pauses for user feedback after loop 5. Use when
  the user asks to improve a mesh, run a mesh loop, score a model or race,
  capture mesh-preview, /mesh-improvement-loop, or "do better" on any asset.
metadata:
  short-description: "10-loop mesh improvement for ships, stations, objects + race scoring"
---

# Mesh Improvement Loop

Orchestrate **10 improvement loops** for one **spaceship**, **station**, or **object** in SharpOpenGl.
Each loop uses two subagents: **mesh-updater** (edits) and **mesh-scorer** (capture + score + suggestions).

Full rubric: `references/evaluation-rubric.md`.

## RTS gameplay viewing context

SharpOpenGl is a **top-down RTS** — players see ships and bases from **above at a slight angle**, not from below or pure side elevation.

| Asset | Primary score driver | De-emphasized |
|-------|---------------------|---------------|
| **Ship** | Panel 1 — oblique top-down (~35° yaw): dorsal silhouette, bow/stern mass, wing span | Belly/underside detail, pure 90° side profile |
| **Station** | Oblique top-down plan mass: pad footprint, ring structures, clustered superstructure | Tall spires/towers floating in empty space |
| **Object** | Compact icon readable at map zoom | — |

**Triangle pattern anti-pattern (significant penalty):** Visible facet triangles on surfaces — sliver strips, fishbone chevrons, micro-facets, or per-triangle luminance seams. Signals **incomplete mesh definition** or **bad texture wrap**. Scorer deducts up to **8 pts** from Geometry (`tri-pattern` in notes) and up to **3 pts** from Materials (`texture-wrap` in notes). Replace with flush boxes/panels, merged coplanar faces, and uniform vertex luminance per material zone.

## Inputs (ask if missing)

| Param | Example | Notes |
|-------|---------|-------|
| `race` | `vesper` | Required for ships/stations; omit for objects |
| `model` | `fighter_basic` | Hull id, station id, or object id |
| `category` | `ship` | `ship` · `station` · `object` |
| `start_loop` | `1` | First loop number |
| `loop_count` | `10` | Default 10 |
| `race_audit` | `false` | Run `--score-race` after loop 5 or 10 |

**Eight races:** terran, vesper, korath, aetherian, nexar, solari, voidborn, cryo.

Resolve repo root as `REPO`. Paths below are relative to `REPO`.

## Directory layout

```
model-improvement/<race>/<model>/do-better.md          # ships & stations
model-improvement/<race>/<model>/scores/loop-NN.json
model-improvement/<race>/race-score.json               # fleet audit (optional)
model-improvement/shared/objects/<object>/do-better.md # race-neutral objects
mesh-loop-NN-<race>-<model>.png                        # screenshot in REPO root
```

## Phase 0 — Bootstrap (loop 1 only, or if `do-better.md` missing)

1. Create target folder + `scores/` (or `shared/objects/<id>/` for objects).
2. If no `do-better.md`, copy `references/do-better-template.md` and fill race/model/category.
3. Read current mesh state (≤3 files) based on category:
   - **Ship:** `RaceShipMeshes.cs`, race builder, `RaceSurfaceDetail.cs`
   - **Station:** `RaceStationMeshes.cs`, `RaceSurfaceDetail.cs` (station detail)
   - **Object:** `ModelMeshSource.cs`, `ProceduralMeshes.cs`
4. Seed loop priorities: **dorsal/plan silhouette** (RTS primary view), materials, race identity, shadows, screenshot readability. Flag visible triangle patterns (incomplete mesh / bad texture wrap) for removal.

Do **not** run subagents until `do-better.md` exists.

## Main loop (repeat for N = start_loop … start_loop + loop_count - 1)

### Step A — Mesh updater subagent

Launch **one** `generalPurpose` subagent per loop.

**Subagent name:** `mesh-updater-loop-NN`

**Prompt must include:**
- Full path to `do-better.md`
- `category` + `race` + `model`
- Implement **this loop's** items under "Next loop focus" and "Remaining gaps"
- Key edit targets by category:
  - **Ship:** `RaceShipMeshes.cs`, `RaceSurfaceDetail.cs`, `RaceMeshWriter.cs`, `race_visuals.json`, `EngineWindow.MeshPreview.cs` (rare)
  - **Station:** `RaceStationMeshes.cs`, `RaceSurfaceDetail.cs`, `race_visuals.json`
  - **Object:** `ProceduralMeshes.cs`, `ModelMeshSource.cs`
- Visual goals: **dorsal/plan form** readable from oblique top-down; **eliminate triangle patterns** (facet strips, bad vertex wrap on flat panels); stations widen pad footprint and cluster deck mass — avoid lone vertical towers
- After edits: `dotnet build`
- **Do not** capture or score — scorer handles that
- Return: files changed + visual summary

### Step B — Mesh scorer subagent

Launch **one** `generalPurpose` subagent after updater finishes.

**Subagent name:** `mesh-scorer-loop-NN`

**Prompt must include:**

```powershell
.grok/skills/mesh-improvement-loop/scripts/capture-and-score.ps1 -Race <race> -Model <model> -Category <category> -Loop <NN>
```

Or manually:

```powershell
dotnet run --project SharpOpenGl -- --mesh-preview --category <category> --model <model> [--race <race>] --screenshot-path mesh-loop-NN-<slug>.png
dotnet run --project SharpOpenGl -- --score-mesh --category <category> --model <model> [--race <race>] --screenshot-path <png> --output model-improvement/.../scores/loop-NN.json
dotnet test SharpOpenGl.Tests --filter "FullyQualifiedName~ModelQualityScorer" --no-build
```

**Parse JSON for:**
- `TotalScore`, `RaceIdentityScore`, `AssetKind`
- All 7 categories (form name varies: Silhouette / Massing / IconRead)
- `Suggestions`
- Geometry notes mentioning `tri-pattern` penalty; Materials notes mentioning `texture-wrap` (if present)

**Update `do-better.md`:**
- Loop, Last score (total + RaceIdentity + Screenshot)
- Score history row
- Remaining gaps from lowest categories
- Next loop focus from `Suggestions` + category notes
- Screenshot note under **Screenshots** (note panel-1 primary RTS weight)

**Optional race audit** (loop 5 or 10, ships/stations only):

```powershell
.grok/skills/mesh-improvement-loop/scripts/score-race.ps1 -Race <race>
```

Report `OverallScore`, `ShipFleetScore`, `StationFleetScore`, `WeakestAssets` to user.

### Step C — Orchestrator checkpoint

After each loop report:
- Total score (delta vs previous)
- RaceIdentity sub-score
- Screenshot path
- One-line change summary

**After loop 5: STOP** for user feedback (merge into **User feedback (loop 5)**).

**After loop 10:** Score trend, best loop, optional race leaderboard comparison.

## Scoring categories (100 total)

| Category | Max | Notes |
|----------|-----|-------|
| Silhouette / Massing / IconRead | 17 | Dorsal/plan readability at RTS zoom |
| Geometry | 17 | Triangle sweet spot (kind-aware); tri-pattern penalty up to −8 |
| Materials | 16 | Luminance bands + range; texture-wrap penalty up to −3 |

| Proportions / Scale | 12 | Envelope or gameplay span |
| SurfaceDetail | 12 | Accents, panel tiers |
| RaceIdentity | 10 | Palette fidelity (objects: clarity baseline) |
| Screenshot | 16 | 3 oblique top-down panels; **panel 1 weighted 55%** |

See `references/evaluation-rubric.md` for thresholds and race rollup.

## Org pipeline integration

When run inside `/ceo`, root CEO spawns mesh-updater/scorer — Manager only queues JSON. See `org-pipeline/references/delegation-protocol.md`.

Verifier jobs (`verifiers-queue.json`, type `mesh-scorer-batch`) must use capture commands from this skill. Report panel-1 (RTS primary) screenshot readability and any `tri-pattern` / `texture-wrap` penalties.

## Rules

- **Full asset** visible in all 3 oblique top-down preview panels (2× camera pullback). Panel 1 is the RTS gameplay angle.
- **No visible triangle patterns** — incomplete mesh facets or bad texture wrap earn up to −8 Geometry / −3 Materials.
- **Stations:** optimize plan-view massing (footprint, deck clusters), not vertical tower landmarks.
- Screenshot in **REPO root** every loop.
- One loop = one updater + one scorer; never skip capture.
- Prefer incremental edits; don't rewrite entire builders unless brief says so.
- Objects are race-neutral (`--race` omitted for score/capture).
- Race score covers 19 ships + 10 stations per race (not objects).
- Component textures: engines ~0.48 lum, weapons ~0.36 lum on race hulls.

## Quick start

**Ship:** `Run mesh improvement loop for vesper fighter_basic`

**Station:** `Run mesh loop for korath command_center station`

**Object:** `Run mesh loop for object shield_generator`

**Race audit:** `Score vesper race fleet`

1. Bootstrap `do-better.md`
2. Loops 1–5: updater → scorer → report
3. Pause for feedback
4. Loops 6–10: merge feedback → continue
5. Optional `--score-all-races` for 8-race comparison