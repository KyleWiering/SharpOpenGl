# Delegation Protocol — Recursive Spawn with CEO Proxy Fallback

The org pipeline is **hierarchical in roles** (CEO → Director → Manager → Worker) and **recursive in intent**: each tier spawns the tier below. Platform nesting limits may block child spawns — when they do, the **root CEO proxies** the wave on behalf of the requesting role.

## Spawn modes

| Mode | Who spawns children | When |
|------|---------------------|------|
| **Direct** | Director spawns Managers; Manager spawns Workers/Verifiers | Root session, or platform allows nesting |
| **Proxy** | CEO spawns on behalf of Director or Manager | Child subagent, or `spawn_subagent` / `Task` fails |

Detect proxy mode when:
- Director or Manager was spawned by CEO (typical Cursor/Grok: nesting depth = 1)
- A spawn tool call returns an error or is unavailable in the child session

**Proxy is not CEO taking over.** CEO executes the queued JSON wave the Director or Manager already authored, then resumes that role's checkpoint.

## Who spawns what

| Spawner (intent) | Subagent role | Skill | Trigger |
|------------------|---------------|-------|---------|
| CEO | Director | `director/SKILL.md` | `dispatch-state.json` phase = `director_planning` or `director_verify` |
| Director (or CEO proxy) | Manager | `manager/SKILL.md` | `managers-queue.json` has `pending` |
| Manager (or CEO proxy) | Worker | `worker/SKILL.md` | `workers-queue.json` has `pending` |
| Manager (or CEO proxy) | Verifier | verifier prompts | `verifiers-queue.json` has `pending` |

## Delegation files

All under `.grok/org/<project-slug>/delegations/`:

| File | Writer | Reader |
|------|--------|--------|
| `dispatch-state.json` | CEO | CEO (state machine) |
| `managers-queue.json` | Director | Director, CEO (proxy) |
| `workers-queue.json` | Manager | Manager, CEO (proxy) |
| `verifiers-queue.json` | Manager | Manager, CEO (proxy) |

Templates: `ceo/references/delegation-*.template.json`

## Return signals

| Signal | Who returns | Meaning | Next action |
|--------|-------------|---------|-------------|
| `DELEGATION_READY managers` | Director | Sections queued; Director could not spawn (or chose proxy) | CEO **proxy_for: director** → spawn Managers |
| `DELEGATION_READY workers` | Manager | Work orders queued; Manager could not spawn | CEO **proxy_for: manager** → spawn Workers |
| `DELEGATION_READY verifier` | Manager | Verifier queued | CEO **proxy_for: manager** → spawn Verifier |
| `SECTION_DONE <section-id>` | Manager checkpoint | Section complete | Director/CEO: next section or verify |
| `SECTION_BLOCKED <section-id>` | Manager | Section stuck | Escalate / human gate |
| `DIRECTOR_DONE` | Director verify | All sections verified | CEO review |
| `DIRECTOR_PLAN_ONLY` | Director | Plan only, no execution | CEO decides whether to run orchestration |
| `SPAWN_FAILED proxy_required <wave>` | Director or Manager | Spawn attempted and failed | CEO runs proxy wave (`managers` \| `workers` \| `verifier`) |

Legacy alias: `DELEGATION_READY managers` and `PROXY_DISPATCH managers` are equivalent — CEO always treats them as a proxy wave when Director is a child.

## CEO dispatch state machine

Phases in `dispatch-state.json`:

```
director_planning
  → director_orchestrate   (Director MODE=orchestrate, or CEO proxy if child)
  → manager_prepare        (proxy_for: director — spawn manager MODE=prepare)
  → worker_dispatch        (proxy_for: manager — spawn workers)
  → verifier_dispatch      (proxy_for: manager — spawn verifier)
  → manager_checkpoint     (proxy_for: director — spawn manager MODE=checkpoint)
  → (loop until SECTION_DONE for all sections)
  → director_verify
  → ceo_review
```

Set `proxy_for` in `dispatch-state.json` during proxy waves: `"director"` | `"manager"` | `null`.

## Director modes

| MODE | Does | Spawns (direct) | Returns |
|------|------|-----------------|---------|
| `plan` | Directive, plan, section tickets, managers-queue | — | `DELEGATION_READY managers` |
| `orchestrate` | Drive section delivery loop | Manager prepare/checkpoint per section | `DIRECTOR_DONE` or proxy signals per wave |
| `verify` | Read all sections, plan sign-off | — | `DIRECTOR_DONE` |

When Director is **root** (`/director` standalone): use MODE=orchestrate and spawn Managers directly.

When Director is **child** (CEO-spawned): complete MODE=plan, return `DELEGATION_READY managers`; CEO runs all orchestration waves as Director's proxy.

## Manager modes

| MODE | Does | Spawns (direct) | Returns |
|------|------|-----------------|---------|
| `prepare` | Iteration, work orders, queues | Worker, Verifier | `DELEGATION_READY workers` or `DELEGATION_READY verifier` |
| `checkpoint` | Review worker/verifier results | — | `SECTION_DONE`, `DELEGATION_READY workers`, or `SECTION_BLOCKED` |

When Manager is **root** (`/manager` standalone): spawn Workers after MODE=prepare.

When Manager is **child**: queue JSON, return `DELEGATION_READY workers`; CEO proxies worker/verifier waves.

## Parallel spawning

Director (direct) or CEO (proxy) may spawn multiple Managers in one turn when sections are independent.

Manager (direct) or CEO (proxy) may spawn multiple Workers in one turn when orders are independent.

## Tool names

| Platform | Spawn tool |
|----------|------------|
| Grok | `spawn_subagent` |
| Cursor | `Task` with `subagent_type: generalPurpose` |

Director and Manager **must attempt** spawn when their skill says direct mode is available. On failure, queue JSON and return the appropriate `DELEGATION_READY` or `SPAWN_FAILED proxy_required` signal — never silently skip the wave.