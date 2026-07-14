---
name: sandbox-map-polish
description: >
  Score and iteratively improve the SharpOpenGl sandbox chunked procedural map:
  terrain scatter, chunk economy, exploration feel, and fog-of-war presentation.
  Critical fog rule — exactly two overlay layers: FogState.Unexplored (dense
  nebula, high alpha) and FogState.Explored (lighter memory fog); FogState.Visible
  has NO overlay (clear view). Use when the user asks for /sandbox-map-polish,
  sandbox map, fog layers, chunked exploration, nebula fog, or map scatter polish.
argument-hint: "<mode>"
metadata:
  short-description: "Sandbox chunked map + two-layer fog scoring and improve loop"
---

# Sandbox Map Polish

Orchestrate **scored evaluation loops** for the sandbox chunked procedural map in SharpOpenGl. Pattern parity with `hud-button-quality` and `combat-visual-polish`: **evaluate → JSON artifact → gate → improve**.

Full rubric: `references/sandbox-map-rubric.md`  
Fog authority: `references/fog-two-layer.md` (**never break Visible = clear**)  
Key paths: `references/key-files.md`

## Critical fog rule (non-negotiable)

Only **two** fog overlay layers exist. `FogState.Visible` renders **no veil**.

| `FogState` | Overlay | Visual |
|------------|---------|--------|
| `Unexplored` | **Yes** — dense nebula | High alpha, higher emit rate (`Config.Unexplored*`) |
| `Explored` | **Yes** — memory fog | Lighter alpha, lower emit rate (`Config.Explored*`) |
| `Visible` | **No overlay** | Clear terrain and entities |

Resolution: `FogNebulaOverlay.ResolveOverlayState` returns `null` when any cell in the chunk is `Visible`.

See `references/fog-two-layer.md` before any fog edit.

## Invocation

```
/sandbox-map-polish score
/sandbox-map-polish improve
```

| Param | Example | Required | Notes |
|-------|---------|----------|-------|
| `mode` | `score` | yes | `score` or `improve` |

**Quick start examples:**

- `/sandbox-map-polish score` — full sandbox map rubric; write `score-01.json`
- `/sandbox-map-polish improve` — implement top fixes, re-score, emit improve report

Resolve repo root as `REPO`. Paths below are relative to `REPO`.

## Scoring model (100 total)

| Category | Max | Measures |
|----------|-----|----------|
| ChunkVariety | 20 | Procedural chunk terrain/feature diversity |
| EconomyScatter | 20 | Resource nodes, features, spawn density per chunk |
| FogTwoLayer | 20 | Unexplored vs Explored distinct; Visible never veiled |
| ExplorationFeel | 20 | Reveal cadence, edge-of-map tension, minimap coherence |
| PerfBudget | 20 | Chunk load radius, emitter caps, camera cull |
| **Overall** | **100** | sum of five categories |

**Regression gate:** `overallScore` ≥ 80 and each category ≥ 16/20.

**Fog gate (hard):** `fogTwoLayer` < 16 → **block merge** regardless of overall score.

## Directory layout (artifact paths)

```
.grok/skills/sandbox-map-polish/references/sandbox-map-rubric.md
.grok/skills/sandbox-map-polish/references/fog-two-layer.md
.grok/skills/sandbox-map-polish/references/score-template.json
.grok/skills/sandbox-map-polish/scores/baseline.json
.grok/skills/sandbox-map-polish/scores/sandbox_map/score-NN.json
.grok/skills/sandbox-map-polish/scores/sandbox_map/improve-report.md
```

## Phase 0 — Bootstrap (first score run, or if baseline missing)

1. Read rubric + seed files (≤3):
   - `SandboxChunkGrid.cs`, `MapGenerator.cs` (chunk generation)
   - `FogNebulaOverlay.cs`, `FogState.cs` (two-layer fog)
   - `EngineWindow.Map.cs` (`UpdateSandboxChunks`, `RenderFogOverlay`, `RevealAreaAt`)
2. Read `GameData/Config/sandbox.json` for `chunkLoadRadius`, `initialRevealRadius`.
3. Run tests:

```powershell
dotnet build
dotnet test SharpOpenGl.Tests --filter "FullyQualifiedName~SandboxChunkGrid|FogNebulaOverlay|FogVisualPalette|SandboxBootstrap"
```

4. If `scores/baseline.json` is missing, score via checklist → write baseline (`target`: `sandbox_map`, `score_number`: 0).
5. Verify fog two-layer contract in tests (`FogNebulaOverlayTests.ResolveOverlayState_*`).
6. Confirm `dotnet build` succeeds before recording any score.

## Score mode

### Step A — Evaluate (checklist)

1. Run tests (filter above).
2. Walk checklists in `sandbox-map-rubric.md`.
3. Audit fog against `fog-two-layer.md`:
   - `ResolveOverlayState` returns `null` for visible chunks?
   - `Config.UnexploredEmitRate` > `Config.ExploredEmitRate`?
   - `Config.UnexploredStartAlpha` > `Config.ExploredStartAlpha`?
   - No third overlay tier or Visible veil hack?
4. Review chunk economy: `SpawnSandboxChunkEconomy`, `MapFeatureSpawner.SpawnChunkEconomy`.
5. Optional capture:

