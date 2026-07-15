using SharpOpenGl.Engine.ECS;
using SharpOpenGl.Engine.Economy;
using SharpOpenGl.Engine.Grid;
using SharpOpenGl.Engine.Missions;

namespace SharpOpenGl.Engine.Persistence;

/// <summary>
/// Captures live gameplay state into a <see cref="SaveData"/> snapshot.
/// </summary>
public static class WorldSaveService
{
    private const string ResourceNodePrefix = "resource_node_";

    /// <summary>Build a save snapshot from the current gameplay context.</summary>
    public static SaveData Capture(WorldSaveContext ctx)
    {
        var data = new SaveData
        {
            SlotName = ctx.SlotName,
            MissionId = ctx.IsSandboxSession
                ? string.Empty
                : ctx.MissionState?.Definition.Id ?? string.Empty,
            ElapsedMissionTime = ctx.MissionState?.ElapsedTime ?? 0f,
            CameraX = ctx.CameraX,
            CameraY = ctx.CameraY,
            CameraZoom = ctx.CameraZoom,
            PlayerResources = CaptureResources(ctx.ResourceManager),
            Entities = CaptureEntities(ctx.World, ctx.MissionState),
            CompletedObjectiveIds = CaptureCompletedObjectives(ctx.MissionState),
            FiredTriggerIds = CaptureFiredTriggers(ctx.MissionState),
            FogStates = CaptureFog(ctx.GridSystem, ctx.FogOfWar.PlayerCount),
        };

        if (ctx.IsSandboxSession)
        {
            data.IsSandboxSession = true;
            data.ProceduralMapSeed = ctx.ProceduralMapSeed;
            data.SandboxSeedText = ctx.SandboxSeedText ?? string.Empty;
        }

        return data;
    }

    private static List<PlayerResourceRecord> CaptureResources(ResourceManager resources)
    {
        var records = new List<PlayerResourceRecord>();
        foreach (var (playerId, pool) in resources.AllPlayers())
        {
            records.Add(new PlayerResourceRecord
            {
                PlayerId = playerId,
                Energy = pool.GetAmount(ResourceType.Energy),
                Minerals = pool.GetAmount(ResourceType.Minerals),
                Data = pool.GetAmount(ResourceType.Data),
                Crew = pool.GetAmount(ResourceType.Crew),
            });
        }

        return records;
    }

    private static List<EntitySaveRecord> CaptureEntities(World world, MissionState? mission)
    {
        var tagByEntity = BuildTagLookup(mission);
        var records = new List<EntitySaveRecord>();

        foreach (var (entity, transform) in world.Query<TransformComponent>())
        {
            if (!ShouldSaveEntity(world, entity))
                continue;

            var record = new EntitySaveRecord
            {
                EntityId = (int)entity.Index,
                X = transform.Position.X,
                Y = transform.Position.Z,
            };

            // Resource nodes encode Amount → Health and MaxAmount → Shields.
            // Depleted nodes persist with Health = 0 while Shields retains MaxAmount.
            ResourceNodeComponent? node = world.GetComponent<ResourceNodeComponent>(entity);
            if (node != null)
            {
                record.TemplateId = ResourceNodeTemplate(node.ResourceType);
                record.PlayerId = 0;
                record.Health = node.Amount;
                record.Shields = node.MaxAmount;
            }
            else
            {
                record.TemplateId = ResolveTemplateId(world, entity);
                record.PlayerId = ResolvePlayerId(world, entity);

                HealthComponent? health = world.GetComponent<HealthComponent>(entity);
                if (health != null)
                {
                    record.Health = health.CurrentHP;
                    record.Shields = health.CurrentShields;
                }
            }

            StanceComponent? stance = world.GetComponent<StanceComponent>(entity);
            if (stance != null)
                record.Stance = stance.CurrentStance.ToString();

            if (tagByEntity.TryGetValue(entity, out string? tag))
                record.Tag = tag;

            UnderConstructionComponent? underConstruction =
                world.GetComponent<UnderConstructionComponent>(entity);
            if (underConstruction != null)
            {
                record.ConstructionBuildProgress = underConstruction.BuildProgress;
                record.ConstructionTotalBuildTime = underConstruction.TotalBuildTime;
            }

            records.Add(record);
        }

        return records;
    }

    private static bool ShouldSaveEntity(World world, Entity entity)
    {
        if (world.HasComponent<ProjectileComponent>(entity)) return false;
        if (world.HasComponent<ParticleEmitterComponent>(entity)) return false;
        if (world.HasComponent<MiningVisualComponent>(entity)) return false;

        return world.HasComponent<HealthComponent>(entity)
            || world.HasComponent<BuildingComponent>(entity)
            || world.HasComponent<ResourceNodeComponent>(entity);
    }

    private static Dictionary<Entity, string> BuildTagLookup(MissionState? mission)
    {
        var lookup = new Dictionary<Entity, string>();
        if (mission == null) return lookup;

        foreach (var (tag, entity) in mission.EntityTags)
            lookup[entity] = tag;

        return lookup;
    }

    private static string ResolveTemplateId(World world, Entity entity)
    {
        EntityNameComponent? named = world.GetComponent<EntityNameComponent>(entity);
        if (named != null && !string.IsNullOrWhiteSpace(named.DefinitionId))
            return named.DefinitionId;

        BuildingComponent? building = world.GetComponent<BuildingComponent>(entity);
        if (building != null)
            return building.BuildingType;

        return "unknown";
    }

    private static int ResolvePlayerId(World world, Entity entity)
    {
        BuildingComponent? building = world.GetComponent<BuildingComponent>(entity);
        if (building != null)
            return building.PlayerId;

        AIControlledComponent? ai = world.GetComponent<AIControlledComponent>(entity);
        if (ai != null)
            return ai.PlayerId;

        ResourceCollectorComponent? collector = world.GetComponent<ResourceCollectorComponent>(entity);
        if (collector != null)
            return collector.PlayerId;

        return 1;
    }

    private static List<string> CaptureCompletedObjectives(MissionState? mission)
    {
        if (mission == null) return [];

        return mission.AllObjectives
            .Where(o => o.IsCompleted)
            .Select(o => o.Id)
            .ToList();
    }

    private static List<string> CaptureFiredTriggers(MissionState? mission)
    {
        if (mission == null) return [];

        return mission.Triggers
            .Where(t => t.HasFired)
            .Select(t => t.Definition.Id)
            .ToList();
    }

    private static Dictionary<string, int> CaptureFog(GridSystem grid, int playerCount)
    {
        var fog = new Dictionary<string, int>();
        for (int playerId = 0; playerId < playerCount; playerId++)
        {
            foreach (GridCell cell in grid.AllCells())
            {
                FogState state = cell.GetFog(playerId);
                if (state == FogState.Unexplored)
                    continue;

                fog[FormatFogKey(playerId, cell.X, cell.Y)] = (int)state;
            }
        }

        return fog;
    }

    internal static string FormatFogKey(int playerId, int x, int y) => $"{playerId}:{x}:{y}";

    internal static string ResourceNodeTemplate(ResourceType type) =>
        $"{ResourceNodePrefix}{type.ToString().ToLowerInvariant()}";

    internal static bool TryParseResourceNodeTemplate(string templateId, out ResourceType type)
    {
        type = default;
        if (!templateId.StartsWith(ResourceNodePrefix, StringComparison.OrdinalIgnoreCase))
            return false;

        string suffix = templateId[ResourceNodePrefix.Length..];
        return Enum.TryParse(suffix, ignoreCase: true, out type);
    }
}