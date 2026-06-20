using OpenTK.Mathematics;
using SharpOpenGl.Engine.ECS;
using SharpOpenGl.Engine.Economy;
using SharpOpenGl.Engine.UI;

namespace SharpOpenGl.Engine.Grid;

/// <summary>Spawns resource nodes, neutral planets, and scenery from <see cref="MapDefinition"/>.</summary>
public static class MapFeatureSpawner
{
    public sealed class MeshHandles
    {
        public int PlanetMeshId { get; init; }
        public int PlanetVertCount { get; init; }
        public int SceneryMeshId { get; init; }
        public int SceneryVertCount { get; init; }
        public int ResourceNodeMeshId { get; init; }
        public int ResourceNodeVertCount { get; init; }
        public int PrimitiveTriangles { get; init; } = 4;
    }

    public static void SpawnAll(
        World world,
        MapDefinition map,
        MeshHandles meshes,
        Action<Vector3, int>? revealArea = null)
    {
        if (map.ResourceNodes.Length > 0)
        {
            foreach (var node in map.ResourceNodes)
                SpawnResourceNode(world, node, meshes, revealArea);
        }

        foreach (var feature in map.MapFeatures)
            SpawnFeature(world, feature, meshes, revealArea);
    }

    public static void SpawnResourceNode(
        World world,
        MapResourceNode node,
        MeshHandles meshes,
        Action<Vector3, int>? revealArea = null)
    {
        if (node.Position.Length < 2) return;

        Vector3 pos = MapCoordinates.GridToWorld(node.Position[0], node.Position[1]);
        var resType = ParseResourceType(node.Type);

        var entity = world.CreateEntity();
        world.AddComponent(entity, new TransformComponent
        {
            Position = pos with { Y = 1f },
            Scale = new Vector3(6f, 6f, 6f),
        });
        world.AddComponent(entity, new RenderComponent
        {
            MeshId = meshes.ResourceNodeMeshId,
            VertexCount = meshes.ResourceNodeVertCount,
            Color = GameplayEntityDisplay.HarvestableColor,
            Visible = true,
            PrimitiveType = meshes.PrimitiveTriangles,
        });
        world.AddComponent(entity, new ResourceNodeComponent
        {
            ResourceType = resType,
            Amount = node.Amount,
            MaxAmount = node.Amount,
            HarvestRate = 10f,
        });
        world.AddComponent(entity, new SelectionComponent { IsSelected = false, SelectionRadius = 14f });
        world.AddComponent(entity, new EntityNameComponent
        {
            DisplayName = $"{resType} Deposit",
            DefinitionId = "resource_node",
        });
        revealArea?.Invoke(pos, 6);
    }

    public static void SpawnFeature(
        World world,
        MapFeatureDefinition feature,
        MeshHandles meshes,
        Action<Vector3, int>? revealArea = null)
    {
        if (feature.Position.Length < 2) return;

        Vector3 pos = MapCoordinates.GridToWorld(feature.Position[0], feature.Position[1]);
        string kind = feature.Kind.ToLowerInvariant();

        if (kind == "harvestable_planet")
        {
            SpawnHarvestablePlanet(world, feature, pos, meshes, revealArea);
            return;
        }

        if (kind == "neutral_planet")
        {
            SpawnNeutralPlanet(world, feature, pos, meshes, revealArea);
            return;
        }

        SpawnScenery(world, feature, pos, meshes, revealArea);
    }

    private static void SpawnHarvestablePlanet(
        World world,
        MapFeatureDefinition feature,
        Vector3 pos,
        MeshHandles meshes,
        Action<Vector3, int>? revealArea)
    {
        float scale = MathF.Max(4f, feature.Scale);
        var resType = ParseResourceType(feature.ResourceType ?? "minerals");
        string name = string.IsNullOrWhiteSpace(feature.Name) ? "Harvest Moon" : feature.Name;

        var entity = world.CreateEntity();
        world.AddComponent(entity, new TransformComponent
        {
            Position = pos with { Y = scale * 0.35f },
            Scale = Vector3.One * (scale / 8f),
        });
        world.AddComponent(entity, new RenderComponent
        {
            MeshId = meshes.PlanetMeshId,
            VertexCount = meshes.PlanetVertCount,
            Color = GameplayEntityDisplay.HarvestableColor,
            Visible = true,
            PrimitiveType = meshes.PrimitiveTriangles,
        });
        world.AddComponent(entity, new ResourceNodeComponent
        {
            ResourceType = resType,
            Amount = feature.Amount,
            MaxAmount = feature.Amount,
            HarvestRate = 12f,
        });
        world.AddComponent(entity, new SelectionComponent { IsSelected = false, SelectionRadius = scale * 0.9f });
        world.AddComponent(entity, new EntityNameComponent { DisplayName = name, DefinitionId = "harvestable_planet" });
        revealArea?.Invoke(pos, Math.Max(8, (int)(scale * 0.5f)));
    }

