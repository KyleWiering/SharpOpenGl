using OpenTK.Mathematics;
using SharpOpenGl.Engine.ECS;
using SharpOpenGl.Engine.Economy;
using SharpOpenGl.Engine.Entities;
using SharpOpenGl.Engine.Events;
using SharpOpenGl.Engine.Rendering;
using Xunit;

namespace SharpOpenGl.Tests.ECS;

/// <summary>Cross-cutting utility articulation tests (miner arm, repair arm, nozzle gimbal, mining VFX smoke).</summary>
public class UtilityArticulationTests
{
    private static EntityDefinition MinerBasicDef() => new()
    {
        Id = "miner_basic",
        Category = "miner",
        Components = new ComponentsDefinition
        {
            ResourceCollector = new ResourceCollectorDefinition(),
        },
    };

    private static EntityDefinition SupportRepairDef() => new()
    {
        Id = "support_repair",
        Category = "support",
        Components = new ComponentsDefinition
        {
            ShipRepair = new ShipRepairDefinition
            {
                RepairRange = 60f,
                RepairRate = 15f,
                RepairableCategories = ["fighter", "gunship", "corvette"],
            },
        },
    };

    private static Entity CreateDamagedFighter(World world, Vector3 position, float hpFraction = 0.2f)
    {
        Entity entity = world.CreateEntity();
        world.AddComponent(entity, new TransformComponent { Position = position });
        world.AddComponent(entity, new HealthComponent { MaxHP = 100f, CurrentHP = 100f * hpFraction });
        world.AddComponent(entity, new CombatTargetComponent { Faction = 0 });
        world.AddComponent(entity, new EntityNameComponent { DefinitionId = "fighter_basic" });
        return entity;
    }

    private static (World world, Entity repairer, Entity arm) BuildRepairAimScene(
        Vector3 repairerPos,
        Vector3 targetPos,
        bool populateActiveTarget)
    {
        var world = new World();
        var repairSystem = new RepairSystem(new EventBus()) { PlayerFaction = 1 };
        var aimSystem = new UtilityArticulationAimSystem();

        Entity repairer = world.CreateEntity();
        world.AddComponent(repairer, new TransformComponent { Position = repairerPos });
        world.AddComponent(repairer, new EntityNameComponent { DefinitionId = "support_repair" });
        world.AddComponent(repairer, new CombatTargetComponent { Faction = 1 });
        world.AddComponent(repairer, new ShipRepairComponent
        {
            RepairRange = 60f,
            RepairRate = 15f,
            RepairableCategories = ["fighter", "gunship", "corvette"],
        });

        UtilityArticulationSpawner.AttachRepairArm(world, repairer, SupportRepairDef(), "terran");

        Entity arm = Entity.Null;
        foreach (var (entity, part) in world.Query<ArticulatedPartComponent>())
        {
            if (part.Owner == repairer && part.PartType == ArticulatedPartType.RepairArm)
            {
                arm = entity;
                break;
            }
        }

        if (populateActiveTarget)
        {
            Entity target = CreateDamagedFighter(world, targetPos);
            world.AddSystem(repairSystem);
            world.Update(0.016f);
            Assert.Equal(target, world.GetComponent<ShipRepairComponent>(repairer)!.ActiveTarget);
        }

        world.AddSystem(aimSystem);
        world.Update(0.016f);

        return (world, repairer, arm);
    }

    private static (World world, Entity miner, Entity node, Entity arm) BuildMinerAimScene(
        CollectorState state,
        Vector3 minerPos,
        Vector3 nodePos)
    {
        var world = new World();
        var aimSystem = new UtilityArticulationAimSystem();

        Entity miner = world.CreateEntity();
        world.AddComponent(miner, new TransformComponent { Position = minerPos });
        world.AddComponent(miner, new EntityNameComponent { DefinitionId = "miner_basic" });

        Entity node = world.CreateEntity();
        world.AddComponent(node, new TransformComponent { Position = nodePos });

        world.AddComponent(miner, new ResourceCollectorComponent
        {
            AssignedNode = node,
            State = state,
        });

        UtilityArticulationSpawner.AttachMinerArms(world, miner, MinerBasicDef(), "terran");

        Entity arm = Entity.Null;
        foreach (var (entity, part) in world.Query<ArticulatedPartComponent>())
        {
            if (part.Owner == miner && part.PartType == ArticulatedPartType.MiningArm)
            {
                arm = entity;
                break;
            }
        }

        world.AddSystem(aimSystem);
        world.Update(0.016f);

        return (world, miner, node, arm);
    }

