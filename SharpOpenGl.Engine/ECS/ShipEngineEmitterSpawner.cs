using OpenTK.Mathematics;
using SharpOpenGl.Engine.Entities;
using SharpOpenGl.Engine.Rendering;

namespace SharpOpenGl.Engine.ECS;

/// <summary>
/// Attaches per-nozzle Terran engine-trail particle emitters to a spawned ship entity.
/// </summary>
public static class ShipEngineEmitterSpawner
{
    /// <summary>
    /// Creates child entities with <see cref="ShipEngineNozzleComponent"/> and
    /// <see cref="ParticleEmitterComponent"/> for each integrated stern nozzle.
    /// </summary>
    /// <returns>Number of nozzle emitter entities created (0 if race is not Terran or nozzles already exist).</returns>
    public static int AttachTerranEmitters(
        World world,
        Entity ship,
        EntityDefinition def,
        string raceId,
        bool idleGlowWhenStationary = false)
    {
        if (!raceId.Equals("terran", StringComparison.OrdinalIgnoreCase))
            return 0;
        if (HasShipEngineNozzles(world, ship))
            return 0;

        string hullKey = RaceVisualSchema.ResolveHullKey(def.Id);
        var (len, wid, hgt) = TerranEngineNozzleLayout.ResolveHullDimensions(def.Id);

        int engineCount = 2;
        if (RaceVisualSchema.TryGetRace("terran", out var race))
            engineCount = Math.Max(2, race.Modifiers.EngineCount);

        IReadOnlyList<Vector3> offsets = TerranEngineNozzleLayout.ComputeLocalOffsets(
            hullKey, len, wid, hgt, engineCount);
        float emitScale = TerranEngineNozzleLayout.ResolveEmitRateScale(hullKey);

        int created = 0;
        foreach (Vector3 offset in offsets)
        {
            Entity nozzleEntity = world.CreateEntity();
            var emitter = ParticleEffects.CreateEngineTrail(Vector3.Zero, -Vector3.UnitZ);
            float emitRate = TerranEngineNozzleLayout.CapEmitRate(emitter, emitter.EmitRate * emitScale);
            emitter.EmitRate = emitRate;

            world.AddComponent(nozzleEntity, new ShipEngineNozzleComponent
            {
                Owner = ship,
                LocalOffset = offset,
                IdleGlowWhenStationary = idleGlowWhenStationary,
                BaseEmitRate = emitRate,
                GimbalNoiseSeed = offset.X * 17.3f + offset.Y * 31.7f + offset.Z * 11.1f,
            });
            world.AddComponent(nozzleEntity, new ParticleEmitterComponent { Emitter = emitter });
            created++;
        }

        return created;
    }

    /// <summary>Counts nozzle child entities owned by <paramref name="owner"/>.</summary>
    public static int CountNozzlesForOwner(World world, Entity owner)
    {
        int count = 0;
        foreach (var (_, nozzle) in world.Query<ShipEngineNozzleComponent>())
        {
            if (nozzle.Owner == owner)
                count++;
        }

        return count;
    }

    private static bool HasShipEngineNozzles(World world, Entity owner)
    {
        foreach (var (_, nozzle) in world.Query<ShipEngineNozzleComponent>())
        {
            if (nozzle.Owner == owner)
                return true;
        }

        return false;
    }
}