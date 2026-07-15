using OpenTK.Mathematics;
using SharpOpenGl.Engine.Config;
using SharpOpenGl.Engine.Entities;
using SharpOpenGl.Engine.Rendering;

namespace SharpOpenGl.Engine.ECS;

/// <summary>
/// Manages transient hull preview entities at shipyard exit pads while production is active.
/// Runs after <see cref="BuildSystem"/> so completed spawns tear down previews immediately.
/// </summary>
public sealed class ShipyardPreviewSystem : GameSystem
{
    private const float ExitOffsetDistance = 35f;
    private const float MeshRefreshThreshold = 0.02f;

    private static readonly HashSet<string> ShipyardTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "shipyard",
        "shipyard_small",
        "shipyard_medium",
        "shipyard_large",
    };

    private readonly Func<string, EntityDefinition?> _definitionLoader;
    private readonly Dictionary<Entity, PreviewMeshState> _meshState = new();

    /// <summary>
    /// Optional GPU upload hook (desktop/browser host wires <c>MeshBuilder.UploadProcedural</c>).
    /// When null, preview mesh data is still built and cached on CPU but <see cref="RenderComponent.MeshId"/> stays unset.
    /// </summary>
    public Func<float[], int, (int MeshId, int VertexCount)>? MeshUploader { get; set; }

    /// <summary>Maps building owner <see cref="BuildingComponent.PlayerId"/> to faction race id.</summary>
    public Func<int, string>? ResolveFactionRaceId { get; set; }

    public ShipyardPreviewSystem(Func<string, EntityDefinition?> definitionLoader)
    {
        _definitionLoader = definitionLoader;
    }

    /// <inheritdoc/>
    public override void Update(World world, float deltaTime)
    {
        foreach (var (buildingEntity, building) in world.Query<BuildingComponent>())
        {
            if (!IsShipyard(building.BuildingType))
                continue;

            UpdateShipyard(world, buildingEntity, building);
        }

        PruneOrphanedPreviews(world);
    }

    private void UpdateShipyard(World world, Entity buildingEntity, BuildingComponent building)
    {
        var preview = GetOrCreatePreviewComponent(world, buildingEntity);

        if (building.BuildQueue.Count == 0 || building.BuildProgress <= 0f)
        {
            ClearPreview(world, buildingEntity, preview);
            return;
        }

        string queuedId = building.BuildQueue.Peek();
        var def = _definitionLoader(queuedId);
        if (def == null || def.BuildTime <= 0f)
        {
            ClearPreview(world, buildingEntity, preview);
            return;
        }

        string raceId = ResolveRaceId(world, buildingEntity, building);
        bool queueChanged = !string.Equals(preview.QueuedDefinitionId, queuedId, StringComparison.Ordinal);
        bool previewMissing = preview.PreviewEntity == Entity.Null || !world.IsAlive(preview.PreviewEntity);
        if (queueChanged || previewMissing)
        {
            if (queueChanged && !string.IsNullOrEmpty(preview.QueuedDefinitionId))
                ShipyardBuildPreviewMeshes.InvalidateRaceHull(raceId, preview.QueuedDefinitionId);

            DestroyPreviewEntity(world, preview);
            preview.PreviewEntity = CreatePreviewEntity(world, buildingEntity, building, def, raceId);
            preview.QueuedDefinitionId = queuedId;
            _meshState.Remove(buildingEntity);
        }

        UpdatePreviewTransform(world, buildingEntity, building, preview.PreviewEntity);

        preview.BuildFraction = Math.Clamp(building.BuildProgress / def.BuildTime, 0f, 1f);
        preview.IsActive = true;

        UpdatePreviewMesh(world, preview.PreviewEntity, buildingEntity, building, def, raceId, preview.BuildFraction);

        var render = world.GetComponent<RenderComponent>(preview.PreviewEntity);
        if (render != null)
            render.Visible = true;
    }

    private void UpdatePreviewMesh(
        World world,
        Entity previewEntity,
        Entity buildingEntity,
        BuildingComponent building,
        EntityDefinition def,
        string raceId,
        float buildFraction)
    {
        var render = world.GetComponent<RenderComponent>(previewEntity);
        if (render == null)
            return;

        float quantized = ShipyardBuildPreviewMeshes.QuantizeFraction(buildFraction);
        if (_meshState.TryGetValue(buildingEntity, out PreviewMeshState state)
            && state.RaceId == raceId
            && state.HullKey == def.Id
            && MathF.Abs(state.Fraction - quantized) < MeshRefreshThreshold)
        {
            return;
        }

        string cacheKey = ShipyardBuildPreviewMeshes.BuildCacheKey(raceId, def.Id, quantized);
        float[] vertices = ShipyardBuildPreviewMeshes.BuildPartial(raceId, def.Id, quantized);
        int primitiveType = ShipyardBuildPreviewMeshes.ResolvePrimitiveType(quantized);

        render.PrimitiveType = primitiveType;
        render.MeshKey = cacheKey;
        render.VertexCount = ProceduralMeshes.VertexCount(vertices);

        if (ShipyardBuildPreviewMeshes.TryResolveGpuMesh(cacheKey, vertices, primitiveType, MeshUploader,
                out int meshId, out int vertexCount))
        {
            render.MeshId = meshId;
            render.VertexCount = vertexCount;
        }

        TeamVisualResolver.ApplyRaceTexturing(render, raceId, building.PlayerId);

        _meshState[buildingEntity] = new PreviewMeshState(raceId, def.Id, quantized);
    }

    private string ResolveRaceId(World world, Entity buildingEntity, BuildingComponent building)
    {
        var raceComponent = world.GetComponent<RaceComponent>(buildingEntity);
        if (raceComponent != null && !string.IsNullOrWhiteSpace(raceComponent.RaceId))
            return raceComponent.RaceId;

        if (ResolveFactionRaceId != null)
            return ResolveFactionRaceId(building.PlayerId);

        return RaceShipMeshes.DefaultRace;
    }

    private static bool IsShipyard(string buildingType) =>
        ShipyardTypes.Contains(buildingType);

    private static ShipyardPreviewComponent GetOrCreatePreviewComponent(World world, Entity buildingEntity)
    {
        var preview = world.GetComponent<ShipyardPreviewComponent>(buildingEntity);
        if (preview != null)
            return preview;

        preview = new ShipyardPreviewComponent();
        world.AddComponent(buildingEntity, preview);
        return preview;
    }

    private void ClearPreview(World world, Entity buildingEntity, ShipyardPreviewComponent preview)
    {
        DestroyPreviewEntity(world, preview);
        preview.QueuedDefinitionId = string.Empty;
        preview.BuildFraction = 0f;
        preview.IsActive = false;
        _meshState.Remove(buildingEntity);
    }

    private static void DestroyPreviewEntity(World world, ShipyardPreviewComponent preview)
    {
        if (preview.PreviewEntity != Entity.Null && world.IsAlive(preview.PreviewEntity))
            world.DestroyEntity(preview.PreviewEntity);
        preview.PreviewEntity = Entity.Null;
    }

    private static Entity CreatePreviewEntity(
        World world,
        Entity buildingEntity,
        BuildingComponent building,
        EntityDefinition def,
        string raceId)
    {
        Entity previewEntity = world.CreateEntity();

        world.AddComponent(previewEntity, new TransformComponent());
        world.AddComponent(previewEntity, new RenderComponent
        {
            MeshKey = ShipyardBuildPreviewMeshes.BuildCacheKey(raceId, def.Id, 0f),
            MeshId = -1,
            Visible = true,
            Color = Vector4.Zero,
            PrimitiveType = ShipyardBuildPreviewMeshes.ResolvePrimitiveType(0f),
        });
        world.AddComponent(previewEntity, new ShipyardPreviewTagComponent
        {
            ParentBuilding = buildingEntity,
        });

        UpdatePreviewTransform(world, buildingEntity, building, previewEntity);
        ApplyShipDisplayScale(world.GetComponent<TransformComponent>(previewEntity));

        return previewEntity;
    }

    private static void UpdatePreviewTransform(
        World world,
        Entity buildingEntity,
        BuildingComponent building,
        Entity previewEntity)
    {
        var buildingTransform = world.GetComponent<TransformComponent>(buildingEntity);
        var previewTransform = world.GetComponent<TransformComponent>(previewEntity);
        if (buildingTransform == null || previewTransform == null)
            return;

        Vector3 exitOffset = ComputeExitOffset(building, buildingTransform);
        previewTransform.Position = buildingTransform.Position + exitOffset;
        previewTransform.EulerAngles = previewTransform.EulerAngles with
        {
            Y = ComputeExitYaw(exitOffset),
        };
    }

    private static Vector3 ComputeExitOffset(BuildingComponent building, TransformComponent buildingTransform)
    {
        Vector3 exitOffset = building.RallyPoint.HasValue
            ? (building.RallyPoint.Value - buildingTransform.Position).Normalized() * ExitOffsetDistance
            : new Vector3(ExitOffsetDistance, 0f, 0f);
        if (exitOffset.LengthSquared < 1f)
            exitOffset = new Vector3(ExitOffsetDistance, 0f, 0f);
        return exitOffset;
    }

    private static float ComputeExitYaw(Vector3 exitOffset)
    {
        if (exitOffset.LengthSquared < 1e-6f)
            return 0f;

        Vector3 dir = exitOffset.Normalized();
        return MathHelper.RadiansToDegrees(MathF.Atan2(dir.X, dir.Z));
    }

    private static void ApplyShipDisplayScale(TransformComponent? transform)
    {
        if (transform != null)
            transform.Scale = Vector3.One * VisualBalance.ShipScaleMultiplier;
    }

    private static void PruneOrphanedPreviews(World world)
    {
        foreach (var (entity, tag) in world.Query<ShipyardPreviewTagComponent>())
        {
            if (!world.IsAlive(tag.ParentBuilding))
                world.DestroyEntity(entity);
        }
    }

    private readonly record struct PreviewMeshState(string RaceId, string HullKey, float Fraction);
}