using SharpOpenGl.Engine.Grid;

namespace SharpOpenGl.Engine.ECS;

/// <summary>
/// Restricts player-controlled combatants to targets visible in fog-of-war.
/// AI units bypass this gate. When grid or fog is unset, all targets are allowed.
/// </summary>
public sealed class CombatFogGate
{
    /// <summary>Grid used to map world positions to fog cells.</summary>
    public GridSystem? Grid { get; set; }

    /// <summary>Fog tracker for the local human player.</summary>
    public FogOfWar? Fog { get; set; }

    /// <summary>0-based fog player index (desktop uses 0 for the human view).</summary>
    public int FogPlayerId { get; set; }

    /// <summary>Returns true when <paramref name="attacker"/> may engage <paramref name="target"/>.</summary>
    public bool CanEngage(World world, Entity attacker, Entity target)
    {
        if (world.HasComponent<AIControlledComponent>(attacker))
            return true;

        if (Grid == null || Fog == null)
            return true;

        var transform = world.GetComponent<TransformComponent>(target);
        if (transform == null)
            return false;

        if (!Grid.WorldToGrid(transform.Position, out int gx, out int gy))
            return false;

        return Fog.IsVisible(FogPlayerId, gx, gy);
    }
}