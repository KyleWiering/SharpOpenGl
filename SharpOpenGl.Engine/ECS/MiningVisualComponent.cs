namespace SharpOpenGl.Engine.ECS;

/// <summary>Kind of mining visual sub-entity spawned during collection.</summary>
public enum MiningVisualKind
{
    Drone,
    EvaCrew,
}

/// <summary>Tags a transient mining visual entity owned by a collector.</summary>
public sealed class MiningVisualComponent
{
    public Entity CollectorEntity { get; set; }
    public MiningVisualKind Kind { get; set; }
    public int SlotIndex { get; set; }
}

/// <summary>Shuttle state for a mining drone traveling node ↔ collector.</summary>
public sealed class MiningDroneComponent
{
    public Entity CollectorEntity { get; set; }
    public Entity NodeEntity { get; set; }
    public float ShuttlePhase { get; set; }
    public bool ReturningToMiner { get; set; }
    public float CargoPayload { get; set; }
    public float ShuttleSpeed { get; set; } = 0.3f;
}

/// <summary>Tracks spawned visuals and animation state on a collector.</summary>
public sealed class MiningVisualStateComponent
{
    public readonly List<Entity> SpawnedVisuals = new();
    public float EvaAnimPhase { get; set; }
}

/// <summary>Active tractor beam VFX from node to collector.</summary>
public sealed class TractorBeamVisualComponent
{
    public Entity NodeEntity { get; set; }
    public float PulsePhase { get; set; }
}

/// <summary>Optional GPU mesh handles for mining visual entities.</summary>
public sealed class MiningVisualMeshHandles
{
    public int DroneMeshId { get; init; }
    public int DroneVertexCount { get; init; }
    public int EvaMeshId { get; init; }
    public int EvaVertexCount { get; init; }
}