namespace SharpOpenGl.Engine.Missions;

/// <summary>High-level lifecycle state of a running mission session.</summary>
public enum MissionPhase
{
    /// <summary>No mission is loaded.</summary>
    None,

    /// <summary>Showing the pre-mission briefing screen.</summary>
    Briefing,

    /// <summary>Mission is actively running and objectives are being evaluated.</summary>
    InProgress,

    /// <summary>All primary objectives completed — mission won.</summary>
    Victory,

    /// <summary>A defeat condition was triggered — mission lost.</summary>
    Defeat,
}
