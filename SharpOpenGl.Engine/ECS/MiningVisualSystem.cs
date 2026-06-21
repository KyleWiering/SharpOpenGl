using OpenTK.Mathematics;
using SharpOpenGl.Engine.Economy;
using SharpOpenGl.Engine.Rendering;

namespace SharpOpenGl.Engine.ECS;

/// <summary>
/// Spawns and drives mode-specific mining visuals while collectors are in
/// <see cref="CollectorState.Collecting"/>. Drone mode shuttles ore on return;
/// EVA mode places crew on node surfaces; tractor mode tags beam VFX state.
/// </summary>
public sealed class MiningVisualSystem : GameSystem
{
    public const float TractorPulseInterval = 0.65f;
    public const float DroneShuttleDuration = 4f;

    /// <summary>Optional mesh handles for spawned visual entities.</summary>
    public MiningVisualMeshHandles? Meshes { get; set; }

    /// <inheritdoc/>
    public override void Update(World world, float deltaTime)
    {
        foreach (var (collectorEntity, collector) in world.Query<ResourceCollectorComponent>())
        {
            if (collector.State == CollectorState.Collecting)
                UpdateCollecting(world, collectorEntity, collector, deltaTime);
            else
                CleanupCollectorVisuals(world, collectorEntity);
        }

        PruneOrphanedVisuals(world);
    }

    private void UpdateCollecting(
        World world, Entity collectorEntity, ResourceCollectorComponent collector, float deltaTime)
    {
        if (!world.IsAlive(collector.AssignedNode))
        {
            CleanupCollectorVisuals(world, collectorEntity);
            return;
        }

        var state = GetOrCreateState(world, collectorEntity);

        switch (collector.HarvestMode)
        {
            case HarvestMode.Drones:
                UpdateDroneMode(world, collectorEntity, collector, state, deltaTime);
                world.RemoveComponent<TractorBeamVisualComponent>(collectorEntity);
                break;

            case HarvestMode.Eva:
                UpdateEvaMode(world, collectorEntity, collector, state, deltaTime);
                world.RemoveComponent<TractorBeamVisualComponent>(collectorEntity);
                break;

            case HarvestMode.TractorBeam:
                UpdateTractorMode(world, collectorEntity, collector, state, deltaTime);
                break;
        }
    }

    private void UpdateDroneMode(
        World world,
        Entity collectorEntity,
        ResourceCollectorComponent collector,
        MiningVisualStateComponent state,
        float deltaTime)
    {
        int desiredDrones = Math.Clamp((int)(collector.HarvestRate / 8f) + 1, 1, 3);
        EnsureDroneCount(world, collectorEntity, collector, state, desiredDrones);

        foreach (Entity visual in state.SpawnedVisuals.ToArray())
        {
            if (!world.IsAlive(visual)) continue;
            var drone = world.GetComponent<MiningDroneComponent>(visual);
            if (drone == null) continue;

            drone.ShuttlePhase += drone.ShuttleSpeed * deltaTime;
            while (drone.ShuttlePhase >= 1f)
            {
                drone.ShuttlePhase -= 1f;
                if (!drone.ReturningToMiner)
                {
                    LoadDroneCargo(world, collector, drone);
                    drone.ReturningToMiner = true;
                }
                else
                {
                    DepositDroneCargo(world, collector, drone);
                    drone.ReturningToMiner = false;
                }
            }

            UpdateDroneTransform(world, visual, drone);
        }
    }

    private static void LoadDroneCargo(
        World world, ResourceCollectorComponent collector, MiningDroneComponent drone)
    {
        if (!world.IsAlive(drone.NodeEntity)) return;

        var node = world.GetComponent<ResourceNodeComponent>(drone.NodeEntity);
        if (node == null || node.IsDepleted) return;

        float payload = collector.HarvestRate * (DroneShuttleDuration * 0.5f);
        float space = collector.CarryCapacity - collector.CarryAmount;
        float actual = Math.Min(payload, Math.Min(node.Amount, space));
        if (actual <= 0f) return;

        node.Amount -= actual;
        drone.CargoPayload = actual;
        collector.CarryType ??= node.ResourceType;

        if (node.IsDepleted)
        {
            node.AssignedCollectors = 0;
            if (node.RespawnTime > 0f)
                node.RespawnCountdown = node.RespawnTime;
        }
    }

