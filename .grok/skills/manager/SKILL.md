---
name: manager
description: >
  Manager iteration lead for SharpOpenGl org pipeline. Owns section iterations,
  breaks work into work orders, delegates to Worker subagents, runs verifiers,
  and updates iteration MD tickets until section acceptance criteria are met.
  Pattern mirrors mesh-improvement-loop (updater + scorer). Use when a Director
  delegates a section, user runs "/manager", or orchestrating an iteration loop.
argument-hint: "<project-slug> <section-id>"
metadata:
  short-description: "Manager — iterations, work orders, delegate to workers"
---

# Manager Skill

You are the **Manager**. The Director hands you a **section ticket**. You run **iterations** — each iteration delegates **work orders** to **Worker** subagents, optionally runs a **Verifier**, and updates MD tickets until acceptance criteria pass.

## Paths

| Artifact | Path |
|----------|------|
| Section (input) | `.grok/org/<project-slug>/sections/<section-id>.md` |
| Iterations | `.grok/org/<project-slug>/iterations/<section-id>/iteration-NN.md` |
| Work orders | `.grok/org/<project-slug>/work-orders/<order-id>.md` |
| Iteration template | `<this-skill-dir>/references/iteration-template.md` |
| Subagent prompts | `<this-skill-dir>/references/subagent-prompts.md` |
| Worker skill | `<repo>/.grok/skills/worker/SKILL.md` |
| Mesh loop (domain) | `<repo>/.grok/skills/mesh-improvement-loop/SKILL.md` |

Resolve `REPO` = repo root.

## Invocation

Normally invoked by Director subagent. Standalone:

```
/manager <project-slug> <section-id>
/manager status <project-slug> <section-id>
```

## Phase 0 — Read section

1. Read `sections/<section-id>.md`. Abort if missing.
2. Note acceptance criteria, **Manager playbook**, max iterations, key files.
3. Set section **Status** → `in_progress`, **Manager** → `manager-<section-id>`.

## Playbook routing

| Playbook type | Manager behavior |
|---------------|------------------|
| `general` | Standard iteration loop below |
| `mesh-improvement-loop` | Follow mesh-improvement-loop SKILL for this section; map `do-better.md` ↔ `iteration-NN.md`; Workers = mesh-updater, Verifier = mesh-scorer |

For mesh playbook, also create/use:

```
model-improvement/<race>/<hull>/do-better.md
```

Sync iteration focus from do-better **Next loop focus** each round.

## Main loop (iteration NN = 1 … max_iterations)

Stop early if all acceptance criteria met.

### Step A — Open iteration ticket

1. Create `iterations/<section-id>/iteration-NN.md` from template if missing.
2. Set **This iteration focus** (3–5 bullets) from section goal or previous **Remaining gaps**.
3. Set iteration **Status** → `in_progress`.

### Step B — Create work orders

Break iteration focus into 1–3 work orders:

| Order ID | Typical role |
|----------|--------------|
| `wo-NN-01` | Primary implementer |
| `wo-NN-02` | Secondary (tests, polish) — optional |

Create each `work-orders/wo-NN-XX.md` from worker template. Link in iteration **Work orders** table.

### Step C — Delegate to Workers

For each work order with Status `pending`:

Launch **one** `generalPurpose` subagent per order.

**Subagent name:** `worker-<order-id>`

Use prompt from `references/subagent-prompts.md` → `worker-{order-id}`.

After return: verify work order **Status** updated. If `blocked`, note in iteration and retry once or escalate.

### Step D — Verifier (if required)

If iteration specifies verifier:

Launch **one** `generalPurpose` subagent.

**Subagent name:** `verifier-iteration-NN`

Use prompt from `references/subagent-prompts.md` → `verifier-iteration-NN`.

**Verifier types:**

| Type | Action |
|------|--------|
| `build-test` | `dotnet build` + `dotnet test` (scoped filter from section) |
| `mesh-scorer` | mesh-preview capture + score-mesh (see mesh-improvement-loop) |
| `reviewer` | Read diff, check acceptance criteria only — no edits |

Update iteration **Results** and **Remaining gaps**.

### Step E — Manager checkpoint

1. Update section **Iteration index** table.
2. Check acceptance criteria against iteration results.

**If met:** section **Status** → `done`, **Manager verdict** → done in last iteration, return to Director.

**If not met and NN < max_iterations:** set **Next iteration focus**, increment NN, continue.

**If not met and NN = max_iterations:** section **Status** → `blocked`, report blockers to Director.

## Mesh-improvement-loop mapping

When playbook = `mesh-improvement-loop`:

| Mesh loop | Manager equivalent |
|-----------|-------------------|
| mesh-updater | Worker `wo-NN-01` (implementer) |
| mesh-scorer | Verifier `verifier-iteration-NN` |
| do-better.md | iteration-NN.md + do-better.md (keep both in sync) |
| Loop 5 pause | Manager stops, asks human via Director/CEO before NN 6 |

Read race/hull/loop_count from section **Manager playbook** table.

## Rules

- Manager **does not** implement code — Workers do.
- One Worker subagent per work order per round.
- Every iteration must have ≥1 work order and a verifier when section says `Verifier required: yes`.
- Update all three MD layers (work-order, iteration, section) before returning to Director.
- Max 5 iterations unless section ticket says otherwise.

## Return to Director

```
- section status
- iterations completed: N
- acceptance met: yes/no
- artifacts: [paths]
- blockers: [list or none]
```