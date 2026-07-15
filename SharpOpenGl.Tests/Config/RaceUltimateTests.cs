using SharpOpenGl.Engine.Config;
using SharpOpenGl.Engine.ECS;
using SharpOpenGl.Engine.Entities;
using Xunit;

namespace SharpOpenGl.Tests.Config;

public class RaceUltimateTests
{
    [Fact]
    public void Config_defines_one_ultimate_per_race()
    {
        RaceUltimateSchema.ResetForTests();
        var ultimates = RaceUltimateSchema.AllUltimates;

        Assert.Equal(8, ultimates.Count);

        string[] expected =
        [
            "terran", "vesper", "korath", "aetherian", "nexar", "solari", "voidborn", "cryo",
        ];

        foreach (string raceId in expected)
        {
            Assert.True(RaceUltimateSchema.TryGetForRace(raceId, out RaceUltimateDefinition? def));
            Assert.False(string.IsNullOrWhiteSpace(def!.AbilityId));
            Assert.False(string.IsNullOrWhiteSpace(def.DisplayName));
            Assert.Equal(RaceUltimatePolicy.UltimateSlot, def.Slot);
            Assert.True(def.Cooldown > 0f);
        }
    }

    [Theory]
    [InlineData("terran", "terran_orbital_salvo", "aoe")]
    [InlineData("vesper", "vesper_precision_beam", "beam")]
    [InlineData("korath", "korath_siege_barrage", "aoe")]
    [InlineData("aetherian", "aetherian_plague_cloud", "aoe")]
    [InlineData("nexar", "nexar_swarm_strike", "aoe")]
    [InlineData("solari", "solari_solar_nova", "aoe")]
    [InlineData("voidborn", "voidborn_gravity_rift", "disable")]
    [InlineData("cryo", "cryo_freeze_field", "disable")]
    public void Ultimate_lookup_matches_race_and_effect_type(string raceId, string abilityId, string effectType)
    {
        RaceUltimateSchema.ResetForTests();

        Assert.True(RaceUltimateSchema.TryGetForRace(raceId, out RaceUltimateDefinition? byRace));
        Assert.Equal(abilityId, byRace!.AbilityId);
        Assert.Equal(effectType, byRace.EffectType, ignoreCase: true);

        Assert.True(RaceUltimateSchema.TryGetByAbilityId(abilityId, out RaceUltimateDefinition? byAbility));
        Assert.Equal(raceId, byAbility!.RaceId);
    }

    [Fact]
    public void Each_ultimate_defines_distinct_castTint_in_config()
    {
        RaceUltimateSchema.ResetForTests();

        var tints = RaceUltimateSchema.AllUltimates
            .Select(u => u.ResolveCastTint())
            .ToList();

        Assert.Equal(8, tints.Count);
        Assert.Equal(8, tints.Distinct().Count());
        foreach (var ultimate in RaceUltimateSchema.AllUltimates)
            Assert.NotNull(ultimate.CastTint);
    }

    [Theory]
    [InlineData("terran", "terran_orbital_salvo")]
    [InlineData("vesper", "vesper_precision_beam")]
    [InlineData("voidborn", "voidborn_gravity_rift")]
    [InlineData("cryo", "cryo_freeze_field")]
    public void Hero_spawn_assigns_race_ultimate_on_slot_two(string raceId, string expectedAbilityId)
    {
        RaceUltimateSchema.ResetForTests();
        var world = new World();
        var def = MakeHeroDefinition();

        Entity hero = new ShipFactory().Create(world, def);
        RaceUltimatePolicy.ApplyAtSpawn(world, hero, raceId);

        var al = world.GetComponent<AbilityListComponent>(hero)!;
        var ultimate = al.GetBySlot(RaceUltimatePolicy.UltimateSlot);

        Assert.NotNull(ultimate);
        Assert.Equal(expectedAbilityId, ultimate!.Id);

        var heroComp = world.GetComponent<HeroComponent>(hero)!;
        Assert.Equal(expectedAbilityId, heroComp.AbilitySlots[RaceUltimatePolicy.UltimateSlot]);
    }

    private static EntityDefinition MakeHeroDefinition() => new()
    {
        Id = "hero_default",
        DisplayName = "Vanguard",
        Category = "hero",
        Components = new ComponentsDefinition
        {
            Health = new HealthDefinition { MaxHP = 1000, Shields = 500, Armor = 50 },
            Movement = new MovementDefinition { Speed = 120, Acceleration = 60, TurnRate = 90 },
            Hero = new HeroDefinition { Level = 1, UpgradeTree = "tech_trees/hero_vanguard" },
            Abilities =
            [
                new AbilityDefinition { Slot = 0, Id = "shield_boost", Cooldown = 30 },
                new AbilityDefinition { Slot = 1, Id = "emp_burst", Cooldown = 60 },
            ],
        },
    };
}