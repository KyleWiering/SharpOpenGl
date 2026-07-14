using OpenTK.Mathematics;
using SharpOpenGl.Engine.ECS;
using SharpOpenGl.Engine.Rendering;
using Xunit;

namespace SharpOpenGl.Tests.Rendering;

/// <summary>
/// Guardrails that ship and station meshes for the same race share substrate profile tokens,
/// shader race-texture indices (0–7), and position-encoded micro-variation band coverage.
/// </summary>
public class StationShipSubstrateParityTests
{
    private const string RepresentativeShip = "fighter_basic";
    private const string RepresentativeStation = "command_center";

    public StationShipSubstrateParityTests() => RaceVisualSchema.ResetForTests();

    [Theory]
    [InlineData("terran")]
    [InlineData("vesper")]
    [InlineData("korath")]
    [InlineData("voidborn")]
    public void Ship_and_station_share_substrate_profile_and_texture_index(string raceId)
    {
        Assert.True(RaceVisualSchema.TryGetRace(raceId, out var race));

        RaceSubstrateProfile profile = RaceSubstrateProfile.ForRace(race);
        var shipEntry = RaceSubstrateCatalog.AllEntries()
            .First(e => e.RaceId == raceId && e.Kind == RaceSubstrateCatalog.SubstrateKind.Ship && e.ModelId == RepresentativeShip);
        var stationEntry = RaceSubstrateCatalog.AllEntries()
            .First(e => e.RaceId == raceId && e.Kind == RaceSubstrateCatalog.SubstrateKind.Station && e.ModelId == RepresentativeStation);

        AssertProfilesEqual(profile, shipEntry.Profile);
        AssertProfilesEqual(profile, stationEntry.Profile);

        int textureIndex = RaceTextureIndex.Resolve(raceId);
        Assert.InRange(textureIndex, 0, 7);
        Assert.Equal(textureIndex, RaceTextureIndex.Resolve(raceId));
    }

    [Theory]
    [InlineData("terran")]
    [InlineData("vesper")]
    [InlineData("korath")]
    [InlineData("voidborn")]
    public void Ship_and_station_use_consistent_micro_variation_slots(string raceId)
    {
        Assert.True(RaceVisualSchema.TryGetRace(raceId, out var race));
        RaceSubstrateProfile profile = RaceSubstrateProfile.ForRace(race);

        float[] shipMesh = RaceShipMeshes.Build(raceId, RepresentativeShip);
        float[] stationMesh = RaceStationMeshes.Build(RepresentativeStation, raceId);

        var shipSlots = CollectMicroVariationSlots(shipMesh, profile);
        var stationSlots = CollectMicroVariationSlots(stationMesh, profile);

        Assert.NotEmpty(shipSlots);
        Assert.NotEmpty(stationSlots);
        Assert.All(shipSlots, slot => Assert.InRange(slot, 0, 7));
        Assert.All(stationSlots, slot => Assert.InRange(slot, 0, 7));

        var shipDistinct = shipSlots.Distinct().ToHashSet();
        var stationDistinct = stationSlots.Distinct().ToHashSet();
        Assert.True(shipDistinct.Count >= 4, $"{raceId} ship should exercise ≥4 micro-variation slots (got {shipDistinct.Count})");
        Assert.True(stationDistinct.Count >= 4, $"{raceId} station should exercise ≥4 micro-variation slots (got {stationDistinct.Count})");

        int sharedSlots = shipDistinct.Intersect(stationDistinct).Count();
        Assert.True(sharedSlots >= 2,
            $"{raceId} ship/station should share ≥2 micro-variation slots under the same profile (shared {sharedSlots})");
    }

    [Fact]
    public void Terran_command_center_supports_team_color_overlay_on_primary_hull()
    {
        float[] mesh = RaceStationMeshes.Build(RepresentativeStation, "terran");
        Assert.NotEmpty(mesh);
        Assert.True(ProceduralMeshes.VertexCount(mesh) >= 30);

        var render = new RenderComponent
        {
            MeshId = 7,
            VertexCount = ProceduralMeshes.VertexCount(mesh),
            Visible = true,
            Color = new Vector4(0.35f, 0.35f, 0.35f, 1f),
        };

        TeamVisualResolver.ApplyRaceTexturing(render, "terran", 2);

        Assert.Equal(RaceTextureIndex.Resolve("terran"), render.RaceTextureIndex);
        Assert.True(render.TeamTint.LengthSquared > 0f);
        Assert.Equal(-1, render.ComponentTextureIndex);
        Assert.Equal(Vector4.Zero, render.Color);

        int primaryHullVerts = CountPrimaryHullVertices(mesh);
        Assert.True(primaryHullVerts >= 12, $"expected primary hull verts for team tint channel, got {primaryHullVerts}");
    }

