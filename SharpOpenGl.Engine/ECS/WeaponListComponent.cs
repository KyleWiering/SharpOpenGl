namespace SharpOpenGl.Engine.ECS;

/// <summary>
/// Holds all weapons mounted on a single entity.
/// Used instead of multiple <see cref="WeaponComponent"/> instances because the ECS
/// pools one component instance per entity per type.
/// </summary>
public sealed class WeaponListComponent
{
    /// <summary>Ordered list of weapons. Index within this list is independent of slot.</summary>
    public List<WeaponComponent> Weapons { get; } = new();

    /// <summary>
    /// Returns the first weapon in slot <paramref name="slot"/>, or <c>null</c> if not found.
    /// </summary>
    public WeaponComponent? GetBySlot(int slot) =>
        Weapons.Find(w => w.Slot == slot);
}
