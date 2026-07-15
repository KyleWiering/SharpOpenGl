using OpenTK.Mathematics;
using SharpOpenGl.Engine.Rendering;
using Xunit;

namespace SharpOpenGl.Tests.Browser;

/// <summary>
/// Headless CPU-side validation mirroring <c>BrowserMeshLibrary.InitializeAsync</c> preload keys.
/// GPU upload is covered by manual WebGL QA.
/// </summary>
public class BrowserMeshLibraryTests
{
    private static readonly Vector3 NeutralTint = new(0.55f, 0.55f, 0.58f);

    [Fact]
    public void AllPartKeys_every_ship_key_builds_nonempty_verts()
    {
        foreach (string partKey in ArticulatedShipPartMeshes.AllPartKeys())
        {
            bool built = ArticulatedShipPartMeshes.TryBuild(partKey, NeutralTint, out float[] vertices);
            Assert.True(built, $"TryBuild failed for ship key '{partKey}'");
            Assert.NotEmpty(vertices);
        }
    }

    [Fact]
    public void AllPartKeyPrefixes_every_station_prefix_builds()
    {
        string defaultRace = RaceShipMeshes.DefaultRace;
        RaceVisualSchema.TryGetRace(defaultRace, out RaceVisualDefinition? race);
        race ??= RaceVisualSchema.AllRaces.FirstOrDefault()
            ?? new RaceVisualDefinition { Id = defaultRace };
        float stationScale = 7f * (0.85f + race.Modifiers.Superstructure * 0.3f);

        foreach (string prefix in ArticulatedStationPartMeshes.AllPartKeyPrefixes)
        {
            bool built = ArticulatedStationPartMeshes.TryBuild(
                prefix, defaultRace, stationScale, out float[] vertices);
            Assert.True(built, $"TryBuild failed for station prefix '{prefix}'");
            Assert.NotEmpty(vertices);
        }
    }

    [Fact]
    public void UtilityPartMeshes_miner_and_repair_keys_build()
    {
        foreach (string hullKey in UtilityPartMeshes.MinerHullKeys)
        {
            float[] miningVerts = UtilityPartMeshes.BuildMiningArmMesh(hullKey);
            Assert.NotEmpty(miningVerts);
            Assert.Equal(0, miningVerts.Length % ProceduralMeshes.Stride);
        }

        float[] repairVerts = UtilityPartMeshes.BuildRepairArmMesh("support_repair");
        Assert.NotEmpty(repairVerts);
        Assert.Equal(0, repairVerts.Length % ProceduralMeshes.Stride);
    }

    [Fact]
    public void AllPartKeys_no_duplicates()
    {
        var keys = ArticulatedShipPartMeshes.AllPartKeys().ToList();
        Assert.Equal(keys.Count, keys.Distinct(StringComparer.OrdinalIgnoreCase).Count());
    }
}