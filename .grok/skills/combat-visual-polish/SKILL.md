---
name: combat-visual-polish
description: >
  Score and iteratively improve combat and harvest visuals in SharpOpenGl:
  weapon VFX, mining/harvester beams, and combat feedback overlays. Goal is
  higher visual quality with LOWER GPU cost — overlays, billboards, line
  primitives, pooled particles, color-only RenderComponent tricks, and reused
  projectile meshes — not new heavy meshes per frame. Use when the user asks for
  /combat-visual-polish, weapons VFX, harvester beams, combat overlays, mining
  beam polish, or projectile readability improvements.
argument-hint: "<mode> [area]"
metadata:
  short-description: "Combat/harvest VFX scoring — higher quality, lower GPU cost"
---

# Combat Visual Polish

Orchestrate **scored evaluation loops** for weapons VFX, harvester/mining beams, and combat feedback in SharpOpenGl. Pattern parity with `hud-button-quality` and `mesh-improvement-loop`: **evaluate → JSON artifact → gate → improve**.

Full rubric: `references/combat-visual-rubric.md`  
Low-cost techniques: `references/render-tricks.md`  
Key paths: `references/key-files.md`

## Design principle: quality ↑, GPU cost ↓

Prefer techniques that add readability without per-frame mesh uploads or draw-call explosions:

| Prefer | Avoid |
|--------|-------|
| Pre-uploaded procedural meshes (`LoadProjectileMeshes`) | `UploadMesh` / `UploadProcedural` per projectile or per frame |
| Streak billboards, beam flashes, additive overlays | New unique hull meshes for every shot |
| Pooled `ParticleEmitter` with hard caps | Unbounded particle spawn |
| `RenderComponent` color/scale-only updates | Rebuilding geometry for color tweaks |
| Line primitives for beams (`BuildBeamStreak`, wave rings) | High-tri torpedo variants per weapon |

## Invocation

```
/combat-visual-polish score [area]
/combat-visual-polish improve [area]
```

| Param | Example | Required | Notes |
|-------|---------|----------|-------|
| `mode` | `score` | yes | `score` or `improve` |
| `area` | `weapons` | no (default `all`) | `weapons` · `harvest` · `combat-feedback` · `all` |

**Quick start examples:**

- `/combat-visual-polish score weapons` — weapon identity + projectile readability
- `/combat-visual-polish score harvest` — tractor/drone/EVA mining feedback
- `/combat-visual-polish improve combat-feedback` — hit flashes, shield pulses, attack hover
- `/combat-visual-polish score all` — full 100-pt rubric

Resolve repo root as `REPO`. Paths below are relative to `REPO`.

## Evaluation areas

| area (invocation) | Target ID | Primary systems |
|-------------------|-----------|-----------------|
| `weapons` | `weapons_vfx` | `WeaponProfiles`, `ProjectileSystem`, `EngineWindow.Projectiles.cs` |
| `harvest` | `harvest_vfx` | `MiningVisualSystem`, `HarvestOrbitSystem`, `EngineWindow.MiningVfx.cs` |
| `combat-feedback` | `combat_feedback` | `CombatSystem`, `AbilitySystem`, shield/attack hover pulses |
| `all` | `combat_visual_all` | All areas above |

## Scoring model (100 total)

| Category | Max | Measures |
|----------|-----|----------|
| Readability | 20 | Shots/beams readable at RTS zoom; origin→target trace clear |
| WeaponIdentity | 20 | Distinct silhouettes/colors per `WeaponVisualKind` |
| HarvestFeedback | 20 | Active mining obvious; mode-specific (drone/EVA/tractor) |
| PerformanceBudget | 20 | No per-frame uploads; particle/mesh caps respected |
| OverlayCraft | 20 | Additive overlays, billboards, line flashes feel intentional |
| **Overall** | **100** | sum of five categories |

**Regression gate:** `overallScore` ≥ 80 and each category ≥ 16/20.

