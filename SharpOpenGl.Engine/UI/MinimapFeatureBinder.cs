using OpenTK.Mathematics;
using SharpOpenGl.Engine.ECS;
using SharpOpenGl.Engine.Economy;
using SharpOpenGl.Engine.Grid;
using SharpOpenGl.Engine.UI.Widgets;

namespace SharpOpenGl.Engine.UI;

/// <summary>Collects economy and planet entities for minimap feature markers.</summary>
public static class MinimapFeatureBinder
{
    public static IReadOnlyList<MinimapFeatureMarker> Collect(
        World world,
        Func<Vector3, FogState> fogStateAt,
        Func<Vector3, Vector2> worldToNorm)
    {
        var markers = new List<MinimapFeatureMarker>();

        foreach (var (entity, node) in world.Query<ResourceNodeComponent>())
        {
            var transform = world.GetComponent<TransformComponent>(entity);
            if (transform == null
                || !MinimapVisibilityPolicy.ShouldShowFeature(fogStateAt(transform.Position)))
                continue;

            var named = world.GetComponent<EntityNameComponent>(entity);
            MinimapFeatureKind kind = named?.DefinitionId == "harvestable_planet"
                ? MinimapFeatureKind.HarvestablePlanet
                : ResourceTypeToKind(node.ResourceType);

            markers.Add(new MinimapFeatureMarker(
                worldToNorm(transform.Position),
                kind,
                Minimap.ColorForFeature(kind)));
        }

        foreach (var (entity, feature) in world.Query<MapFeatureComponent>())
        {
            if (feature.Kind is not (MapFeatureKind.NeutralPlanet or MapFeatureKind.Scenery))
                continue;

            var transform = world.GetComponent<TransformComponent>(entity);
            if (transform == null
                || !MinimapVisibilityPolicy.ShouldShowFeature(fogStateAt(transform.Position)))
                continue;

            MinimapFeatureKind kind = feature.Kind == MapFeatureKind.NeutralPlanet
                ? MinimapFeatureKind.NeutralPlanet
                : Minimap.SceneryFeatureTypeToKind(feature.FeatureType);

            markers.Add(new MinimapFeatureMarker(
                worldToNorm(transform.Position),
                kind,
                Minimap.ColorForFeature(kind)));
        }

        return markers;
    }

    private static MinimapFeatureKind ResourceTypeToKind(ResourceType resourceType) => resourceType switch
    {
        ResourceType.Energy => MinimapFeatureKind.ResourceEnergy,
        ResourceType.Minerals => MinimapFeatureKind.ResourceMinerals,
        ResourceType.Data => MinimapFeatureKind.ResourceData,
        ResourceType.Crew => MinimapFeatureKind.ResourceCrew,
        _ => MinimapFeatureKind.ResourceMinerals,
    };
}