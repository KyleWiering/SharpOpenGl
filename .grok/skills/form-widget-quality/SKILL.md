---
name: form-widget-quality
description: >
  Score and improve menu form/widget UX for SharpOpenGl: text fit, legibility,
  button sizing, form layout, and visual hierarchy across MainMenu, Settings,
  Briefing, Save/Load, and MultiplayerSetup screens. Each loop: checklist-evaluate,
  write score-NN.json, gate vs baseline. Use when improving menu forms,
  /form-widget-quality, or Manager sections under superior-menu-skills project.
argument-hint: "<mode> <screen>"
metadata:
  short-description: "Menu form/widget quality scoring and improve reports"
---

# Form Widget Quality

Orchestrate **scored evaluation loops** for menu and form UI in SharpOpenGl. Complements `build-tree-playability` (BuildMapPanel/HUD legibility) with **menu screen and form widget** scoring.

Full rubric: `.grok/org/superior-menu-skills/rubrics/form-widget-rubric.md`

## Invocation

```
/form-widget-quality <mode> <screen>
```

| Param | Example | Required | Notes |
|-------|---------|----------|-------|
| `mode` | `score` \| `improve` | yes | First positional after skill name |
| `screen` | `main_menu` | yes | Target ID from rubric evaluation targets |

**Quick start examples:**

- `/form-widget-quality score main_menu` — checklist evaluate MainMenu, write `score-NN.json`
- `/form-widget-quality score settings` — score Settings form widgets
- `/form-widget-quality improve briefing` — re-score, diff vs baseline, emit improve report
- `/form-widget-quality score all_forms` — union checklist across all menu screens

Resolve repo root as `REPO`. Paths below are relative to `REPO`.

## When to use

- Manager section: `section-form-widget-quality`
- CEO re-score after menu UX worker changes
- User asks to score menu button legibility, form layout, or run form-widget iteration
- Suite orchestrator (`menu-system-suite`) requests per-skill score input

## Scoring model

| Category | Max | Measures |
|----------|-----|----------|
| TextFit | 20 | Labels/buttons stay inside bounds; no clipping or overflow |
| Legibility | 20 | Glyph clarity at 12–16px; segment font spot-check |
| ButtonSizing | 20 | Button/touch targets via `UIScaler`; browser ≥44×44 logical px |
| FormLayout | 20 | Spacing, alignment, panel hierarchy, scroll when needed |
| VisualHierarchy | 20 | Contrast via `MenuTheme`; primary/secondary actions; disabled states |
| **Overall** | **100** | sum of five categories |

`overallScore` = sum of textFit + legibility + buttonSizing + formLayout + visualHierarchy.

## SharpOpenGl paths

| Area | Path |
|------|------|
| Scaling | `SharpOpenGl.Engine/UI/UIScaler.cs` |
| Theme | `SharpOpenGl.Engine/UI/MenuTheme.cs` |
| Layout | `SharpOpenGl.Engine/UI/AdaptiveLayout.cs` |
| Widgets | `SharpOpenGl.Engine/UI/Widgets/Button.cs`, `Label.cs`, `Panel.cs` |
| Text | `SharpOpenGl.Engine/UI/UITextDrawing.cs`, `UIFontMetrics.cs` |
| Main menu | `SharpOpenGl.Engine/UI/Screens/MainMenuScreen.cs` |
| Settings | `SharpOpenGl.Engine/UI/Screens/SettingsScreen.cs` |
| Briefing | `SharpOpenGl.Engine/UI/Screens/BriefingScreen.cs` |
| Save | `SharpOpenGl.Engine/UI/Screens/SaveGameScreen.cs` |
| Load | `SharpOpenGl.Engine/UI/Screens/LoadGameScreen.cs` |
| Multiplayer | `SharpOpenGl.Engine/UI/Screens/MultiplayerSetupScreen.cs` |

**Seed files (≤3 per evaluation):** `Button.cs`, `UITextDrawing.cs`, target screen `.cs`.

## Directory layout (artifact paths)

```
.grok/org/superior-menu-skills/rubrics/form-widget-rubric.md
.grok/org/superior-menu-skills/rubrics/resolution-matrix.md
.grok/org/superior-menu-skills/scores/form-widget-quality/baseline.json
.grok/org/superior-menu-skills/scores/form-widget-quality/<target>/score-NN.json
.grok/org/superior-menu-skills/scores/form-widget-quality/<target>/improve-report.md
.grok/skills/form-widget-quality/references/score-template.json
.grok/skills/form-widget-quality/references/improve-report-template.md
```

