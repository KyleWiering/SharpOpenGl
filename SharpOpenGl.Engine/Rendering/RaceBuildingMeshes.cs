namespace SharpOpenGl.Engine.Rendering;

/// <summary>Builds race-tinted station meshes with per-race substrate geometry.</summary>
public static class RaceBuildingMeshes
{
    public static float[] Build(string buildingType, string raceId) =>
        RaceStationMeshes.Build(buildingType, raceId);
}