using OpenTK.Mathematics;
using SharpOpenGl.Engine.Rendering;

namespace SharpOpenGl.Engine.ECS;

/// <summary>
/// Refreshes scaffold meshes for entities with <see cref="UnderConstructionComponent"/>.
/// Runs after <see cref="ConstructionSystem"/> so completion swaps happen the same frame.
/// </summary>
public sealed class StructureConstructionVisualSystem : GameSystem
{
    private const float MeshRefreshThreshold = 0.02f;

    private readonly Dictionary<Entity, MeshState> _meshState = new();

    public Func<float[], int, (int MeshId, int VertexCount)>? MeshUploader { get; set; }
    public Func<int, string>? ResolveFactionRaceId { get; set; }

    public override void Update(World world, float deltaTime)
    {
        foreach (var (entity, underConstruction) in world.Query<UnderConstructionComponent>())
            UpdateStructureMesh(world, entity, underConstruction);

        PruneStaleState(world);
    }

    private void UpdateStructureMesh(World world, Entity entity, UnderConstructionComponent underConstruction)
    {
        var building = world.GetComponent<BuildingComponent>(entity);
        var render = world.GetComponent<RenderComponent>(entity);
        if (building == null || render == null || underConstruction.TotalBuildTime <= 0f)
            return;

        float buildFraction = Math.Clamp(
            underConstruction.BuildProgress / underConstruction.TotalBuildTime, 0f, 1f);
        string raceId = ResolveRaceId(world, entity, underConstruction);
        string buildingType = building.BuildingType;

        float quantized = StructureConstructionMeshes.QuantizeFraction(buildFraction);
        if (_meshState.TryGetValue(entity, out MeshState state)
            && state.RaceId == raceId
            && state.BuildingType == buildingType
            && MathF.Abs(state.Fraction - quantized) < MeshRefreshThreshold)
        {
            return;
        }

        string cacheKey = StructureConstructionMeshes.BuildCacheKey(buildingType, raceId, quantized);
        float[] vertices = StructureConstructionMeshes.BuildPartial(buildingType, raceId, buildFraction);
        int primitiveType = StructureConstructionMeshes.ResolvePrimitiveType(buildFraction);

        render.PrimitiveType = primitiveType;
        render.MeshKey = cacheKey;
        render.VertexCount = ProceduralMeshes.VertexCount(vertices);
        render.Visible = true;
        render.Color = Vector4.Zero;

        if (StructureConstructionMeshes.TryResolveGpuMesh(
                cacheKey, vertices, primitiveType, MeshUploader, out int meshId, out int vertexCount))
        {
            render.MeshId = meshId;
            render.VertexCount = vertexCount;
        }

        TeamVisualResolver.ApplyRaceTexturing(render, raceId, underConstruction.PlayerId);
        _meshState[entity] = new MeshState(raceId, buildingType, quantized);
    }

    private string ResolveRaceId(World world, Entity entity, UnderConstructionComponent underConstruction)
    {
        var raceComponent = world.GetComponent<RaceComponent>(entity);
        if (raceComponent != null && !string.IsNullOrWhiteSpace(raceComponent.RaceId))
            return raceComponent.RaceId;

        if (ResolveFactionRaceId != null)
            return ResolveFactionRaceId(underConstruction.PlayerId);

        return RaceShipMeshes.DefaultRace;
    }

    private void PruneStaleState(World world)
    {
        foreach (var entity in _meshState.Keys.ToArray())
        {
            if (!world.IsAlive(entity) || !world.HasComponent<UnderConstructionComponent>(entity))
                _meshState.Remove(entity);
        }
    }

    private readonly record struct MeshState(string RaceId, string BuildingType, float Fraction);
}