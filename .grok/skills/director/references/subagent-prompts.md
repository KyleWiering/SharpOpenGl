# Spawn prompts — Director → Manager

Director uses these when **direct spawn mode** is available (root session).
When Director is a child, return `DELEGATION_READY managers` — CEO copies these prompts as proxy.

Replace `{project-slug}`, `{section-id}`, `{REPO}`.

## Director → manager (MODE=prepare)

```
You are the Manager subagent for section "{section-id}" in MODE=prepare.

Read: {REPO}/.grok/skills/manager/SKILL.md
Section: {REPO}/.grok/org/{project-slug}/sections/{section-id}.md
MODE: prepare

Create iteration + work orders, fill workers-queue.json and verifiers-queue.json.
Attempt to spawn Workers directly; if blocked, return SIGNAL: DELEGATION_READY workers.
End with SIGNAL line.
```

## Director → manager (MODE=checkpoint)

```
You are the Manager subagent for section "{section-id}" in MODE=checkpoint.

Workers and verifier (if any) have finished.
Read work orders and iteration MD under {REPO}/.grok/org/{project-slug}/.
Update section ticket.
End with SIGNAL: SECTION_DONE | DELEGATION_READY workers | SECTION_BLOCKED
```

## CEO proxy variant (MODE=prepare)

Same prompt as above, prefixed:

```
Spawned by: CEO proxying for Director.
```

## CEO proxy variant (MODE=checkpoint)

Same checkpoint prompt, prefixed:

```
Spawned by: CEO proxying for Director.
```