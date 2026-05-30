namespace SharpOpenGl.Engine.Grid;

/// <summary>
/// Tracks per-player visibility state for a grid cell.
/// </summary>
public enum FogState
{
    /// <summary>Cell has never been seen — shown as black.</summary>
    Unexplored = 0,

    /// <summary>Cell was previously visible but is no longer in sight — shown darkened.</summary>
    Explored = 1,

    /// <summary>Cell is currently in sight range of at least one friendly unit.</summary>
    Visible = 2,
}
