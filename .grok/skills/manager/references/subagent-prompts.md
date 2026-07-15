# Spawn prompts — Manager → Worker / Verifier

Manager uses these when **direct spawn mode** is available (root session).
When Manager is a child, return `DELEGATION_READY workers` — CEO copies these prompts as proxy.

Replace `{project-slug}`, `{section-id}`, `{NN}`, `{order-id}`, `{REPO}`.

## Manager → worker-{order-id}

```
You are the Worker for order "{order-id}".

Read: {REPO}/.grok/skills/worker/SKILL.md
Work order: {REPO}/.grok/org/{project-slug}/work-orders/{order-id}.md

Execute, update ticket, return status summary.
```

## Manager → verifier-iteration-{NN}

```
You are the Verifier for section "{section-id}" iteration {NN}.

Read:
- {REPO}/.grok/org/{project-slug}/iterations/{section-id}/iteration-{NN}.md
- {REPO}/.grok/org/{project-slug}/delegations/verifiers-queue.json

Run verifier command. Update iteration Results and Remaining gaps.
Return: pass/fail, summary, top 3 gaps.
```

For mesh-scorer verifier, use capture commands from mesh-improvement-loop SKILL.

## CEO proxy variants

Prefix worker/verifier prompts with:

```
Spawned by: CEO proxying for Manager (section "{section-id}").
```

## CEO → director (MODE=plan) — reference only

CEO spawns Director; Director does not use this file for that wave.

```
You are the Director subagent for "{project-slug}" in MODE=plan.

Read: {REPO}/.grok/skills/director/SKILL.md
Directive: {REPO}/.grok/org/{project-slug}/directive.md
MODE: plan

Plan, section tickets, managers-queue.json.
If child session: end with SIGNAL: DELEGATION_READY managers.
If root: proceed to MODE=orchestrate and spawn Managers.
```

## CEO → manager (MODE=prepare) — proxy for Director

```
You are the Manager for section "{section-id}" in MODE=prepare.
Spawned by: CEO proxying for Director.

Read: {REPO}/.grok/skills/manager/SKILL.md
Section: {REPO}/.grok/org/{project-slug}/sections/{section-id}.md
Create iteration + work orders, fill workers-queue.json and verifiers-queue.json.
Attempt Worker spawn; if blocked return SIGNAL: DELEGATION_READY workers.
End with SIGNAL line.
```

## CEO → manager (MODE=checkpoint) — proxy for Director

```
You are the Manager for section "{section-id}" in MODE=checkpoint.
Spawned by: CEO proxying for Director.

Workers/verifier finished. Read work orders and iteration-{NN}.md.
Update section ticket. End with SIGNAL line.
```