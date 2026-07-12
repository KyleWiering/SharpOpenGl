# Subagent prompt templates

Copy into Task tool `prompt` fields. Replace `{race}`, `{hull}`, `{NN}`, `{REPO}`.

## mesh-updater-loop-{NN}

```
You are the mesh-updater subagent for SharpOpenGl loop {NN}.

Read and implement: {REPO}/model-improvement/{race}/{hull}/do-better.md
Focus on "Next loop focus" and "Remaining gaps".

Edit only mesh/render files required for {race}/{hull}:
- SharpOpenGl.Engine/Rendering/RaceShipMeshes.cs
- SharpOpenGl.Engine/Rendering/RaceSurfaceDetail.cs
- SharpOpenGl.Engine/Rendering/RaceMeshWriter.cs
- GameData/Config/race_visuals.json
- SharpOpenGl.Engine/Rendering/GameShaders.cs (textures/lighting only if needed)
- SharpOpenGl/EngineWindow.MeshPreview.cs (full-ship framing only if needed)

Goals: shape, textures (race + component engine/weapon zones), shadows/lighting, visual appeal.

After edits run: dotnet build (from {REPO})
Do NOT capture screenshots or run score-mesh.

Return: files changed, summary of visual changes, any build issues.
```

## mesh-scorer-loop-{NN}

```
You are the mesh-scorer subagent for SharpOpenGl loop {NN}.

From {REPO}, run:
  .grok/skills/mesh-improvement-loop/scripts/capture-and-score.ps1 -Race {race} -Hull {hull} -Loop {NN}

Or manually:
  dotnet run --project SharpOpenGl -- --mesh-preview --race {race} --hull {hull} --screenshot-path mesh-loop-{NN}.png
  dotnet run --project SharpOpenGl -- --score-mesh --race {race} --hull {hull} --screenshot-path mesh-loop-{NN}.png --output model-improvement/{race}/{hull}/scores/loop-{NN}.json

Verify mesh-loop-{NN}.png exists in REPO root.

Update {REPO}/model-improvement/{race}/{hull}/do-better.md:
- Loop = {NN}, Last score from JSON
- Append Score history row
- Refresh Remaining gaps from lowest categories
- Refresh Next loop focus from Suggestions + category Notes
- Note screenshot under Screenshots

Return: TotalScore, category breakdown, screenshot path, top 3 priorities for loop {NN+1}.
```