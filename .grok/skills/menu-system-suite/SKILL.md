---
name: menu-system-suite
description: >
  Meta-orchestrator for SharpOpenGl menu UI quality: runs form-widget-quality,
  hud-button-quality, and menu-continuity in sequence, merges weighted scores,
  applies resolution-matrix viewport gates, UI test regression, and baseline
  comparison. Modes score, improve, full. Use for /menu-system-suite or
  superior-menu-skills project sign-off.
argument-hint: "<mode> [target]"
metadata:
  short-description: "Unified menu UI quality scoring orchestrator"
---

# Menu System Suite

Orchestrate **unified UI quality scoring** across three child skills plus a resolution-matrix gate. Complements `build-tree-playability` (build-tree UX) — **do not merge** build-tree scores into suite scores.

**Project:** `.grok/org/superior-menu-skills/`  
**Rubrics:** `rubrics/form-widget-rubric.md`, `hud-button-rubric.md`, `menu-continuity-rubric.md`, `resolution-matrix.md`

## Invocation

```
/menu-system-suite <mode> [target]
```

| Param | Example | Required | Notes |
|-------|---------|----------|-------|
| `mode` | `score` | yes | `score` \| `improve` \| `full` |
| `target` | `main_menu_flow` | no (default `default`) | Screen/flow id scored by child skills |

**Quick start examples:**

- `/menu-system-suite score` — run all three child score modes, merge, write suite JSON
- `/menu-system-suite score main_menu_flow` — score specific target
- `/menu-system-suite improve` — score first, then emit improve report from template
- `/menu-system-suite full default` — score + resolution matrix + improve in one pass

Resolve repo root as `REPO`. Paths below are relative to `REPO`.

## When to use

- Manager section `section-menu-system-suite` sign-off
- CEO re-score after menu/HUD worker changes
- User asks for unified menu quality score or menu regression check
- Before release when all three child skills have baselines

## Child skills (dependency)

| Skill | Path | Status |
|-------|------|--------|
| form-widget-quality | `.grok/skills/form-widget-quality/SKILL.md` | required |
| hud-button-quality | `.grok/skills/hud-button-quality/SKILL.md` | required |
| menu-continuity | `.grok/skills/menu-continuity/SKILL.md` | required |

If a child SKILL.md is missing, run a **paper walkthrough** using rubrics directly and document blockers in the suite report. Do not block suite delivery — score from rubric checklists manually until child skills exist.

## Directory layout (artifact paths)

```
.grok/org/superior-menu-skills/rubrics/resolution-matrix.md
.grok/org/superior-menu-skills/scores/form-widget-quality/<target>/score-NN.json
.grok/org/superior-menu-skills/scores/hud-button-quality/<target>/score-NN.json
.grok/org/superior-menu-skills/scores/menu-continuity/<target>/score-NN.json
.grok/org/superior-menu-skills/scores/suite/<target>/score-NN.json
.grok/org/superior-menu-skills/scores/suite/baseline.json
.grok/org/superior-menu-skills/references/improve-report.md
.grok/org/superior-menu-skills/scores/README.md
.grok/skills/menu-system-suite/references/unified-score-template.json
```

## Pattern parity with build-tree-playability

Both skills share **evaluate → write JSON artifact → gate → repeat**. They are **orthogonal**.

| Aspect | build-tree-playability | menu-system-suite |
|--------|------------------------|-------------------|
| Rubric | `playability-rubric.md` | three child rubrics + resolution matrix |
| Baseline | `scores/baseline.json` | per-skill baselines + `scores/suite/baseline.json` |
| Loop artifact | `loop-NN.json` | `score-NN.json` per skill + suite merge |
| Score template | `references/score-template.json` | `references/unified-score-template.json` |
| Evaluate modes | checklist / playthrough | child `score` + resolution walk |
| Pass threshold | overall ≥80; categories ≥16 | weighted ≥80 + all gates |
| Primary metric | Build-tree UX | Menu forms + HUD + continuity |