When scoring a single `area`, score only relevant categories but still write all five (mark N/A areas with notes and full credit only if truly out of scope).

## Directory layout (artifact paths)

```
.grok/skills/combat-visual-polish/references/combat-visual-rubric.md
.grok/skills/combat-visual-polish/references/render-tricks.md
.grok/skills/combat-visual-polish/references/score-template.json
.grok/skills/combat-visual-polish/scores/baseline.json
.grok/skills/combat-visual-polish/scores/<target>/score-NN.json
.grok/skills/combat-visual-polish/scores/<target>/improve-report.md
```

## Phase 0 — Bootstrap (first score run, or if baseline missing)

1. Read rubric + seed files (≤3 per area):
   - **weapons:** `WeaponProfiles.cs`, `EngineWindow.Projectiles.cs`, `ProceduralMeshes.cs` (bolt/beam/pulse builders)
   - **harvest:** `MiningVisualSystem.cs`, `MiningVisualComponent.cs`, `EngineWindow.MiningVfx.cs`
   - **combat-feedback:** `CombatSystem.cs`, `AbilitySystem.cs`, `EngineWindow.cs` (attack hover / shield ring pulses)
2. Run tests:

```powershell
dotnet build
dotnet test SharpOpenGl.Tests --filter "FullyQualifiedName~CombatSystem|CombatBalance|MiningVisual|UtilityArticulation|Projectile"
```

3. If `scores/baseline.json` is missing, score `all` via checklist → write baseline (`target`: `combat_visual_all`, `score_number`: 0).
4. Note top 3 deductions / `topBlockers` and any **PerformanceBudget** red flags (per-frame upload, uncapped emitters).
5. Confirm `dotnet build` succeeds before recording any score.

## Score mode

### Step A — Evaluate (checklist)

1. Run tests (filter above).
2. Walk per-area checklists in `combat-visual-rubric.md`.
3. Audit GPU cost per `render-tricks.md`:
   - Projectile meshes registered once in `LoadProjectileMeshes`?
   - Mining beams use pooled overlays / line streaks, not new mesh per pulse?
   - Particle counts bounded (`FogNebulaOverlay` pattern applies to combat particles too)?
4. Optional gameplay capture:

```powershell
dotnet run --project SharpOpenGl -- --screenshot --screenshot-path artifacts/combat-<area>.png
```

### Step B — Write score

1. Copy `references/score-template.json`.
2. Set `target` to the target ID (e.g. `weapons_vfx`, `harvest_vfx`).
3. Set `area` to the invocation area.
4. Fill category scores (0–20 each), `notes`, and `deductions` array.
5. Set `overallScore` = sum of five categories.
6. Set `passed` = `overallScore` ≥ `passThreshold` (80) and all categories ≥ 16.
7. List `topBlockers` (≤4) and `nextActions` — prioritize **PerformanceBudget** regressions first.
8. Record `testsRun` and `testsPassed`.
9. Save to `.grok/skills/combat-visual-polish/scores/<target>/score-NN.json` (zero-pad NN).

### Step C — Gate

| Result | Action |
|--------|--------|
| `overallScore` ≥ 80 and all categories ≥ 16 | Area may sign off (plus tests green) |
| Any category < 16 | File work order for lowest category |
| `performanceBudget` < 16 | **Block merge** — fix GPU regressions before polish |
| `testsPassed` false | Fix failing combat/render tests before claiming pass |

## Improve mode

Improve mode runs a **single iteration loop**: updater subagent → re-score → report.

### Step 1 — Visual updater subagent

Launch **one** `generalPurpose` subagent.

**Subagent name:** `combat-visual-updater-<area>-NN`

