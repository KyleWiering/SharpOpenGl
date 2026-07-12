---
name: mesh-improvement-loop
description: >
  Run a 10-loop iterative ship mesh improvement pipeline for SharpOpenGl.
  Each loop: update geometry/textures/shadows from do-better.md, capture a
  full-ship screenshot, score the model, and write suggestions back.
  Pauses for user feedback after loop 5. Use when the user asks to improve
  a ship mesh, run a mesh loop, score a model, capture mesh-preview,
  /mesh-improvement-loop, or "do better" on a race/hull.
metadata:
  short-description: "10-loop ship mesh improvement with capture and scoring"
---

# Mesh Improvement Loop

Orchestrate **10 improvement loops** for one race/hull ship in SharpOpenGl.
Each loop uses two subagents: **mesh-updater** (edits) and **mesh-scorer** (capture + score + suggestions).

## Inputs (ask if missing)

| Param | Example | Notes |
|-------|---------|-------|
| `race` | `vesper` | Race id in `GameData/Config/race_visuals.json` |
| `hull` | `fighter_basic` | Hull id / ship definition id |
| `start_loop` | `1` | First loop number (continue from prior work if needed) |
| `loop_count` | `10` | Total loops to run (default 10) |

Resolve repo root as `REPO`. All paths below are relative to `REPO`.

## Directory layout

```
model-improvement/<race>/<hull>/do-better.md     # living brief — read every loop
model-improvement/<race>/<hull>/scores/loop-NN.json
mesh-loop-NN.png                                  # screenshot in REPO root (required)
```

## Phase 0 — Bootstrap (loop 1 only, or if `do-better.md` missing)

1. Create `model-improvement/<race>/<hull>/` and `scores/` if needed.
2. If no `do-better.md`, copy `references/do-better-template.md` from this skill and fill in race/hull.
3. Read current mesh state (≤3 files):
   - `SharpOpenGl.Engine/Rendering/RaceShipMeshes.cs` — builder routing
   - Race-specific builder (grep `Build<race>` or vasudan/korath/etc.)
   - `SharpOpenGl.Engine/Rendering/RaceSurfaceDetail.cs` — accents, weapon/engine tris
4. Seed **Loop priorities** in `do-better.md`: shape/silhouette, textures (race + component zones), baked shadows/lighting, visual appeal (2010s hard-surface target).

Do **not** run subagents until `do-better.md` exists.

## Main loop (repeat for N = start_loop … start_loop + loop_count - 1)

### Step A — Mesh updater subagent

Launch **one** `generalPurpose` subagent per loop.

**Subagent name:** `mesh-updater-loop-NN`

**Prompt must include:**
- Full path to `model-improvement/<race>/<hull>/do-better.md`
- Instruction: implement **this loop's** items under "Next loop focus" and "Remaining gaps"
- Scope: geometry, vertex colors/material bands, race textures, component texture zones (engine ~0.48 lum, weapon ~0.36 lum), lighting/shadows, mesh-preview camera if needed
- Key edit targets (open only what you touch):
  - `SharpOpenGl.Engine/Rendering/RaceShipMeshes.cs`
  - `SharpOpenGl.Engine/Rendering/RaceSurfaceDetail.cs`
  - `SharpOpenGl.Engine/Rendering/RaceMeshWriter.cs` (`HullMaterial` luminance)
  - `GameData/Config/race_visuals.json` (palette)
  - `SharpOpenGl.Engine/Rendering/GameShaders.cs` (only if texture/lighting change needed)
  - `SharpOpenGl/EngineWindow.MeshPreview.cs` (camera/scale for full-ship framing)
- After edits: `dotnet build` (fix errors before returning)
- **Do not** capture screenshot or update score — scorer handles that
- Return: list of files changed + 2–3 sentence summary of visual changes

### Step B — Mesh scorer subagent

Launch **one** `generalPurpose` subagent after updater finishes.

