using OpenTK.Mathematics;
using SharpOpenGl.Engine.ECS;

namespace SharpOpenGl.Engine.Rendering;

/// <summary>Resolves faction ownership for team insignia and aura rendering.</summary>
public static class TeamVisualResolver
{
    public static int ResolvePlayerId(World world, Entity entity)
    {
        var building = world.GetComponent<BuildingComponent>(entity);
        if (building != null)
            return building.PlayerId;

        var combat = world.GetComponent<CombatTargetComponent>(entity);
        if (combat != null && combat.Faction > 0)
            return combat.Faction;

        var collector = world.GetComponent<ResourceCollectorComponent>(entity);
        if (collector != null && collector.PlayerId > 0)
            return collector.PlayerId;

        return 1;
    }

    public static bool HasTeamVisuals(World world, Entity entity) =>
        world.GetComponent<RenderComponent>(entity)?.RaceTextureIndex >= 0;

    /// <summary>Applies race substrate index and per-player team tint to a render component.</summary>
    public static void ApplyRaceTexturing(RenderComponent render, string raceId, int playerId)
    {
        render.RaceTextureIndex = RaceTextureIndex.Resolve(raceId);
        render.TeamTint = PlayerColorPalette.GetTint(playerId);
        render.ComponentTextureIndex = -1;
        render.Color = Vector4.Zero;
    }
}