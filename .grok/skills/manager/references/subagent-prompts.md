# Manager subagent prompt templates

Replace `{project-slug}`, `{section-id}`, `{NN}`, `{order-id}`, `{REPO}`.

## worker-{order-id}

```
You are the Worker subagent for work order "{order-id}".

Read and follow: {REPO}/.grok/skills/worker/SKILL.md

Your input:
- Work order: {REPO}/.grok/org/{project-slug}/work-orders/{order-id}.md
- Section: {REPO}/.grok/org/{project-slug}/sections/{section-id}.md

Execute the work order. Update the work order MD with your report.
Do NOT spawn subagents.

Return: status, files changed, summary, blockers.
```

## verifier-iteration-{NN}

```
You are the Verifier subagent for iteration {NN}, section "{section-id}".

Read:
- {REPO}/.grok/org/{project-slug}/iterations/{section-id}/iteration-{NN}.md
- {REPO}/.grok/org/{project-slug}/sections/{section-id}.md

Run the verifier command from the iteration file (build-test, mesh-scorer, or reviewer).
Update iteration-{NN}.md with Results and Remaining gaps.

Return: pass/fail, test output summary, top 3 gaps for next iteration.
```