    private static void DepositDroneCargo(
        World world, ResourceCollectorComponent collector, MiningDroneComponent drone)
    {
        if (drone.CargoPayload <= 0f) return;

        collector.CarryAmount += drone.CargoPayload;
        drone.CargoPayload = 0f;

        if (collector.IsFull && world.IsAlive(drone.NodeEntity))
        {
            var node = world.GetComponent<ResourceNodeComponent>(drone.NodeEntity);
            if (node != null && !node.IsDepleted)
                node.AssignedCollectors = Math.Max(0, node.AssignedCollectors - 1);
            collector.State = CollectorState.Returning;
        }
    }

    private static void UpdateDroneTransform(World world, Entity droneEntity, MiningDroneComponent drone)
    {
        var transform = world.GetComponent<TransformComponent>(droneEntity);
        var minerTf = world.GetComponent<TransformComponent>(drone.CollectorEntity);
        var nodeTf = world.GetComponent<TransformComponent>(drone.NodeEntity);
        if (transform == null || minerTf == null || nodeTf == null) return;

        Vector3 from = drone.ReturningToMiner ? nodeTf.Position : minerTf.Position;
        Vector3 to = drone.ReturningToMiner ? minerTf.Position : nodeTf.Position;
        float t = drone.ShuttlePhase;
        Vector3 pos = Vector3.Lerp(from, to, t);
        pos.Y = MathF.Max(from.Y, to.Y) + 2f + MathF.Sin(t * MathF.PI) * 1.5f;
        transform.Position = pos;
    }

    private void EnsureDroneCount(
        World world,
        Entity collectorEntity,
        ResourceCollectorComponent collector,
        MiningVisualStateComponent state,
        int count)
    {
        int existing = state.SpawnedVisuals.Count(e =>
            world.IsAlive(e) && world.HasComponent<MiningDroneComponent>(e));

        for (int i = existing; i < count; i++)
            SpawnDrone(world, collectorEntity, collector, state, i);
    }

    private void SpawnDrone(
        World world,
        Entity collectorEntity,
        ResourceCollectorComponent collector,
        MiningVisualStateComponent state,
        int slot)
    {
        Entity drone = world.CreateEntity();
        world.AddComponent(drone, new MiningVisualComponent
        {
            CollectorEntity = collectorEntity,
            Kind = MiningVisualKind.Drone,
            SlotIndex = slot,
        });
        world.AddComponent(drone, new MiningDroneComponent
        {
            CollectorEntity = collectorEntity,
            NodeEntity = collector.AssignedNode,
            ShuttleSpeed = 1f / DroneShuttleDuration,
        });
        world.AddComponent(drone, new TransformComponent { Position = Vector3.Zero });

        if (Meshes != null)
        {
            world.AddComponent(drone, new RenderComponent
            {
                MeshId = Meshes.DroneMeshId,
                VertexCount = Meshes.DroneVertexCount,
                Color = new Vector4(0.85f, 0.75f, 0.2f, 1f),
                Visible = true,
                PrimitiveType = 4,
            });
        }

        state.SpawnedVisuals.Add(drone);
    }

    private void UpdateEvaMode(
        World world,
        Entity collectorEntity,
        ResourceCollectorComponent collector,
        MiningVisualStateComponent state,
        float deltaTime)
    {
        if (!IsEvaEligibleNode(world, collector.AssignedNode))
        {
            CleanupCollectorVisuals(world, collectorEntity);
            return;
        }

        int desiredCrew = Math.Clamp((int)(collector.HarvestRate / 10f) + 1, 1, 2);
        int existing = state.SpawnedVisuals.Count(e =>
            world.IsAlive(e) &&
            world.GetComponent<MiningVisualComponent>(e)?.Kind == MiningVisualKind.EvaCrew);

        for (int i = existing; i < desiredCrew; i++)
            SpawnEvaCrew(world, collectorEntity, collector, state, i);

        state.EvaAnimPhase += deltaTime * 3f;
        var nodeTf = world.GetComponent<TransformComponent>(collector.AssignedNode);
        if (nodeTf == null) return;

        foreach (Entity visual in state.SpawnedVisuals)
        {
            if (!world.IsAlive(visual)) continue;
            var kind = world.GetComponent<MiningVisualComponent>(visual);
            if (kind?.Kind != MiningVisualKind.EvaCrew) continue;

            var transform = world.GetComponent<TransformComponent>(visual);
            if (transform == null) continue;

            float angle = kind.SlotIndex * MathF.PI + state.EvaAnimPhase * 0.5f;
            float radius = 6f + kind.SlotIndex * 1.5f;
            transform.Position = nodeTf.Position + new Vector3(
                MathF.Cos(angle) * radius,
                1.2f + MathF.Sin(state.EvaAnimPhase + kind.SlotIndex) * 0.15f,
                MathF.Sin(angle) * radius);
            transform.EulerAngles = transform.EulerAngles with { Y = angle * (180f / MathF.PI) };

            EnsureEvaParticles(world, visual, transform.Position, state.EvaAnimPhase);
        }
    }

