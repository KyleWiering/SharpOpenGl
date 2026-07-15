using OpenTK.Mathematics;
using SharpOpenGl.Engine.Entities;
using SharpOpenGl.Engine.ECS;
using SharpOpenGl.Engine.Rendering;
using SharpOpenGl.Engine.UI;
using SharpOpenGl.Rendering;

namespace SharpOpenGl;

public partial class EngineWindow
{
    private string _playerRaceId = RaceShipMeshes.DefaultRace;
    private string _aiRaceId = "korath";
    private int _humanPlayerId = 1;
    private readonly Dictionary<int, string> _factionRaceIds = new();
    private readonly Dictionary<string, (int vao, int vbo, int vertCount)> _raceMeshCache = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, (int vao, int vbo, int vertCount)> _raceBuildingMeshCache = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, (int vao, int vbo, int vertCount)> _stationPartMeshCache = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, (int vao, int vbo, int vertCount)> _articulatedPartMeshCache = new(StringComparer.OrdinalIgnoreCase);
    private bool _fleetGalleryMode;

    private (int vao, int vertCount) GetDesignMesh(ShipDesignSpec design)
    {
        if (!_raceMeshCache.TryGetValue(design.DesignId, out var mesh))
        {
            mesh = UploadProceduralDesignMesh(design);
            if (mesh.vao == 0)
            {
                string designKey = MeshManifest.DesignKey(design.RaceId, design.DesignId);
                var objMesh = TryGetObjMesh(designKey);
                if (objMesh.vao != 0)
                    mesh = (objMesh.vao, 0, objMesh.vertCount);
            }

            if (mesh.vao == 0)
            {
                Console.WriteLine($"[Mesh] Procedural and OBJ failed for '{design.DesignId}', using fighter fallback.");
                var fallback = MeshBuilder.UploadProcedural(
                    RaceShipMeshes.BuildForDefinition("fighter_basic", design.RaceId));
                mesh = (fallback.vao, fallback.vbo, fallback.vertexCount);
            }

            _raceMeshCache[design.DesignId] = mesh;
        }

        return (mesh.vao, mesh.vertCount);
    }

