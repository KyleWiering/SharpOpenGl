using OpenTK.Mathematics;
using SharpOpenGl.Engine.Build;
using SharpOpenGl.Engine.ECS;
using SharpOpenGl.Engine.Economy;
using SharpOpenGl.Engine.Entities;
using SharpOpenGl.Engine.Grid;
using SharpOpenGl.Engine.Missions;

namespace SharpOpenGl.Engine.Multiplayer;

/// <summary>Runtime dependencies required to apply <see cref="IGameCommand"/> instances.</summary>
public sealed class GameCommandContext
{
    public required World World { get; init; }

    public int PlayerId { get; init; } = 1;

    public MissionState? MissionState { get; init; }

    public Func<string, EntityDefinition?>? DefinitionLoader { get; init; }

    public ResourceManager? Resources { get; init; }

    public SupplySystem? Supply { get; init; }

    /// <summary>Optional squad helper for multi-unit move routes.</summary>
    public Action<World, IReadOnlyList<Entity>, Vector3, bool>? AssignSquadMove { get; init; }

    /// <summary>Optional build-map catalog for prerequisite checks during demo placement.</summary>
    public BuildMapCatalog? BuildMapCatalog { get; init; }

    /// <summary>Optional structure placement helper for demo scripts.</summary>
    public Func<string, Vector3, bool>? PlaceBuilding { get; init; }

    /// <summary>Optional grid for fog refresh after movement commands.</summary>
    public GridSystem? Grid { get; init; }

    /// <summary>
    /// Optional fog tracker. When both <see cref="Grid"/> and this property are set,
    /// <see cref="GameCommandExecutor"/> refreshes fog for <see cref="PlayerId"/> after successful moves.
    /// </summary>
    public FogOfWar? Fog { get; init; }
}