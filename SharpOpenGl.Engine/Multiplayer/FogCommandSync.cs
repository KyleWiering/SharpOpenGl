using SharpOpenGl.Engine.ECS;
using SharpOpenGl.Engine.Grid;

namespace SharpOpenGl.Engine.Multiplayer;

/// <summary>
/// Refreshes fog-of-war for a commanding player after deterministic command execution.
/// Mirrors <see cref="FogOfWarSystem"/> visibility rules without requiring an ECS tick.
/// </summary>
public static class FogCommandSync
{
    /// <summary>
    /// Recomputes fog for <paramref name="playerId"/> from unit sight contributors owned by that player.
    /// </summary>
    /// <param name="grid">Grid used to map world positions to fog cells.</param>
    /// <param name="fog">Fog tracker updated via the standard downgrade-and-reveal pipeline.</param>
    /// <param name="world">Live simulation world containing sight entities.</param>
    /// <param name="playerId">1-based commanding game player id.</param>
    public static void RefreshPlayerFog(GridSystem grid, FogOfWar fog, World world, int playerId)
    {
        int fogIndex = CombatFogGate.DefaultGamePlayerToFogIndex(playerId);
        var units = new List<(int X, int Y, int SightRange)>();

        foreach (var (entity, sight) in world.Query<SightRadiusComponent>())
        {
            if (!ContributesSightForPlayer(world, entity, playerId))
                continue;

            var transform = world.GetComponent<TransformComponent>(entity);
            if (transform == null)
                continue;

            if (grid.WorldToGrid(transform.Position, out int gx, out int gy))
                units.Add((gx, gy, sight.Radius));
        }

        fog.Update(fogIndex, units);
    }

    private static bool ContributesSightForPlayer(World world, Entity entity, int playerId)
    {
        var ai = world.GetComponent<AIControlledComponent>(entity);
        if (ai != null)
            return playerId > 0 && ai.PlayerId == playerId;

        var building = world.GetComponent<BuildingComponent>(entity);
        if (building != null && building.PlayerId == playerId)
            return true;

        var combat = world.GetComponent<CombatTargetComponent>(entity);
        return combat != null && combat.Faction == playerId;
    }
}