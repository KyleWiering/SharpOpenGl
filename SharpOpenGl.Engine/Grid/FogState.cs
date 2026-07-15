namespace SharpOpenGl.Engine.Grid;

/// <summary>
/// Tracks per-player visibility state for a grid cell.
/// </summary>
public enum FogState
{
    /// <summary>Cell has never been seen — dense nebula fog overlay.</summary>
    Unexplored = 0,

    /// <summary>Cell was previously visible but is no longer in sight — lighter memory fog overlay.</summary>
    Explored = 1,

    /// <summary>Cell is currently in sight range of at least one friendly unit — no fog overlay.</summary>
    Visible = 2,
}