    [Fact]
    public void Race_visuals_json_all_eight_races_have_explicit_substrate_blocks()
    {
        Assert.Equal(8, RaceVisualSchema.AllRaces.Count);

        foreach (var race in RaceVisualSchema.AllRaces)
        {
            Assert.NotNull(race.Substrate);
            Assert.False(string.IsNullOrWhiteSpace(race.Substrate!.Pattern));
            Assert.True(race.Substrate.UvScale > 0);
            Assert.True(race.Substrate.MicroFrequency > 0);

            var profile = RaceSubstrateProfile.ForRace(race);
            Assert.Equal(race.Substrate.Pattern, profile.Pattern);
            Assert.Equal(race.Substrate.UvScale, profile.UvScale, 4);
            Assert.Equal(race.Substrate.PanelDepth, profile.PanelDepth, 4);
            Assert.Equal(race.Substrate.Grit, profile.Grit, 4);
            Assert.Equal(race.Substrate.MicroFrequency, profile.MicroFrequency, 4);
        }
    }

    [Fact]
    public void Terran_substrate_tokens_match_fleet_overhaul_baseline()
    {
        Assert.True(RaceVisualSchema.TryGetRace("terran", out var race));
        Assert.NotNull(race.Substrate);

        Assert.True(race.Substrate!.Grit <= 0.10f);
        Assert.True(race.Substrate.PanelDepth >= 0.14f);
        Assert.Equal("retro", race.Substrate.Pattern);

        Assert.True(race.Palette.Primary[0] >= 0.85f);
        Assert.True(race.Palette.Primary[1] >= 0.85f);
        Assert.True(race.Palette.Secondary[0] >= 0.55f && race.Palette.Secondary[0] <= 0.70f);
        Assert.True(race.Palette.Accent[0] >= 0.90f && race.Palette.Accent[1] < 0.55f);
        Assert.True(race.Palette.Engine[2] >= 0.85f);
    }

    private static void AssertProfilesEqual(RaceSubstrateProfile expected, RaceSubstrateProfile actual)
    {
        Assert.Equal(expected.Pattern, actual.Pattern);
        Assert.Equal(expected.UvScale, actual.UvScale, 4);
        Assert.Equal(expected.PanelDepth, actual.PanelDepth, 4);
        Assert.Equal(expected.Grit, actual.Grit, 4);
        Assert.Equal(expected.AccentBoost, actual.AccentBoost, 4);
        Assert.Equal(expected.MicroFrequency, actual.MicroFrequency, 4);
    }

    /// <summary>Mirrors the panel term in <see cref="RaceMeshWriter.ApplySubstrateVariation"/>.</summary>
    private static int QuantizeMicroVariationSlot(float x, float y, float z, RaceSubstrateProfile profile)
    {
        float freq = profile.MicroFrequency;
        float panel = 0.5f + 0.5f * MathF.Sin(x * freq) * MathF.Cos(z * freq * 0.73f);
        return Math.Clamp((int)MathF.Floor(panel * 8f), 0, 7);
    }

    private static List<int> CollectMicroVariationSlots(float[] mesh, RaceSubstrateProfile profile)
    {
        var slots = new List<int>();
        for (int i = 0; i < mesh.Length; i += ProceduralMeshes.Stride)
        {
            float lum = (mesh[i + 3] + mesh[i + 4] + mesh[i + 5]) / 3f;
            if (lum is < 0.55f or > 0.92f)
                continue;

            slots.Add(QuantizeMicroVariationSlot(mesh[i], mesh[i + 1], mesh[i + 2], profile));
        }

        return slots;
    }

    private static int CountPrimaryHullVertices(float[] mesh)
    {
        int count = 0;
        for (int i = 0; i < mesh.Length; i += ProceduralMeshes.Stride)
        {
            float lum = (mesh[i + 3] + mesh[i + 4] + mesh[i + 5]) / 3f;
            if (lum is >= 0.55f and <= 0.92f)
                count++;
        }

        return count;
    }
}