**Subagent name:** `mesh-scorer-loop-NN`

**Prompt must include:**
- Run capture (PowerShell, from `REPO`):

```powershell
dotnet run --project SharpOpenGl -- --mesh-preview --race <race> --hull <hull> --screenshot-path mesh-loop-NN.png
dotnet run --project SharpOpenGl -- --score-mesh --race <race> --hull <hull> --screenshot-path mesh-loop-NN.png --output model-improvement/<race>/<hull>/scores/loop-NN.json
dotnet test SharpOpenGl.Tests --filter "FullyQualifiedName~Export_and_score" --no-build
```

- Verify `mesh-loop-NN.png` exists in **REPO root** (not skill dir, not subfolder)
- Parse `loop-NN.json` for `TotalScore`, category breakdown, `Suggestions`
- Update `do-better.md`:
  - Set **Loop** to NN, **Last score** to total + screenshot subscore
  - Append row to **Score history** table
  - Replace **Remaining gaps** from lowest-scoring categories
  - Replace **Next loop focus** with concrete, actionable items derived from `Suggestions` and category notes
  - Add one line under **Screenshots**: `mesh-loop-NN.png` + brief visual note
- Return: total score, screenshot path, top 3 improvement priorities for next loop

### Step C — Orchestrator checkpoint

After each loop, tell the user:
- Loop number and total score (delta vs previous if known)
- Path to `mesh-loop-NN.png`
- One-line summary of what changed

**After loop 5 (when N == start_loop + 4): STOP.**

Ask the user for feedback before loops 6–10:
- What looks wrong in the latest screenshot?
- Priorities: shape, textures, engines/weapons, lighting, era (1990s vs 2010s)?
- Continue as-is, pivot, or cap triangle budget?

Do **not** start loop 6 until the user replies. Merge feedback into `do-better.md` **User feedback (loop 5)** section, then continue.

**After loop 10:** Summarize score trend, best loop, and recommend whether to keep or revert.

## do-better.md contract

The brief drives both subagents. Keep sections:

1. **Header** — race, hull, loop, last score
2. **Score history** — table of loops
3. **What works** — preserve across loops
4. **Remaining gaps** — from scorer categories
5. **Next loop focus** — 3–5 bullet tasks for updater
6. **User feedback (loop 5)** — filled after pause
7. **Workflow** — capture commands (for humans)

Template: `references/do-better-template.md` in this skill.

## Scoring categories (for scorer subagent)

| Category | Max | Levers |
|----------|-----|--------|
| Silhouette | 20 | aspect ratio, forward/readability, height |
| Geometry | 20 | triangle count (penalty >180 tris) |
| Materials | 20 | luminance band variety |
| Proportions | 15 | envelope vs hull profile |
| SurfaceDetail | 15 | keel lines, accent ratio (~10% target) |
| Screenshot | 10 | contrast, edges, foreground fill — needs PNG |

## Rules

- **Full ship** must be visible in every capture (mesh-preview mode, not cropped HUD).
- Screenshot filename: `mesh-loop-NN.png` in **REPO root** every loop.
- One loop = one updater + one scorer; never skip capture.
- Prefer incremental edits; avoid rewriting entire builders unless brief says so.
- Do not edit unrelated ships/races unless brief expands scope.
- If capture fails, scorer fixes build/runtime, retries once, then reports blocker.
- Component textures blend on race hulls via vertex luminance; engines/weapons on dedicated geometry use `ComponentTextureIndex` — mention in brief when relevant.

## Quick start (orchestrator)

User: "Run mesh improvement loop for vesper fighter_basic"

1. Bootstrap `model-improvement/vesper/fighter_basic/do-better.md` if missing
2. For loops 1–5: updater → scorer → report → screenshot path
3. Pause — ask user for feedback
4. For loops 6–10: merge feedback → updater → scorer → report
5. Final summary with best `mesh-loop-NN.png`