Reference: `.grok/skills/build-tree-playability/SKILL.md`

## Orchestration sequence

Run child skills **in sequence** (checklist order; parallel optional for human operators):

### Step 1 — form-widget-quality

```
/form-widget-quality score [target]
```

- Read rubric: `rubrics/form-widget-rubric.md`
- Target mapping: `default` → `all_forms`; or use explicit target (`main_menu`, `settings`, etc.)
- Write: `scores/form-widget-quality/<target>/score-NN.json`
- Gate: `overallScore` ≥80 and each category ≥16/20

### Step 2 — hud-button-quality

```
/hud-button-quality score [target]
```

- Read rubric: `rubrics/hud-button-rubric.md`
- Target mapping: `default` → `gameplay_hud`
- Write: `scores/hud-button-quality/<target>/score-NN.json`
- Gate: same as form-widget

### Step 3 — menu-continuity

```
/menu-continuity score [target]
```

- Read rubric: `rubrics/menu-continuity-rubric.md`
- Target mapping: `default` → `full_menu_tour`
- Write: `scores/menu-continuity/<target>/score-NN.json`
- Gate: same as form-widget

### Step 4 — Resolution matrix

Walk `.grok/org/superior-menu-skills/rubrics/resolution-matrix.md` per viewport:

| Viewport ID | Dimensions | How to evaluate |
|-------------|------------|-----------------|
| `desktop_default` | 1024×768 | Default `dotnet run --project SharpOpenGl` window |
| `reference_logical` | 1920×1080 | `UIScaler.ReferenceSize`; scaler integration tests |
| `browser_viewport` | varies (~390×844) | `dotnet run --project SharpOpenGl.Browser` or documented WASM path |

For each applicable screen/region row, score Pass (1), Minor (0.5), or Fail (0).  
**Viewport pass:** ≥90% of applicable rows (≥1.0 equivalent).  
**Suite resolution gate:** all three viewports must pass.

### Step 5 — Regression gate (UI tests)

```powershell
dotnet test SharpOpenGl.Tests --filter "FullyQualifiedName~UI"
```

Record `uiTestsPassed` and failing test FQNs in `regressionGate.uiTestsFailed[]`.

### Step 6 — Merge suite JSON

1. Copy `references/unified-score-template.json`
2. Embed child score summaries from Step 1–3
3. Fill `resolutionGate` from Step 4
4. Compute `weightedOverall` (see below)
5. Run baseline comparison (Step 7)
6. Save: `.grok/org/superior-menu-skills/scores/suite/<target>/score-NN.json`

### Step 7 — Improve (mode = `improve` or `full`)

Fill `.grok/org/superior-menu-skills/references/improve-report.md` template.  
Suggested output: `.grok/org/superior-menu-skills/reports/<target>/improve-NN.md`

## Unified scoring model

| Component | Weight | Source |
|-----------|--------|--------|
| form-widget-quality overall | 30% | `scores/form-widget-quality/<target>/score-NN.json` |
| hud-button-quality overall | 30% | `scores/hud-button-quality/<target>/score-NN.json` |
| menu-continuity overall | 30% | `scores/menu-continuity/<target>/score-NN.json` |
| resolution-matrix gate | 10% | 100 if all viewports pass; 0 otherwise |

**Formula:**

```
weightedOverall = 0.30 × formWidget.overallScore
                + 0.30 × hudButton.overallScore
                + 0.30 × menuContinuity.overallScore
                + 0.10 × resolutionGateScore
```

Suite `passed` only if **all** of:

- `weightedOverall` ≥ **80**
- Each child skill `passed` (overall ≥80 **and** every category floor ≥16/20)
- Resolution matrix: **all three viewports** pass (≥90% applicable rows each)
- `regressionGate.uiTestsPassed` = true
- `dotnet build` succeeds

## Regression gates

On every suite run, cite on failure:

```powershell
dotnet test SharpOpenGl.Tests --filter "FullyQualifiedName~UI"
```

Include failing test FQNs in suite JSON `regressionGate.uiTestsFailed[]`.

**Baseline regression** (see `scores/README.md`):

- Suite fails regression if unified drops **>5 pts** from baseline
- Or any child skill regresses
- Or any resolution viewport flips pass → fail

## Baseline comparison

| Artifact | When written |
|----------|--------------|
| `scores/form-widget-quality/baseline.json` | First form-widget score |
| `scores/hud-button-quality/baseline.json` | First hud-button score |
| `scores/menu-continuity/baseline.json` | First menu-continuity score |
| `scores/suite/baseline.json` | First suite merge (after child baselines exist) |

Per-target scores: `scores/suite/<target>/score-NN.json`

Compare `weightedOverall` and per-skill `overallScore` against baseline; set `baselineComparison.deltaOverall`, `regressed`, and child deltas in suite JSON. Flag regressions in improve report §1.

## Screenshot hook

Capture evidence per screen before or during resolution walk:

```powershell
# Navigate to screen in game, then:
dotnet run --project SharpOpenGl -- --screenshot --screenshot-path artifacts/ui-<screen>-1024.png

# Gameplay HUD — start mission/skirmish first:
dotnet run --project SharpOpenGl -- --screenshot --screenshot-path artifacts/ui-hud-1024.png
```

CLI flags are parsed in `SharpOpenGl/Program.cs` (`--screenshot`, `--screenshot-path`).  
`EngineWindow.cs` captures after frame 5 in screenshot mode.

Store paths in suite JSON `evidence.screenshots[]`.

**Suggested screenshot set for `default` target:**

| Screen | Path |
|--------|------|
| MainMenu | `artifacts/ui-mainmenu-1024.png` |
| MissionSelect | `artifacts/ui-missionselect-1024.png` |
| Briefing | `artifacts/ui-briefing-1024.png` |
| Settings | `artifacts/ui-settings-1024.png` |
| Gameplay HUD | `artifacts/ui-hud-1024.png` |

## Accessibility floor

Document and enforce during browser viewport evaluation:

- **Min touch target:** 44×44 logical px (browser viewport)
- **Contrast:** verify text vs `MenuTheme` fills (`ButtonText` on `ButtonNormal`, `BodyTextColor` on `PanelBackground`, etc.) — see `SharpOpenGl.Engine/UI/MenuTheme.cs`
- **Pointer routing:** touch tap via `UIManager.HandlePointerTapped`; no hover-only critical paths on browser
- **Checklist:** resolution-matrix `browser_viewport` section + form-widget `Accessibility` category

## Paper walkthrough (end-to-end checklist)

Use when child skills are missing or for first suite validation. A human operator can complete this without automation.

### Prerequisites

- [ ] `dotnet build` succeeds
- [ ] Rubrics and resolution-matrix read
- [ ] Target chosen (default: `default`)

### A — Bootstrap build + tests

```powershell
dotnet build
dotnet test SharpOpenGl.Tests --filter "FullyQualifiedName~UI"
```

Note pass/fail count and any failing FQNs.

### B — Score form-widget (paper)

1. Open `rubrics/form-widget-rubric.md` for target `all_forms`
2. Walk TextFit, Legibility, Contrast, Layout, Accessibility checklists
3. Score each category 0–20; sum → `overallScore`
4. Set `passed` = overall ≥80 and all categories ≥16
5. Write `scores/form-widget-quality/all_forms/score-01.json` (use child template when available; else mirror rubric fields)

### C — Score hud-button (paper)

1. Open `rubrics/hud-button-rubric.md` for target `gameplay_hud`
2. Run filtered tests: `FullyQualifiedName~HudTextFit|ShipControlBar|BuildMapPanel|GameplayHUD`
3. Score HitTarget, StateFeedback, VisualClarity, LabelFit, HUDDensity
4. Write `scores/hud-button-quality/gameplay_hud/score-01.json`

