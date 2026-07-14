using OpenTK.Mathematics;
using SharpOpenGl.Engine.ECS;
using SharpOpenGl.Engine.Economy;
using SharpOpenGl.Engine.Entities;
using SharpOpenGl.Engine.Grid;
using SharpOpenGl.Engine.Missions;

namespace SharpOpenGl.Engine.Persistence;

/// <summary>
/// Rebuilds ECS gameplay state from a <see cref="SaveData"/> snapshot.
/// </summary>
public static class WorldLoadService
{
    /// <summary>Restore entities, resources, mission progress, and fog from <paramref name="data"/>.</summary>
    public static WorldLoadResult Restore(WorldLoadContext ctx, SaveData data)
    {
        RestoreResources(ctx.ResourceManager, data.PlayerResources);
        RestoreMissionProgress(ctx.MissionState, data);
        RestoreFog(ctx.GridSystem, ctx.FogOfWar, data.FogStates);

        var idMap = new Dictionary<int, Entity>();
        Entity hero = Entity.Null;
        Entity commandCenter = Entity.Null;

        foreach (EntitySaveRecord record in data.Entities)
        {
            Entity entity = SpawnEntity(ctx, record);
            idMap[record.EntityId] = entity;

            if (!string.IsNullOrWhiteSpace(record.Tag) && ctx.MissionState != null)
                ctx.MissionState.RegisterEntityTag(record.Tag, entity);

            if (ctx.World.HasComponent<HeroComponent>(entity))
                hero = entity;

            BuildingComponent? building = ctx.World.GetComponent<BuildingComponent>(entity);
            if (building != null
                && building.BuildingType.Equals("command_center", StringComparison.OrdinalIgnoreCase))
                commandCenter = entity;
        }

        return new WorldLoadResult
        {
            EntityCount = data.Entities.Count,
            HeroEntity = hero,
            CommandCenterEntity = commandCenter,
            EntityIdMap = idMap,
            IsSandboxSession = data.IsSandboxSession,
            ProceduralMapSeed = data.ProceduralMapSeed,
            SandboxSeedText = data.SandboxSeedText ?? string.Empty,
        };
    }

    private static void RestoreResources(ResourceManager resources, List<PlayerResourceRecord> records)
    {
        foreach (var record in records)
        {
            var pool = resources.GetPlayer(record.PlayerId)
                       ?? resources.AddPlayer(record.PlayerId);
            pool.SetStartingAmount(ResourceType.Energy, record.Energy);
            pool.SetStartingAmount(ResourceType.Minerals, record.Minerals);
            pool.SetStartingAmount(ResourceType.Data, record.Data);
            pool.SetStartingAmount(ResourceType.Crew, record.Crew);
        }
    }

    private static void RestoreMissionProgress(MissionState? mission, SaveData data)
    {
        if (mission == null) return;

        mission.ElapsedTime = data.ElapsedMissionTime;

        foreach (string objectiveId in data.CompletedObjectiveIds)
        {
            ObjectiveProgress? objective = mission.FindObjective(objectiveId);
            if (objective != null)
                objective.IsCompleted = true;
        }

        foreach (string triggerId in data.FiredTriggerIds)
        {
            TriggerProgress? trigger = mission.FindTrigger(triggerId);
            if (trigger != null)
                trigger.HasFired = true;
        }

        if (mission.Phase == MissionPhase.Briefing)
            mission.Phase = MissionPhase.InProgress;
    }

    private static void RestoreFog(
        GridSystem grid,
        FogOfWar fog,
        Dictionary<string, int> fogStates)
    {
        int maxPlayers = fog.PlayerCount;
        foreach (var (key, ordinal) in fogStates)
        {
            if (!TryParseFogKey(key, out int keyPlayer, out int x, out int y))
                continue;
            if ((uint)keyPlayer >= (uint)maxPlayers)
                continue;
            if (!Enum.IsDefined(typeof(FogState), ordinal))
                continue;

            GridCell? cell = grid.GetCell(x, y);
            cell?.SetFog(keyPlayer, (FogState)ordinal);
        }
    }

    private static Entity SpawnEntity(WorldLoadContext ctx, EntitySaveRecord record)
    {
        if (WorldSaveService.TryParseResourceNodeTemplate(record.TemplateId, out ResourceType resType))
            return SpawnResourceNode(ctx, record, resType);

        EntityDefinition? def = ctx.ResolveDefinition(record.TemplateId);
        if (def == null)
            return SpawnFallbackEntity(ctx, record);

        bool isEnemy = record.PlayerId > 1
            || (record.PlayerId == 2 && !record.TemplateId.Contains("hero", StringComparison.OrdinalIgnoreCase));
        Entity entity = ctx.UnitFactory.Create(ctx.World, def);

        ApplyTransform(ctx.World, entity, record);
        ApplyHealth(ctx.World, entity, record);
        ApplyStance(ctx.World, entity, record);
        ApplyConstructionState(ctx, entity, def, record);
        ctx.FinalizeUnit?.Invoke(entity, def, record.PlayerId, isEnemy);

        return entity;
    }