**Prompt must include:**
- Active `area` and paths from `references/key-files.md`
- Implement top items from latest `score-NN.json` `nextActions` and `topBlockers`
- **Mandatory constraints** from `references/render-tricks.md`:
  - No per-frame `UploadMesh` / `UploadProcedural`
  - Reuse `WeaponProfiles.MeshKey` and pre-registered projectile VAOs
  - Mining beams: line streaks, color pulses, `TractorBeamVisualComponent` — not new geometry per tick
  - Particle caps and emitter pooling
- After edits: `dotnet build`
- **Do not** score — orchestrator re-runs score mode
- Return: files changed + visual summary + performance notes

### Step 2 — Re-score

Re-run **Score mode** Steps A–B for the same `area`.

### Step 3 — Improve report

1. Load `scores/baseline.json` (or previous `score-NN.json`).
2. Write `scores/<target>/improve-report.md` with:
   - Summary (one paragraph)
   - Scores vs baseline / previous iteration
   - Category deltas (highlight **PerformanceBudget** and **Readability**)
   - Top 5 fixes with file path + S/M/L effort
   - Test failures (if any) and next actions

## Score JSON schema

Aligned with `references/score-template.json`.

| Field | Type | Notes |
|-------|------|-------|
| `project_slug` | string | always `combat-visual-polish` |
| `target` | string | target ID |
| `area` | string | `weapons` · `harvest` · `combat-feedback` · `all` |
| `score_number` | int | 0 = baseline; 1+ = iteration |
| `scored_at` | ISO8601 | UTC timestamp |
| `scorer` | string | `director`, `manager`, `worker`, `ceo` |
| `mode` | string | `checklist` |
| `readability` | object | `{ score, max: 20, notes }` |
| `weaponIdentity` | object | `{ score, max: 20, notes }` |
| `harvestFeedback` | object | `{ score, max: 20, notes }` |
| `performanceBudget` | object | `{ score, max: 20, notes }` |
| `overlayCraft` | object | `{ score, max: 20, notes }` |
| `deductions` | array | `{ issue, points, category }` |
| `overallScore` | int | 0–100 |
| `passThreshold` | int | 80 |
| `passed` | bool | gate result |
| `topBlockers` | string[] | highest-impact fixes |
| `testsRun` | string | command or `not run` |
| `testsPassed` | bool\|null | null if not run |
| `nextActions` | string[] | suggested work-order targets |

## Worker hints by low score

| Low category | Typical fixes |
|--------------|---------------|
| Readability | Stronger bolt length/alpha; beam origin flash; wave ring scale curve in `BuildProjectileModelMatrix` |
| WeaponIdentity | Distinct `WeaponProfiles` colors; unique `MeshKey` per `WeaponVisualKind`; avoid defaulting to laser_bolt |
| HarvestFeedback | `MiningVisualSystem` tractor pulse interval; drone shuttle visibility; `EngineWindow.MiningVfx.cs` beam draw |
| PerformanceBudget | Remove per-frame uploads; cap particles; reuse VAOs from `LoadProjectileMeshes` |
| OverlayCraft | Additive blend passes; short-lived billboard flashes; line-mode wave rings |

## Rules

- Score **after** each meaningful VFX change, not before.
- Always write `score-NN.json` — scores are artifacts for Director verify.
- **Never** trade PerformanceBudget for overlay flash — fix cost first, then polish.
- Projectile mesh keys must stay in sync: `WeaponProfiles.MeshKey` ↔ `LoadProjectileMeshes` ↔ `ProceduralMeshes` builders.
- `dotnet build` must succeed before recording a passing score.
- Harvest modes (`Drones`, `Eva`, `TractorBeam`) each need distinct low-cost feedback — do not clone laser bolts for all three.

## Related skills

- `.grok/skills/mesh-improvement-loop/SKILL.md` — hull/station geometry (orthogonal)
- `.grok/skills/hud-button-quality/SKILL.md` — HUD UX (orthogonal)
- `.grok/skills/sandbox-map-polish/SKILL.md` — fog/map overlays (shares overlay patterns)