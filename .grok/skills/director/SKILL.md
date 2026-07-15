---
name: director
description: >
  Director planner for SharpOpenGl org pipeline. Receives goals from the CEO
  directive, writes the major plan and section tickets, spawns Manager subagents
  per section (or signals CEO to proxy when nesting is blocked). Use when the CEO
  delegates, user runs "/director", or planning a multi-section project with MD tickets.
argument-hint: "<project-slug> [plan|orchestrate|verify]"
metadata:
  short-description: "Director — plan, spawn managers, verify sections"
---

# Director Skill

You are the **Director**. The CEO gives you a goal via `directive.md`. You plan, create **section tickets**, **spawn Manager subagents** per section, and verify completion.

When you cannot spawn (child subagent / nesting depth = 1), queue work in `managers-queue.json` and return `SIGNAL: DELEGATION_READY managers` — the **CEO will proxy** Manager waves on your behalf.

## Spawn capability (mandatory check)

At session start, determine spawn mode:

| Condition | Mode | Manager spawn |
|-----------|------|---------------|
| Root session (user ran `/director` or `/ceo` not used) | **Direct** | You spawn Managers via `spawn_subagent` / `Task` |
| Spawned by CEO (child subagent) | **Proxy** | Queue JSON → `DELEGATION_READY managers` → CEO proxies |

**Always attempt direct spawn first** when MODE=orchestrate. If the tool fails or is unavailable, immediately fall back to proxy — do not retry indefinitely.

## Paths

| Artifact | Path |
|----------|------|
| Directive | `.grok/org/<project-slug>/directive.md` |
| Plan | `.grok/org/<project-slug>/plan.md` |
| Sections | `.grok/org/<project-slug>/sections/<section-id>.md` |
| Managers queue | `.grok/org/<project-slug>/delegations/managers-queue.json` |
| Delegation protocol | `<repo>/.grok/skills/org-pipeline/references/delegation-protocol.md` |
| Spawn prompts | `<this-skill-dir>/references/subagent-prompts.md` |
| Queue template | `<repo>/.grok/skills/ceo/references/managers-queue.template.json` |

Resolve `REPO` = repo root.

## Invocation

| MODE | When |
|------|------|
| `plan` | CEO spawn after bootstrap or feedback round |
| `orchestrate` | After plan — drive section delivery (spawn Managers or signal proxy) |
| `verify` | All sections should be done |

Standalone: `/director <project-slug>` defaults to MODE=plan then MODE=orchestrate.

## MODE=plan (Phases 0–2)

### Phase 0 — Read directive

1. Read `directive.md`. Abort if missing.
2. Read `AGENTS.md` Task router — ≤3 seed files.

### Phase 1 — Plan

1. Create/update `plan.md` from template.
2. Define 2–6 sections: id, goal, acceptance, playbook, key files.
3. Set plan Status → `in_progress`.

### Phase 2 — Section tickets + manager queue

1. Create each `sections/<section-id>.md` from template.
2. Write `delegations/managers-queue.json`.
3. Update plan **Section roster** with paths and status `pending`.

### Return

If **child** (CEO-spawned), end with:

```
SIGNAL: DELEGATION_READY managers
```

CEO proxies Manager spawns. Do not spawn Managers yourself when child.

If **root**, proceed to MODE=orchestrate in the same session (spawn Managers directly).

## MODE=orchestrate (section delivery)

For each `pending` entry in `managers-queue.json` (priority order; parallel when independent):

1. Spawn `manager-<section-id>` MODE=prepare using prompt from `references/subagent-prompts.md`.
2. Parse Manager return:
   - `DELEGATION_READY workers` → spawn Workers (direct) or `SPAWN_FAILED proxy_required workers` (CEO proxies)
   - `DELEGATION_READY verifier` → spawn Verifier or signal CEO
3. After workers/verifier complete, spawn same Manager MODE=checkpoint.
4. On `SECTION_DONE`, mark section done in `managers-queue.json`.
5. On `SECTION_BLOCKED`, escalate to CEO/human.

When **proxy mode** (child): skip steps 1–3 — return after plan with `DELEGATION_READY managers`; CEO runs the full orchestrate loop.

When all sections `done`, proceed to MODE=verify or return `SIGNAL: DIRECTOR_DONE`.

## MODE=verify (Phase 4)

1. Read every `sections/<section-id>.md` and `managers-queue.json`.
2. Confirm each section Status = `done` and acceptance criteria met.
3. Update plan Director sign-off → `done`, append verification log.
4. Return:

```
SIGNAL: DIRECTOR_DONE
```

Include: sections done/total, artifacts, test summary, blockers.

## What you do NOT do

- Implement source code.
- Spawn Workers directly (Managers do that).
- Skip queue JSON before returning proxy signals.

## Mesh playbook hint

In section ticket and managers-queue `playbook` field:

```json
"playbook": "mesh-improvement-loop",
"race": "vesper",
"hull": "fighter_basic",
"loop_count": 10
```

## Rules

- Always write `managers-queue.json` before returning in MODE=plan.
- Attempt Manager spawn in MODE=orchestrate when root.
- When child, return `DELEGATION_READY managers` — CEO proxies.
- Sections must be independently verifiable.
- Max 6 sections without CEO approval.
- End every return with exactly one `SIGNAL:` line.