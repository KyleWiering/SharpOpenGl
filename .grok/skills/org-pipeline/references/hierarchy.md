# Org Pipeline — Hierarchy & Handoffs

Four connected skills form a delegation chain. Each level owns an MD "ticket" file; subagents carry the handoff.

```
Human → /ceo → Director subagent → Manager subagent(s) → Worker subagent(s)
```

## Ticket layout (per project)

All runtime artifacts live under `.grok/org/<project-slug>/` (gitignored).

```
.grok/org/<project-slug>/
  directive.md              # CEO ticket — human goal, CEO verdict loop
  plan.md                   # Director ticket — major plan, section roster
  sections/<section-id>.md  # Director → Manager handoff (work package)
  iterations/<section-id>/iteration-NN.md   # Manager iteration brief
  work-orders/<order-id>.md # Manager → Worker task card
```

## Roles

| Role | Skill | Owns | Delegates to | Decides |
|------|-------|------|--------------|---------|
| Human | — | Primary direction | CEO | Strategic intent |
| CEO | `/ceo` | `directive.md` | Director | When the **goal** is done |
| Director | `/director` | `plan.md`, `sections/*.md` | Manager | When each **section** is done |
| Manager | `/manager` | `iterations/*`, `work-orders/*` | Worker | When each **iteration** meets acceptance |
| Worker | `/worker` | executes `work-orders/*` | — | Task completion only |

## Status values (all tickets)

`pending` · `in_progress` · `blocked` · `done` · `rejected`

## Subagent naming

| Handoff | Subagent name pattern |
|---------|----------------------|
| CEO → Director | `director-<project-slug>` |
| Director → Manager | `manager-<section-id>` |
| Manager → Worker | `worker-<order-id>` |
| Manager → Verifier (optional) | `verifier-<iteration-NN>` |

## Skill cross-references

Each skill's SKILL.md tells the orchestrator which child skill to invoke via Task subagent prompt:

- CEO reads: `../director/SKILL.md` path in subagent prompt
- Director reads: `../manager/SKILL.md`
- Manager reads: `../worker/SKILL.md`

Domain-specific manager loops (e.g. mesh improvement) can be referenced in `sections/<id>.md` under **Manager playbook**.

## Project slug

Derive from goal: lowercase, hyphens, 2–48 chars (e.g. `vesper-fighter-polish`, `save-load-ui`).