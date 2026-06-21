using SharpOpenGl.Engine.Config;
using SharpOpenGl.Engine.ECS;

namespace SharpOpenGl.Engine.Entities;

/// <summary>Applies per-race shield doctrine to spawned ship entities.</summary>
public static class RaceShieldPolicy
{
    /// <summary>
    /// Adjust <see cref="HealthComponent"/> shields from hull values and attach <see cref="RaceComponent"/>.
    /// Non-shield races always get MaxShields=0; shielded races scale hull shields by race multiplier.
    /// </summary>
    public static void ApplyAtSpawn(World world, Entity entity, string raceId)
    {
        var health = world.GetComponent<HealthComponent>(entity);
        if (health == null) return;

        if (!RaceShieldSchema.TryGetRace(raceId, out RaceShieldDefinition? doctrine))
            return;

        if (world.HasComponent<RaceComponent>(entity))
            world.GetComponent<RaceComponent>(entity)!.RaceId = raceId;
        else
            world.AddComponent(entity, new RaceComponent { RaceId = raceId });

        if (!doctrine.HasShields)
        {
            health.MaxShields = 0f;
            health.CurrentShields = 0f;
            health.ShieldRegenPerSecond = 0f;
            return;
        }

        float hullShields = health.MaxShields;
        health.MaxShields = hullShields * doctrine.ShieldMultiplier;
        health.CurrentShields = health.MaxShields;
        health.ShieldRegenPerSecond = doctrine.RegenPerSecond ?? 0f;
    }
}