---
name: hud-button-quality
description: >
  Score and improve gameplay HUD button quality for SharpOpenGl: discoverability,
  hit targets, label clarity, state feedback, and HUD density. Each loop:
  checklist-evaluate against hud-button-rubric.md, write score-NN.json, gate.
  Use when improving HUD UX, /hud-button-quality, or Manager sections under
  superior-menu-skills project.
argument-hint: "<mode> [hud-area]"
metadata:
  short-description: "HUD button quality scoring — score and improve modes"
---

# HUD Button Quality

Orchestrate **scored evaluation loops** for gameplay HUD buttons in SharpOpenGl. Pattern parity with `build-tree-playability` and `form-widget-quality`: **evaluate → JSON artifact → gate**.

Full rubric: `.grok/org/superior-menu-skills/rubrics/hud-button-rubric.md`

## Invocation

```
/hud-button-quality score [hud-area]
/hud-button-quality improve [hud-area]
```

| Param | Example | Required | Notes |
|-------|---------|----------|-------|
| `mode` | `score` | yes | `score` or `improve` |
| `hud-area` | `GameplayHUD` | no (default `GameplayHUD`) | One of: GameplayHUD, ShipControlBar, ResourceBar, BuildPanel, UnitInfoPanel |

**Quick start examples:**

- `/hud-button-quality score GameplayHUD` — checklist evaluate full HUD; write `score-01.json`
- `/hud-button-quality score ShipControlBar` — ship bar command buttons only
- `/hud-button-quality improve BuildPanel` — re-score, diff vs baseline, emit improve report

Resolve repo root as `REPO`. Paths below are relative to `REPO`.

## When to use

- Manager section: `section-hud-button-quality`
- CEO re-score after worker HUD changes
- User asks to score HUD button discoverability, hit targets, or label fit

## Evaluation targets

| hud-area (invocation) | Target ID | Primary widgets |
|-----------------------|-----------|-----------------|
| `GameplayHUD` | `gameplay_hud` | Full `GameplayHUD` composite |
| `ShipControlBar` | `ship_control` | `ShipControlBar` command buttons |
| `ResourceBar` | `resource_bar` | `ResourceBar` affordances |
| `BuildPanel` | `build_panel` | `BuildMapPanel` / `BuildPanel` |
| `UnitInfoPanel` | `unit_info` | `UnitInfoPanel` action buttons |

## Scoring model

| Category | Max | Measures |
|----------|-----|----------|
| Discoverability | 20 | HUD commands findable; builder/build-panel affordances obvious |
| HitTarget | 20 | Clickable area ≥ minimum; logical coords via `UIScaler` |
| LabelClarity | 20 | Command/cost labels readable; icon + label pairing |
| StateFeedback | 20 | Hover, pressed, disabled, locked, unaffordable distinct |
| HudDensity | 20 | Panels don't overlap at 1024×768 default window |
| **Overall** | **100** | sum of five categories |

**Regression gate:** `overallScore` ≥ 80 and each category ≥ 16/20.

## Directory layout (artifact paths)

```
.grok/org/superior-menu-skills/rubrics/hud-button-rubric.md
.grok/org/superior-menu-skills/scores/hud-button-quality/baseline.json
.grok/org/superior-menu-skills/scores/hud-button-quality/<target>/score-NN.json
.grok/org/superior-menu-skills/scores/hud-button-quality/<target>/improve-report.md
.grok/skills/hud-button-quality/references/score-template.json
.grok/skills/hud-button-quality/references/improve-report-template.md
```

## Pattern parity with build-tree-playability and form-widget-quality

| Aspect | build-tree-playability | form-widget-quality | hud-button-quality |
|--------|------------------------|---------------------|-------------------|
| Rubric | `playability-rubric.md` | `form-widget-rubric.md` | `hud-button-rubric.md` |
| Baseline | `scores/baseline.json` | `scores/.../baseline.json` | `scores/hud-button-quality/baseline.json` |
| Loop artifact | `loop-NN.json` | `score-NN.json` | `score-NN.json` |
| Evaluate mode | checklist / playthrough | checklist | checklist |
| Pass threshold | overall ≥80; category ≥16 | overall ≥80; category ≥16 | overall ≥80; category ≥16 |
| Scope | Build-tree flow UX | Menu forms | Gameplay HUD buttons |

These skills are **orthogonal** — never merge menu form scores into HUD scores or vice versa.

## Phase 0 — Bootstrap (first score run, or if baseline missing)

1. Read rubric + seed files (≤3):
   - `SharpOpenGl.Engine/UI/Screens/GameplayHUD.cs`
   - Target widget `.cs` (e.g. `ShipControlBar.cs`, `BuildMapPanel.cs`)
   - `SharpOpenGl.Engine/UI/Widgets/Button.cs` or `SharpOpenGl.Engine/UI/UIScaler.cs`
2. Run HUD tests:

```powershell
dotnet build
dotnet test SharpOpenGl.Tests --filter "FullyQualifiedName~HudTextFit|ShipControlBar|BuildMapPanel|GameplayHUD|Minimap|UnitInfo"
```

3. If `scores/hud-button-quality/baseline.json` is missing, score `GameplayHUD` via checklist → write baseline (`target`: `gameplay_hud`, `score_number`: 0).
4. Note top 3 deductions / `topBlockers`.
5. Confirm `dotnet build` succeeds before recording any score.

## Score mode

### Step A — Evaluate (checklist)

1. Run tests (filter above).
2. Walk per-widget checklists in `hud-button-rubric.md` for the active `hud-area`.
3. Apply rubric deductions (clip, cramped hit rect, obscure affordance, etc.).
4. Optional screenshot:

