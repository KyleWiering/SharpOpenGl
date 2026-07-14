using OpenTK.Mathematics;
using SharpOpenGl.Engine.Rendering;
using Xunit;

namespace SharpOpenGl.Tests.Rendering;

public class ArticulatedShipPartMeshesTests
{
    private const int TriBudget = 80;

    [Fact]
    public void BuildTurretYawBase_corvette_fast_produces_vertices()
    {
        float[] mesh = ArticulatedShipPartMeshes.BuildTurretYawBase(
            "corvette_fast", new Vector3(0.5f, 0.6f, 0.7f));

        Assert.NotEmpty(mesh);
        Assert.Equal(0, mesh.Length % ProceduralMeshes.Stride);
    }

    [Fact]
    public void BuildBombBayDoor_bomber_heavy_produces_vertices()
    {
        float[] port = ArticulatedShipPartMeshes.BuildBombBayDoor(
            "bomber_heavy", new Vector3(0.55f, 0.5f, 0.52f), mirrored: false);
        float[] starboard = ArticulatedShipPartMeshes.BuildBombBayDoor(
            "bomber_heavy", new Vector3(0.55f, 0.5f, 0.52f), mirrored: true);

        Assert.NotEmpty(port);
        Assert.NotEmpty(starboard);
        Assert.True(ArticulatedShipPartMeshes.TriangleCount(port) <= 60);
        Assert.True(ArticulatedShipPartMeshes.TriangleCount(starboard) <= 60);
    }

    [Fact]
    public void BuildScoutSensorDish_scout_light_tri_count_under_budget()
    {
        float[] mesh = ArticulatedShipPartMeshes.BuildScoutSensorDish(
            "scout_light", new Vector3(0.45f, 0.55f, 0.6f));
        int tris = ArticulatedShipPartMeshes.TriangleCount(mesh);

        Assert.True(tris > 0);
        Assert.True(tris <= TriBudget, $"scout_light dish used {tris} tris (budget {TriBudget})");
    }

    [Fact]
    public void BuildTurretPitchBarrel_destroyer_assault_tri_count_under_budget()
    {
        float[] mesh = ArticulatedShipPartMeshes.BuildTurretPitchBarrel(
            "destroyer_assault", new Vector3(0.45f, 0.5f, 0.55f));
        int tris = ArticulatedShipPartMeshes.TriangleCount(mesh);

        Assert.True(tris > 0);
        Assert.True(tris <= TriBudget, $"destroyer_assault barrel used {tris} tris (budget {TriBudget})");
    }

    [Fact]
    public void TryBuild_unknown_key_returns_false()
    {
        bool built = ArticulatedShipPartMeshes.TryBuild(
            "articulated/turret_yaw/unknown_hull", Vector3.One, out float[] vertices);

        Assert.False(built);
        Assert.Empty(vertices);
    }

    [Fact]
    public void TryBuild_articulated_launcher_pod_drone_swarm_roundtrip()
    {
        const string key = "articulated/launcher_pod/drone_swarm";
        var tint = new Vector3(0.35f, 0.4f, 0.42f);

        Assert.True(ArticulatedShipPartMeshes.TryBuild(key, tint, out float[] direct));
        float[] wrapped = ProceduralMeshes.BuildArticulatedShipPart(key, tint);

        Assert.Equal(direct.Length, wrapped.Length);
        Assert.Equal(direct, wrapped);
        Assert.True(ArticulatedShipPartMeshes.TriangleCount(direct) <= 50);
    }

    [Fact]
    public void ProceduralMeshes_BuildArticulatedShipPart_roundtrip()
    {
        const string key = "articulated/turret_pitch/gunship_heavy";
        var tint = new Vector3(0.4f, 0.55f, 0.65f);

        Assert.True(ArticulatedShipPartMeshes.TryBuild(key, tint, out float[] direct));
        float[] wrapped = ProceduralMeshes.BuildArticulatedShipPart(key, tint);

        Assert.Equal(direct.Length, wrapped.Length);
        Assert.Equal(direct, wrapped);
    }

    [Theory]
    [InlineData("corvette", "corvette_fast")]
    [InlineData("gunship", "gunship_heavy")]
    [InlineData("destroyer", "destroyer_assault")]
    public void Hull_aliases_resolve_to_canonical_keys(string alias, string canonical)
    {
        string yawKey = ArticulatedShipPartMeshes.BuildPartKey("turret_yaw", alias);
        string pitchKey = ArticulatedShipPartMeshes.BuildPartKey("turret_pitch", alias);

        Assert.EndsWith(canonical, yawKey, StringComparison.Ordinal);
        Assert.EndsWith(canonical, pitchKey, StringComparison.Ordinal);

        Assert.True(ArticulatedShipPartMeshes.TryBuild(yawKey, Vector3.One, out float[] yawVerts));
        Assert.True(ArticulatedShipPartMeshes.TryBuild(pitchKey, Vector3.One, out float[] pitchVerts));
        Assert.NotEmpty(yawVerts);
        Assert.NotEmpty(pitchVerts);
        Assert.True(ArticulatedShipPartMeshes.TriangleCount(yawVerts) <= TriBudget);
        Assert.True(ArticulatedShipPartMeshes.TriangleCount(pitchVerts) <= TriBudget);
    }

    [Fact]
    public void AllPartKeys_includes_six_special_hull_families()
    {
        var keys = ArticulatedShipPartMeshes.AllPartKeys().ToList();

        Assert.Contains("articulated/bay_door/bomber_heavy/port", keys);
        Assert.Contains("articulated/deck_segment/carrier_command", keys);
        Assert.Contains("articulated/launcher_pod/drone_swarm", keys);
        Assert.Contains("articulated/sensor_dish/scout_light", keys);
        Assert.Contains("articulated/wing_flap/fighter_basic/left", keys);
        Assert.Contains("articulated/wing_flap/interceptor_mk2/right", keys);

        foreach (string family in ArticulatedShipPartMeshes.SpecialHullFamilyKeys)
            Assert.Contains(keys, k => k.Contains(family, StringComparison.Ordinal));

        Assert.All(keys, key => Assert.True(ArticulatedShipPartMeshes.TryBuild(key, Vector3.One, out _)));
    }
}