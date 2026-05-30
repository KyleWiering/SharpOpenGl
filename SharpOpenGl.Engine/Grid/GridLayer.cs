namespace SharpOpenGl.Engine.Grid;

/// <summary>
/// Vertical layers within a map. Units can occupy different layers
/// allowing orbital units to fly above surface units.
/// </summary>
public enum GridLayer
{
    /// <summary>The main game plane — ships, bases, resource nodes.</summary>
    Surface = 0,

    /// <summary>The high-altitude layer — orbital units and stations.</summary>
    Orbital = 1,
}
