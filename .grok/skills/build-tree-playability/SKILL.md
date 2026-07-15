---
name: build-tree-playability
description: >
  Score and iterate RTS build-tree UX for SharpOpenGl: builder ship flow,
  icon build menus, tiered unlocks, font legibility, and skirmish parity.
  Each loop: play or checklist-evaluate, score against playability-rubric.md,
  write loop-NN.json, fix blockers. Use when improving build tree UX,
  /build-tree-playability, or Manager sections under build-tree-ux project.
argument-hint: "<section-id> [loop-number] [mode]"
metadata:
  short-description: "50-loop build-tree UX playability scoring"
---

# Build Tree Playability Loop

Orchestrate **scored iteration loops** for build-tree UX in SharpOpenGl. Complements `mesh-improvement-loop` (geometry) with **interaction and readability** scoring.

Full rubric: `.grok/org/build-tree-ux/rubrics/playability-rubric.md`

## Invocation

```
/build-tree-playability <section-id> [loop-number] [mode]
```

| Param | Example | Required | Notes |
|-------|---------|----------|-------|
| `section_id` | `section-builder-ux` | yes | Manager section being scored |
| `loop` | `3` | no (default 1) | Loop number within section |
| `mode` | `checklist` | no (default `checklist`) | `checklist` or `playthrough` |
| `target` | `mission_build_tree` | playthrough only | Mission/map id for manual playthrough |

**Quick start examples:**

- `Score section-builder-ux loop 1` — checklist evaluate, write `loop-01.json`
- `/build-tree-playability section-font-legibility 2 playthrough` — mission playthrough score
- `Re-score baseline after UX changes` — compare against `scores/baseline.json`

Resolve repo root as `REPO`. Paths below are relative to `REPO`.

## When to use

- Manager sections: `section-builder-ux`, `section-font-legibility`, `section-build-tree-data`, `section-build-mission`, `section-skirmish-build`, `section-playability-rubric`
- CEO re-score after worker changes
- User asks to score build menu intuitiveness or run build-tree iteration

## Scoring model

| Category | Max | In Overall | Primary section |
|----------|-----|------------|-----------------|
| Discoverability | 20 | yes | section-builder-ux |
| BuildFlow | 20 | yes | section-builder-ux |
| VisualClarity | 20 | yes | section-builder-ux |
| Legibility | 20 | yes | section-font-legibility |
| TreeDepth | 20 | yes | section-build-tree-data |
| **Overall** | **100** | | sum of five primaries |
| SkirmishParity | 20 | **no** (supplemental) | section-skirmish-build |

`overallScore` = sum of the five primary categories only. `skirmishParity` is reported separately unless CEO requests weighted merge.

## Directory layout (artifact paths)

```
.grok/org/build-tree-ux/rubrics/playability-rubric.md     # category definitions + checklists
.grok/org/build-tree-ux/scores/baseline.json              # loop 0 pre-improvement snapshot
.grok/org/build-tree-ux/scores/<section-id>/loop-NN.json  # per-iteration scores
.grok/org/build-tree-ux/scores/README.md                  # 50-loop budget + pass gates
.grok/org/build-tree-ux/scripts/mission_build_tree-playthrough.md  # playthrough checklist (v1 manual)
.grok/skills/build-tree-playability/references/score-template.json
```

## Pattern parity with mesh-improvement-loop

Both skills share an **evaluate → write loop JSON → gate → repeat** structure. They are **orthogonal** — never merge mesh geometry scores into playability scores.

| Aspect | mesh-improvement-loop | build-tree-playability |
|--------|----------------------|------------------------|
| Rubric | `references/evaluation-rubric.md` | `rubrics/playability-rubric.md` |
| Baseline | `do-better.md` + loop 1 seed | `scores/baseline.json` (loop 0) |
| Loop artifact | `model-improvement/.../scores/loop-NN.json` | `scores/<section-id>/loop-NN.json` |
| Score template | embedded in `--score-mesh` CLI | `references/score-template.json` |
| Evaluate modes | capture + automated scorer | checklist (default) or manual playthrough |
| Pass threshold | per-asset total ≥ target | overall ≥80; section categories ≥16 |
| Loop budget | 10 per asset | 50 project-wide (see README) |
| Subagents | mesh-updater + mesh-scorer | Manager workers (no subagents in v1) |
| Primary metric | Silhouette, Geometry, Materials (visual) | Discoverability, BuildFlow, UX readability |

Reference: `.grok/skills/mesh-improvement-loop/SKILL.md` — Phase 0 bootstrap, main loop Steps A–C, orchestrator checkpoint, score history trend.

## Phase 0 — Bootstrap (loop 1 per project, or if baseline missing)

1. Read rubric + current UX files (≤3):
   - `SharpOpenGl.Engine/UI/Widgets/BuildMapPanel.cs`
   - `SharpOpenGl.Engine/UI/Widgets/ShipControlBar.cs`
   - `SharpOpenGl.Engine/GameData/build_map.json`
2. If `scores/baseline.json` is missing, score current state via checklist → write baseline (loop 0, `section_id: "baseline"`).
3. Note top 3 deductions / `topBlockers` to fix first.
4. Confirm `dotnet build` succeeds before recording any score.

Do **not** schedule UX worker changes until baseline exists.

## Scoring loop (repeat per Manager iteration)

### Step A — Evaluate

**Checklist mode (fast, default):**

