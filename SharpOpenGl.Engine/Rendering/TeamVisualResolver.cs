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
}