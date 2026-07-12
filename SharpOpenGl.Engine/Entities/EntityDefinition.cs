using System.Text.Json.Serialization;

namespace SharpOpenGl.Engine.Entities;

// ── Nested DTOs ────────────────────────────────────────────────────────────────

/// <summary>Per-resource build / spawn cost.</summary>
public sealed class CostDefinition
{
    public int Energy    { get; set; }
    public int Minerals  { get; set; }
    public int Data      { get; set; }
    public int Crew      { get; set; }
}

/// <summary>JSON shape for a health block inside an entity definition.</summary>
public sealed class HealthDefinition
{
    public float MaxHP   { get; set; } = 100f;
    public float Shields { get; set; }
    public float Armor   { get; set; }
}

/// <summary>JSON shape for a movement block.</summary>
public sealed class MovementDefinition
{
    public float Speed        { get; set; } = 60f;
    public float Acceleration { get; set; } = 60f;
    public float TurnRate     { get; set; } = 90f;
}

/// <summary>JSON shape for a single weapon entry.</summary>
public sealed class WeaponDefinition
{
    public int    Slot           { get; set; }
    public string Type           { get; set; } = "laser";
    public float  Damage         { get; set; }
    public float  Range          { get; set; }
    public float  FireRate       { get; set; }
    public string ProjectileType { get; set; } = "default";
}

/// <summary>JSON shape for a single ability entry.</summary>
public sealed class AbilityDefinition
{
    public int    Slot     { get; set; }
    public string Id       { get; set; } = string.Empty;
    public float  Cooldown { get; set; }
}

/// <summary>JSON shape for hero-specific data.</summary>
public sealed class HeroDefinition
{
    public int    Level       { get; set; } = 1;
    public int    XP          { get; set; }
    public string UpgradeTree { get; set; } = string.Empty;
}

/// <summary>JSON shape for squad-member data.</summary>
public sealed class SquadMemberDefinition
{
    public int   FormationSlot   { get; set; } = -1;
    public float[]? FormationOffset { get; set; }
}

/// <summary>JSON shape for a resource collector block.</summary>
public sealed class ResourceCollectorDefinition
{
    /// <summary>Extraction mode: drones, eva, or tractor_beam.</summary>
    public string HarvestMode { get; set; } = "drones";

    /// <summary>World-unit range at which collection begins (mode-specific default if 0).</summary>
    public float HarvestRange { get; set; }

    /// <summary>Base units per second extracted from a node.</summary>
    public float HarvestRate { get; set; } = 5f;

    /// <summary>Multiplier applied to <see cref="HarvestRate"/> per mode tuning.</summary>
    public float HarvestRateMultiplier { get; set; } = 1f;

    public float CarryCapacity { get; set; } = 50f;
}

/// <summary>JSON shape for a building block.</summary>
public sealed class BuildingDefinition
{
    public string   BuildingType   { get; set; } = "generic";
    public float    ProductionRate { get; set; } = 1f;
    public string[] BuildQueue     { get; set; } = [];
    public int[]    Footprint      { get; set; } = [1, 1];
}

/// <summary>Bag of optional component blocks inside an entity definition.</summary>
public sealed class ComponentsDefinition
{
    public HealthDefinition?      Health      { get; set; }
    public MovementDefinition?    Movement    { get; set; }
    public WeaponDefinition[]?    Weapons     { get; set; }
    public AbilityDefinition[]?   Abilities   { get; set; }
    public HeroDefinition?        Hero        { get; set; }
    public SquadMemberDefinition? SquadMember { get; set; }
    public BuildingDefinition?    Building    { get; set; }
    public ResourceCollectorDefinition? ResourceCollector { get; set; }

    /// <summary>Fog-of-war reveal radius in grid cells.</summary>
    public int SightRadius { get; set; } = 5;
}

// ── Root DTO ──────────────────────────────────────────────────────────────────

/// <summary>
/// Top-level JSON definition for any spawnable entity (ship, unit, or building).
/// Loaded from <c>GameData/Ships/*.json</c>, <c>GameData/Units/*.json</c>, etc.
/// </summary>
public sealed class EntityDefinition
{
    /// <summary>Unique string identifier (must match the filename without extension).</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Human-readable display name shown in UI.</summary>
    public string DisplayName { get; set; } = "Unknown";

    /// <summary>Broad category tag (e.g. "hero", "fighter", "worker", "base").</summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>Asset key for the preferred mesh, relative to the mesh root.</summary>
    public string Mesh { get; set; } = string.Empty;

    public string? ComponentTexture { get; set; }

    /// <summary>
    /// Fallback mesh key used when <see cref="Mesh"/> cannot be loaded.
    /// Defaults to "default", resolved via <see cref="Rendering.MeshRegistry"/> fallback.
    /// </summary>
    public string FallbackMesh { get; set; } = "default";

    /// <summary>Component data blocks parsed from JSON.</summary>
    public ComponentsDefinition? Components { get; set; }

    /// <summary>Resource cost to produce this entity.</summary>
    public CostDefinition? Cost { get; set; }

    /// <summary>Seconds required to build/spawn this entity.</summary>
    public float BuildTime { get; set; }

    /// <summary>
    /// List of entity definition IDs that this building can produce.
    /// Only relevant for building-type entities (shipyard, command center).
    /// </summary>
    public List<string>? Producible { get; set; }

    // JSON comment fields are ignored automatically (AllowTrailingCommas + SkipComments).
}
