using OpenTK.Mathematics;
using SharpOpenGl.Engine.ECS;
using SharpOpenGl.Engine.Economy;
using Xunit;

namespace SharpOpenGl.Tests.ECS;

public class AIPlayerSystemTests
{
    [Fact]
    public void AI_assigns_idle_miner_to_nearest_resource_node()
    {
        var world = new World();
        world.AddSystem(new AIPlayerSystem());

        Entity miner = world.CreateEntity();
        world.AddComponent(miner, new TransformComponent { Position = Vector3.Zero });
        world.AddComponent(miner, new MovementComponent { Speed = 10f });
        world.AddComponent(miner, new AIControlledComponent { PlayerId = 2 });
        world.AddComponent(miner, new ResourceCollectorComponent
        {
            PlayerId = 2,
            State = CollectorState.Idle,
            HarvestRange = 28f,
        });

        Entity nearNode = world.CreateEntity();
        world.AddComponent(nearNode, new TransformComponent { Position = new Vector3(20f, 0f, 0f) });
        world.AddComponent(nearNode, new ResourceNodeComponent
        {
            ResourceType = ResourceType.Minerals,
            Amount = 500f,
            MaxAmount = 500f,
        });

        Entity farNode = world.CreateEntity();
        world.AddComponent(farNode, new TransformComponent { Position = new Vector3(200f, 0f, 0f) });
        world.AddComponent(farNode, new ResourceNodeComponent
        {
            ResourceType = ResourceType.Minerals,
            Amount = 500f,
            MaxAmount = 500f,
        });

        world.Update(3.1f);

        var collector = world.GetComponent<ResourceCollectorComponent>(miner)!;
        Assert.Equal(CollectorState.MovingToNode, collector.State);
        Assert.Equal(nearNode, collector.AssignedNode);
        Assert.False(float.IsNaN(collector.OrbitAngle));

        world.Dispose();
    }

    [Fact]
    public void TryAdvanceTowardEnemy_sets_path_to_nearest_hostile()
    {
        var world = new World();

        Entity aiShip = world.CreateEntity();
        world.AddComponent(aiShip, new TransformComponent { Position = Vector3.Zero });
        var movement = new MovementComponent { Speed = 40f };
        world.AddComponent(aiShip, movement);
        var ai = new AIControlledComponent { PlayerId = 2, Aggressiveness = 0.8f };
        world.AddComponent(aiShip, ai);
        world.AddComponent(aiShip, new CombatTargetComponent { Faction = 2 });
        var weapons = new WeaponListComponent();
        weapons.Weapons.Add(new WeaponComponent { Range = 200f });
        world.AddComponent(aiShip, weapons);

        Entity nearEnemy = world.CreateEntity();
        world.AddComponent(nearEnemy, new TransformComponent { Position = new Vector3(250f, 0f, 0f) });
        world.AddComponent(nearEnemy, new HealthComponent { MaxHP = 100f, CurrentHP = 100f });
        world.AddComponent(nearEnemy, new CombatTargetComponent { Faction = 1 });

        Entity farEnemy = world.CreateEntity();
        world.AddComponent(farEnemy, new TransformComponent { Position = new Vector3(500f, 0f, 0f) });
        world.AddComponent(farEnemy, new HealthComponent { MaxHP = 100f, CurrentHP = 100f });
        world.AddComponent(farEnemy, new CombatTargetComponent { Faction = 1 });

        bool advanced = AIPlayerSystem.TryAdvanceTowardEnemy(
            world, aiShip, ai, movement, Vector3.Zero, weapons);

        Assert.True(advanced);
        Assert.Equal(new Vector3(250f, 0f, 0f), movement.PathTarget);

        world.Dispose();
    }

    [Fact]
    public void AI_skips_patrol_while_collector_is_harvesting()
    {
        var world = new World();
        world.AddSystem(new AIPlayerSystem());

        Entity miner = world.CreateEntity();
        world.AddComponent(miner, new TransformComponent { Position = Vector3.Zero });
        world.AddComponent(miner, new MovementComponent { Speed = 10f, PathTarget = new Vector3(5f, 0f, 0f) });
        world.AddComponent(miner, new AIControlledComponent { PlayerId = 2 });
        world.AddComponent(miner, new ResourceCollectorComponent
        {
            PlayerId = 2,
            State = CollectorState.Collecting,
            AssignedNode = world.CreateEntity(),
        });

        world.Update(3.1f);

        var movement = world.GetComponent<MovementComponent>(miner)!;
        Assert.Equal(new Vector3(5f, 0f, 0f), movement.PathTarget);

        world.Dispose();
    }

