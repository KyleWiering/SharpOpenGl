namespace SharpOpenGl.Engine.ECS;

/// <summary>
/// Regenerates shields out of combat for races with a positive regen rate.
/// </summary>
public sealed class ShieldRegenSystem : GameSystem
{
    public override void Update(World world, float deltaTime)
    {
        foreach (var (entity, health) in world.Query<HealthComponent>())
        {
            if (health.MaxShields <= 0f || health.ShieldRegenPerSecond <= 0f)
                continue;
            if (health.CurrentShields >= health.MaxShields)
                continue;
            if (CombatState.IsInCombat(world, entity))
                continue;

            health.CurrentShields = Math.Min(
                health.MaxShields,
                health.CurrentShields + health.ShieldRegenPerSecond * deltaTime);
        }
    }
}

/// <summary>Shared combat-state helpers for shield regen and VFX.</summary>
public static class CombatState
{
    public static bool IsInCombat(World world, Entity entity)
    {
        var ct = world.GetComponent<CombatTargetComponent>(entity);
        if (ct != null && ct.CurrentTarget != Entity.Null && world.IsAlive(ct.CurrentTarget))
            return true;

        foreach (var (_, otherCt) in world.Query<CombatTargetComponent>())
        {
            if (otherCt.CurrentTarget == entity)
                return true;
        }

        return false;
    }
}