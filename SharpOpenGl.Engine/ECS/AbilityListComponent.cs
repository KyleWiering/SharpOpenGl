namespace SharpOpenGl.Engine.ECS;

/// <summary>
/// Holds all abilities attached to a single entity.
/// A single list component is used so each entity has exactly one pool entry.
/// </summary>
public sealed class AbilityListComponent
{
    /// <summary>All ability slots in activation order.</summary>
    public List<AbilityComponent> Abilities { get; } = new();

    /// <summary>Returns the ability in the given slot, or <c>null</c> if not found.</summary>
    public AbilityComponent? GetBySlot(int slot) =>
        Abilities.Find(a => a.Slot == slot);
}
