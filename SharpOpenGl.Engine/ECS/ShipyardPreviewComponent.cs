namespace SharpOpenGl.Engine.ECS;

/// <summary>
/// Tracks the transient build-preview hull entity for a shipyard building.
/// Attached to the parent <see cref="BuildingComponent"/> entity.
/// </summary>
public sealed class ShipyardPreviewComponent
{
    /// <summary>Child/transient hull entity shown at the yard exit pad.</summary>
    public Entity PreviewEntity { get; set; } = Entity.Null;

    /// <summary>Definition key for <c>BuildQueue[0]</c> while the preview is active.</summary>
    public string QueuedDefinitionId { get; set; } = string.Empty;

    /// <summary>Normalized build progress in the range 0–1.</summary>
    public float BuildFraction { get; set; }

    /// <summary>When true, the preview should be rendered this frame.</summary>
    public bool IsActive { get; set; }
}