    private void SpawnEvaCrew(
        World world,
        Entity collectorEntity,
        ResourceCollectorComponent collector,
        MiningVisualStateComponent state,
        int slot)
    {
        Entity crew = world.CreateEntity();
        world.AddComponent(crew, new MiningVisualComponent
        {
            CollectorEntity = collectorEntity,
            Kind = MiningVisualKind.EvaCrew,
            SlotIndex = slot,
        });
        world.AddComponent(crew, new TransformComponent { Position = Vector3.Zero });

        if (Meshes != null)
        {
            world.AddComponent(crew, new RenderComponent
            {
                MeshId = Meshes.EvaMeshId,
                VertexCount = Meshes.EvaVertexCount,
                Color = new Vector4(0.9f, 0.92f, 0.95f, 1f),
                Visible = true,
                PrimitiveType = 4,
            });
        }

        state.SpawnedVisuals.Add(crew);
    }

    private static void EnsureEvaParticles(
        World world, Entity crewEntity, Vector3 position, float animPhase)
    {
        if (MathF.Sin(animPhase * 2f) < 0.3f) return;

        if (!world.HasComponent<ParticleEmitterComponent>(crewEntity))
        {
            world.AddComponent(crewEntity, new ParticleEmitterComponent
            {
                Emitter = ParticleEffects.CreateImpactBurst(position, 0.35f),
            });
        }

        var emitter = world.GetComponent<ParticleEmitterComponent>(crewEntity)!;
        emitter.Emitter.Origin = position;
    }

    private static void UpdateTractorMode(
        World world,
        Entity collectorEntity,
        ResourceCollectorComponent collector,
        MiningVisualStateComponent state,
        float deltaTime)
    {
        CleanupCollectorVisuals(world, collectorEntity, keepState: true);

        if (!world.HasComponent<TractorBeamVisualComponent>(collectorEntity))
        {
            world.AddComponent(collectorEntity, new TractorBeamVisualComponent
            {
                NodeEntity = collector.AssignedNode,
            });
        }

        var beam = world.GetComponent<TractorBeamVisualComponent>(collectorEntity)!;
        beam.NodeEntity = collector.AssignedNode;
        beam.PulsePhase += deltaTime * 4f;
        collector.TractorPulseTimer += deltaTime;
    }

    private static bool IsEvaEligibleNode(World world, Entity nodeEntity)
    {
        var named = world.GetComponent<EntityNameComponent>(nodeEntity);
        if (named?.DefinitionId == "harvestable_planet")
            return true;

        // Standard map resource nodes (asteroid-style deposits).
        return world.HasComponent<ResourceNodeComponent>(nodeEntity);
    }

    private static MiningVisualStateComponent GetOrCreateState(World world, Entity collectorEntity)
    {
        var state = world.GetComponent<MiningVisualStateComponent>(collectorEntity);
        if (state != null) return state;

        state = new MiningVisualStateComponent();
        world.AddComponent(collectorEntity, state);
        return state;
    }

    private static void CleanupCollectorVisuals(World world, Entity collectorEntity, bool keepState = false)
    {
        var state = world.GetComponent<MiningVisualStateComponent>(collectorEntity);
        if (state != null)
        {
            foreach (Entity visual in state.SpawnedVisuals)
            {
                if (world.IsAlive(visual))
                    world.DestroyEntity(visual);
            }
            state.SpawnedVisuals.Clear();
            if (!keepState)
                world.RemoveComponent<MiningVisualStateComponent>(collectorEntity);
        }

        world.RemoveComponent<TractorBeamVisualComponent>(collectorEntity);
    }

    private static void PruneOrphanedVisuals(World world)
    {
        foreach (var (entity, visual) in world.Query<MiningVisualComponent>())
        {
            if (!world.IsAlive(visual.CollectorEntity))
                world.DestroyEntity(entity);
            else
            {
                var collector = world.GetComponent<ResourceCollectorComponent>(visual.CollectorEntity);
                if (collector == null || collector.State != CollectorState.Collecting)
                    world.DestroyEntity(entity);
            }
        }
    }
}