using OpenTK.Mathematics;
using SharpOpenGl.Engine.Config;
using SharpOpenGl.Engine.Events;

namespace SharpOpenGl.Engine.ECS;

/// <summary>
/// Centralizes damage application and combat-feedback event publishing so shield-break
/// detection stays consistent across projectile, instant, and ability damage paths.
/// </summary>
public static class CombatDamagePublisher
{
    /// <summary>
    /// Applies damage, publishes <see cref="DamageDealtEvent"/>, and emits
    /// <see cref="CombatRingVfxEvent"/> when shields deplete to zero.
    /// </summary>
    public static float ApplyAndPublish(
        World world, EventBus bus, Entity attacker, Entity target, float baseDamage)
    {
        var health = world.GetComponent<HealthComponent>(target);
        if (health == null) return 0f;

        float shieldsBefore = health.CurrentShields;
        float final = DamageCalculator.Apply(baseDamage, health);

        bus.Publish(new DamageDealtEvent(attacker.Index, target.Index, baseDamage, final));

        if (shieldsBefore > 0f && health.CurrentShields <= 0f && health.MaxShields > 0f)
            PublishShieldBreak(world, bus, target);

        return final;
    }

    private static void PublishShieldBreak(World world, EventBus bus, Entity target)
    {
        var transform = world.GetComponent<TransformComponent>(target);
        if (transform == null) return;

        float radius = world.GetComponent<SelectionComponent>(target)?.SelectionRadius ?? 7f;
        string raceId = world.GetComponent<RaceComponent>(target)?.RaceId ?? string.Empty;
        Vector4 tint = string.IsNullOrEmpty(raceId)
            ? RaceShieldSchema.DefaultShieldTint
            : RaceShieldSchema.ResolveShieldTint(raceId);

        bus.Publish(new CombatRingVfxEvent(
            transform.Position,
            CombatRingVfxKind.ShieldBreak,
            radius,
            tint));
    }
}