## Pattern parity with build-tree-playability

Both skills share an **evaluate → write JSON artifact → gate → repeat** structure. They are **orthogonal** — never merge build-tree playability scores into form-widget scores.

| Aspect | build-tree-playability | form-widget-quality |
|--------|------------------------|---------------------|
| Rubric | `rubrics/playability-rubric.md` | `rubrics/form-widget-rubric.md` |
| Baseline | `scores/baseline.json` | `scores/form-widget-quality/baseline.json` |
| Loop artifact | `scores/<section-id>/loop-NN.json` | `scores/form-widget-quality/<target>/score-NN.json` |
| Score template | `references/score-template.json` | `references/score-template.json` |
| Evaluate mode | checklist or playthrough | checklist (default) |
| Pass threshold | overall ≥80; categories ≥16 | overall ≥80; categories ≥16 |
| Improve mode | Manager work orders | improve report (no auto-implement) |
| Primary scope | BuildMapPanel, ShipControlBar | Menu screens, form widgets |

Reference: `.grok/skills/build-tree-playability/SKILL.md`

## Phase 0 — Bootstrap (first score per project, or if baseline missing)

1. Read rubric + seed files (≤3):
   - `SharpOpenGl.Engine/UI/Widgets/Button.cs`
   - `SharpOpenGl.Engine/UI/UITextDrawing.cs`
   - Target screen `.cs` (e.g. `MainMenuScreen.cs`)
2. Confirm `dotnet build` succeeds.
3. Run UI tests:

```powershell
dotnet test SharpOpenGl.Tests --filter "FullyQualifiedName~ScreenTextBounds|ButtonScaledText|UITextDrawing"
```

4. Walk checklist in `form-widget-rubric.md` for `all_forms` (or primary screen) at reference resolutions in `resolution-matrix.md`.
5. If `scores/form-widget-quality/baseline.json` is missing, score current state → write baseline (`target: all_forms`, `score_number: 0`).
6. Note top 3 `topBlockers` to fix first.

Do **not** schedule UX worker changes until baseline exists.

## Score mode workflow

### Step A — Evaluate

1. Run tests:

```powershell
dotnet test SharpOpenGl.Tests --filter "FullyQualifiedName~ScreenTextBounds|ButtonScaledText|UITextDrawing"
```

2. Walk per-screen checklist in `form-widget-rubric.md` for `<screen>` at:
   - 1024×768 (desktop default)
   - 1920×1080 (`UIScaler.ReferenceSize`)
   - browser viewport (touch target floor)
3. Apply rubric deductions (overflow, undersized buttons, contrast, layout overlap).
4. Optional: capture screenshot evidence (see Screenshot hook below).

### Step B — Write score

1. Copy `references/score-template.json`.
2. Set `target` to evaluation target (e.g. `main_menu`, `all_forms`).
3. Increment `score_number` (zero-pad filename: `score-01.json`).
4. Fill category scores (0–20 each), `notes`, and `deductions` array.
5. Set `overallScore` = sum of five categories.
6. Set `passed` = `overallScore` ≥ `passThreshold` (80) and all categories ≥16.
7. List `topBlockers` (≤4) and `nextActions` for Manager work orders.
8. Record `testsRun` and `testsPassed`.
9. Save to `.grok/org/superior-menu-skills/scores/form-widget-quality/<target>/score-NN.json`.

### Step C — Gate (orchestrator checkpoint)

After each score report:

- `overallScore` and delta vs previous score (or baseline)
- Lowest category + deduction summary
- `testsPassed` status
- One-line blocker summary

| Result | Action |
|--------|--------|
| `overallScore` ≥ 80 and all categories ≥ 16 | Target may sign off (plus tests green) |
| Any category < 14 | Schedule improve pass; file work order for lowest category |
| Tests failing | Block sign-off; cite failing test class |

## Improve mode workflow

Improve mode **does not auto-implement** fixes — it produces a prioritized report.

1. Run **score mode** first for `<screen>`.
2. Diff vs `baseline.json` (and previous `score-NN.json` if present).
3. Copy `references/improve-report-template.md` → fill Summary, category deltas, Top 5 fixes.
4. Each fix must include: issue description, file path, effort **S** / **M** / **L**.
5. Save to `.grok/org/superior-menu-skills/scores/form-widget-quality/<target>/improve-report.md`.
6. Return report to Manager/CEO for work order scheduling.

## Regression gate

| Gate | Threshold |
|------|-----------|
| Overall | ≥80 / 100 |
| Category floor | ≥16 / 20 per category |
| UI tests | Green for filter below |

