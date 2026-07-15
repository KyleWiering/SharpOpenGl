namespace SharpOpenGl.Engine.ECS;

/// <summary>
/// Allows an entity to restore hull HP on nearby friendly or neutral ships.
/// Driven each frame by <see cref="RepairSystem"/>.
/// </summary>
public sealed class ShipRepairComponent
{
    /// <summary>World-unit radius within which repair can be applied.</summary>
    public float RepairRange { get; set; } = 60f;

    /// <summary>Hit points restored per second while actively repairing.</summary>
    public float RepairRate { get; set; } = 12f;

    /// <summary>
    /// Entity categories this repairer may restore (e.g. fighter, gunship, corvette).
    /// Matched against the target's definition id prefix.
    /// </summary>
    public string[] RepairableCategories { get; set; } = [];

    /// <summary>Entity being repaired this frame; <see cref="Entity.Null"/> when none in range.</summary>
    public Entity ActiveTarget { get; set; } = Entity.Null;
}