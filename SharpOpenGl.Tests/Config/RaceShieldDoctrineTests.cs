using SharpOpenGl.Engine.Config;
using SharpOpenGl.Engine.ECS;
using SharpOpenGl.Engine.Entities;
using Xunit;

namespace SharpOpenGl.Tests.Config;

public class RaceShieldDoctrineTests
{
    [Fact]
    public void Config_defines_all_eight_races_with_at_least_two_no_shield()
    {
        RaceShieldSchema.ResetForTests();
        var races = RaceShieldSchema.AllRaces;

        Assert.Equal(8, races.Count);
        Assert.True(races.Count(r => !r.HasShields) >= 2);
        Assert.Contains(races, r => r.Id.Equals("korath", StringComparison.OrdinalIgnoreCase) && !r.HasShields);
        Assert.Contains(races, r => r.Id.Equals("nexar", StringComparison.OrdinalIgnoreCase) && !r.HasShields);
    }

    [Theory]
    [InlineData("korath")]
    [InlineData("nexar")]
    public void No_shield_races_spawn_with_zero_shields(string raceId)
    {
        RaceShieldSchema.ResetForTests();
        var world = new World();
        var def = MakeGunshipDefinition();

        Entity entity = new ShipFactory().Create(world, def);
        RaceShieldPolicy.ApplyAtSpawn(world, entity, raceId);
        var health = world.GetComponent<HealthComponent>(entity)!;

        Assert.Equal(0f, health.MaxShields);
        Assert.Equal(0f, health.CurrentShields);
        Assert.Equal(0f, health.ShieldRegenPerSecond);
        Assert.Equal(raceId, world.GetComponent<RaceComponent>(entity)!.RaceId);
    }

    [Theory]
    [InlineData("terran", 120f)]
    [InlineData("solari", 150f)]
    public void Shielded_races_spawn_with_scaled_hull_shields(string raceId, float expectedMaxShields)
    {
        RaceShieldSchema.ResetForTests();
        var world = new World();
        var def = MakeGunshipDefinition();

        Entity entity = new ShipFactory().Create(world, def);
        RaceShieldPolicy.ApplyAtSpawn(world, entity, raceId);
        var health = world.GetComponent<HealthComponent>(entity)!;

        Assert.Equal(expectedMaxShields, health.MaxShields);
        Assert.Equal(expectedMaxShields, health.CurrentShields);
        Assert.True(health.ShieldRegenPerSecond > 0f);
    }

    [Fact]
    public void ShieldRegenSystem_regenerates_out_of_combat_only()
    {
        RaceShieldSchema.ResetForTests();
        var world = new World();
        var def = MakeGunshipDefinition();
        Entity entity = new ShipFactory().Create(world, def);
        RaceShieldPolicy.ApplyAtSpawn(world, entity, "terran");

        var health = world.GetComponent<HealthComponent>(entity)!;
        health.CurrentShields = 40f;

        var regen = new ShieldRegenSystem();
        regen.Update(world, 1f);
        Assert.True(health.CurrentShields > 40f);

        health.CurrentShields = 40f;
        world.AddComponent(entity, new CombatTargetComponent
        {
            Faction = 1,
            CurrentTarget = world.CreateEntity(),
        });
        regen.Update(world, 1f);
        Assert.Equal(40f, health.CurrentShields);
    }

    [Fact]
    public void Korath_never_regenerates_even_with_positive_regen_in_schema()
    {
        RaceShieldSchema.ResetForTests();
        var world = new World();
        var def = MakeGunshipDefinition();
        Entity entity = new ShipFactory().Create(world, def);
        RaceShieldPolicy.ApplyAtSpawn(world, entity, "korath");

        var health = world.GetComponent<HealthComponent>(entity)!;
        var regen = new ShieldRegenSystem();
        regen.Update(world, 5f);
        Assert.Equal(0f, health.CurrentShields);
        Assert.Equal(0f, health.ShieldRegenPerSecond);
    }

    private static EntityDefinition MakeGunshipDefinition() => new()
    {
        Id = "gunship_heavy",
        DisplayName = "Aegis Gunship",
        Category = "gunship",
        Components = new ComponentsDefinition
        {
            Health = new HealthDefinition { MaxHP = 450, Shields = 120, Armor = 45 },
            Movement = new MovementDefinition { Speed = 42, Acceleration = 35, TurnRate = 70 },
        },
    };
}