# Do Better — {category} / {race} / {model}

**Loop:** 0 (not started)  
**Category:** {category} (ship | station | object)  
**Race:** {race_display_name} (— if object)  
**Model:** {model}  
**Last score:** — / 100 (RaceIdentity — / 10)

## Score history

| Loop | Total | RaceId | Screenshot | Notes |
|------|-------|--------|------------|-------|
| — | — | — | — | Initial brief |

## What works

- (baseline strengths — fill after loop 1)

## Loop priorities

1. **Dorsal/plan form** — silhouette (ship), plan massing (station), or icon readability (object) at **oblique top-down RTS zoom**
2. **No triangle patterns** — visible facet tris = incomplete mesh or bad texture wrap; use flush panels/boxes + uniform vertex lum per zone
3. **Textures / materials** — race palette + component zones (ships/stations)
4. **Race identity** — primary/secondary/accent bands from `race_visuals.json`
5. **Shadows / lighting** — baked vertex contrast; avoid flat grey surfaces
6. **Screenshot** — panel 1 (primary RTS angle) readable; all 3 oblique top-down panels balanced

## Remaining gaps

- (from `ModelQualityScorer` categories after each loop)

## Next loop focus

- [ ] Establish baseline geometry and palette
- [ ] Add surface accents appropriate to category (dorsal/deck, not belly-only)
- [ ] Tune race material bands (ships/stations)
- [ ] Verify 3-view oblique top-down mesh-preview at 2× pullback
- [ ] Audit for triangle patterns (facet strips, fishbone, bad vertex wrap)

## User feedback (loop 5)

_(Orchestrator fills after user review.)_

## Screenshots

- `mesh-loop-NN-{race}-{model}.png` — 3-panel oblique top-down preview (panel 1 = RTS primary, 55% weight)

## Workflow

```powershell
dotnet run --project SharpOpenGl -- --mesh-preview --category {category} --model {model} [--race {race}] --screenshot-path mesh-loop-NN.png
dotnet run --project SharpOpenGl -- --score-mesh --category {category} --model {model} [--race {race}] --screenshot-path mesh-loop-NN.png --output model-improvement/{race}/{model}/scores/loop-NN.json
dotnet run --project SharpOpenGl -- --score-race --race {race} --output model-improvement/{race}/race-score.json
```

## Key files

| Category | Path |
|----------|------|
| Ship routing | `SharpOpenGl.Engine/Rendering/RaceShipMeshes.cs` |
| Station routing | `SharpOpenGl.Engine/Rendering/RaceStationMeshes.cs` |
| Objects | `SharpOpenGl.Engine/Rendering/ModelMeshSource.cs`, `ProceduralMeshes.cs` |
| Surface accents | `SharpOpenGl.Engine/Rendering/RaceSurfaceDetail.cs` |
| Race palette | `GameData/Config/race_visuals.json` |
| Preview camera | `SharpOpenGl/EngineWindow.MeshPreview.cs` |
| Scorer | `SharpOpenGl.Engine/Rendering/ModelQualityScorer.cs` |
| Rubric | `.grok/skills/mesh-improvement-loop/references/evaluation-rubric.md` |