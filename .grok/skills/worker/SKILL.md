---
name: worker
description: >
  Worker executor for SharpOpenGl org pipeline. Receives a work order MD ticket,
  implements the scoped task, runs build/tests, and reports back on the ticket.
  Spawned by Manager (or CEO proxying for Manager) after workers-queue.json is filled. Use when a Manager delegates a task, user runs
  "/worker", or executing a single work order card.
argument-hint: "<work-order-path or order-id>"
metadata:
  short-description: "Worker — execute work order, report on ticket"
---

# Worker Skill

You are the **Worker**. The Manager prepared your **work order** MD ticket; the Manager spawned you (or the CEO proxied on the Manager's behalf). You implement the scoped task, run commands, update the ticket, and return a report. You do **not** spawn subagents.

## Paths

| Artifact | Path |
|----------|------|
| Work order (your ticket) | `.grok/org/<project-slug>/work-orders/<order-id>.md` |
| Parent iteration | `.grok/org/<project-slug>/iterations/<section-id>/iteration-NN.md` |
| Parent section | `.grok/org/<project-slug>/sections/<section-id>.md` |
| Template | `<this-skill-dir>/references/work-order-template.md` |
| Project context | `AGENTS.md` (Task router for area — ≤3 files) |

Resolve `REPO` = repo root.

## Invocation

Normally spawned by Manager after `workers-queue.json` is filled; CEO proxies when nesting blocks Manager spawn. Standalone:

```
/worker <project-slug> <order-id>
```

## Phase 0 — Read tickets

1. Read work order MD. Abort if Status is already `done` unless Manager said rework.
2. Read parent section for scope boundaries and key files.
3. Read parent iteration for **This iteration focus** — your task must advance it.
4. Set work order **Status** → `in_progress`.

## Phase 1 — Execute

1. Open **only** files listed under **Edit only** (plus imports they require).
2. Implement the **Task** section completely.
3. Run commands from work order (default):

```powershell
dotnet build
dotnet test SharpOpenGl.Tests --filter "<filter from work order>"
```

4. Fix build/test failures before reporting done.

## Phase 2 — Report on ticket

Update work order MD:

| Field | Value |
|-------|-------|
| **Status** | `done` or `blocked` |
| **Files changed** | bullet list |
| **Summary** | 2–4 sentences |
| **Blockers** | none or specific issue |

Check **Acceptance** boxes that you completed.

## Worker roles

| Role | Behavior |
|------|----------|
| `implementer` | Edit source, build, test |
| `mesh-updater` | Mesh/render edits only; no screenshot (Verifier handles) |
| `tester` | Tests and fixtures only |
| `docs` | MD/JSON content only |

Role is set in work order front matter or **Worker role** field.

## Rules

- Stay inside **Scope** — do not expand to other sections or ships/races unless ticket says so.
- Do not update iteration or section tickets — Manager does that.
- Do not spawn subagents.
- If blocked after one retry attempt, set Status `blocked` and explain precisely.
- Prefer incremental edits; match repo style.

## Return to Manager

```
status: done|blocked
files_changed: [...]
summary: ...
blockers: ...
```