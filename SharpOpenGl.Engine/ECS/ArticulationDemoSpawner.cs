using OpenTK.Mathematics;
using SharpOpenGl.Engine.Config;
using SharpOpenGl.Engine.Entities;
using SharpOpenGl.Engine.Rendering;

namespace SharpOpenGl.Engine.ECS;

/// <summary>
/// Spawns owner + articulated child pairs for iteration demos (idle yaw sweep and Round 2 roster showcase).
/// </summary>
public static class ArticulationDemoSpawner
{
    /// <summary>GPU mesh handles for hull and articulated sub-part rendering.</summary>
    public sealed class MeshHandles
    {
        public required int HullMeshId { get; init; }
        public required int HullVertexCount { get; init; }
        public required int PartMeshId { get; init; }
        public required int PartVertexCount { get; init; }
    }

    private static readonly (string Id, string Label)[] Round2ShowcaseRoster =
    [
        ("dreadnought", "capital"),
        ("bomber_heavy", "special_hull"),
        ("sensor_array", "station"),
        ("miner_basic", "utility"),
    ];

    /// <summary>
    /// Spawns a compact Round 2 articulation roster: capital, special hull bay, station dish, utility arm.
    /// </summary>
    public static IReadOnlyList<(Entity Owner, IReadOnlyList<Entity> Parts)> SpawnRound2Showcase(
        World world,
        Vector3 origin,
        IReadOnlyDictionary<string, MeshHandles>? meshLookup = null)
    {
        var spawned = new List<(Entity Owner, IReadOnlyList<Entity> Parts)>();
        float spacing = 36f;

        for (int i = 0; i < Round2ShowcaseRoster.Length; i++)
        {
            (string id, string _) = Round2ShowcaseRoster[i];
            Vector3 slotOrigin = origin + new Vector3((i - 1.5f) * spacing, 0f, 0f);
            spawned.Add(SpawnShowcaseEntry(world, id, slotOrigin, meshLookup));
        }

        return spawned;
    }

    /// <summary>
    /// Creates an owner hull entity and one child part with idle sweep enabled.
    /// </summary>
    /// <returns>Owner and articulated part entity handles.</returns>
    public static (Entity Owner, Entity Part) Spawn(
        World world,
        Vector3 ownerPosition,
        MeshHandles meshes)
    {
        Vector3 scale = Vector3.One * VisualBalance.ShipScaleMultiplier;

        Entity owner = world.CreateEntity();
        world.AddComponent(owner, new TransformComponent
        {
            Position = ownerPosition,
            Scale = scale,
        });
        world.AddComponent(owner, new RenderComponent
        {
            MeshId = meshes.HullMeshId,
            VertexCount = meshes.HullVertexCount,
            Color = new Vector4(0.45f, 0.75f, 1f, 1f),
            Visible = true,
            PrimitiveType = 4,
        });

        Entity part = world.CreateEntity();
        world.AddComponent(part, new ArticulatedPartComponent
        {
            Owner = owner,
            PartType = ArticulatedPartType.TurretYaw,
            LocalPivotOffset = new Vector3(0f, 0.5f, 0f),
            MeshLocalOffset = Vector3.Zero,
            YawMin = -90f,
            YawMax = 90f,
            PitchMin = -15f,
            PitchMax = 45f,
            IdleSweepEnabled = true,
            IdleSweepSpeed = 30f,
            HasAimTarget = false,
        });
        world.AddComponent(part, new RenderComponent
        {
            MeshId = meshes.PartMeshId,
            VertexCount = meshes.PartVertexCount,
            Color = new Vector4(1f, 0.4f, 0.2f, 1f),
            Visible = true,
            PrimitiveType = 4,
        });

        return (owner, part);
    }

    private static (Entity Owner, IReadOnlyList<Entity> Parts) SpawnShowcaseEntry(
        World world,
        string showcaseId,
        Vector3 position,
        IReadOnlyDictionary<string, MeshHandles>? meshLookup)
    {
        if (TrySpawnFromDefinition(world, showcaseId, position, out Entity owner, out List<Entity> parts))
            return (owner, parts);

        if (meshLookup != null && meshLookup.TryGetValue(showcaseId, out MeshHandles? meshes))
        {
            (Entity meshOwner, Entity meshPart) = Spawn(world, position, meshes);
            return (meshOwner, [meshPart]);
        }

        owner = world.CreateEntity();
        world.AddComponent(owner, new TransformComponent
        {
            Position = position,
            Scale = Vector3.One * VisualBalance.ShipScaleMultiplier,
        });
        world.AddComponent(owner, new EntityNameComponent { DefinitionId = showcaseId });
        return (owner, []);
    }

    private static bool TrySpawnFromDefinition(
        World world,
        string showcaseId,
        Vector3 position,
        out Entity owner,
        out List<Entity> parts)
    {
        parts = [];
        owner = Entity.Null;

        EntityDefinition? def = LoadShowcaseDefinition(showcaseId);
        if (def == null)
            return false;

        if (def.Components?.Building != null)
        {
            owner = new BaseFactory().Create(world, def);
            var buildingTf = world.GetComponent<TransformComponent>(owner);
            if (buildingTf != null)
            {
                buildingTf.Position = position;
                buildingTf.Scale = Vector3.One * (7f * 0.85f);
            }
        }
        else
        {
            owner = new ShipFactory().Create(world, def);
            var shipTf = world.GetComponent<TransformComponent>(owner);
            if (shipTf != null)
            {
                shipTf.Position = position;
                shipTf.Scale = Vector3.One * VisualBalance.ShipScaleMultiplier;
            }
        }

        foreach (var (entity, part) in world.Query<ArticulatedPartComponent>())
        {
            if (ArticulationDrawHelper.ResolveRootOwner(world, part) == owner)
                parts.Add(entity);
        }

        return parts.Count > 0;
    }

    private static EntityDefinition? LoadShowcaseDefinition(string showcaseId)
    {
        string? gameData = ResolveGameDataRoot();
        if (gameData == null)
            return null;

        string subfolder = showcaseId is "sensor_array" or "defense_turret" ? "Bases" : "Ships";
        string path = Path.Combine(gameData, subfolder, $"{showcaseId}.json");
        if (!File.Exists(path))
            return null;

        try
        {
            string json = File.ReadAllText(path);
            return System.Text.Json.JsonSerializer.Deserialize<EntityDefinition>(
                json,
                new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch
        {
            return null;
        }
    }

    private static string? ResolveGameDataRoot()
    {
        string? dir = AppDomain.CurrentDomain.BaseDirectory;
        while (dir != null)
        {
            string candidate = Path.Combine(dir, "GameData");
            if (Directory.Exists(candidate))
                return candidate;
            dir = Directory.GetParent(dir)?.FullName;
        }

        return null;
    }
}