    /// <summary>
    /// Procedural ships carry vertex-color material bands required by race/component shaders.
    /// Disk OBJs are pos+normal only and render flat without procedural texture wrap.
    /// </summary>
    private static (int vao, int vbo, int vertCount) UploadProceduralDesignMesh(ShipDesignSpec design)
    {
        try
        {
            Vector3? tint = null;
            if (RaceVisualSchema.TryGetRace(design.RaceId, out var race) && race.Palette.Primary.Length >= 3)
                tint = new Vector3(race.Palette.Primary[0], race.Palette.Primary[1], race.Palette.Primary[2]);

            float[] vertices = RaceShipMeshes.BuildDesign(design, tint);
            if (vertices.Length == 0)
                return (0, 0, 0);

            var uploaded = MeshBuilder.UploadProcedural(vertices);
            return (uploaded.vao, uploaded.vbo, uploaded.vertexCount);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Mesh] Procedural build failed '{design.DesignId}': {ex.Message}");
            return (0, 0, 0);
        }
    }

    /// <summary>
    /// Procedural stations carry vertex-color material bands required by race/component shaders.
    /// Disk OBJs are pos+normal only and render dark/flat without substrate luminance wrap.
    /// </summary>
    private static (int vao, int vbo, int vertCount) UploadProceduralStationMesh(string buildingType, string raceId)
    {
        try
        {
            float[] vertices = RaceBuildingMeshes.Build(buildingType, raceId);
            if (vertices.Length == 0)
                return (0, 0, 0);

            var uploaded = MeshBuilder.UploadProcedural(vertices);
            return (uploaded.vao, uploaded.vbo, uploaded.vertexCount);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Mesh] Procedural station build failed '{buildingType}/{raceId}': {ex.Message}");
            return (0, 0, 0);
        }
    }

    private (int vao, int vertCount, Vector3 scale) ResolveRaceBuildingMesh(string buildingType, string raceId)
    {
        string cacheKey = $"{buildingType}:{raceId}";
        if (!_raceBuildingMeshCache.TryGetValue(cacheKey, out var mesh))
        {
            mesh = UploadProceduralStationMesh(buildingType, raceId);
            if (mesh.vao == 0)
            {
                string stationKey = MeshManifest.StationKey(raceId, buildingType);
                var objMesh = TryGetObjMesh(stationKey);
                if (objMesh.vao != 0)
                {
                    Console.WriteLine($"[Mesh] Procedural station failed for '{buildingType}/{raceId}', using OBJ fallback (may render dark).");
                    mesh = (objMesh.vao, 0, objMesh.vertCount);
                }
            }

            if (mesh.vao == 0)
            {
                Console.WriteLine($"[Mesh] Station mesh missing for '{buildingType}/{raceId}', using command_center fallback.");
                mesh = UploadProceduralStationMesh("command_center", raceId);
            }

            _raceBuildingMeshCache[cacheKey] = mesh;
        }

        Vector3 scale = buildingType switch
        {
            "shipyard_small" => new Vector3(1.6f, 1.6f, 1.6f),
            "shipyard_medium" or "shipyard" => new Vector3(2.2f, 2.2f, 2.2f),
            "shipyard_large" => new Vector3(2.8f, 2.8f, 2.8f),
            "defense_turret" => new Vector3(1.4f, 1.4f, 1.4f),
            "missile_battery" => new Vector3(1.8f, 1.8f, 1.8f),
            "sensor_array" => new Vector3(1.8f, 1.8f, 1.8f),
            "resource_refinery" => new Vector3(2f, 2f, 2f),
            "repair_bay" => new Vector3(2.4f, 2.4f, 2.4f),
            "power_reactor" => new Vector3(1.8f, 1.8f, 1.8f),
            "supply_depot" => new Vector3(1.6f, 1.6f, 1.6f),
            _ => new Vector3(2f, 2f, 2f),
        };

        return (mesh.vao, mesh.vertCount, scale);
    }

    private static (int vao, int vbo, int vertCount) UploadProceduralStationPartMesh(string partKey, string raceId)
    {
        try
        {
            RaceVisualSchema.TryGetRace(raceId, out RaceVisualDefinition? race);
            race ??= RaceVisualSchema.AllRaces.FirstOrDefault()
                ?? new RaceVisualDefinition { Id = RaceShipMeshes.DefaultRace };
            float styleScale = 0.85f + race.Modifiers.Superstructure * 0.3f;
            float stationScale = 7f * styleScale;

            float[] vertices = ArticulatedStationPartMeshes.TryBuild(partKey, raceId, stationScale, out float[] built)
                ? built
                : [];

            if (vertices.Length == 0)
                return (0, 0, 0);

            var uploaded = MeshBuilder.UploadProcedural(vertices);
            return (uploaded.vao, uploaded.vbo, uploaded.vertexCount);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Mesh] Procedural station part build failed '{partKey}/{raceId}': {ex.Message}");
            return (0, 0, 0);
        }
    }

    private (int vao, int vertCount) ResolveStationPartMesh(string partKey, string raceId)
    {
        string cacheKey = $"{partKey}:{raceId}";
        if (!_stationPartMeshCache.TryGetValue(cacheKey, out var mesh))
        {
            mesh = UploadProceduralStationPartMesh(partKey, raceId);
            _stationPartMeshCache[cacheKey] = mesh;
        }

        return (mesh.vao, mesh.vertCount);
    }

    private (int vao, int vertCount, Vector4 color) ResolveRaceMeshForDefinition(
        EntityDefinition def, int playerId, bool isEnemy)
    {
        string raceId = ResolveFactionRaceId(playerId, isEnemy);
        ShipDesignSpec design = ShipDesignCatalog.Resolve(def.Id, raceId);

        var (vao, vertCount) = GetDesignMesh(design);
        return (vao, vertCount, Vector4.Zero);
    }

    private static void ApplyRaceTexturing(RenderComponent render, string raceId, int playerId) =>
        TeamVisualResolver.ApplyRaceTexturing(render, raceId, playerId);

    private string ResolveFactionRaceId(int playerId, bool isEnemy) =>
        _factionRaceIds.TryGetValue(playerId, out string? raceId)
            ? raceId
            : isEnemy ? _aiRaceId : _playerRaceId;

    private void ConfigureFactionRaces(IReadOnlyList<MultiplayerPlayerSlot> players)
    {
        _factionRaceIds.Clear();
        _humanPlayerId = 1;

        int factionId = 1;
        foreach (var player in players.OrderBy(p => p.SlotIndex))
        {
            _factionRaceIds[factionId] = player.RaceId;
            if (player.IsHuman)
                _humanPlayerId = factionId;
            factionId++;
        }

        _playerRaceId = _factionRaceIds.GetValueOrDefault(_humanPlayerId, RaceShipMeshes.DefaultRace);
        _aiRaceId = _factionRaceIds.Values.FirstOrDefault(id => !id.Equals(_playerRaceId, StringComparison.OrdinalIgnoreCase))
            ?? "korath";
    }

    private void ConfigureDefaultFactionRaces()
    {
        _factionRaceIds.Clear();
        _humanPlayerId = 1;
        _factionRaceIds[1] = _playerRaceId;
        _factionRaceIds[2] = _aiRaceId;
    }

    private void ConfigureFleetGalleryFactions()
    {
        _factionRaceIds.Clear();
        _humanPlayerId = 1;
        for (int i = 0; i < RaceTextureIndex.AllRaceIds.Count; i++)
            _factionRaceIds[i + 1] = RaceTextureIndex.AllRaceIds[i];

        // Screenshot-only: two adjacent gallery zones share Vesper race but keep distinct playerIds (P2 vs P3).
        if (_screenshotMode)
            _factionRaceIds[3] = "vesper";

        _playerRaceId = RaceShipMeshes.DefaultRace;
        _aiRaceId = "vesper";
    }

    private void ResolveArticulatedPartMeshes()
    {
        if (_world == null) return;

        foreach (var (entity, render) in _world.Query<RenderComponent>())
        {
            if (render.MeshId >= 0 || string.IsNullOrEmpty(render.MeshKey)) continue;

            (int vao, int vertCount) mesh;
            if (render.MeshKey.StartsWith(ArticulatedShipPartMeshes.KeyPrefix, StringComparison.OrdinalIgnoreCase))
                mesh = GetArticulatedPartMesh(entity, render.MeshKey);
            else if (IsStationPartMeshKey(render.MeshKey))
                mesh = GetStationPartMesh(entity, render.MeshKey);
            else
                continue;

            if (mesh.vao <= 0) continue;

            render.MeshId = mesh.vao;
            render.VertexCount = mesh.vertCount;
        }
    }

    private static bool IsStationPartMeshKey(string meshKey) =>
        ArticulatedStationPartMeshes.AllPartKeyPrefixes.Any(prefix =>
            meshKey.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));

    private (int vao, int vertCount) GetStationPartMesh(Entity entity, string meshKey)
    {
        string raceId = ResolveArticulatedPartRaceId(entity);
        var (vao, vertCount) = ResolveStationPartMesh(meshKey, raceId);
        if (vao > 0)
            _assetManager?.RegisterProceduralMesh(ArticulatedStationPartMeshes.ResolveMeshKey(meshKey, raceId));
        return (vao, vertCount);
    }

    private (int vao, int vertCount) GetArticulatedPartMesh(Entity entity, string meshKey)
    {
        if (_articulatedPartMeshCache.TryGetValue(meshKey, out var cached))
            return (cached.vao, cached.vertCount);

        Vector3 tint = ResolveArticulatedPartTint(entity);
        if (!ArticulatedShipPartMeshes.TryBuild(meshKey, tint, out float[] vertices) || vertices.Length == 0)
            return (0, 0);

        var uploaded = MeshBuilder.UploadProcedural(vertices);
        _articulatedPartMeshCache[meshKey] = (uploaded.vao, uploaded.vbo, uploaded.vertexCount);
        _assetManager?.RegisterProceduralMesh(meshKey);
        return (uploaded.vao, uploaded.vertexCount);
    }

    private string ResolveArticulatedPartRaceId(Entity entity)
    {
        if (_world == null) return _playerRaceId;

        var part = _world.GetComponent<ArticulatedPartComponent>(entity);
        Entity owner = part != null
            ? ArticulationDrawHelper.ResolveRootOwner(_world, part)
            : entity;

        int playerId = TeamVisualResolver.ResolvePlayerId(_world, owner);
        return ResolveFactionRaceId(playerId, isEnemy: playerId != _humanPlayerId);
    }

    private Vector3 ResolveArticulatedPartTint(Entity entity)
    {
        const float neutralR = 0.55f, neutralG = 0.55f, neutralB = 0.58f;
        if (_world == null) return new Vector3(neutralR, neutralG, neutralB);

        string raceId = ResolveArticulatedPartRaceId(entity);
        if (RaceVisualSchema.TryGetRace(raceId, out var race) && race.Palette.Primary.Length >= 3)
        {
            return new Vector3(
                race.Palette.Primary[0],
                race.Palette.Primary[1],
                race.Palette.Primary[2]);
        }

        return new Vector3(neutralR, neutralG, neutralB);
    }

    private void PrewarmFleetGalleryMeshes()
    {
        foreach (string raceId in RaceTextureIndex.AllRaceIds)
        {
            foreach (string shipId in FleetGalleryLayout.AllShipIds)
            {
                if (!_definitions.TryGetValue(shipId, out var def))
                    def = _assetManager?.Load<EntityDefinition>($"Ships/{shipId}");
                if (def == null) continue;

                ShipDesignSpec design = ShipDesignCatalog.Resolve(def.Id, raceId);
                GetDesignMesh(design);
            }

            foreach (string baseId in FleetGalleryLayout.AllBaseIds)
                ResolveRaceBuildingMesh(baseId, raceId);
        }
    }
}