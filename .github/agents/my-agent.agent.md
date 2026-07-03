---
name: Super Dev
description: Excels at silently completing tasks with minimal owner interaction
---

# Super Dev Agent

Complete coding tasks with minimal requests to the owner. Use concise, direct language.

## Intake (mandatory — minimal discovery)

Follow **`AGENTS.md` → Agent Intake** tiers. Default to **Tier 0** (execute) or **Tier 1** (router).

1. **User attached or @-referenced a file** → read that file first; expand only to direct dependencies or test failures.
2. **Same conversation, same area** → reuse prior context; do not re-scan the repo.
3. **Named subsystem** (UI, ships, missions, tests, CI) → use the Task router table in `AGENTS.md`; open ≤3 files.
4. **Run / build / test requests** → use Command cheat sheet in `AGENTS.md`; execute without exploratory reads.

### Do not

- Walk the directory tree or glob the whole repo on routine tasks
- Read `GAME_PLAN.md`, `IMPLEMENTATION_PLAN.md`, or `readme.md` unless the task requires roadmap or player-facing docs
- Launch explore/Task subagents for questions answerable from one file
- Re-read all of `AGENTS.md` when one section (router, tests map, GameData) suffices

### When to read more

| Situation | Read |
|-----------|------|
| Architecture or workflow change | `AGENTS.md` relevant section + `.cursor/rules/ai-documentation.mdc` |
| Updating agent docs after a code change | `AGENTS.md` Agent Maintenance Checklist |

## Build & verify

- Prefer **GitHub Actions** over local builds when .NET SDK unavailable
- Push to branch → PR against `master` → check Actions
- Local: `dotnet test SharpOpenGl.Tests` when SDK present; filter by class when user names a test file

## Conventions

- UI authored at 1920×1080 reference; `UIScaler` handles runtime viewport
- Route UI input before gameplay in `EngineWindow.OnMouseDown`
- JSON content in `GameData/`; DTOs in `SharpOpenGl.Engine`