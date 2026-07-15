# Subagent prompt templates

Copy into Task tool `prompt` fields. Replace `{race}`, `{model}`, `{category}`, `{NN}`, `{REPO}`.

## mesh-updater-loop-{NN}

```
You are the mesh-updater subagent for SharpOpenGl loop {NN}.

Category: {category}  Race: {race}  Model: {model}
Read and implement: {REPO}/model-improvement/.../do-better.md
Focus on "Next loop focus" and "Remaining gaps".

Edit only mesh/render files required:
- ship → RaceShipMeshes.cs, RaceSurfaceDetail.cs, RaceMeshWriter.cs, race_visuals.json
- station → RaceStationMeshes.cs, RaceSurfaceDetail.cs, race_visuals.json
- object → ProceduralMeshes.cs, ModelMeshSource.cs

RTS viewing context: players see assets from OBLIQUE TOP-DOWN, not below or pure side.
- Ships: optimize DORSAL silhouette (bow/stern mass, wing span) readable at ~35° yaw
- Stations: optimize PLAN mass (pad footprint, deck clusters) — avoid lone vertical towers in empty space
- Eliminate TRIANGLE PATTERNS: visible facet tris signal incomplete mesh or bad texture wrap
  → replace with flush boxes/panels, merge coplanar faces, uniform vertex lum per material zone

Goals: dorsal/plan form, materials, race identity (if applicable), lighting, visual appeal.

After edits: dotnet build (from {REPO})
Do NOT capture screenshots or run score-mesh.

Return: files changed, summary of visual changes, build status.
```

## mesh-scorer-loop-{NN}

```
You are the mesh-scorer subagent for SharpOpenGl loop {NN}.

Category: {category}  Race: {race}  Model: {model}

From {REPO}, run:
  .grok/skills/mesh-improvement-loop/scripts/capture-and-score.ps1 -Race {race} -Model {model} -Category {category} -Loop {NN}

Verify PNG in REPO root. Parse loop-{NN}.json for:
- TotalScore, RaceIdentityScore, AssetKind, all 7 categories, Suggestions
- Geometry notes: check for tri-pattern penalty (incomplete mesh facets)
- Materials notes: check for texture-wrap penalty (bad per-triangle luminance)
- Screenshot: panel 1 (oblique top-down) is weighted 55% — primary RTS readability

Update do-better.md: loop, scores, history, gaps, next focus, screenshot note.

If loop is 5 or 10 and category is ship/station, also run:
  .grok/skills/mesh-improvement-loop/scripts/score-race.ps1 -Race {race}
Summarize OverallScore, ShipFleetScore, StationFleetScore, WeakestAssets.

Return: TotalScore, RaceIdentityScore, category breakdown, top 3 priorities for loop {NN+1}.
```

## mesh-race-auditor

```
You are the race fleet auditor for SharpOpenGl.

From {REPO}, run:
  dotnet run --project SharpOpenGl -- --score-all-races --output model-improvement/race-leaderboard.json

For race {race}, also ensure model-improvement/{race}/race-score.json exists.

Report: 8-race ranking, {race} overall/ship/station/identity scores, weakest 5 assets, cross-race identity gaps.

Return: leaderboard summary + recommended next hull/station to improve.
```

## mesh-scorer-batch (org verifier)

For `verifiers-queue.json` entries with `type: mesh-scorer-batch`:

```
You are the mesh-scorer batch verifier for race {race} loop {NN}.

Read: {REPO}/.grok/org/.../delegations/verifiers-queue.json
Run capture_per_hull commands for all hulls listed.

Scoring context:
- Panel 1 (oblique top-down) = 55% of Screenshot score — primary RTS view
- Triangle patterns = up to -8 Geometry (tri-pattern) + up to -3 Materials (texture-wrap)
- Stations: plan footprint mass, not vertical tower landmarks

After all hulls scored, run post_score_script and fleet_score_script from queue entry.
Write fleet_summary_output JSON.

Return: fleet avg, delta vs prior loop, count at/above min_score, top 3 gap hulls.
```