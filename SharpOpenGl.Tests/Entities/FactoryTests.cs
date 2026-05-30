using SharpOpenGl.Engine.ECS;
using SharpOpenGl.Engine.Entities;
using Xunit;

namespace SharpOpenGl.Tests.Entities;

/// <summary>
/// Unit tests for <see cref="ShipFactory"/>, <see cref="BaseFactory"/>,
/// and <see cref="UnitFactory"/>.
/// All tests run without disk I/O (no AssetManager, inline definitions).
/// </summary>
public class FactoryTests
{
    // ── Helpers ───────────────────────────────────────────────────────────────

    private static EntityDefinition MakeHeroDefinition() => new()
    {
        Id          = "hero_default",
        DisplayName = "Vanguard",
        Category    = "hero",
        Mesh        = "meshes/hero_vanguard.obj",
        FallbackMesh = "default",
        Components  = new ComponentsDefinition
        {
            Health   = new HealthDefinition   { MaxHP = 1000, Shields = 500, Armor = 50 },
            Movement = new MovementDefinition { Speed = 120, Acceleration = 60, TurnRate = 90 },
            Weapons  =
            [
                new WeaponDefinition { Slot = 0, Type = "laser",   Damage = 25,  Range = 300, FireRate = 4.0f },
                new WeaponDefinition { Slot = 1, Type = "missile", Damage = 100, Range = 600, FireRate = 0.5f },
            ],
            Abilities =
            [
                new AbilityDefinition { Slot = 0, Id = "shield_boost", Cooldown = 30 },
                new AbilityDefinition { Slot = 1, Id = "emp_burst",    Cooldown = 60 },
            ],
            Hero = new HeroDefinition { Level = 1, XP = 0, UpgradeTree = "tech_trees/hero_vanguard" },
        },
    };

    private static EntityDefinition MakeFighterDefinition() => new()
    {
        Id          = "fighter_basic",
        DisplayName = "Interceptor Mk.I",
        Category    = "fighter",
        Mesh        = "meshes/fighter_basic.obj",
        FallbackMesh = "default",
        Components  = new ComponentsDefinition
        {
            Health   = new HealthDefinition   { MaxHP = 100, Shields = 0, Armor = 10 },
            Movement = new MovementDefinition { Speed = 80,  Acceleration = 120, TurnRate = 180 },
            Weapons  =
            [
                new WeaponDefinition { Slot = 0, Type = "laser", Damage = 15, Range = 200, FireRate = 2.0f },
            ],
            SquadMember = new SquadMemberDefinition { FormationSlot = 0, FormationOffset = [10f, 0f, 0f] },
        },
    };

    private static EntityDefinition MakeBaseDefinition() => new()
    {
        Id          = "command_center",
        DisplayName = "Command Center",
        Category    = "base",
        Mesh        = "meshes/command_center.obj",
        FallbackMesh = "default",
        Components  = new ComponentsDefinition
        {
            Health   = new HealthDefinition { MaxHP = 2000, Armor = 100 },
            Building = new BuildingDefinition
            {
                BuildingType   = "command_center",
                ProductionRate = 1.0f,
                BuildQueue     = [],
                Footprint      = [2, 2],
            },
        },
    };

    // ── ShipFactory — hero ─────────────────────────────────────────────────────

    [Fact]
    public void ShipFactory_creates_live_entity()
    {
        var world = new World();
        var def   = MakeHeroDefinition();
        Entity e  = new ShipFactory().Create(world, def);
        Assert.True(world.IsAlive(e));
    }

    [Fact]
    public void ShipFactory_hero_has_HealthComponent_with_correct_values()
    {
        var world = new World();
        Entity e  = new ShipFactory().Create(world, MakeHeroDefinition());
        var hp    = world.GetComponent<HealthComponent>(e);

        Assert.NotNull(hp);
        Assert.Equal(1000f, hp.MaxHP);
        Assert.Equal(1000f, hp.CurrentHP);
        Assert.Equal(500f,  hp.MaxShields);
        Assert.Equal(500f,  hp.CurrentShields);
        Assert.Equal(50f,   hp.Armor);
    }

    [Fact]
    public void ShipFactory_hero_has_MovementComponent()
    {
        var world = new World();
        Entity e  = new ShipFactory().Create(world, MakeHeroDefinition());
        var mv    = world.GetComponent<MovementComponent>(e);

        Assert.NotNull(mv);
        Assert.Equal(120f, mv.Speed);
        Assert.Equal(60f,  mv.Acceleration);
        Assert.Equal(90f,  mv.TurnRate);
        Assert.Null(mv.PathTarget);
    }

    [Fact]
    public void ShipFactory_hero_has_two_weapons()
    {
        var world = new World();
        Entity e  = new ShipFactory().Create(world, MakeHeroDefinition());
        var wl    = world.GetComponent<WeaponListComponent>(e);

        Assert.NotNull(wl);
        Assert.Equal(2, wl.Weapons.Count);

        var laser   = wl.GetBySlot(0);
        var missile = wl.GetBySlot(1);

        Assert.NotNull(laser);
        Assert.Equal("laser",   laser.Type);
        Assert.Equal(25f,       laser.Damage);

        Assert.NotNull(missile);
        Assert.Equal("missile", missile.Type);
        Assert.Equal(100f,      missile.Damage);
    }

