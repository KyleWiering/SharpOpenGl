---
name: menu-continuity
description: >
  Score and improve menu flow continuity for SharpOpenGl: navigation paths,
  MenuTheme cohesion, back/Esc patterns, and screen transitions from MainMenu
  through gameplay pause/save/load. Use when running /menu-continuity score
  or improve, or Manager sections under superior-menu-skills menu continuity.
argument-hint: "score|improve [flow-id]"
metadata:
  short-description: "Menu flow continuity scoring and improve loop"
---

# Menu Continuity

Orchestrate **scored evaluation** and **improve reporting** for menu navigation and visual continuity across the UI stack. Complements `form-widget-quality` (per-screen widgets) and `hud-button-quality` (gameplay HUD) — scores are **orthogonal**.

Full rubric: `.grok/org/superior-menu-skills/rubrics/menu-continuity-rubric.md`

## Invocation

```
/menu-continuity score [flow]
/menu-continuity improve [flow]
```

| Param | Example | Required | Notes |
|-------|---------|----------|-------|
| `mode` | `score` | yes | `score` or `improve` |
| `flow` | `full_menu_tour` | no | Defaults to `full_menu_tour` |

### Flow IDs

| Flow ID | Path |
|---------|------|
| `campaign_entry` | MainMenu → MissionSelect → Briefing → Loading → Gameplay |
| `pause_cycle` | Gameplay → Pause → Resume / Save / Load / Settings |
| `save_load` | MainMenu ↔ LoadGame; Pause → SaveGame |
| `full_menu_tour` | All primary menu screens + one gameplay pause (baseline default) |

**Quick start examples:**

- `/menu-continuity score` — baseline checklist for `full_menu_tour`
- `/menu-continuity score campaign_entry` — campaign path only
- `/menu-continuity improve pause_cycle` — re-score pause flow, diff vs baseline, emit improve report
- `Re-score after menu UX changes` — compare against `baseline.json`

Resolve repo root as `REPO`. Paths below are relative to `REPO`.

## When to use

- Manager section: `section-menu-continuity`
- CEO re-score after worker menu navigation changes
- User asks to audit menu flow, theme consistency, or back/Esc behaviour
- Regression gate before menu-system-suite sign-off

## Scoring model

| Category | JSON key | Max | Measures |
|----------|----------|-----|----------|
| NavConsistency | `navConsistency` | 20 | Expected paths; no dead ends; star map → briefing → start |
| ThemeCohesion | `themeCohesion` | 20 | `MenuTheme` colours, fonts, backgrounds |
| FlowLogic | `flowLogic` | 20 | Campaign entry, pause cycle, save/load coherence |
| BackCancelPatterns | `backCancelPatterns` | 20 | Back, Esc, cancel → correct prior screen |
| ScreenTransitions | `screenTransitions` | 20 | Loading states; context preserved; desktop/browser parity |
| **Overall** | `overallScore` | **100** | Sum of five categories |

**Regression gate:** `overallScore` ≥ 80 **and** every category ≥ 16 (`categoryFloor`).

## Directory layout

```
.grok/org/superior-menu-skills/rubrics/menu-continuity-rubric.md
.grok/org/superior-menu-skills/artifacts/continuity-flow-map.md   # canonical flow map
.grok/org/superior-menu-skills/scores/menu-continuity/baseline.json
.grok/org/superior-menu-skills/scores/menu-continuity/<target>/score-NN.json
.grok/skills/menu-continuity/references/score-template.json
.grok/skills/menu-continuity/references/improve-report-template.md
.grok/skills/menu-continuity/references/continuity-flow-map.md    # summary + link
```

## Pattern parity

Aligned with `build-tree-playability` and `form-widget-quality`: **evaluate → JSON artifact → gate → improve report**.

