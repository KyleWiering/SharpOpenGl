using SharpOpenGl.Engine.Grid;

namespace SharpOpenGl.Engine.ECS;

/// <summary>
/// Updates fog-of-war visibility based on entity positions and their sight radii.
/// Runs after movement systems to ensure positions are up-to-date.
/// </summary>
public sealed class FogOfWarSystem : GameSystem
{
    private readonly GridSystem _grid;
    private readonly FogOfWar _fog;
    private readonly int _playerId;

    /// <param name="grid">The grid system to convert positions.</param>
    /// <param name="fog">The fog-of-war tracker.</param>
    /// <param name="playerId">The player whose units reveal the fog.</param>
    public FogOfWarSystem(GridSystem grid, FogOfWar fog, int playerId = 0)
    {
        _grid = grid;
        _fog = fog;
        _playerId = playerId;
    }

    /// <inheritdoc/>
    public override void Update(World world, float deltaTime)
    {
        var units = new List<(int X, int Y, int SightRange)>();

        foreach (var (entity, sight) in world.Query<SightRadiusComponent>())
        {
            var transform = world.GetComponent<TransformComponent>(entity);
            if (transform == null) continue;

            if (_grid.WorldToGrid(transform.Position, out int gx, out int gy))
            {
                units.Add((gx, gy, sight.Radius));
            }
        }

        _fog.Update(_playerId, units);
    }
}