    [Fact]
    public void MinerArm_aims_at_assigned_node_when_collecting()
    {
        var (world, _, _, arm) = BuildMinerAimScene(
            CollectorState.Collecting,
            minerPos: Vector3.Zero,
            nodePos: new Vector3(8f, 3f, -12f));

        Assert.NotEqual(Entity.Null, arm);
        var part = world.GetComponent<ArticulatedPartComponent>(arm)!;

        Assert.True(part.HasAimTarget);
        Assert.NotEqual(0f, part.TargetYaw);
        Assert.NotEqual(0f, part.TargetPitch);
    }

    [Fact]
    public void MinerArm_stows_when_not_collecting()
    {
        var (world, _, _, arm) = BuildMinerAimScene(
            CollectorState.Idle,
            minerPos: Vector3.Zero,
            nodePos: new Vector3(8f, 3f, -12f));

        Assert.NotEqual(Entity.Null, arm);
        var part = world.GetComponent<ArticulatedPartComponent>(arm)!;

        Assert.False(part.HasAimTarget);
        Assert.Equal(0f, part.TargetYaw);
        Assert.Equal(0f, part.TargetPitch);
    }

    [Fact]
    public void AttachMinerArms_creates_one_mining_arm_child()
    {
        var world = new World();
        Entity miner = world.CreateEntity();
        var def = MinerBasicDef();

        int first = UtilityArticulationSpawner.AttachMinerArms(world, miner, def, "terran");
        int second = UtilityArticulationSpawner.AttachMinerArms(world, miner, def, "terran");

        Assert.Equal(1, first);
        Assert.Equal(0, second);
        Assert.Equal(1, UtilityArticulationSpawner.CountMiningArmsForOwner(world, miner));

        foreach (var (entity, part) in world.Query<ArticulatedPartComponent>())
        {
            if (part.Owner != miner || part.PartType != ArticulatedPartType.MiningArm)
                continue;

            var render = world.GetComponent<RenderComponent>(entity);
            Assert.NotNull(render);
            Assert.Equal(UtilityPartMeshes.MiningArmMeshKey("miner_basic"), render!.MeshKey);
            Assert.True(render.Visible);
        }
    }

    [Fact]
    public void EngineNozzle_gimbal_shifts_exhaust_origin_when_thrusting()
    {
        var world = new World();
        var system = new ShipEngineEmitterSystem();

        Entity ship = world.CreateEntity();
        world.AddComponent(ship, new TransformComponent { Position = Vector3.Zero, Scale = Vector3.One });
        world.AddComponent(ship, new MovementComponent { Velocity = new Vector3(2f, 0f, 0f) });

        Vector3 localOffset = new Vector3(0.4f, 0.1f, -1.2f);
        Entity nozzleEntity = world.CreateEntity();
        var emitter = ParticleEffects.CreateEngineTrail(Vector3.Zero, -Vector3.UnitZ);
        world.AddComponent(nozzleEntity, new ShipEngineNozzleComponent
        {
            Owner = ship,
            LocalOffset = localOffset,
            GimbalPitch = 2f,
            BaseEmitRate = emitter.EmitRate,
        });
        world.AddComponent(nozzleEntity, new ParticleEmitterComponent { Emitter = emitter });

        system.Update(world, 0.016f);

        var ownerTf = world.GetComponent<TransformComponent>(ship)!;
        Vector3 baseline = ComputeNozzleOrigin(ownerTf, localOffset, 0f, 0f);
        Vector3 pitched = emitter.Origin;

        Assert.NotEqual(baseline, pitched);
        Assert.True(pitched.Y > baseline.Y);
    }

