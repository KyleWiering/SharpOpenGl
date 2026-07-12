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

    private (int vao, int vertCount, Vector3 scale) ResolveRaceBuildingMesh(string buildingType, string raceId)
    {
        string cacheKey = $"{buildingType}:{raceId}";
        if (!_raceBuildingMeshCache.TryGetValue(cacheKey, out var mesh))
        {
            string stationKey = MeshManifest.StationKey(raceId, buildingType);
            var objMesh = TryGetObjMesh(stationKey);
            if (objMesh.vao != 0)
            {
                mesh = (objMesh.vao, 0, objMesh.vertCount);
            }
            else
            {
                float[] vertices = RaceBuildingMeshes.Build(buildingType, raceId);
                var uploaded = MeshBuilder.UploadProcedural(vertices);
                mesh = (uploaded.vao, uploaded.vbo, uploaded.vertexCount);
            }

            _raceBuildingMeshCache[cacheKey] = mesh;
        }

        Vector3 scale = buildingType switch
        {
            "shipyard_small" => new Vector3(1.6f, 1.6f, 1.6f),
            "shipyard_medium" or "shipyard" => new Vector3(2.2f, 2.2f, 2.2f),
            "shipyard_large" => new Vector3(2.8f, 2.8f, 2.8f),
            "defense_turret" => new Vector3(1.4f, 1.4f, 1.4f),
            "sensor_array" => new Vector3(1.8f, 1.8f, 1.8f),
            "resource_refinery" => new Vector3(2f, 2f, 2f),
            "repair_bay" => new Vector3(2.4f, 2.4f, 2.4f),
            "power_reactor" => new Vector3(1.8f, 1.8f, 1.8f),
            "supply_depot" => new Vector3(1.6f, 1.6f, 1.6f),
            _ => new Vector3(2f, 2f, 2f),
        };

        return (mesh.vao, mesh.vertCount, scale);
    }

    private (int vao, int vertCount, Vector4 color) ResolveRaceMeshForDefinition(
        EntityDefinition def, int playerId, bool isEnemy)
    {
        string raceId = ResolveFactionRaceId(playerId, isEnemy);
        ShipDesignSpec design = ShipDesignCatalog.Resolve(def.Id, raceId);

        var (vao, vertCount) = GetDesignMesh(design);
        return (vao, vertCount, Vector4.Zero);
    }

    private static void ApplyRaceTexturing(RenderComponent render, string raceId, int playerId)
    {
        render.RaceTextureIndex = RaceTextureIndex.Resolve(raceId);
        render.TeamTint = PlayerColorPalette.GetTint(playerId);
        render.Color = Vector4.Zero;
    }

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
        _playerRaceId = RaceShipMeshes.DefaultRace;
        _aiRaceId = "vesper";
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