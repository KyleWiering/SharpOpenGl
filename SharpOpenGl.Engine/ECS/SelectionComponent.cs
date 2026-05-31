namespace SharpOpenGl.Engine.ECS;

/// <summary>
/// Marker component for entities that can be selected by the player.
/// When <see cref="IsSelected"/> is true, UI highlights are shown.
/// </summary>
public sealed class SelectionComponent
{
    /// <summary>Whether this entity is currently selected by the player.</summary>
    public bool IsSelected { get; set; }

    /// <summary>Radius used for click-to-select hit testing (world units).</summary>
    public float SelectionRadius { get; set; } = 2f;
}
