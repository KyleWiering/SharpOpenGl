namespace SharpOpenGl.Engine.ECS;

/// <summary>Non-unit map objects: neutral planets and decorative scenery.</summary>
public enum MapFeatureKind
{
    Scenery,
    NeutralPlanet,
}

/// <summary>Marks inspectable map features (planets, asteroid clusters, nebulae).</summary>
public sealed class MapFeatureComponent
{
    public MapFeatureKind Kind { get; init; }
    public string FeatureType { get; init; } = string.Empty;
    public string Subtitle { get; init; } = string.Empty;
}