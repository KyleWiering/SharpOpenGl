namespace SharpOpenGl.Engine.ECS;

/// <summary>
/// Allows a mobile unit to place base structures within <see cref="PlacementRange"/>.
/// Driven by player build commands and shared placement validation.
/// </summary>
public sealed class StructureBuilderComponent
{
    /// <summary>Default placement radius when JSON omits <c>placementRange</c>.</summary>
    public const float DefaultPlacementRange = 80f;

    /// <summary>World-unit radius within which structures may be placed.</summary>
    public float PlacementRange { get; set; } = DefaultPlacementRange;

    /// <summary>Whitelist of <c>GameData/Bases/*.json</c> ids this unit may place.</summary>
    public List<string> BuildableIds { get; set; } = [];
}