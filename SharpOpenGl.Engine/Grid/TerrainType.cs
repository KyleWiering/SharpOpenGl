namespace SharpOpenGl.Engine.Grid;

/// <summary>
/// Terrain types for map cells. Terrain affects movement speed, line-of-sight,
/// and which unit types may enter the cell.
/// </summary>
public enum TerrainType
{
    /// <summary>Open space — no movement penalty.</summary>
    Space = 0,

    /// <summary>Dense asteroid cluster — slows ships, blocks line-of-sight.</summary>
    AsteroidField = 1,

    /// <summary>
    /// Colorful nebula — medium movement penalty, disrupts sensors (shorter sight range).
    /// </summary>
    Nebula = 2,

    /// <summary>Debris field — moderate movement penalty, light sensor disruption.</summary>
    Debris = 3,

    /// <summary>Impassable boundary or solid object.</summary>
    Impassable = 4,
}