    private static Entity SpawnResourceNode(WorldLoadContext ctx, EntitySaveRecord record, ResourceType type)
    {
        Entity entity = ctx.World.CreateEntity();
        ctx.World.AddComponent(entity, new TransformComponent
        {
            Position = new Vector3(record.X, 1f, record.Y),
            Scale = new Vector3(6f, 6f, 6f),
        });
        // Shields stores MaxAmount; Health stores Amount (including 0 for depleted nodes).
        float maxAmount = record.Shields > 0f
            ? record.Shields
            : record.Health > 0f ? record.Health : 5000f;
        float amount = record.Health;
        ctx.World.AddComponent(entity, new ResourceNodeComponent
        {
            ResourceType = type,
            Amount = amount,
            MaxAmount = maxAmount,
            HarvestRate = 10f,
        });
        ctx.World.AddComponent(entity, new SelectionComponent
        {
            IsSelected = false,
            SelectionRadius = 14f,
        });
        ctx.FinalizeUnit?.Invoke(entity, null, 0, false);
        return entity;
    }

    private static Entity SpawnFallbackEntity(WorldLoadContext ctx, EntitySaveRecord record)
    {
        Entity entity = ctx.World.CreateEntity();
        ctx.World.AddComponent(entity, new TransformComponent
        {
            Position = new Vector3(record.X, 0f, record.Y),
        });
        ctx.World.AddComponent(entity, new HealthComponent
        {
            MaxHP = MathF.Max(record.Health, 1f),
            CurrentHP = record.Health,
            CurrentShields = record.Shields,
            MaxShields = MathF.Max(record.Shields, 0f),
        });
        ctx.World.AddComponent(entity, new EntityNameComponent
        {
            DefinitionId = record.TemplateId,
            DisplayName = record.TemplateId.Replace('_', ' '),
        });
        ctx.FinalizeUnit?.Invoke(entity, null, record.PlayerId, record.PlayerId > 1);
        return entity;
    }

    private static void ApplyTransform(World world, Entity entity, EntitySaveRecord record)
    {
        TransformComponent? transform = world.GetComponent<TransformComponent>(entity);
        if (transform == null) return;

        float y = transform.Position.Y;
        transform.Position = new Vector3(record.X, y, record.Y);
    }

    private static void ApplyHealth(World world, Entity entity, EntitySaveRecord record)
    {
        HealthComponent? health = world.GetComponent<HealthComponent>(entity);
        if (health == null) return;

        health.CurrentHP = record.Health;
        health.CurrentShields = record.Shields;
    }

    private static void ApplyConstructionState(
        WorldLoadContext ctx, Entity entity, EntityDefinition? def, EntitySaveRecord record)
    {
        if (def == null || record.ConstructionTotalBuildTime <= 0f)
            return;

        var world = ctx.World;
        var building = world.GetComponent<BuildingComponent>(entity);
        if (building == null)
            return;

        if (record.ConstructionBuildProgress >= record.ConstructionTotalBuildTime)
        {
            CompleteLoadedConstruction(world, entity, def, record.PlayerId);
            return;
        }

        building.ProductionRate = 0f;
        world.RemoveComponent<WeaponListComponent>(entity);
        world.RemoveComponent<CombatTargetComponent>(entity);
        world.RemoveComponent<SightRadiusComponent>(entity);

        world.AddComponent(entity, new UnderConstructionComponent
        {
            DefinitionId = def.Id,
            BuildProgress = record.ConstructionBuildProgress,
            TotalBuildTime = record.ConstructionTotalBuildTime,
            PlayerId = record.PlayerId,
        });
    }

    private static void CompleteLoadedConstruction(
        World world, Entity entity, EntityDefinition def, int playerId)
    {
        var building = world.GetComponent<BuildingComponent>(entity);
        if (building != null)
        {
            var buildingDef = def.Components?.Building;
            building.ProductionRate = buildingDef?.ProductionRate ?? 1f;
        }

        if (def.Components?.Weapons is { Length: > 0 } &&
            world.GetComponent<WeaponListComponent>(entity) == null)
        {
            FactoryHelpers.ApplyWeapons(world, entity, def.Components.Weapons);
            world.AddComponent(entity, new CombatTargetComponent
            {
                Faction = playerId,
                TargetingMode = TargetPriority.Closest,
                Priority = 50,
            });
        }

        if (world.GetComponent<SightRadiusComponent>(entity) == null)
        {
            int sight = def.Components?.SightRadius > 0 ? def.Components.SightRadius : 10;
            world.AddComponent(entity, new SightRadiusComponent { Radius = sight });
        }

        var health = world.GetComponent<HealthComponent>(entity);
        if (health != null)
        {
            var healthDef = def.Components?.Health;
            float maxHp = healthDef?.MaxHP ?? health.MaxHP;
            health.MaxHP = maxHp;
            health.CurrentHP = maxHp;
            health.Armor = healthDef?.Armor ?? health.Armor;
        }

        world.RemoveComponent<UnderConstructionComponent>(entity);
    }

    private static void ApplyStance(World world, Entity entity, EntitySaveRecord record)
    {
        if (string.IsNullOrWhiteSpace(record.Stance))
            return;

        if (!Enum.TryParse(record.Stance, ignoreCase: true, out Stance stance))
            return;

        StanceComponent? stanceComp = world.GetComponent<StanceComponent>(entity);
        if (stanceComp != null)
            stanceComp.CurrentStance = stance;
        else
            world.AddComponent(entity, new StanceComponent { CurrentStance = stance });
    }

    private static bool TryParseFogKey(string key, out int playerId, out int x, out int y)
    {
        playerId = 0;
        x = 0;
        y = 0;

        string[] parts = key.Split(':');
        if (parts.Length != 3)
            return false;

        return int.TryParse(parts[0], out playerId)
            && int.TryParse(parts[1], out x)
            && int.TryParse(parts[2], out y);
    }
}