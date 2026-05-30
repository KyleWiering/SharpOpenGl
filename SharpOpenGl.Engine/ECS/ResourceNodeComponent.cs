using SharpOpenGl.Engine.Economy;

namespace SharpOpenGl.Engine.ECS;

/// <summary>
/// A resource deposit on the map that can be harvested by collector units.
/// Attached to a map entity alongside a <see cref="TransformComponent"/> that
/// gives the node its world-space position.
/// </summary>
public sealed class ResourceNodeComponent
{
    /// <summary>Which of the four resource types this node provides.</summary>
    public ResourceType ResourceType { get; set; }

    /// <summary>Current units of resource remaining in this node.</summary>
    public float Amount { get; set; }

    /// <summary>
    /// Maximum capacity of this node (used as the respawn fill amount and
    /// for display as a depletion fraction).
    /// </summary>
    public float MaxAmount { get; set; }

    /// <summary>Units per second extracted by a single collector at this node.</summary>
    public float HarvestRate { get; set; } = 10f;

    /// <summary><c>true</c> when <see cref="Amount"/> has reached zero.</summary>
    public bool IsDepleted => Amount <= 0f;

    /// <summary>
    /// Seconds after depletion before the node refills to <see cref="MaxAmount"/>.
    /// A value of 0 means the node never respawns (finite resource).
    /// </summary>
    public float RespawnTime { get; set; }

    /// <summary>
    /// Remaining countdown in seconds until respawn.
    /// Only meaningful when <see cref="IsDepleted"/> is <c>true</c>.
    /// </summary>
    public float RespawnCountdown { get; set; }

    /// <summary>Number of collector entities currently assigned to and actively harvesting this node.</summary>
    public int AssignedCollectors { get; set; }

    /// <summary>Depletion fraction [0, 1] — 1 means full, 0 means depleted.</summary>
    public float DepletionFraction => MaxAmount > 0f ? Amount / MaxAmount : 0f;
}