    [Fact]
    public void ShipFactory_hero_has_HeroComponent_with_ability_slots()
    {
        var world = new World();
        Entity e  = new ShipFactory().Create(world, MakeHeroDefinition());
        var hero  = world.GetComponent<HeroComponent>(e);

        Assert.NotNull(hero);
        Assert.Equal(1, hero.Level);
        Assert.Equal("tech_trees/hero_vanguard", hero.UpgradeTreeKey);
        Assert.Equal("shield_boost", hero.AbilitySlots[0]);
        Assert.Equal("emp_burst",    hero.AbilitySlots[1]);
    }

    [Fact]
    public void ShipFactory_hero_has_TransformComponent()
    {
        var world = new World();
        Entity e  = new ShipFactory().Create(world, MakeHeroDefinition());
        Assert.NotNull(world.GetComponent<TransformComponent>(e));
    }

    [Fact]
    public void ShipFactory_hero_has_RenderComponent()
    {
        var world = new World();
        Entity e  = new ShipFactory().Create(world, MakeHeroDefinition());
        Assert.NotNull(world.GetComponent<RenderComponent>(e));
    }

    // ── ShipFactory — fighter ─────────────────────────────────────────────────

    [Fact]
    public void ShipFactory_fighter_has_SquadMemberComponent()
    {
        var world = new World();
        Entity e  = new ShipFactory().Create(world, MakeFighterDefinition());
        var sm    = world.GetComponent<SquadMemberComponent>(e);

        Assert.NotNull(sm);
        Assert.Equal(0, sm.FormationSlot);
        Assert.Equal(-1, sm.SquadId);
        Assert.Equal(10f, sm.FormationOffset.X);
    }

    [Fact]
    public void ShipFactory_fighter_does_not_have_HeroComponent()
    {
        var world = new World();
        Entity e  = new ShipFactory().Create(world, MakeFighterDefinition());
        Assert.Null(world.GetComponent<HeroComponent>(e));
    }

    // ── BaseFactory ───────────────────────────────────────────────────────────

    [Fact]
    public void BaseFactory_creates_live_entity()
    {
        var world = new World();
        Entity e  = new BaseFactory().Create(world, MakeBaseDefinition());
        Assert.True(world.IsAlive(e));
    }

    [Fact]
    public void BaseFactory_has_HealthComponent()
    {
        var world = new World();
        Entity e  = new BaseFactory().Create(world, MakeBaseDefinition());
        var hp    = world.GetComponent<HealthComponent>(e);

        Assert.NotNull(hp);
        Assert.Equal(2000f, hp.MaxHP);
        Assert.Equal(100f,  hp.Armor);
    }

    [Fact]
    public void BaseFactory_has_BuildingComponent()
    {
        var world = new World();
        Entity e  = new BaseFactory().Create(world, MakeBaseDefinition());
        var bld   = world.GetComponent<BuildingComponent>(e);

        Assert.NotNull(bld);
        Assert.Equal("command_center", bld.BuildingType);
        Assert.Equal(1f,               bld.ProductionRate);
        Assert.Equal(new[] { 2, 2 },   bld.Footprint);
        Assert.Empty(bld.BuildQueue);
    }

    [Fact]
    public void BaseFactory_does_not_have_MovementComponent()
    {
        var world = new World();
        Entity e  = new BaseFactory().Create(world, MakeBaseDefinition());
        Assert.Null(world.GetComponent<MovementComponent>(e));
    }

    // ── UnitFactory routing ───────────────────────────────────────────────────

    [Fact]
    public void UnitFactory_routes_base_category_to_BaseFactory()
    {
        var world = new World();
        Entity e  = new UnitFactory().Create(world, MakeBaseDefinition());
        Assert.NotNull(world.GetComponent<BuildingComponent>(e));
    }

    [Fact]
    public void UnitFactory_routes_hero_category_to_ShipFactory()
    {
        var world = new World();
        Entity e  = new UnitFactory().Create(world, MakeHeroDefinition());
        Assert.NotNull(world.GetComponent<HeroComponent>(e));
    }

    [Fact]
    public void UnitFactory_routes_fighter_category_to_ShipFactory()
    {
        var world = new World();
        Entity e  = new UnitFactory().Create(world, MakeFighterDefinition());
        Assert.NotNull(world.GetComponent<HealthComponent>(e));
        Assert.NotNull(world.GetComponent<MovementComponent>(e));
    }

    // ── Default Fallback ──────────────────────────────────────────────────────

    [Fact]
    public void ShipFactory_falls_back_to_default_mesh_when_mesh_is_empty()
    {
        // When mesh is empty the factory should not throw and entity should still be alive.
        var world = new World();
        var def   = MakeHeroDefinition();
        def.Mesh  = string.Empty;

        Entity e  = new ShipFactory().Create(world, def);
        Assert.True(world.IsAlive(e));
        // RenderComponent should still be attached (using fallback/default mesh id = -1)
        Assert.NotNull(world.GetComponent<RenderComponent>(e));
    }

    [Fact]
    public void SpawnWithNoComponents_does_not_throw()
    {
        var world = new World();
        var def   = new EntityDefinition { Id = "bare", Category = "fighter" };
        Entity e  = new ShipFactory().Create(world, def);
        Assert.True(world.IsAlive(e));
    }
}
