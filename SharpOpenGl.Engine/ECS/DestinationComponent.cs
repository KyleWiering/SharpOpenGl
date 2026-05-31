using OpenTK.Mathematics;

namespace SharpOpenGl.Engine.ECS;

/// <summary>
/// Assigned to an entity to command it to move to a specific grid location.
/// Removed automatically upon arrival.
/// </summary>
public sealed class DestinationComponent
{
    /// <summary>Target world-space position the entity should move toward.</summary>
    public Vector3 Target { get; set; }

    /// <summary>Grid X coordinate of the destination cell.</summary>
    public int GridX { get; set; }

    /// <summary>Grid Y coordinate of the destination cell.</summary>
    public int GridY { get; set; }
}
