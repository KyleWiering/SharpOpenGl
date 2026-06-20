using System.Text.Json.Serialization;

namespace SharpOpenGl.Engine.Grid;

// ── Root ──────────────────────────────────────────────────────────────────────

/// <summary>
/// Deserialization model for a map JSON file (e.g. <c>GameData/Maps/sector_alpha.json</c>).
/// </summary>
public sealed class MapDefinition
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("displayName")]
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>[width, height] in cells.</summary>
    [JsonPropertyName("gridSize")]
    public int[] GridSize { get; set; } = [64, 64];

    [JsonPropertyName("cellSize")]
    public float CellSize { get; set; } = 1.0f;

    [JsonPropertyName("layers")]
    public string[] Layers { get; set; } = ["surface"];

    [JsonPropertyName("terrain")]
    public MapTerrain? Terrain { get; set; }

    [JsonPropertyName("spawnPoints")]
    public MapSpawnPoint[] SpawnPoints { get; set; } = [];

    [JsonPropertyName("resourceNodes")]
    public MapResourceNode[] ResourceNodes { get; set; } = [];

    [JsonPropertyName("mapFeatures")]
    public MapFeatureDefinition[] MapFeatures { get; set; } = [];
}

// ── Terrain ───────────────────────────────────────────────────────────────────

/// <summary>Terrain configuration section of a map definition.</summary>
public sealed class MapTerrain
{
    /// <summary>Terrain type applied to all cells not covered by a region.</summary>
    [JsonPropertyName("default")]
    public string Default { get; set; } = "space";

    [JsonPropertyName("regions")]
    public MapTerrainRegion[] Regions { get; set; } = [];
}

/// <summary>
/// A terrain override region — either a list of specific cells or a rectangular area.
/// </summary>
public sealed class MapTerrainRegion
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "space";

    /// <summary>Specific cells: [[x,y], [x,y], …]</summary>
    [JsonPropertyName("cells")]
    public int[][]? Cells { get; set; }

    /// <summary>Rectangular area: [minX, minY, maxX, maxY] (inclusive).</summary>
    [JsonPropertyName("rect")]
    public int[]? Rect { get; set; }
}

// ── Spawn & Resources ─────────────────────────────────────────────────────────

/// <summary>Player spawn point on the map.</summary>
public sealed class MapSpawnPoint
{
    [JsonPropertyName("player")]
    public int Player { get; set; } = 1;

    /// <summary>[x, y] grid coordinate.</summary>
    [JsonPropertyName("position")]
    public int[] Position { get; set; } = [0, 0];

    [JsonPropertyName("layer")]
    public string Layer { get; set; } = "surface";
}

/// <summary>A resource node placed on the map.</summary>
public sealed class MapResourceNode
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "energy";

    /// <summary>[x, y] grid coordinate.</summary>
    [JsonPropertyName("position")]
    public int[] Position { get; set; } = [0, 0];

    [JsonPropertyName("amount")]
    public int Amount { get; set; } = 1000;
}

/// <summary>Inspectable planet or scenery placed on the map.</summary>
public sealed class MapFeatureDefinition
{
    /// <summary><c>neutral_planet</c>, <c>harvestable_planet</c>, or <c>scenery</c>.</summary>
    [JsonPropertyName("kind")]
    public string Kind { get; set; } = "scenery";

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("subtitle")]
    public string? Subtitle { get; set; }

    /// <summary>[x, y] grid coordinate.</summary>
    [JsonPropertyName("position")]
    public int[] Position { get; set; } = [0, 0];

    [JsonPropertyName("scale")]
    public float Scale { get; set; } = 10f;

    /// <summary>For scenery: asteroid_field, nebula, debris, etc.</summary>
    [JsonPropertyName("featureType")]
    public string? FeatureType { get; set; }

    /// <summary>For harvestable_planet only.</summary>
    [JsonPropertyName("resourceType")]
    public string? ResourceType { get; set; }

    [JsonPropertyName("amount")]
    public int Amount { get; set; } = 5000;
}