```powershell
dotnet run --project SharpOpenGl -- --screenshot --screenshot-path artifacts/hud-<target>.png
```

### Step B — Write score

1. Copy `references/score-template.json`.
2. Set `target` to the target ID (e.g. `gameplay_hud`, `ship_control`).
3. Fill category scores (0–20 each), `notes`, and `deductions` array.
4. Set `overallScore` = sum of discoverability + hitTarget + labelClarity + stateFeedback + hudDensity.
5. Set `passed` = `overallScore` ≥ `passThreshold` (80) and all categories ≥ 16.
6. List `topBlockers` (≤4) and `nextActions`.
7. Record `testsRun` and `testsPassed`.
8. Save to `.grok/org/superior-menu-skills/scores/hud-button-quality/<target>/score-NN.json` (zero-pad NN, e.g. `score-01.json`).

### Step C — Gate

| Result | Action |
|--------|--------|
| `overallScore` ≥ 80 and all categories ≥ 16 | Target may sign off (plus tests green) |
| Any category < 16 | File work order for lowest category |
| `testsPassed` false | Fix failing HUD tests before claiming pass |

## Improve mode

1. Re-run **Score mode** Steps A–B for the same `hud-area`.
2. Load `scores/hud-button-quality/baseline.json` (or previous `score-NN.json`).
3. Copy `references/improve-report-template.md` → `scores/hud-button-quality/<target>/improve-report.md`.
4. Fill Summary, Scores vs baseline, Category deltas.
5. Emit **Top 5 fixes** with file path + S/M/L effort — prioritize **HitTarget** and **LabelClarity** regressions first.
6. List test failures (if any) and next actions.

## Score JSON schema

Aligned with `references/score-template.json`.

| Field | Type | Notes |
|-------|------|-------|
| `project_slug` | string | always `superior-menu-skills` |
| `target` | string | target ID (e.g. `gameplay_hud`) |
| `score_number` | int | 0 = baseline; 1+ = iteration |
| `scored_at` | ISO8601 | UTC timestamp |
| `scorer` | string | `director`, `manager`, `worker`, `ceo` |
| `mode` | string | `checklist` |
| `discoverability` | object | `{ score, max: 20, notes }` |
| `hitTarget` | object | `{ score, max: 20, notes }` |
| `labelClarity` | object | `{ score, max: 20, notes }` |
| `stateFeedback` | object | `{ score, max: 20, notes }` |
| `hudDensity` | object | `{ score, max: 20, notes }` |
| `deductions` | array | `{ issue, points, category }` |
| `overallScore` | int | sum of five categories (0–100) |
| `passThreshold` | int | 80 |
| `passed` | bool | gate result |
| `topBlockers` | string[] | highest-impact fixes |
| `testsRun` | string | command or `not run` |
| `testsPassed` | bool\|null | null if not run |
| `nextActions` | string[] | suggested work-order targets |

## Key source files (read ≤3 per evaluation)

| hud-area | Primary files |
|----------|---------------|
| GameplayHUD | `GameplayHUD.cs`, `Button.cs`, `UIScaler.cs` |
| ShipControlBar | `ShipControlBar.cs`, `Button.cs`, `GameplayHUD.cs` |
| ResourceBar | `ResourceBar.cs`, `GameplayHUD.cs`, `UIScaler.cs` |
| BuildPanel | `BuildMapPanel.cs`, `BuildPanel.cs`, `Button.cs` |
| UnitInfoPanel | `UnitInfoPanel.cs`, `GameplayHUD.cs`, `Button.cs` |

## HUD tests to cite

| Test class | Evidence for |
|------------|--------------|
| `HudTextFitTests` | LabelClarity, HudDensity |
| `ShipControlBarTests` | HitTarget, StateFeedback |
| `ShipControlBarBuilderTests` | Discoverability, LabelClarity |
| `BuildMapPanelTests` | HitTarget, StateFeedback |
| `GameplayHUDInputTests` | HitTarget, Discoverability |
| `MinimapTests` | HitTarget (GameplayHUD composite) |
| `UnitInfoPanelShieldTests` | LabelClarity |
| `UnitInfoPanelMiningTests` | LabelClarity |

## Worker hints by low score

| Low category | Typical fixes |
|--------------|---------------|
| Discoverability | `GameplayHUD.cs` top-bar labels; `ShipControlBar` Build visibility |
| HitTarget | `ShipControlBar` button sizes; `BuildMapPanel` tile hit rects |
| LabelClarity | `Button.TooltipHint`; `UIFontMetrics.FitFontSize`; `HudTextFitTests` |
| StateFeedback | `Button` colour states; `BuildMapPanel` locked/unaffordable colours |
| HudDensity | `GameplayHUD` anchor positions; panel sizes at 1024×768 |

## Rules

- Score **after** each meaningful HUD change, not before.
- Always write `score-NN.json` — scores are artifacts for Director verify.
- Do **not** conflate menu form scores (`form-widget-quality`) or build-tree scores (`build-tree-playability`) with HUD button scores.
- `dotnet build` must succeed before recording a passing score.
- Coordinate rules: physical coords → `UIManager.HandlePointerTapped`; logical coords → `UIScreen`/`Widget`.

## Related skills

- `.grok/skills/menu-system-suite/SKILL.md` — unified orchestrator (invokes this skill + siblings)
- `.grok/skills/form-widget-quality/SKILL.md` — menu forms (orthogonal)
- `.grok/skills/menu-continuity/SKILL.md` — menu flow and theme continuity (orthogonal)
- `.grok/skills/build-tree-playability/SKILL.md` — build-tree flow UX (orthogonal)
- `.grok/skills/manager/SKILL.md` — section iteration ownership
- `.grok/skills/director/SKILL.md` — plan and verify sections