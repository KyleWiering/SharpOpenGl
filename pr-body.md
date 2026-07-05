## Summary

Migrates all procedural ship, station, and effect geometry to organized Wavefront OBJ files with manifest-driven loading. Also updates agent documentation to reduce redundant repo discovery on new requests.

## Model migration

- **755 OBJ assets** under `GameData/Meshes/` (Ships/Designs/Stations per race, environment, projectiles, effects, shared)
- `GameData/Config/mesh_manifest.json` — canonical key → path registry
- `ProceduralMeshExporter`, `ModelMigrationExporter`, `MeshManifest`, `MeshAssetService`
- Desktop loader prefers disk OBJ with procedural fallback (`EngineWindow.RaceMeshes`, `EngineWindow.MeshAssets`)
- Ship Designer: race/hull cycling + manifest mesh keys
- Tests: `ModelMigrationExporterTests`, `ModelMigrationProofTests` (764 manifest entries verified)
- Docs: `docs/MODEL_MIGRATION_PLAN.md`, `docs/MODEL_MIGRATION_PROGRESS.md`

## Agent intake

- **Agent Intake** section in `AGENTS.md` — tiers, task router, command cheat sheet
- Updated `.github/agents/my-agent.agent.md` and `.cursor/rules/ai-documentation.mdc`
- `readme.md` AI pointer routes through intake instead of full-repo reads

## Verify

```bash
dotnet test SharpOpenGl.Tests --filter "FullyQualifiedName~ModelMigrationProofTests"
dotnet test SharpOpenGl.Tests --filter "FullyQualifiedName~RaceSubstrateCatalogTests"
dotnet build && dotnet run --project SharpOpenGl
```

## Excluded from commit

Local scratch files (`_patch_startup.py`, `*-test.png`) left untracked.