```powershell
dotnet run --project SharpOpenGl -- --screenshot --screenshot-path artifacts/sandbox-map.png
```

### Step B — Write score

1. Copy `references/score-template.json`.
2. Set `target` to `sandbox_map`.
3. Fill category scores (0–20 each), `notes`, and `deductions` array.
4. Set `overallScore` = sum of five categories.
5. Set `passed` = `overallScore` ≥ `passThreshold` (80) and all categories ≥ 16.
6. Set `fogGatePassed` = `fogTwoLayer` ≥ 16.
7. List `topBlockers` (≤4) and `nextActions`.
8. Record `testsRun` and `testsPassed`.
9. Save to `.grok/skills/sandbox-map-polish/scores/sandbox_map/score-NN.json`.

### Step C — Gate

| Result | Action |
|--------|--------|
| `overallScore` ≥ 80 and all categories ≥ 16 | Sandbox map may sign off (plus tests green) |
| `fogTwoLayer` < 16 | **Block merge** — fix fog contract first |
| `perfBudget` < 16 | Reduce emitter count / chunk radius before scatter polish |
| `testsPassed` false | Fix failing grid/fog tests before claiming pass |

## Improve mode

Improve mode runs a **single iteration loop**: updater subagent → re-score → report.

### Step 1 — Map updater subagent

Launch **one** `generalPurpose` subagent.

**Subagent name:** `sandbox-map-updater-NN`

**Prompt must include:**
- Paths from `references/key-files.md`
- Implement top items from latest `score-NN.json` `nextActions` and `topBlockers`
- **Mandatory fog constraints** from `references/fog-two-layer.md`:
  - Never add overlay for `FogState.Visible`
  - Only tune `Unexplored` vs `Explored` via `FogNebulaOverlay.Config` or `FogVisualPalette`
  - `ResolveOverlayState` logic must remain: `anyVisible → null`
- Chunk edits: `MapGenerator.GenerateChunk`, `MapFeatureSpawner`, `sandbox.json`
- After edits: `dotnet build` + fog/grid tests
- **Do not** score — orchestrator re-runs score mode
- Return: files changed + visual summary + fog/perf notes

### Step 2 — Re-score

Re-run **Score mode** Steps A–B.

### Step 3 — Improve report

1. Load `scores/baseline.json` (or previous `score-NN.json`).
2. Write `scores/sandbox_map/improve-report.md` with:
   - Summary
   - Scores vs baseline
   - Category deltas (highlight **FogTwoLayer** and **PerfBudget**)
   - Top 5 fixes with file path + S/M/L effort
   - Test failures and next actions

## Score JSON schema

Aligned with `references/score-template.json`.

| Field | Type | Notes |
|-------|------|-------|
| `project_slug` | string | always `sandbox-map-polish` |
| `target` | string | always `sandbox_map` |
| `score_number` | int | 0 = baseline; 1+ = iteration |
| `scored_at` | ISO8601 | UTC timestamp |
| `scorer` | string | `director`, `manager`, `worker`, `ceo` |
| `mode` | string | `checklist` |
| `chunkVariety` | object | `{ score, max: 20, notes }` |
| `economyScatter` | object | `{ score, max: 20, notes }` |
| `fogTwoLayer` | object | `{ score, max: 20, notes }` |
| `explorationFeel` | object | `{ score, max: 20, notes }` |
| `perfBudget` | object | `{ score, max: 20, notes }` |
| `deductions` | array | `{ issue, points, category }` |
| `overallScore` | int | 0–100 |
| `passThreshold` | int | 80 |
| `passed` | bool | gate result |
| `fogGatePassed` | bool | `fogTwoLayer` ≥ 16 |
| `topBlockers` | string[] | highest-impact fixes |
| `testsRun` | string | command or `not run` |
| `testsPassed` | bool\|null | null if not run |
| `nextActions` | string[] | suggested work-order targets |

## Worker hints by low score

| Low category | Typical fixes |
|--------------|---------------|
| ChunkVariety | `MapGenerator.GenerateChunk` biome weights; terrain height/scatter seeds |
| EconomyScatter | `MapFeatureSpawner.SpawnChunkEconomy` density; `sandbox.json` tuning |
| FogTwoLayer | `FogNebulaOverlay.Config` alpha/rate split; **do not** veil Visible |
| ExplorationFeel | `RevealAreaAt`, `initialRevealRadius`, `FogOfWarSystem` sight range |
| PerfBudget | `chunkLoadRadius`, `MaxActiveChunks`, camera bounds cull in `Sync` |

## Rules

- Score **after** each meaningful map/fog change.
- Always write `score-NN.json` — artifacts for Director verify.
- **Never** add a third fog overlay tier or dim Visible cells.
- `FogNebulaOverlay.ResolveOverlayState` is the single source of truth for overlay presence.
- Chunk loading must stay lazy — `UpdateSandboxChunks` around camera, not full-map upfront.
- `dotnet build` must succeed before recording a passing score.
- Run `FogNebulaOverlayTests` after any fog change.

## Related skills

- `.grok/skills/combat-visual-polish/SKILL.md` — shares overlay/particle patterns (orthogonal scope)
- `.grok/skills/mesh-improvement-loop/SKILL.md` — map feature meshes (orthogonal)
- `.grok/skills/hud-button-quality/SKILL.md` — minimap HUD (orthogonal)