    private static void SpawnNeutralPlanet(
        World world,
        MapFeatureDefinition feature,
        Vector3 pos,
        MeshHandles meshes,
        Action<Vector3, int>? revealArea)
    {
        float scale = MathF.Max(6f, feature.Scale);
        string name = string.IsNullOrWhiteSpace(feature.Name) ? "Neutral World" : feature.Name;
        string subtitle = feature.Subtitle ?? "Neutral — no faction allegiance";

        var entity = world.CreateEntity();
        world.AddComponent(entity, new TransformComponent
        {
            Position = pos with { Y = scale * 0.35f },
            Scale = Vector3.One * (scale / 8f),
        });
        world.AddComponent(entity, new RenderComponent
        {
            MeshId = meshes.PlanetMeshId,
            VertexCount = meshes.PlanetVertCount,
            Color = GameplayEntityDisplay.NeutralColor,
            Visible = true,
            PrimitiveType = meshes.PrimitiveTriangles,
        });
        world.AddComponent(entity, new MapFeatureComponent
        {
            Kind = MapFeatureKind.NeutralPlanet,
            FeatureType = "planet",
            Subtitle = subtitle,
        });
        world.AddComponent(entity, new CombatTargetComponent { Faction = 0, Priority = 1 });
        world.AddComponent(entity, new SelectionComponent { IsSelected = false, SelectionRadius = scale });
        world.AddComponent(entity, new EntityNameComponent { DisplayName = name, DefinitionId = "neutral_planet" });
        world.AddComponent(entity, new SightRadiusComponent { Radius = Math.Max(6, (int)(scale * 0.4f)) });
        revealArea?.Invoke(pos, Math.Max(10, (int)(scale * 0.6f)));
    }

    private static void SpawnScenery(
        World world,
        MapFeatureDefinition feature,
        Vector3 pos,
        MeshHandles meshes,
        Action<Vector3, int>? revealArea)
    {
        float scale = MathF.Max(3f, feature.Scale);
        string featureType = feature.FeatureType ?? "scenery";
        string name = string.IsNullOrWhiteSpace(feature.Name) ? FormatSceneryName(featureType) : feature.Name;

        var entity = world.CreateEntity();
        world.AddComponent(entity, new TransformComponent
        {
            Position = pos with { Y = scale * 0.2f },
            Scale = Vector3.One * (scale / 6f),
        });
        world.AddComponent(entity, new RenderComponent
        {
            MeshId = meshes.SceneryMeshId,
            VertexCount = meshes.SceneryVertCount,
            Color = new Vector4(0.55f, 0.58f, 0.62f, 1f),
            Visible = true,
            PrimitiveType = meshes.PrimitiveTriangles,
        });
        world.AddComponent(entity, new MapFeatureComponent
        {
            Kind = MapFeatureKind.Scenery,
            FeatureType = featureType,
            Subtitle = feature.Subtitle ?? "Scenery — inspect only",
        });
        world.AddComponent(entity, new SelectionComponent { IsSelected = false, SelectionRadius = scale * 0.85f });
        world.AddComponent(entity, new EntityNameComponent { DisplayName = name, DefinitionId = featureType });
        revealArea?.Invoke(pos, Math.Max(5, (int)(scale * 0.4f)));
    }

    public static ResourceType ParseResourceType(string type) => type.ToLowerInvariant() switch
    {
        "minerals" or "mineral" => ResourceType.Minerals,
        "data" => ResourceType.Data,
        "crew" => ResourceType.Crew,
        _ => ResourceType.Energy,
    };

    public static string FormatSceneryName(string featureType) => featureType switch
    {
        "asteroid_field" => "Asteroid Field",
        "nebula" => "Nebula",
        "debris" => "Debris Field",
        _ => featureType.Replace('_', ' '),
    };
}