| Aspect | build-tree-playability | menu-continuity |
|--------|------------------------|-----------------|
| Rubric | `playability-rubric.md` | `menu-continuity-rubric.md` |
| Baseline | `scores/baseline.json` | `scores/menu-continuity/baseline.json` |
| Loop artifact | `loop-NN.json` | `score-NN.json` |
| Modes | checklist / playthrough | checklist (+ optional manual walkthrough) |
| Pass gate | overall ≥80, categories ≥16 | same |

## Core engine concepts

### UIManager screen stack

`SharpOpenGl.Engine/UI/UIManager.cs` manages a LIFO stack of `UIScreen` instances:

- `Push` / `Pop` / `Replace` / `Clear`
- Input routed to `Current` screen: `HandleKey`, `HandlePointerTapped`
- Overlays (`IsOverlay = true`) render atop prior screen — Pause, SaveGame, MissionVictory

Wiring lives in `SharpOpenGl/EngineWindow.cs`, `EngineWindow.Gameplay.cs`, `EngineWindow.SaveLoad.cs`.

### MenuTheme

`SharpOpenGl.Engine/UI/MenuTheme.cs` — shared colours and `ApplyNavButton` / `ApplyPanel` / `ApplyScreenTitle`.

`MenuStarfieldBackground` (`MenuStarfieldBackground.cs`) — animated background on title-stack screens.

### Back / Escape per transition

| Context | Back button | Escape |
|---------|-------------|--------|
| MissionSelect | `Pop` → MainMenu | `Pop` (stack > 1) |
| Briefing | `Pop` → MissionSelect | `Pop` |
| LoadGame / Settings / Multiplayer | `Pop` → caller | `Pop` |
| Pause | Resume button | `Pop` (resume) |
| SaveGame overlay | Cancel → `Pop` → Pause | — |
| Playing (HUD) | — | Push Pause (or cancel placement mode) |

Full per-edge tables: `.grok/org/superior-menu-skills/artifacts/continuity-flow-map.md`

## Phase 0 — Bootstrap

1. Read rubric + flow map + ≤3 context files:
   - `SharpOpenGl.Engine/UI/UIManager.cs`
   - `SharpOpenGl.Engine/UI/MenuTheme.cs`
   - Active flow's primary screen `.cs` (e.g. `MissionSelectScreen.cs`)
2. If `scores/menu-continuity/baseline.json` is **missing**, run **score** for `full_menu_tour` first and write baseline.
3. Note top 3 `topBlockers` for improve mode.
4. Confirm `dotnet build` succeeds before recording any score.

Do **not** schedule menu worker changes until baseline exists.

## Score workflow

### Step A — Evaluate

1. Run continuity tests:

```powershell
dotnet test SharpOpenGl.Tests --filter "FullyQualifiedName~UIManager|MissionSelect|SaveLoad|MultiplayerSetup|Briefing"
```

2. Walk rubric checklist for the active flow (see rubric per-transition table).
3. Walk flow-map edges for the target (`campaign_entry`, `pause_cycle`, `save_load`, or all for `full_menu_tour`).
4. Apply rubric deductions; record evidence in category `notes`.
5. **Optional manual walkthrough:** launch desktop/browser, perform each edge; add `--screenshot` captures per transition.

### Step B — Write score JSON

1. Copy `references/score-template.json`.
2. Set `target` to flow id; increment `score_number` (zero-pad filename: `score-01.json`).
3. Fill five category scores (0–20), `deductions`, `overallScore`.
4. Set `passed` = `overallScore` ≥ 80 and all categories ≥ `categoryFloor` (16).
5. List `topBlockers` (≤4) and `nextActions` with screen `.cs` paths.
6. Record `testsRun`, `testsPassed`, `flowEdgesWalked`.
7. Save to `.grok/org/superior-menu-skills/scores/menu-continuity/<target>/score-NN.json`.
8. If this is the first project score, also write `baseline.json` (same content, `target: "full_menu_tour"`, `score_number: 0`).

### Step C — Gate

