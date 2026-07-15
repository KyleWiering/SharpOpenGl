using SharpOpenGl.Engine.Rendering;
using Xunit;

namespace SharpOpenGl.Tests.Rendering;

/// <summary>
/// Gameplay stations must use procedural vertex-color meshes (not OBJ pos+normal) so race
/// substrate shaders receive luminance bands. See EngineWindow.RaceMeshes.ResolveRaceBuildingMesh.
/// </summary>
public class RaceBuildingMeshGameplayTests
{
    public RaceBuildingMeshGameplayTests() => RaceVisualSchema.ResetForTests();

    [Theory]
    [InlineData("terran")]
    [InlineData("vesper")]
    [InlineData("korath")]
    [InlineData("voidborn")]
    public void Procedural_station_luminance_tracks_ship_band(string raceId)
    {
        float[] ship = RaceShipMeshes.Build(raceId, "fighter_basic");
        float[] station = RaceBuildingMeshes.Build("command_center", raceId);
        Assert.NotEmpty(station);
        Assert.Equal(0, station.Length % ProceduralMeshes.Stride);

        float shipLum = MeanVertexLuminance(ship);
        float stationLum = MeanVertexLuminance(station);
        Assert.True(stationLum >= shipLum * 0.70f,
            $"{raceId} station lum {stationLum:F2} should track fighter {shipLum:F2} (procedural vertex-color, not OBJ normals)");
    }

    [Fact]
    public void Terran_station_luminance_matches_ship_band()
    {
        float[] ship = RaceShipMeshes.Build("terran", "fighter_basic");
        float[] station = RaceBuildingMeshes.Build("command_center", "terran");

        float shipLum = MeanVertexLuminance(ship);
        float stationLum = MeanVertexLuminance(station);

        Assert.True(stationLum >= shipLum * 0.85f,
            $"terran station lum {stationLum:F2} should be within 15% of fighter {shipLum:F2} for gameplay parity");
    }

    private static float MeanVertexLuminance(float[] mesh)
    {
        float sum = 0f;
        int count = 0;
        for (int i = 3; i < mesh.Length; i += ProceduralMeshes.Stride)
        {
            float lum = 0.299f * mesh[i] + 0.587f * mesh[i + 1] + 0.114f * mesh[i + 2];
            sum += lum;
            count++;
        }

        return count > 0 ? sum / count : 0f;
    }
}