### D — Score menu-continuity (paper)

1. Open `rubrics/menu-continuity-rubric.md` for target `full_menu_tour`
2. Walk NavigationFlow, ThemeConsistency, TransitionContinuity, ScreenParity, BackNav
3. Optionally launch game: MainMenu → MissionSelect → Briefing → back; Pause cycle
4. Write `scores/menu-continuity/full_menu_tour/score-01.json`

### E — Resolution matrix

1. Open `rubrics/resolution-matrix.md`
2. For each menu screen + HUD region, score rows at 1024×768, 1920×1080, browser
3. Compute `passRate` per viewport; mark `passed` if ≥0.90
4. Capture screenshots (§ Screenshot hook) for failed rows

### F — Merge suite JSON

1. Copy `references/unified-score-template.json` → `scores/suite/default/score-01.json`
2. Fill `childScores` from B–D
3. Fill `resolutionGate` from E
4. Compute `weightedOverall`
5. Fill `regressionGate` from step A
6. Add `evidence.screenshots[]`
7. If `scores/suite/baseline.json` missing and children have baselines, write baseline; else compare deltas

### G — Gate decision

| Check | Pass? |
|-------|-------|
| weightedOverall ≥ 80 | |
| All child skills passed | |
| All resolution viewports passed | |
| UI tests green | |
| No baseline regression | |

If mode is `improve` or `full`, complete improve report from `references/improve-report.md`.

### H — Report

- List `topBlockers` (≤4) and `nextActions` in suite JSON
- Manager uses improve report for work orders

## Score JSON schema (suite)

Copy from `references/unified-score-template.json`.

| Field | Type | Notes |
|-------|------|-------|
| `project_slug` | string | `superior-menu-skills` |
| `skill` | string | `menu-system-suite` |
| `target` | string | e.g. `default` |
| `scoreNumber` | int | matches `score-NN` |
| `scored_at` | ISO8601 | UTC |
| `childScores` | object | paths + embedded summaries for three skills |
| `resolutionGate` | object | `desktop_default`, `reference_logical`, `browser_viewport` |
| `weightedComponents` | object | weight, score, contribution per component |
| `weightedOverall` | int | 0–100 |
| `passThreshold` | int | 80 |
| `passed` | bool | all gates |
| `regressionGate` | object | `uiTestsRun`, `uiTestsPassed`, `uiTestsFailed[]` |
| `evidence.screenshots` | string[] | artifact paths |
| `baselineComparison` | object | `baselinePath`, `deltaOverall`, `regressed` |
| `topBlockers` | string[] | highest-impact fixes |
| `nextActions` | string[] | ordered follow-ups |

## Target mapping (`default`)

| Child skill | Default target when suite target = `default` |
|-------------|-----------------------------------------------|
| form-widget-quality | `all_forms` |
| hud-button-quality | `gameplay_hud` |
| menu-continuity | `full_menu_tour` |

Custom suite targets should document child target mapping in the score JSON `notes` field.

## Rules

- Score **after** meaningful UI changes, not before.
- Always write `score-NN.json` — scores are artifacts for Director/Manager verify.
- Do **not** merge `build-tree-playability` scores into suite scores.
- Do **not** merge mesh scores into suite scores.
- `dotnet build` must succeed before recording a passing suite score.
- Child skill rewrites are out of scope — only invoke and merge.

## Related skills

- `.grok/skills/form-widget-quality/SKILL.md` — menu form/widget scoring (child)
- `.grok/skills/hud-button-quality/SKILL.md` — gameplay HUD button scoring (child)
- `.grok/skills/menu-continuity/SKILL.md` — menu flow and continuity scoring (child)
- `.grok/skills/build-tree-playability/SKILL.md` — build-tree UX (orthogonal; do not merge scores)
- `.grok/skills/manager/SKILL.md` — section iteration ownership
- `.grok/skills/director/SKILL.md` — plan and verify sections