| Result | Action |
|--------|--------|
| Pass (≥80, all ≥16) | Section may sign off for this flow |
| Any category < 16 | Run **improve** mode; file work order for lowest category |
| Tests failed | Fix or document blocker; do not mark passed |

## Improve workflow

1. Re-run **score** for the same `flow` (produces new `score-NN.json`).
2. Load `baseline.json` (or prior score) for comparison.
3. Fill `references/improve-report-template.md`:
   - Score delta table
   - Top navigation/theme breaks with screen `.cs` paths and S/M/L effort
   - Flow-specific edge table
   - Suggested work orders (P0/P1)
4. Emit report inline or save as `.grok/org/superior-menu-skills/scores/menu-continuity/<target>/improve-NN.md`.
5. Do **not** merge scores from `form-widget-quality` or `hud-button-quality`.

## Score JSON schema

Aligned with `references/score-template.json`.

| Field | Type | Notes |
|-------|------|-------|
| `project_slug` | string | `superior-menu-skills` |
| `skill` | string | `menu-continuity` |
| `target` | string | flow id |
| `score_number` | int | 0 = baseline |
| `scored_at` | ISO8601 | UTC |
| `scorer` | string | `worker`, `manager`, `ceo` |
| `mode` | string | `checklist` (default) |
| `categories` | object | five keys per rubric |
| `deductions` | array | `{ issue, points, category }` |
| `overallScore` | int | 0–100 |
| `passThreshold` | int | 80 |
| `categoryFloor` | int | 16 |
| `passed` | bool | gate result |
| `topBlockers` | string[] | highest-impact fixes |
| `testsRun` | string | command |
| `testsPassed` | bool\|null | |
| `nextActions` | string[] | work-order hints |
| `flowEdgesWalked` | string[] | edges evaluated |
| `screenshots` | string[] | optional capture paths |

## Test suites (cite on every score)

| Suite | File | Validates |
|-------|------|-----------|
| `UIManagerTests` | `SharpOpenGl.Tests/UI/UIManagerTests.cs` | Stack push/pop/replace/clear |
| `MissionSelectScreenTests` | `SharpOpenGl.Tests/UI/MissionSelectScreenTests.cs` | Star map, Start Mission |
| `SaveLoadScreenTests` | `SharpOpenGl.Tests/UI/SaveLoadScreenTests.cs` | Pause save, load slots |
| `MultiplayerSetupScreenTests` | `SharpOpenGl.Tests/UI/MultiplayerSetupScreenTests.cs` | Skirmish setup |

Briefing layout: `ScreenTextBoundsTests` (Briefing section).

## Worker hints by low category

| Low category | Typical fixes |
|--------------|---------------|
| `navConsistency` | Wire missing `*Requested` handlers in `EngineWindow.cs` |
| `themeCohesion` | Apply `MenuTheme.ApplyPanel` on overlay screens |
| `flowLogic` | Wire `LoadingScreen`; add Pause → Load path |
| `backCancelPatterns` | Fix `BackRequested` / `HandleEscapePressed` routing |
| `screenTransitions` | Preserve mission/slot context; browser parity in `BrowserGameHost` |

## Related skills

- `.grok/skills/menu-system-suite/SKILL.md` — unified orchestrator (invokes this skill + siblings)
- `.grok/skills/form-widget-quality/SKILL.md` — per-screen widget quality (orthogonal)
- `.grok/skills/hud-button-quality/SKILL.md` — gameplay HUD overlay (orthogonal)
- `.grok/skills/build-tree-playability/SKILL.md` — scoring loop pattern reference
- `.grok/skills/manager/SKILL.md` — section iteration ownership

## Rules

- Score **after** meaningful menu changes, not before.
- Always write `score-NN.json` — scores are artifacts for Director verify.
- `dotnet build` must succeed before recording a passing score.
- This section requires **no gameplay C# changes** for skill/rubric work — documentation and scoring only.
- Stay inside flow map; do not conflate with widget or HUD rubrics.