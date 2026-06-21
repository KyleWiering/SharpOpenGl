using SharpOpenGl.Engine.ECS;
using SharpOpenGl.Engine.Economy;
using SharpOpenGl.Engine.Entities;
using SharpOpenGl.Engine.Grid;
using SharpOpenGl.Engine.Missions;

namespace SharpOpenGl.Engine.Persistence;

/// <summary>
/// Dependencies required to rebuild gameplay state from <see cref="SaveData"/>.
/// </summary>
public sealed class WorldLoadContext
{
    public required World World { get; init; }
    public required ResourceManager ResourceManager { get; init; }
    public MissionState? MissionState { get; init; }
    public required GridSystem GridSystem { get; init; }
    public required FogOfWar FogOfWar { get; init; }
    public required int FogPlayerId { get; init; }
    public required UnitFactory UnitFactory { get; init; }
    public required Func<string, EntityDefinition?> ResolveDefinition { get; init; }

    /// <summary>
    /// Optional post-spawn hook (mesh assignment, AI flags, building occupancy, etc.).
    /// Parameters: entity, definition (may be null for resource nodes), playerId, isEnemy.
    /// </summary>
    public Action<Entity, EntityDefinition?, int, bool>? FinalizeUnit { get; init; }
}