Cite test evidence:

- `SharpOpenGl.Tests/UI/ScreenTextBoundsTests` — menu screen text within viewport/bounds
- `SharpOpenGl.Tests/UI/ButtonScaledTextTests` — scaled button labels at 1024×768
- `SharpOpenGl.Tests/UI/UITextDrawingTests` — wrap, truncate, content width helpers

```powershell
dotnet test SharpOpenGl.Tests --filter "FullyQualifiedName~ScreenTextBounds|ButtonScaledText|UITextDrawing"
```

## Screenshot hook

Capture menu screen evidence at reference resolution:

```powershell
# Navigate to target screen in-game first, then:
dotnet run --project SharpOpenGl -- --screenshot --screenshot-path artifacts/ui-<screen>-1024.png
```

For browser viewport, use Blazor devtools or documented WASM capture path. Store paths in score JSON `evidence.screenshots[]`.

Supported `<screen>` values for artifacts: `main_menu`, `settings`, `briefing`, `save_load`, `multiplayer_setup`.

## Score JSON schema

Aligned with `scores/form-widget-quality/baseline.json`. Copy from `references/score-template.json`.

| Field | Type | Notes |
|-------|------|-------|
| `project_slug` | string | always `superior-menu-skills` |
| `target` | string | `main_menu`, `settings`, `briefing`, `save_load`, `multiplayer_setup`, `all_forms` |
| `score_number` | int | 0 = baseline; 1+ = iteration |
| `scored_at` | ISO8601 | UTC timestamp |
| `scorer` | string | `worker`, `manager`, `ceo` |
| `mode` | string | `checklist` |
| `categories` | object | five keys: textFit, legibility, buttonSizing, formLayout, visualHierarchy |
| `categories.*.score` | int | 0–20 |
| `categories.*.max` | int | always 20 |
| `categories.*.notes` | string | evidence for score |
| `deductions` | array | `{ issue, points, category }` — remove empty template row when saving |
| `overallScore` | int | sum of five categories (0–100) |
| `passThreshold` | int | 80 |
| `passed` | bool | gate result |
| `topBlockers` | string[] | highest-impact fixes |
| `testsRun` | string | command or `not run` |
| `testsPassed` | bool\|null | null if not run |
| `nextActions` | string[] | suggested work-order targets |
| `evidence.screenshots` | string[] | optional capture paths |

## Evaluation targets → screens

| Target ID | Screen class(es) |
|-----------|------------------|
| `main_menu` | `MainMenuScreen` |
| `settings` | `SettingsScreen` |
| `briefing` | `BriefingScreen` |
| `save_load` | `SaveGameScreen`, `LoadGameScreen` |
| `multiplayer_setup` | `MultiplayerSetupScreen` |
| `all_forms` | Union of all above |

## Worker hints by low score

| Low category | Typical fixes |
|--------------|---------------|
| TextFit | `Button.cs` truncation, `Label.WrapWidth`, `ScrollPanel` for overflow |
| Legibility | `GLUIRenderer.GetCharSegments`, `UIFontMetrics`, menu `FontSize` floors |
| ButtonSizing | `UIScaler` / `ScaledUIRenderer`, `Button.Size` minimums for browser |
| FormLayout | `AdaptiveLayout`, screen anchor constants, row spacing in screen `.cs` |
| VisualHierarchy | `MenuTheme` palette, primary/secondary button styles, disabled alpha |

## Rules

- Score **after** each meaningful menu UX change, not before.
- Always write `score-NN.json` — scores are artifacts for Director verify.
- Do **not** conflate build-tree playability scores with form-widget scores.
- `dotnet build` must succeed before recording a passing score.
- Improve mode emits suggestions only — implementation requires explicit human/work-order approval.
- Docs and scoring artifacts only in this section — no gameplay C# unless blocked by missing test data (document instead).

## Related skills

- `.grok/skills/menu-system-suite/SKILL.md` — unified orchestrator (invokes this skill + siblings)
- `.grok/skills/hud-button-quality/SKILL.md` — gameplay HUD buttons (orthogonal)
- `.grok/skills/menu-continuity/SKILL.md` — menu flow and theme continuity (orthogonal)
- `.grok/skills/build-tree-playability/SKILL.md` — BuildMapPanel/HUD legibility (orthogonal scope; not menu forms)
- `.grok/skills/manager/SKILL.md` — section iteration ownership
- `.grok/skills/director/SKILL.md` — plan and verify sections