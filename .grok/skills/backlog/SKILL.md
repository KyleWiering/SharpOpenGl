---
name: backlog
description: >-
  Build and run a 30-item SharpOpenGl work backlog. Collects ideas interactively
  (up to 30), stores them in .grok/backlog/backlog.json, and implements items one
  at a time via /implement. Use when the user wants a backlog, work queue, idea
  collection, "/backlog", "/backlog collect", "/backlog next", "/backlog run",
  or "implement the next backlog item".
argument-hint: "[collect | next | run | status | add | skip | edit] [args]"
---

# Backlog Skill — SharpOpenGl

Orchestrate a **30-item work backlog** for this repo. Collect ideas from the user, persist them, then implement each item in order.

## Paths

| Artifact | Path |
|----------|------|
| Backlog store (canonical) | `.grok/backlog/backlog.json` |
| Human-readable view | `.grok/backlog/BACKLOG.md` |
| Item format reference | `<this-skill-dir>/references/backlog-format.md` |
| Project context | `AGENTS.md` (Agent Intake + Active Branch only — no full-repo skim) |

Read `references/backlog-format.md` before your first write.

## Constants

- `TARGET_COUNT = 30` — collection loop stops here unless the user says `done` earlier.
- Status values: `pending`, `in_progress`, `done`, `skipped`.

## Invocation

```
/backlog                  → status (or start collect if backlog empty)
/backlog collect          → idea collection loop (up to 30)
/backlog next             → implement the next pending item
/backlog run              → implement all pending items sequentially (30-loop)
/backlog status           → show progress table
/backlog add <text>       → append one item without full collect loop
/backlog skip <id>        → mark item skipped
/backlog edit <id> <text> → replace title/description
```

Parse the subcommand from the argument string. Default to `status` when no subcommand is given.

---

## Mode: `collect` (idea loop)

**Goal:** Fill the backlog with up to 30 implementable work items by asking the user.

### Setup

1. Read `AGENTS.md` → **Agent Intake** + **Active Branch Context** only (do not skim `readme.md` or roadmap docs).
2. Load `.grok/backlog/backlog.json` (create empty scaffold if missing — see format reference).
3. Count existing non-skipped items. Let `n = count`.
4. If `n >= 30`, tell the user the backlog is full and suggest `/backlog next` or `/backlog status`. Stop.

### Collection loop

For each slot `n+1` through `30`, ask the user **one idea at a time** in regular conversation (do NOT use structured option prompts for free-text idea input):

```
Backlog item {n}/30 — what's the idea?
  • Type your idea (feature, fix, polish, content, test, docs)
  • `done` — stop collecting early
  • `suggest` — I'll propose 3 ideas from GAME_PLAN / IMPLEMENTATION_PLAN / codebase gaps; you pick one or pass
  • `skip` — skip this slot
```

**After each answer:**

| User says | Action |
|-----------|--------|
| Concrete idea | Normalize into a backlog item (title ≤ 80 chars, 1–3 sentence description, optional `area` tag). Append to JSON. Confirm: "Added #{id}: {title}". Increment n. |
| `done` | Stop loop. Go to Finalize. |
| `suggest` | Read `GAME_PLAN.md`, `IMPLEMENTATION_PLAN.md`, and grep for `TODO`/`FIXME` in `SharpOpenGl.Engine/` and `SharpOpenGl/`. Propose 3 distinct, implementable ideas sized for a single PR each. User picks 1, `skip`, or types their own. |
| `skip` | Do not add an item; advance slot counter only if user wants to burn a slot, otherwise re-ask same number. |

**Quality bar for each item:**

- Single PR scope — completable in one `/implement` pass.
- Has clear acceptance criteria (2–4 bullet points you write).
- Tagged with one `area`: `ecs`, `combat`, `ui`, `rendering`, `missions`, `economy`, `audio`, `browser`, `content`, `tests`, `infra`, `polish`.
- Not a duplicate of an existing backlog item (check titles).

### Finalize

1. Write `.grok/backlog/backlog.json`.
2. Regenerate `.grok/backlog/BACKLOG.md` (see format reference).
3. Show summary table: id, title, area, status.
4. Tell user: "Backlog ready — run `/backlog next` to implement item #1, or `/backlog run` for the full 30-loop."

---

## Mode: `add`

Append one item from inline text. Auto-assign next id. Write JSON + regenerate MD. Confirm addition.

---

## Mode: `status`

Load backlog. Print:

```
SharpOpenGl Backlog — {done}/{total} done, {pending} pending, {skipped} skipped
```

Then a markdown table: `# | Status | Area | Title`.

If empty, suggest `/backlog collect`.

---

## Mode: `next` (implement one item)

1. Load backlog. Find lowest-id item with `status: "pending"`.
2. If none, report "Backlog complete" and stop.
3. Set item `status` → `in_progress`, write JSON.
4. Announce: **"Implementing backlog #{id}: {title}"**
5. Invoke the **`implement` skill** for this item:
   - Read `~/.grok/bundled/skills/implement/SKILL.md` and follow it.
   - Pass the full item as the implementation task: title, description, acceptance criteria, area.
   - Route via `AGENTS.md` **Task router** for the item's `area` — open ≤3 files; no full-repo discovery.
6. On implement skill success:
   - Set `status` → `done`, set `completed_at` to today (ISO date).
   - Write JSON + regenerate MD.
   - Run `dotnet test` if not already run by implement skill.
   - Report completion. Show next pending item if any.
7. On failure: leave `in_progress`, report error, let user retry `/backlog next`.

---

## Mode: `run` (30-loop)

Implement **every** pending item sequentially without waiting for the user between items.

```
for each pending item (id ascending):
  1. announce progress: "Backlog run: item {i}/{pending_count} — #{id} {title}"
  2. execute Mode: next (steps 3–7)
  3. if failure: stop run, report which item failed, leave backlog state accurate
  4. continue to next pending
```

After all items done or stopped on failure, print final status table.

**Important:** Do not ask permission between items during `run`. Only stop on failure or when backlog is exhausted.

---

## Mode: `skip` / `edit`

- `skip <id>`: set status `skipped`, write files, confirm.
- `edit <id> <text>`: update title (first line) and description (rest). Reset to `pending` if was `done`. Write files.

---

## Regenerating BACKLOG.md

After every JSON mutation, rewrite `.grok/backlog/BACKLOG.md`:

```markdown
# SharpOpenGl Agent Backlog

> Auto-generated from `.grok/backlog/backlog.json`. Do not edit by hand.

**Progress:** {done}/{total} done · {pending} pending · {skipped} skipped

| # | Status | Area | Title |
|---|--------|------|-------|
| 1 | pending | ui | ... |

## Items

### #1 — {title}
- **Status:** pending
- **Area:** ui
- **Description:** ...
- **Acceptance:**
  - ...
```

---

## Tool-call discipline

- Every backlog mutation must write both `backlog.json` and `BACKLOG.md` in the same turn.
- Do not narrate future actions — execute them.
- During `collect`, ask exactly one question per turn until the user responds.
- During `run`, spawn implement subagents per the implement skill; do not implement directly in the orchestrator.

## First-time use

If the user runs `/backlog` with an empty backlog, briefly explain the 30-item loop, read project context, and start `collect` mode immediately.