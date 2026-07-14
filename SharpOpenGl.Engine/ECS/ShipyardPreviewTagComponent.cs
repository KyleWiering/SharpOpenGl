namespace SharpOpenGl.Engine.ECS;

/// <summary>
/// Reverse link from a shipyard preview hull entity back to its parent building.
/// </summary>
public sealed class ShipyardPreviewTagComponent
{
    /// <summary>Parent shipyard building entity that owns this preview.</summary>
    public Entity ParentBuilding { get; set; }
}