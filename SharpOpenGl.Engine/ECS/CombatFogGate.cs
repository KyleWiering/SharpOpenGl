using SharpOpenGl.Engine.Grid;

namespace SharpOpenGl.Engine.ECS;

/// <summary>
/// Restricts combat engagement to targets visible in fog-of-war for the firing entity's team.
/// Applies to player-controlled and AI units. Visibility is queried exclusively through
/// <see cref="FogOfWar.IsVisible"/> (updated by <see cref="FogOfWarSystem"/>); this gate does
/// not perform separate line-of-sight math.
/// When <see cref="Grid"/> or <see cref="Fog"/> is unset, all targets are allowed (unit-test fallback).
/// </summary>
public sealed class CombatFogGate
{
    /// <summary>Grid used to map world positions to fog cells.</summary>
    public GridSystem? Grid { get; set; }

    /// <summary>Per-team fog tracker consumed by <see cref="CanEngage"/>.</summary>
    public FogOfWar? Fog { get; set; }

    /// <summary>
    /// Optional override mapping a 1-based game player id to a 0-based fog player index.
    /// When unset, <see cref="DefaultGamePlayerToFogIndex"/> is used (player 1 → fog 0).
    /// </summary>
    public Func<int, int>? GamePlayerToFogIndex { get; set; }

    /// <summary>Default 1-based game player id → 0-based fog index (player 1 → 0).</summary>
    public static int DefaultGamePlayerToFogIndex(int gamePlayerId) =>
        Math.Max(0, gamePlayerId - 1);

    /// <summary>Maps a 1-based game player id to the fog layer index used by <see cref="FogOfWar"/>.</summary>
    public int ToFogPlayerIndex(int gamePlayerId) =>
        GamePlayerToFogIndex?.Invoke(gamePlayerId) ?? DefaultGamePlayerToFogIndex(gamePlayerId);

    /// <summary>
    /// Resolves the 0-based fog player index for <paramref name="attacker"/> from
    /// <see cref="AIControlledComponent.PlayerId"/>, <see cref="BuildingComponent.PlayerId"/>,
    /// or <see cref="CombatTargetComponent.Faction"/>.
    /// </summary>
    public static int ResolveFogPlayerId(World world, Entity attacker)
    {
        var ai = world.GetComponent<AIControlledComponent>(attacker);
        if (ai != null)
            return DefaultGamePlayerToFogIndex(ai.PlayerId);

        var building = world.GetComponent<BuildingComponent>(attacker);
        if (building != null)
            return DefaultGamePlayerToFogIndex(building.PlayerId);

        var ct = world.GetComponent<CombatTargetComponent>(attacker);
        if (ct != null)
            return DefaultGamePlayerToFogIndex(ct.Faction);

        return 0;
    }

    /// <summary>Returns true when <paramref name="attacker"/> may engage <paramref name="target"/>.</summary>
    public bool CanEngage(World world, Entity attacker, Entity target)
    {
        if (Grid == null || Fog == null)
            return true;

        var transform = world.GetComponent<TransformComponent>(target);
        if (transform == null)
            return false;

        if (!Grid.WorldToGrid(transform.Position, out int gx, out int gy))
            return false;

        int fogPlayerId = ResolveFogPlayerId(world, attacker);
        return Fog.IsVisible(fogPlayerId, gx, gy);
    }
}