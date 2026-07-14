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
        public int AsteroidFieldMeshId { get; init; }
        public int AsteroidFieldVertCount { get; init; }
        public int NebulaMeshId { get; init; }
        public int NebulaVertCount { get; init; }
        public int SceneryMeshId { get; init; }
        public int SceneryVertCount { get; init; }
        public int IonStormMeshId { get; init; }
        public int IonStormVertCount { get; init; }
        public int WormholeRemnantMeshId { get; init; }
        public int WormholeRemnantVertCount { get; init; }
        public int ResourceNodeMeshId { get; init; }
        public int ResourceNodeVertCount { get; init; }
        public int PrimitiveTriangles { get; init; } = 4;
    }

    public readonly record struct SceneryAppearance(int MeshId, int VertexCount, Vector4 Color);

    public static readonly Vector4 AsteroidFieldTint = new(0f, 0f, 0f, 0f);
    public static readonly Vector4 NebulaTint = new(0f, 0f, 0f, 0f);
    public static readonly Vector4 DebrisTint = new(0.68f, 0.62f, 0.55f, 1f);
    public static readonly Vector4 IonStormTint = new(0.55f, 0.28f, 0.95f, 1f);
    public static readonly Vector4 WormholeRemnantTint = new(0.22f, 0.82f, 1f, 1f);
    public static readonly Vector4 DefaultSceneryTint = new(0.55f, 0.58f, 0.62f, 1f);

    /// <summary>
    /// Spawn economy entities for a procedural sandbox chunk using global cell placement.
    /// Reuses <see cref="SpawnResourceNode"/> / <see cref="SpawnFeature"/> — no duplicate spawn logic.
    /// </summary>
    public static void SpawnChunkEconomy(
        World world,
        MapDefinition chunkMap,
        int chunkX,
        int chunkY,
        float cellSize,
        Vector2 gridWorldOrigin,
        MeshHandles meshes,
        Action<Vector3, int>? revealArea = null)
    {
        int baseGx = chunkX * SandboxChunkCoords.ChunkCells;
        int baseGy = chunkY * SandboxChunkCoords.ChunkCells;

        foreach (var node in chunkMap.ResourceNodes)
        {
            if (node.Position.Length < 2) continue;
            Vector3 pos = SandboxChunkCoords.GlobalCellToWorld(
                baseGx + node.Position[0], baseGy + node.Position[1], cellSize, gridWorldOrigin);
            SpawnResourceNode(world, node, meshes, pos, revealArea);
        }

        foreach (var feature in chunkMap.MapFeatures)
        {
            if (feature.Position.Length < 2) continue;
            Vector3 pos = SandboxChunkCoords.GlobalCellToWorld(
                baseGx + feature.Position[0], baseGy + feature.Position[1], cellSize, gridWorldOrigin);
            SpawnFeature(world, feature, meshes, pos, revealArea);
        }
    }

    public static void SpawnAll(
        World world,
        MapDefinition map,
        MeshHandles meshes,
        Action<Vector3, int>? revealArea = null)
    {
        int gridExtent = MapCoordinates.ResolveGridExtent(map);
        float cellSize = MapCoordinates.ResolveCellSize(map);

        if (map.ResourceNodes.Length > 0)
        {
            foreach (var node in map.ResourceNodes)
                SpawnResourceNode(world, node, meshes, gridExtent, cellSize, revealArea);
        }

        foreach (var feature in map.MapFeatures)
            SpawnFeature(world, feature, meshes, gridExtent, cellSize, revealArea);
    }

    public static void SpawnResourceNode(
        World world,
        MapResourceNode node,
        MeshHandles meshes,
        int gridExtent,
        float cellSize,
        Action<Vector3, int>? revealArea = null)
    {
        if (node.Position.Length < 2) return;

        Vector3 pos = MapCoordinates.GridToWorld(node.Position[0], node.Position[1], gridExtent, cellSize);
        SpawnResourceNode(world, node, meshes, pos, revealArea);
    }

    public static void SpawnResourceNode(
        World world,
        MapResourceNode node,
        MeshHandles meshes,
        Vector3 pos,
        Action<Vector3, int>? revealArea = null)
    {
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
        int gridExtent,
        float cellSize,
        Action<Vector3, int>? revealArea = null)
    {
        if (feature.Position.Length < 2) return;

        Vector3 pos = MapCoordinates.GridToWorld(feature.Position[0], feature.Position[1], gridExtent, cellSize);
        SpawnFeature(world, feature, meshes, pos, revealArea);
    }

    public static void SpawnFeature(
        World world,
        MapFeatureDefinition feature,
        MeshHandles meshes,
        Vector3 pos,
        Action<Vector3, int>? revealArea = null)
    {
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
        string featureType = feature.FeatureType ?? "scenery";
        float scale = MathF.Max(DefaultSceneryScale(featureType), feature.Scale);
        string name = string.IsNullOrWhiteSpace(feature.Name) ? FormatSceneryName(featureType) : feature.Name;

        var entity = world.CreateEntity();
        world.AddComponent(entity, new TransformComponent
        {
            Position = pos with { Y = scale * 0.2f },
            Scale = Vector3.One * (scale / 6f),
        });
        var appearance = ResolveSceneryAppearance(featureType, meshes);
        world.AddComponent(entity, new RenderComponent
        {
            MeshId = appearance.MeshId,
            VertexCount = appearance.VertexCount,
            Color = appearance.Color,
            Visible = true,
            PrimitiveType = meshes.PrimitiveTriangles,
        });
        world.AddComponent(entity, new MapFeatureComponent
        {
            Kind = MapFeatureKind.Scenery,
            FeatureType = featureType,
            Subtitle = feature.Subtitle ?? DefaultScenerySubtitle(featureType),
        });
        world.AddComponent(entity, new SelectionComponent
        {
            IsSelected = false,
            SelectionRadius = DefaultScenerySelectionRadius(featureType, scale),
        });
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
        "ion_storm" => "Ion Storm",
        "wormhole_remnant" => "Wormhole Remnant",
        _ => featureType.Replace('_', ' '),
    };

    public static float DefaultSceneryScale(string featureType) => featureType.ToLowerInvariant() switch
    {
        "asteroid_field" => 8f,
        "nebula" => 12f,
        "debris" => 7f,
        "ion_storm" => 10f,
        "wormhole_remnant" => 9f,
        _ => 6f,
    };

    public static float DefaultScenerySelectionRadius(string featureType, float scale) =>
        featureType.ToLowerInvariant() switch
        {
            "asteroid_field" => scale * 0.95f,
            "nebula" => scale * 1.05f,
            "debris" => scale * 0.8f,
            "ion_storm" => scale * 1.1f,
            "wormhole_remnant" => scale * 1.0f,
            _ => scale * 0.85f,
        };

    public static string DefaultScenerySubtitle(string featureType) => featureType.ToLowerInvariant() switch
    {
        "asteroid_field" => "Dense rock belt — blocks line of sight",
        "nebula" => "Sensor interference — reduced vision",
        "debris" => "Salvage drift — light cover",
        "ion_storm" => "Electromagnetic storm — hazardous transit",
        "wormhole_remnant" => "Collapsed gate remnant — inspect only",
        _ => "Scenery — inspect only",
    };

    public static SceneryAppearance ResolveSceneryAppearance(string featureType, MeshHandles meshes)
    {
        string normalized = featureType.ToLowerInvariant();
        return normalized switch
        {
            "asteroid_field" => new(
                meshes.AsteroidFieldMeshId,
                meshes.AsteroidFieldVertCount,
                AsteroidFieldTint),
            "nebula" => new(
                meshes.NebulaMeshId,
                meshes.NebulaVertCount,
                NebulaTint),
            "debris" => new(
                meshes.SceneryMeshId,
                meshes.SceneryVertCount,
                DebrisTint),
            "ion_storm" => new(
                meshes.IonStormMeshId,
                meshes.IonStormVertCount,
                IonStormTint),
            "wormhole_remnant" => new(
                meshes.WormholeRemnantMeshId,
                meshes.WormholeRemnantVertCount,
                WormholeRemnantTint),
            _ => new(
                meshes.SceneryMeshId,
                meshes.SceneryVertCount,
                DefaultSceneryTint),
        };
    }
}