# Org Pipeline — Hierarchy & Handoffs

Four connected skills form a delegation chain. Each level owns MD tickets and **spawns the tier below** when the platform allows. When nesting is blocked, the **CEO proxies** that wave on behalf of the requesting role.

```
Human → /ceo (root)
          ├─ spawn Director (plan)
          │     └─ [direct] spawn Manager(s) per section
          │     └─ [proxy]  CEO spawns Manager(s) on Director's behalf
          │           └─ [direct] Manager spawn Worker(s)
          │           └─ [proxy]  CEO spawns Worker(s) on Manager's behalf
          ├─ spawn Director (verify) or CEO review
          └─ mark directive done
```

See `delegation-protocol.md` for spawn modes and the full state machine.

## Ticket layout (per project)

Runtime under `.grok/org/<project-slug>/`:

```
.grok/org/<project-slug>/
  directive.md
  plan.md
  sections/<section-id>.md
  iterations/<section-id>/iteration-NN.md
  work-orders/<order-id>.md
  delegations/
    dispatch-state.json      # includes proxy_for during proxy waves
    managers-queue.json
    workers-queue.json
    verifiers-queue.json
```

## Roles

| Role | Skill | Owns | Spawns (intent) | Proxy fallback |
|------|-------|------|-----------------|----------------|
| Human | — | Direction | CEO | — |
| CEO | `/ceo` | `directive.md`, `dispatch-state.json` | Director | Proxies Manager/Worker/Verifier waves |
| Director | `/director` | `plan.md`, `sections/*.md` | Manager | CEO proxies when child |
| Manager | `/manager` | `iterations/*`, `work-orders/*` | Worker, Verifier | CEO proxies when child |
| Worker | `/worker` | executes `work-orders/*` | — | — |

## Status values

`pending` · `in_progress` · `blocked` · `done` · `rejected`

## Subagent naming

| Role | Name pattern |
|------|--------------|
| Director | `director-<project-slug>` |
| Manager | `manager-<section-id>` |
| Worker | `worker-<order-id>` |
| Verifier | `verifier-iteration-NN` |

## SIGNAL codes

| Signal | Who returns | CEO / parent does |
|--------|-------------|-------------------|
| `DELEGATION_READY managers` | Director | Proxy: spawn Managers |
| `DELEGATION_READY workers` | Manager prepare | Proxy: spawn Workers |
| `DELEGATION_READY verifier` | Manager prepare | Proxy: spawn Verifier |
| `SECTION_DONE` | Manager checkpoint | Next section or verify |
| `SECTION_BLOCKED` | Manager | Escalate / human gate |
| `DIRECTOR_DONE` | Director verify | CEO review |
| `SPAWN_FAILED proxy_required <wave>` | Director or Manager | CEO runs that proxy wave |

## Nesting rule

1. **Try direct spawn** — Director → Manager, Manager → Worker/Verifier.
2. **On failure or child session** — queue JSON, return `DELEGATION_READY` or `SPAWN_FAILED proxy_required`; CEO executes the wave and updates `dispatch-state.json` `proxy_for`.