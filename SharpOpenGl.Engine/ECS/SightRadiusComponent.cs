namespace SharpOpenGl.Engine.ECS;

/// <summary>
/// Entities with this component reveal fog-of-war cells within their sight radius.
/// </summary>
public sealed class SightRadiusComponent
{
    /// <summary>Number of grid cells this entity can see in each direction.</summary>
    public int Radius { get; set; } = 5;
}
