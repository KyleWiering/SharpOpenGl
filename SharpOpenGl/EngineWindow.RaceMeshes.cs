using OpenTK.Mathematics;
using SharpOpenGl.Engine.Entities;
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

    private (int vao, int vertCount) GetDesignMesh(ShipDesignSpec design)
    {
        if (!_raceMeshCache.TryGetValue(design.DesignId, out var mesh))
        {
            Vector3? tint = null;
            if (RaceVisualSchema.TryGetRace(design.RaceId, out var race) && race.Palette.Primary.Length >= 3)
                tint = new Vector3(race.Palette.Primary[0], race.Palette.Primary[1], race.Palette.Primary[2]);

            float[] vertices = RaceShipMeshes.BuildDesign(design, tint);
            var uploaded = MeshBuilder.UploadProcedural(vertices);
            mesh = (uploaded.vao, uploaded.vbo, uploaded.vertexCount);
            _raceMeshCache[design.DesignId] = mesh;
        }

        return (mesh.vao, mesh.vertCount);
    }

    private (int vao, int vertCount, Vector4 color) ResolveRaceMeshForDefinition(
        EntityDefinition def, int playerId, bool isEnemy)
    {
        string raceId = ResolveFactionRaceId(playerId, isEnemy);
        ShipDesignSpec design = isEnemy
            ? ShipDesignCatalog.ResolveForEnemy(def.Id)
            : ShipDesignCatalog.Resolve(def.Id, raceId);

        var (vao, vertCount) = GetDesignMesh(design);
        Vector4 color = ResolveFactionTeamColor(design.RaceId, playerId == _humanPlayerId);
        return (vao, vertCount, color);
    }

    private string ResolveFactionRaceId(int playerId, bool isEnemy) =>
        _factionRaceIds.TryGetValue(playerId, out string? raceId)
            ? raceId
            : isEnemy ? _aiRaceId : _playerRaceId;

    private static Vector4 ResolveFactionTeamColor(string raceId, bool isHumanPlayer)
    {
        if (RaceVisualSchema.TryGetRace(raceId, out var race) && race.Palette.Primary.Length >= 3)
        {
            return new Vector4(
                race.Palette.Primary[0],
                race.Palette.Primary[1],
                race.Palette.Primary[2],
                1f);
        }

        return isHumanPlayer
            ? GameplayEntityDisplay.FriendlyColor
            : GameplayEntityDisplay.HostileColor;
    }

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

    private static Vector4 ResolveFriendlyTint(EntityDefinition def)
    {
        string id = def.Id.ToLowerInvariant();
        string cat = def.Category.ToLowerInvariant();
        if (id.Contains("hero")) return new Vector4(0.2f, 0.8f, 1f, 1f);
        if (id.Contains("dreadnought") || cat.Contains("dreadnought")) return new Vector4(0.85f, 0.3f, 0.55f, 1f);
        if (id.Contains("carrier")) return new Vector4(0.6f, 0.6f, 0.8f, 1f);
        if (id.Contains("cruiser") || cat.Contains("cruiser")) return new Vector4(0.55f, 0.5f, 0.9f, 1f);
        if (id.Contains("gunship") || cat.Contains("gunship")) return new Vector4(0.95f, 0.45f, 0.25f, 1f);
        if (id.Contains("destroyer") || cat.Contains("destroyer")) return new Vector4(0.7f, 0.2f, 0.9f, 1f);
        if (id.Contains("frigate") || cat.Contains("frigate")) return new Vector4(0.5f, 0.7f, 0.95f, 1f);
        if (id.Contains("corvette") || cat.Contains("corvette")) return new Vector4(0.45f, 0.8f, 1f, 1f);
        if (id.Contains("bomber") || cat.Contains("bomber")) return new Vector4(0.9f, 0.5f, 0.2f, 1f);
        if (id.Contains("miner") || cat.Contains("miner")) return new Vector4(0.9f, 0.8f, 0.2f, 1f);
        if (id.Contains("transport") || id.Contains("freighter") || id.Contains("hauler") || cat.Contains("transport"))
            return new Vector4(0.55f, 0.75f, 0.95f, 1f);
        if (id.Contains("drone") || cat.Contains("drone")) return new Vector4(0.75f, 0.9f, 1f, 1f);
        if (id.Contains("support") || cat.Contains("support")) return new Vector4(0.65f, 0.85f, 0.75f, 1f);
        if (id.Contains("scout") || cat.Contains("scout")) return new Vector4(0.55f, 0.95f, 1f, 1f);
        if (id.Contains("fighter") || id.Contains("interceptor") || cat.Contains("fighter"))
            return new Vector4(0.4f, 1f, 0.4f, 1f);
        return new Vector4(0.5f, 0.8f, 1f, 1f);
    }
}