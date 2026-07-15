# Menu Continuity — Improve Report

**Target flow:** `<flow-id>`  
**Baseline:** `.grok/org/superior-menu-skills/scores/menu-continuity/baseline.json`  
**Re-score:** `.grok/org/superior-menu-skills/scores/menu-continuity/<target>/score-NN.json`  
**Generated:** `<ISO8601>`

## Score delta

| Category | Baseline | Current | Δ |
|----------|----------|---------|---|
| navConsistency | | | |
| themeCohesion | | | |
| flowLogic | | | |
| backCancelPatterns | | | |
| screenTransitions | | | |
| **Overall** | | | |

**Gate:** overall ≥80, each category ≥16 — **PASS / FAIL**

## Top navigation / theme breaks

List highest-impact continuity issues (≤5). Each row must cite a screen `.cs` path and effort.

| # | Issue | Category | Screen file(s) | Effort | Fix hint |
|---|-------|----------|----------------|--------|----------|
| 1 | | | `SharpOpenGl.Engine/UI/Screens/<Screen>.cs` | S/M/L | |
| 2 | | | | | |
| 3 | | | | | |

**Effort key:** S = doc/test only or single handler wire; M = one screen + EngineWindow hook; L = multi-screen + scene/load pipeline.

## Flow-specific breaks

### `<flow-id>`

| Edge | Symptom | Root cause file | Suggested change |
|------|---------|-----------------|------------------|
| e.g. Briefing → Gameplay | No loading feedback | `EngineWindow.Gameplay.cs` | Push `LoadingScreen` before scene transition |
| | | | |

## Regression gate

- [ ] `overallScore` ≥ 80
- [ ] All categories ≥ 16
- [ ] `dotnet build` succeeds
- [ ] UI continuity tests green

## Suggested work orders

| Priority | Action | Owner section | Files |
|----------|--------|---------------|-------|
| P0 | | section-menu-continuity | |
| P1 | | | |

## Orthogonal skills (do not merge scores)

- **form-widget-quality** — per-screen widget text/layout on a single screen
- **hud-button-quality** — gameplay HUD button hit targets during play

## Tests to re-run

```powershell
dotnet test SharpOpenGl.Tests --filter "FullyQualifiedName~UIManager|MissionSelect|SaveLoad|MultiplayerSetup|Briefing"
```