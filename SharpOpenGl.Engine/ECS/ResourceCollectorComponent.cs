using SharpOpenGl.Engine.Economy;

namespace SharpOpenGl.Engine.ECS;

/// <summary>State machine phases for a resource-collecting unit.</summary>
public enum CollectorState
{
    /// <summary>Not assigned to any node; waiting for orders.</summary>
    Idle,

    /// <summary>Travelling toward the assigned resource node.</summary>
    MovingToNode,

    /// <summary>Actively extracting resources at the assigned node.</summary>
    Collecting,

    /// <summary>Cargo hold full (or node depleted); travelling back to deposit target.</summary>
    Returning,

    /// <summary>At the deposit target; transferring resources to the player's pool.</summary>
    Depositing
}

/// <summary>
/// Allows an entity to harvest <see cref="ResourceNodeComponent"/> entities
/// and deposit collected resources to a base building.
/// Driven each frame by <see cref="ResourceSystem"/>.
/// </summary>
public sealed class ResourceCollectorComponent
{
    /// <summary>Player that owns this collector.</summary>
    public int PlayerId { get; set; }

    /// <summary>
    /// Entity handle of the resource node currently targeted.
    /// <see cref="Entity.Null"/> when not assigned.
    /// </summary>
    public Entity AssignedNode { get; set; } = Entity.Null;

    /// <summary>
    /// Entity handle of the building where harvested resources are deposited.
    /// <see cref="Entity.Null"/> when not assigned.
    /// </summary>
    public Entity DepositTarget { get; set; } = Entity.Null;

    /// <summary>How this collector extracts resources.</summary>
    public HarvestMode HarvestMode { get; set; } = HarvestMode.Drones;

    /// <summary>World-unit radius within which harvesting is allowed.</summary>
    public float HarvestRange { get; set; } = 28f;

    /// <summary>Effective extraction rate (units/sec) from assigned nodes.</summary>
    public float HarvestRate { get; set; } = 5f;

    /// <summary>Maximum units of resource this collector can carry at once.</summary>
    public float CarryCapacity { get; set; } = 50f;

    /// <summary>Current amount of resource being carried.</summary>
    public float CarryAmount { get; set; }

    /// <summary>Pulse timer for tractor-beam burst extraction.</summary>
    public float TractorPulseTimer { get; set; }

    /// <summary>Resource type currently being carried or targeted for collection.</summary>
    public ResourceType? CarryType { get; set; }

    /// <summary>Current phase of the collection state machine.</summary>
    public CollectorState State { get; set; } = CollectorState.Idle;

    /// <summary><c>true</c> when the cargo hold is at capacity.</summary>
    public bool IsFull => CarryAmount >= CarryCapacity;
}
