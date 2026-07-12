# Do Better — {race} / {hull}

**Loop:** 0 (not started)  
**Race:** {race_display_name}  
**Hull:** {hull}  
**Last score:** — / 100

## Score history

| Loop | Total | Screenshot | Notes |
|------|-------|------------|-------|
| — | — | — | Initial brief |

## What works

- (baseline strengths to preserve — fill after loop 1 score)

## Loop priorities (textures, shape, shadows, visual appeal)

Improve the procedural ship mesh and how it renders in mesh-preview:

1. **Shape / silhouette** — readable fighter/capital profile; target aspect ~0.41 for fighters; strong nose and wing sweep.
2. **Textures** — race plating via `race_visuals.json` + shader; component zones on engines (~lum 0.48) and weapons (~lum 0.36).
3. **Shadows / lighting** — baked vertex contrast + `raceLighting` in shader; avoid flat grey hulls.
4. **Visual appeal** — 2010s hard-surface: panel lines, accent bands, nacelles, canopy frame, teal/cyan engine glow.

## Remaining gaps

- (populated by mesh-scorer after each loop from `ModelQualityScorer` categories)

## Next loop focus

- [ ] Establish baseline geometry and palette
- [ ] Add surface accents (leading edges, spine band)
- [ ] Tune engine/weapon material bands for component texture blends
- [ ] Adjust mesh-preview framing if ship is clipped

## User feedback (loop 5)

_(Orchestrator fills after user review of `mesh-loop-05.png`.)_

## Screenshots

- Compare captures in repo root: `mesh-loop-NN.png`

## Workflow

```powershell
dotnet run --project SharpOpenGl -- --mesh-preview --race {race} --hull {hull} --screenshot-path mesh-loop-NN.png
dotnet run --project SharpOpenGl -- --score-mesh --race {race} --hull {hull} --screenshot-path mesh-loop-NN.png --output model-improvement/{race}/{hull}/scores/loop-NN.json
dotnet test SharpOpenGl.Tests --filter "FullyQualifiedName~Export_and_score_{race}"
```

## Key files

| Area | Path |
|------|------|
| Mesh routing | `SharpOpenGl.Engine/Rendering/RaceShipMeshes.cs` |
| Surface accents | `SharpOpenGl.Engine/Rendering/RaceSurfaceDetail.cs` |
| Material bands | `SharpOpenGl.Engine/Rendering/RaceMeshWriter.cs` |
| Race palette | `GameData/Config/race_visuals.json` |
| Race / component GLSL | `SharpOpenGl.Engine/Rendering/GameShaders.cs` |
| Preview camera | `SharpOpenGl/EngineWindow.MeshPreview.cs` |
| Scorer | `SharpOpenGl.Engine/Rendering/ModelQualityScorer.cs` |