1. Run tests (section filter when applicable):

```powershell
dotnet test SharpOpenGl.Tests --filter "FullyQualifiedName~Build|FullyQualifiedName~UI"
```

2. Walk checklist in `playability-rubric.md` for section-relevant categories.
3. Apply rubric deductions (overflow, missing icons, placement bugs, etc.).
4. Optional: launch game and verify builder → Build → place one structure.

**Playthrough mode (mission / skirmish):**

1. Follow `.grok/org/build-tree-ux/scripts/mission_build_tree-playthrough.md` (or skirmish script when available).
2. Score all five primary categories.
3. Score `skirmishParity` when evaluating `section-skirmish-build` or project sign-off.

### Step B — Write score

1. Copy `references/score-template.json`.
2. Replace `<section-id>` with the active Manager section (e.g. `section-builder-ux`).
3. Fill category scores (0–20 each), `notes`, and `deductions` array.
4. Set `overallScore` = sum of discoverability + buildFlow + visualClarity + legibility + treeDepth.
5. Set `passed` = `overallScore` ≥ `passThreshold` (80) and section-owned categories ≥16.
6. List `topBlockers` (≤4) and `nextActions` for Manager work orders.
7. Record `testsRun` and `testsPassed`.
8. Save to `.grok/org/build-tree-ux/scores/<section-id>/loop-NN.json` (zero-pad NN, e.g. `loop-01.json`).

### Step C — Gate (orchestrator checkpoint)

After each loop report:

- `overallScore` and delta vs previous loop (or baseline)
- Lowest category + deduction summary
- `testsPassed` status
- One-line blocker summary

| Result | Action |
|--------|--------|
| `overallScore` ≥ 80 and section categories ≥ 16 | Section may sign off (plus tests green) |
| Any primary category < 14 | Schedule another loop; file work order for lowest category |
| Loop budget exhausted | Escalate to CEO with score trend |

**After loop 5 (optional):** pause for user feedback on UX direction.

**After project budget (~50 loops):** report score trend, best loop per section, baseline delta.

## Score JSON schema

Aligned with `scores/baseline.json`. Copy from `references/score-template.json`.

| Field | Type | Notes |
|-------|------|-------|
| `project_slug` | string | always `build-tree-ux` |
| `section_id` | string | Manager section; `baseline` for loop 0 |
| `loop` | int | 0 = baseline; 1+ = iteration |
| `scored_at` | ISO8601 | UTC timestamp |
| `scorer` | string | `director`, `manager`, `ceo` |
| `mode` | string | `checklist` or `playthrough` |
| `target` | string\|null | mission/map id for playthrough |
| `categories` | object | six keys; five primaries + supplemental `skirmishParity` |
| `categories.*.score` | int\|null | 0–20; `skirmishParity` may be null |
| `categories.*.max` | int | always 20 |
| `categories.*.notes` | string | evidence for score |
| `deductions` | array | `{ issue, points, category }` — remove empty template row when saving |
| `overallScore` | int | sum of five primaries (0–100) |
| `passThreshold` | int | 80 |
| `passed` | bool | gate result for this loop |
| `topBlockers` | string[] | highest-impact fixes |
| `testsRun` | string | command or `not run` |
| `testsPassed` | bool\|null | null if not run |
| `nextActions` | string[] | suggested section/work-order targets |

## Category → section mapping

| Category | Primary owner section |
|----------|----------------------|
| Discoverability | section-builder-ux |
| BuildFlow | section-builder-ux |
| VisualClarity | section-builder-ux |
| Legibility | section-font-legibility |
| TreeDepth | section-build-tree-data |
| SkirmishParity | section-skirmish-build |

## Worker hints by low score

| Low category | Typical fixes |
|--------------|---------------|
| Discoverability | ShipControlBar Build visibility, HUD hint text |
| BuildFlow | `EngineWindow.BuildMap.cs` placement/cancel wiring |
| VisualClarity | `BuildMapPanel` icons, color states |
| Legibility | `GLUIRenderer.GetCharSegments`, `UIFontMetrics`, `UITextDrawing` |
| TreeDepth | `build_map.json` prerequisites, entity costs |
| SkirmishParity | `GameData/Maps/*.json` spawns, skirmish start resources |

## 50-loop budget

See `.grok/org/build-tree-ux/scores/README.md`. CEO tracks total loops across sections; do not exceed 50 without approval.

| Section | Suggested loops |
|---------|-----------------|
| section-playability-rubric | 2 |
| section-build-tree-data | 10 |
| section-font-legibility | 8 |
| section-builder-ux | 15 |
| section-build-mission | 8 |
| section-skirmish-build | 7 |
| **Total** | **50** |

CEO may reallocate unused loops to the lowest-scoring category.

## Rules

- Score **after** each meaningful UX change, not before.
- Always write `loop-NN.json` — scores are artifacts for Director verify.
- Do **not** conflate mesh scores (`model-improvement/.../loop-NN.json`) with playability scores.
- `dotnet build` must succeed before recording a passing loop.
- This section (playability rubric) requires **no gameplay C# changes** — documentation and scoring only.

## Related skills

- `.grok/skills/mesh-improvement-loop/SKILL.md` — ship/station geometry (orthogonal)
- `.grok/skills/manager/SKILL.md` — section iteration ownership
- `.grok/skills/director/SKILL.md` — plan and verify sections