    [Fact]
    public void TryRetreatWhenWounded_sets_path_to_base_when_hp_below_quarter()
    {
        var world = new World();

        Entity commandCenter = world.CreateEntity();
        world.AddComponent(commandCenter, new TransformComponent { Position = new Vector3(0f, 0f, 200f) });
        world.AddComponent(commandCenter, new BuildingComponent
        {
            BuildingType = "command_center",
            PlayerId = 2,
        });

        Entity aiShip = world.CreateEntity();
        world.AddComponent(aiShip, new TransformComponent { Position = Vector3.Zero });
        var movement = new MovementComponent { Speed = 40f };
        world.AddComponent(aiShip, movement);
        var ai = new AIControlledComponent { PlayerId = 2, Aggressiveness = 0.8f };
        world.AddComponent(aiShip, ai);
        world.AddComponent(aiShip, new CombatTargetComponent { Faction = 2 });
        world.AddComponent(aiShip, new HealthComponent { MaxHP = 100f, CurrentHP = 20f });

        Entity enemy = world.CreateEntity();
        world.AddComponent(enemy, new TransformComponent { Position = new Vector3(50f, 0f, 0f) });
        world.AddComponent(enemy, new HealthComponent { MaxHP = 100f, CurrentHP = 100f });
        world.AddComponent(enemy, new CombatTargetComponent { Faction = 1 });

        bool retreated = AIPlayerSystem.TryRetreatWhenWounded(
            world, aiShip, ai, movement, Vector3.Zero);

        Assert.True(retreated);
        Assert.Equal(new Vector3(0f, 0f, 200f), movement.PathTarget);
        Assert.Equal(Entity.Null, world.GetComponent<CombatTargetComponent>(aiShip)!.CurrentTarget);

        world.Dispose();
    }

    [Fact]
    public void TryRetreatWhenWounded_skips_when_hp_above_quarter()
    {
        var world = new World();

        Entity aiShip = world.CreateEntity();
        world.AddComponent(aiShip, new TransformComponent { Position = Vector3.Zero });
        var movement = new MovementComponent { Speed = 40f };
        world.AddComponent(aiShip, movement);
        var ai = new AIControlledComponent { PlayerId = 2 };
        world.AddComponent(aiShip, ai);
        world.AddComponent(aiShip, new HealthComponent { MaxHP = 100f, CurrentHP = 30f });

        Entity enemy = world.CreateEntity();
        world.AddComponent(enemy, new TransformComponent { Position = new Vector3(50f, 0f, 0f) });
        world.AddComponent(enemy, new HealthComponent { MaxHP = 100f, CurrentHP = 100f });
        world.AddComponent(enemy, new CombatTargetComponent { Faction = 1 });

        bool retreated = AIPlayerSystem.TryRetreatWhenWounded(
            world, aiShip, ai, movement, Vector3.Zero);

        Assert.False(retreated);
        Assert.Null(movement.PathTarget);

        world.Dispose();
    }

    [Fact]
    public void TryFocusFireWeakestInRange_locks_lowest_hp_enemy_in_weapon_range()
    {
        var world = new World();

        Entity aiShip = world.CreateEntity();
        world.AddComponent(aiShip, new TransformComponent { Position = Vector3.Zero });
        var ai = new AIControlledComponent { PlayerId = 2, Aggressiveness = 0.8f };
        world.AddComponent(aiShip, ai);
        var combatTarget = new CombatTargetComponent { Faction = 2, TargetingMode = TargetPriority.Closest };
        world.AddComponent(aiShip, combatTarget);
        var weapons = new WeaponListComponent();
        weapons.Weapons.Add(new WeaponComponent { Range = 200f });
        world.AddComponent(aiShip, weapons);

        Entity healthyEnemy = world.CreateEntity();
        world.AddComponent(healthyEnemy, new TransformComponent { Position = new Vector3(30f, 0f, 0f) });
        world.AddComponent(healthyEnemy, new HealthComponent { MaxHP = 100f, CurrentHP = 90f });
        world.AddComponent(healthyEnemy, new CombatTargetComponent { Faction = 1 });

        Entity woundedEnemy = world.CreateEntity();
        world.AddComponent(woundedEnemy, new TransformComponent { Position = new Vector3(80f, 0f, 0f) });
        world.AddComponent(woundedEnemy, new HealthComponent { MaxHP = 100f, CurrentHP = 15f });
        world.AddComponent(woundedEnemy, new CombatTargetComponent { Faction = 1 });

        Entity farEnemy = world.CreateEntity();
        world.AddComponent(farEnemy, new TransformComponent { Position = new Vector3(500f, 0f, 0f) });
        world.AddComponent(farEnemy, new HealthComponent { MaxHP = 100f, CurrentHP = 5f });
        world.AddComponent(farEnemy, new CombatTargetComponent { Faction = 1 });

        bool focused = AIPlayerSystem.TryFocusFireWeakestInRange(
            world, aiShip, ai, combatTarget, Vector3.Zero, weapons);

        Assert.True(focused);
        Assert.Equal(woundedEnemy, combatTarget.CurrentTarget);
        Assert.Equal(TargetPriority.LowestHP, combatTarget.TargetingMode);

        world.Dispose();
    }

    [Fact]
    public void FindDepositTarget_prefers_command_center_for_player()
    {
        var world = new World();

        Entity refinery = world.CreateEntity();
        world.AddComponent(refinery, new TransformComponent { Position = new Vector3(10f, 0f, 0f) });
        world.AddComponent(refinery, new BuildingComponent
        {
            BuildingType = "resource_refinery",
            PlayerId = 2,
        });

        Entity commandCenter = world.CreateEntity();
        world.AddComponent(commandCenter, new TransformComponent { Position = new Vector3(50f, 0f, 0f) });
        world.AddComponent(commandCenter, new BuildingComponent
        {
            BuildingType = "command_center",
            PlayerId = 2,
        });

        Entity deposit = AIPlayerSystem.FindDepositTarget(world, 2, Vector3.Zero);
        Assert.Equal(commandCenter, deposit);

        world.Dispose();
    }
}