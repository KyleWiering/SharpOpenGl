using SharpOpenGl.Engine.ECS;

namespace SharpOpenGl.Engine.Grid;

/// <summary>
/// A single cell within the game grid. Stores all spatial and state data for one tile.
/// </summary>
public sealed class GridCell
{
    /// <summary>Column index (X axis) in grid-space.</summary>
    public int X { get; }

    /// <summary>Row index (Y axis) in grid-space.</summary>
    public int Y { get; }

    /// <summary>Vertical layer this cell belongs to.</summary>
    public GridLayer Layer { get; }

    /// <summary>Terrain type — affects movement cost and line-of-sight.</summary>
    public TerrainType Terrain { get; set; } = TerrainType.Space;

    /// <summary>
    /// The entity currently occupying this cell, or <see cref="Entity.Null"/> if empty.
    /// Only one entity may occupy a cell per layer at a time.
    /// </summary>
    public Entity Occupant { get; set; } = Entity.Null;

    /// <summary>
    /// Whether a resource node is present on this cell.
    /// The resource entity itself is tracked in <see cref="ResourceEntity"/>.
    /// </summary>
    public bool HasResource => ResourceEntity != Entity.Null;

    /// <summary>The resource node entity on this cell, or <see cref="Entity.Null"/>.</summary>
    public Entity ResourceEntity { get; set; } = Entity.Null;

    /// <summary>Per-player fog-of-war state. Indexed by player ID (0-based).</summary>
    private readonly FogState[] _fogStates;

    /// <param name="x">Column index.</param>
    /// <param name="y">Row index.</param>
    /// <param name="layer">Vertical layer.</param>
    /// <param name="maxPlayers">Maximum number of players (allocates fog array).</param>
    public GridCell(int x, int y, GridLayer layer, int maxPlayers = 8)
    {
        X = x;
        Y = y;
        Layer = layer;
        _fogStates = new FogState[maxPlayers];
    }

    /// <summary>Get the fog-of-war state for <paramref name="playerId"/>.</summary>
    public FogState GetFog(int playerId) =>
        (uint)playerId < (uint)_fogStates.Length ? _fogStates[playerId] : FogState.Unexplored;

    /// <summary>Set the fog-of-war state for <paramref name="playerId"/>.</summary>
    public void SetFog(int playerId, FogState state)
    {
        if ((uint)playerId < (uint)_fogStates.Length)
            _fogStates[playerId] = state;
    }

    /// <summary>
    /// Returns the movement cost for this cell.
    /// Returns <see cref="float.MaxValue"/> for impassable cells.
    /// </summary>
    public float MovementCost => Terrain switch
    {
        TerrainType.Space       => 1.0f,
        TerrainType.Nebula      => 2.0f,
        TerrainType.Debris      => 1.5f,
        TerrainType.AsteroidField => 3.0f,
        TerrainType.Impassable  => float.MaxValue,
        _                       => 1.0f,
    };

    /// <summary>Returns <c>true</c> if units may enter this cell.</summary>
    public bool IsPassable => Terrain != TerrainType.Impassable;

    /// <inheritdoc/>
    public override string ToString() => $"GridCell({X},{Y},{Layer},{Terrain})";
}
