using SharpOpenGl.Engine.Grid;

namespace SharpOpenGl.Engine.UI;

/// <summary>Rules for what appears on the tactical minimap relative to fog-of-war.</summary>
public static class MinimapVisibilityPolicy
{
    /// <summary>Economy and scenery markers require prior discovery (explored or in sight).</summary>
    public static bool ShouldShowFeature(FogState state) =>
        state is FogState.Explored or FogState.Visible;

    /// <summary>Friendly units appear once their cell has been discovered; enemies only while in sight.</summary>
    public static bool ShouldShowUnit(FogState state, bool isFriendly) =>
        isFriendly
            ? state is FogState.Explored or FogState.Visible
            : state == FogState.Visible;
}