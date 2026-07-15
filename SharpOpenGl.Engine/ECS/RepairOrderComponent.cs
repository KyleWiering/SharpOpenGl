namespace SharpOpenGl.Engine.ECS;

/// <summary>
/// Player-issued repair order on a repair-capable unit.
/// When set, <see cref="RepairSystem"/> prefers this target over auto-selection.
/// </summary>
public sealed class RepairOrderComponent
{
    /// <summary>Explicit repair target, or <see cref="Entity.Null"/> for automatic targeting.</summary>
    public Entity Target { get; set; } = Entity.Null;
}