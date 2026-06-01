using OpenTK.Mathematics;

namespace SharpOpenGl.Engine.ECS;

/// <summary>
/// Marks an entity as a stationary base structure.
/// Manages its production queue and grid footprint.
/// </summary>
public sealed class BuildingComponent
{
    /// <summary>Building type identifier (e.g. "command_center", "barracks").</summary>
    public string BuildingType { get; set; } = "generic";

    /// <summary>
    /// Ordered list of entity-definition keys queued for production.
    /// The first entry is currently being built.
    /// </summary>
    public Queue<string> BuildQueue { get; set; } = new();

    /// <summary>
    /// Production speed multiplier (1.0 = normal speed).
    /// Higher values reduce effective build time.
    /// </summary>
    public float ProductionRate { get; set; } = 1f;

    /// <summary>
    /// Seconds elapsed on the currently-building item.
    /// Reset to 0 when an item completes or the queue is cleared.
    /// </summary>
    public float BuildProgress { get; set; }

    /// <summary>Grid-space footprint: [columns, rows].</summary>
    public int[] Footprint { get; set; } = [1, 1];

    /// <summary>
    /// Rally point where newly produced units move to after spawning.
    /// Null means units spawn in place without auto-move.
    /// </summary>
    public Vector3? RallyPoint { get; set; }

    /// <summary>Player who owns this building.</summary>
    public int PlayerId { get; set; } = 1;

    /// <summary>
    /// List of entity definition IDs this building can produce.
    /// </summary>
    public List<string> Producible { get; set; } = new();
}
