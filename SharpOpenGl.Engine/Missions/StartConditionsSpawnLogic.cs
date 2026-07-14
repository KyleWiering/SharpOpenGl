namespace SharpOpenGl.Engine.Missions;

/// <summary>
/// Pure helpers for mission start base/building spawn selection.
/// Keeps host-layer OpenGL spawn code testable without a window.
/// </summary>
public static class StartConditionsSpawnLogic
{
    /// <summary>
    /// True when the mission explicitly lists buildings to place at start
    /// (completed structures near player spawn; skips free default base).
    /// </summary>
    public static bool HasExplicitStartingBuildings(StartConditionsDefinition? start) =>
        start?.StartingBuildings is { Length: > 0 };

    /// <summary>
    /// Whether to call the host's free <c>SpawnPlayerBase</c> path.
    /// False when buildings are listed explicitly or <see cref="StartConditionsDefinition.SpawnDefaultBase"/> is false.
    /// Null start conditions keep legacy campaign behavior (spawn default base).
    /// </summary>
    public static bool ShouldSpawnDefaultBase(StartConditionsDefinition? start)
    {
        if (start == null)
            return true;
        if (HasExplicitStartingBuildings(start))
            return false;
        return start.SpawnDefaultBase;
    }
}
