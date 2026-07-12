using OpenTK.Mathematics;

namespace SharpOpenGl.Engine.Rendering;

/// <summary>Batch-exports all procedural models to organized OBJ files and builds the manifest.</summary>
public static class ModelMigrationExporter
{
    public sealed record ExportResult(int Total, int Succeeded, int Failed, IReadOnlyList<string> Errors);

    public static bool ExportShip(string gameDataRoot, string raceId, string hullId)
    {
        RaceVisualSchema.ResetForTests();
        RaceVisualSchema.Load();

        string rel = $"Ships/{raceId}/{hullId}.obj";
        string fullPath = Path.Combine(gameDataRoot, "Meshes", rel.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        float[] mesh = RaceShipMeshes.Build(raceId, hullId);
        ProceduralMeshExporter.WriteObj(mesh, fullPath, hullId);
        return mesh.Length > 0;
    }

    public static ExportResult ExportAll(string gameDataRoot)
    {
        RaceVisualSchema.ResetForTests();
        RaceVisualSchema.Load();

        var entries = new List<MeshManifestEntry>();
        var errors = new List<string>();
        int succeeded = 0;
        int failed = 0;

        void Export(string key, string relativePath, string category, string? raceId, string modelId,
            string displayName, Func<float[]> build)
        {
            string fullPath = Path.Combine(gameDataRoot, "Meshes", relativePath.Replace('/', Path.DirectorySeparatorChar));
            try
            {
                float[] mesh = build();
                if (mesh.Length == 0)
                    throw new InvalidOperationException("Empty mesh.");
                ProceduralMeshExporter.WriteObj(mesh, fullPath, modelId);
                entries.Add(new MeshManifestEntry
                {
                    Key = key,
                    Category = category,
                    RaceId = raceId,
                    ModelId = modelId,
                    DisplayName = displayName,
                    RelativePath = relativePath,
                });
                succeeded++;
            }
            catch (Exception ex)
            {
                failed++;
                errors.Add($"{key}: {ex.Message}");
            }
        }

        foreach (string raceId in RaceTextureIndex.AllRaceIds)
        {
            foreach (string shipId in FleetGalleryLayout.AllShipIds)
            {
                string rel = $"Ships/{raceId}/{shipId}.obj";
                string key = MeshManifest.ShipKey(raceId, shipId);
                Export(key, rel, "ship", raceId, shipId, $"{raceId} {shipId}", () =>
                    RaceShipMeshes.Build(raceId, shipId));
            }

            foreach (string baseId in FleetGalleryLayout.AllBaseIds)
            {
                string rel = $"Stations/{raceId}/{baseId}.obj";
                string key = MeshManifest.StationKey(raceId, baseId);
                Export(key, rel, "station", raceId, baseId, $"{raceId} {baseId}", () =>
                    RaceStationMeshes.Build(baseId, raceId));
            }

            foreach (var design in ShipDesignCatalog.GetByRace(raceId))
            {
                string rel = $"Designs/{raceId}/{design.DesignId}.obj";
                string key = MeshManifest.DesignKey(raceId, design.DesignId);
                Export(key, rel, "design", raceId, design.DesignId, design.DisplayName, () =>
                    RaceShipMeshes.BuildDesign(design));
            }
        }

        ExportEnvironment(entries, errors, ref succeeded, ref failed, gameDataRoot);
        ExportProjectiles(entries, errors, ref succeeded, ref failed, gameDataRoot);
        ExportEffects(entries, errors, ref succeeded, ref failed, gameDataRoot);
        ExportUnitsAndShared(entries, errors, ref succeeded, ref failed, gameDataRoot);

        MeshManifest.Save(gameDataRoot, entries);
        return new ExportResult(succeeded + failed, succeeded, failed, errors);
    }

    private static void ExportEnvironment(List<MeshManifestEntry> entries, List<string> errors,
        ref int succeeded, ref int failed, string gameDataRoot)
    {
        var specs = new (string id, string subfolder, Func<float[]> build)[]
        {
            ("neutral_planet", "planets", () => ProceduralMeshes.BuildPlanetSphere(new Vector3(0.75f, 0.8f, 0.9f), 4f)),
            ("harvestable_planet", "planets", () => ProceduralMeshes.BuildPlanetSphere(new Vector3(0.55f, 0.85f, 0.45f), 4f)),
            ("asteroid_field", "scenery", () => ProceduralMeshes.BuildAsteroidFieldCluster(new Vector3(0.6f, 0.55f, 0.5f), 3f)),
            ("nebula", "scenery", () => ProceduralMeshes.BuildNebulaCloud(3f)),
            ("debris", "scenery", () => ProceduralMeshes.BuildSceneryCluster(new Vector3(0.5f, 0.5f, 0.55f), 3f)),
        };

        foreach (var (id, subfolder, build) in specs)
            TryExport(entries, errors, ref succeeded, ref failed, gameDataRoot,
                $"meshes/environment/{id}.obj", $"Environment/{subfolder}/{id}.obj",
                "environment", null, id, id, build);
    }

    private static void ExportProjectiles(List<MeshManifestEntry> entries, List<string> errors,
        ref int succeeded, ref int failed, string gameDataRoot)
    {
        var specs = new (string id, Func<float[]> build)[]
        {
            ("laser", () => ProceduralMeshes.BuildLaserBolt(new Vector3(1f, 0.5f, 0.35f))),
            ("beam", () => ProceduralMeshes.BuildBeamStreak(new Vector3(0.55f, 0.95f, 1f))),
            ("torpedo", () => ProceduralMeshes.BuildTorpedo(new Vector3(0.9f, 0.9f, 0.95f))),
            ("missile", () => ProceduralMeshes.BuildRocket(new Vector3(1f, 0.75f, 0.2f))),
            ("bomb", () => ProceduralMeshes.BuildBomb(new Vector3(1f, 0.55f, 0.15f))),
            ("cannon", () => ProceduralMeshes.BuildEnergyPulse(new Vector3(0.7f, 0.45f, 1f))),
            ("wave", () => ProceduralMeshes.BuildWaveRing(new Vector3(0.4f, 1f, 0.85f))),
        };

        foreach (var (id, build) in specs)
            TryExport(entries, errors, ref succeeded, ref failed, gameDataRoot,
                MeshManifest.ProjectileKey(id), $"Projectiles/{id}.obj",
                "projectile", null, id, id, build);
    }

    private static void ExportEffects(List<MeshManifestEntry> entries, List<string> errors,
        ref int succeeded, ref int failed, string gameDataRoot)
    {
        var specs = new (string id, Func<float[]> build)[]
        {
            ("selection_ring", () => ProceduralMeshes.BuildSelectionRing(new Vector3(0f, 1f, 0f), 3f)),
            ("engine_trail", () => ProceduralMeshes.BuildEngineTrail(new Vector3(1f, 0.6f, 0.1f), 2.5f)),
            ("move_target", () => ProceduralMeshes.BuildMoveTarget(new Vector3(0f, 1f, 0.5f), 2f)),
            ("team_aura_disc", () => ProceduralMeshes.BuildTeamAuraDisc()),
            ("resource_node", () => ProceduralMeshes.BuildResourceNodeMarker(new Vector3(0.9f, 0.85f, 0.2f), 2f)),
            ("mining_drone", () => ProceduralMeshes.BuildMiningDrone(new Vector3(0.9f, 0.8f, 0.25f))),
            ("eva_astronaut", () => ProceduralMeshes.BuildEvaAstronaut(new Vector3(0.92f, 0.94f, 0.98f))),
        };

        foreach (var (id, build) in specs)
            TryExport(entries, errors, ref succeeded, ref failed, gameDataRoot,
                MeshManifest.EffectKey(id), $"Effects/{id}.obj",
                "effect", null, id, id, build);
    }

    private static void ExportUnitsAndShared(List<MeshManifestEntry> entries, List<string> errors,
        ref int succeeded, ref int failed, string gameDataRoot)
    {
        TryExport(entries, errors, ref succeeded, ref failed, gameDataRoot,
            "meshes/units/drone_worker.obj", "Units/drone_worker.obj",
            "unit", null, "drone_worker", "Drone Worker",
            () => ProceduralMeshes.BuildMiningDrone(new Vector3(0.7f, 0.75f, 0.8f), 1f));

        TryExport(entries, errors, ref succeeded, ref failed, gameDataRoot,
            "meshes/units/shield_generator.obj", "Units/shield_generator.obj",
            "unit", null, "shield_generator", "Shield Generator",
            () => ProceduralMeshes.BuildShieldGenerator(new Vector3(0.55f, 0.82f, 0.95f)));

        TryExport(entries, errors, ref succeeded, ref failed, gameDataRoot,
            "meshes/shared/default_ship.obj", "Shared/default_ship.obj",
            "shared", null, "default_ship", "Default Ship",
            () => ProceduralMeshes.BuildShipMesh(new Vector3(0.5f, 0.5f, 0.8f)));

        TryExport(entries, errors, ref succeeded, ref failed, gameDataRoot,
            "meshes/shared/default_base.obj", "Shared/default_base.obj",
            "shared", null, "default_base", "Default Base",
            () => RaceStationMeshes.Build("command_center", RaceShipMeshes.DefaultRace));

        TryExport(entries, errors, ref succeeded, ref failed, gameDataRoot,
            "meshes/shared/default_projectile.obj", "Shared/default_projectile.obj",
            "shared", null, "default_projectile", "Default Projectile",
            () => ProceduralMeshes.BuildLaserBolt(new Vector3(1f, 0.8f, 0.2f)));
    }

    private static void TryExport(List<MeshManifestEntry> entries, List<string> errors,
        ref int succeeded, ref int failed, string gameDataRoot,
        string key, string relativePath, string category, string? raceId, string modelId,
        string displayName, Func<float[]> build)
    {
        string fullPath = Path.Combine(gameDataRoot, "Meshes", relativePath.Replace('/', Path.DirectorySeparatorChar));
        try
        {
            float[] mesh = build();
            ProceduralMeshExporter.WriteObj(mesh, fullPath, modelId);
            entries.Add(new MeshManifestEntry
            {
                Key = key,
                Category = category,
                RaceId = raceId,
                ModelId = modelId,
                DisplayName = displayName,
                RelativePath = relativePath,
            });
            succeeded++;
        }
        catch (Exception ex)
        {
            failed++;
            errors.Add($"{key}: {ex.Message}");
        }
    }
}