    [Fact]
    public void RepairArm_aims_at_active_target_in_range()
    {
        var (world, _, arm) = BuildRepairAimScene(
            repairerPos: Vector3.Zero,
            targetPos: new Vector3(10f, 4f, -8f),
            populateActiveTarget: true);

        Assert.NotEqual(Entity.Null, arm);
        var part = world.GetComponent<ArticulatedPartComponent>(arm)!;

        Assert.True(part.HasAimTarget);
        Assert.NotEqual(0f, part.TargetYaw);
        Assert.NotEqual(-20f, part.TargetPitch);
    }

    [Fact]
    public void RepairArm_stows_when_no_target()
    {
        var (world, _, arm) = BuildRepairAimScene(
            repairerPos: Vector3.Zero,
            targetPos: new Vector3(10f, 0f, 0f),
            populateActiveTarget: false);

        Assert.NotEqual(Entity.Null, arm);
        var part = world.GetComponent<ArticulatedPartComponent>(arm)!;

        Assert.False(part.HasAimTarget);
        Assert.Equal(0f, part.TargetYaw);
        Assert.Equal(-20f, part.TargetPitch);
    }

    [Fact]
    public void AttachRepairArm_creates_one_repair_arm_child()
    {
        var world = new World();
        Entity repairer = world.CreateEntity();
        var def = SupportRepairDef();

        int first = UtilityArticulationSpawner.AttachRepairArm(world, repairer, def, "terran");
        int second = UtilityArticulationSpawner.AttachRepairArm(world, repairer, def, "terran");

        Assert.Equal(1, first);
        Assert.Equal(0, second);
        Assert.Equal(1, UtilityArticulationSpawner.CountRepairArmsForOwner(world, repairer));

        foreach (var (entity, part) in world.Query<ArticulatedPartComponent>())
        {
            if (part.Owner != repairer || part.PartType != ArticulatedPartType.RepairArm)
                continue;

            var render = world.GetComponent<RenderComponent>(entity);
            Assert.NotNull(render);
            Assert.Equal(UtilityPartMeshes.RepairArmMeshKey("support_repair"), render!.MeshKey);
            Assert.True(render.Visible);
        }
    }

    [Fact]
    public void MiningVisualSystem_tractor_mode_still_tags_beam_visual()
    {
        var rm = new ResourceManager();
        rm.AddPlayer(1);
        var world = new World();
        world.AddSystem(new ResourceSystem(rm));
        world.AddSystem(new MiningVisualSystem());

        Entity node = world.CreateEntity();
        world.AddComponent(node, new ResourceNodeComponent
        {
            ResourceType = ResourceType.Minerals,
            Amount = 500f,
            MaxAmount = 500f,
            HarvestRate = 10f,
        });
        world.AddComponent(node, new TransformComponent { Position = new Vector3(0f, 0f, 50f) });

        Entity collector = world.CreateEntity();
        world.AddComponent(collector, new ResourceCollectorComponent
        {
            PlayerId = 1,
            AssignedNode = node,
            HarvestMode = HarvestMode.TractorBeam,
            HarvestRange = HarvestModeDefaults.DefaultRange(HarvestMode.TractorBeam),
            HarvestRate = 15f,
            CarryCapacity = 100f,
            State = CollectorState.Collecting,
        });
        world.AddComponent(collector, new TransformComponent { Position = Vector3.Zero });

        world.Update(0f);

        Assert.True(world.HasComponent<TractorBeamVisualComponent>(collector));
        var beam = world.GetComponent<TractorBeamVisualComponent>(collector)!;
        Assert.Equal(node, beam.NodeEntity);
    }

    private static Vector3 ComputeNozzleOrigin(
        TransformComponent ownerTf,
        Vector3 localOffset,
        float gimbalYaw,
        float gimbalPitch)
    {
        var nozzle = new ShipEngineNozzleComponent
        {
            LocalOffset = localOffset,
            GimbalYaw = gimbalYaw,
            GimbalPitch = gimbalPitch,
        };
        Matrix4 rot = ShipEngineEmitterSystem.BuildNozzleRotation(ownerTf, nozzle);
        Vector3 scaled = new Vector3(
            localOffset.X * ownerTf.Scale.X,
            localOffset.Y * ownerTf.Scale.Y,
            localOffset.Z * ownerTf.Scale.Z);
        return ownerTf.Position + Vector3.TransformNormal(scaled, rot);
    }
}