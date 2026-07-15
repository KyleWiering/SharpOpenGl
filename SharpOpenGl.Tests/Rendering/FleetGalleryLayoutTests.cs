using OpenTK.Mathematics;
using SharpOpenGl.Engine.Rendering;
using Xunit;

namespace SharpOpenGl.Tests.Rendering;

public class FleetGalleryLayoutTests
{
    [Fact]
    public void AllShipIds_includes_full_roster()
    {
        Assert.Equal(19, FleetGalleryLayout.AllShipIds.Length);
        Assert.Contains("hero_default", FleetGalleryLayout.AllShipIds);
        Assert.Contains("miner_tractor", FleetGalleryLayout.AllShipIds);
    }

    [Fact]
    public void AllBaseIds_includes_all_station_types()
    {
        Assert.Equal(16, FleetGalleryLayout.AllBaseIds.Length);
        Assert.Contains("command_center", FleetGalleryLayout.AllBaseIds);
        Assert.Contains("shipyard_large", FleetGalleryLayout.AllBaseIds);
        Assert.Contains("missile_battery", FleetGalleryLayout.AllBaseIds);
        Assert.Contains("fortress_core", FleetGalleryLayout.AllBaseIds);
        Assert.Contains("fabrication_hub", FleetGalleryLayout.AllBaseIds);
    }

    [Fact]
    public void BaseOffsets_are_unique_for_sixteen_station_types()
    {
        var offsets = new HashSet<Vector3>();
        for (int i = 0; i < FleetGalleryLayout.AllBaseIds.Length; i++)
            Assert.True(offsets.Add(FleetGalleryLayout.BaseOffset(i)), $"duplicate base offset at index {i}");
    }

    [Fact]
    public void Base_row_clears_deepest_utility_ship_row()
    {
        RaceVisualSchema.Load();
        int deepestUtilityIndex = FleetGalleryLayout.AllShipIds.Length - 1;
        Vector3 utilityPos = FleetGalleryLayout.ShipOffset(deepestUtilityIndex);
        float utilityRadius = FleetGalleryLayout.GalleryBoundingRadius(
            FleetGalleryLayout.AllShipIds[deepestUtilityIndex]);

        for (int i = 0; i < FleetGalleryLayout.AllBaseIds.Length; i++)
        {
            Vector3 basePos = FleetGalleryLayout.BaseOffset(i);
            float dx = utilityPos.X - basePos.X;
            float dz = utilityPos.Z - basePos.Z;
            float dist = MathF.Sqrt(dx * dx + dz * dz);
            Assert.True(dist >= utilityRadius + 8f,
                $"base {FleetGalleryLayout.AllBaseIds[i]} overlaps utility row (gap {dist:0.##})");
        }
    }

    [Fact]
    public void ZoneAnchors_cover_eight_races()
    {
        Assert.Equal(8, FleetGalleryLayout.ZoneAnchors.Length);
        Assert.Equal("terran", FleetGalleryLayout.RaceForZone(0));
        Assert.Equal("cryo", FleetGalleryLayout.RaceForZone(7));
    }

    [Fact]
    public void PlayerIdForZone_maps_one_to_one_with_races()
    {
        for (int zone = 0; zone < 8; zone++)
            Assert.Equal(zone + 1, FleetGalleryLayout.PlayerIdForZone(zone));
    }

    [Fact]
    public void Fleet_gallery_all_meshes_build_without_throw()
    {
        RaceVisualSchema.Load();
        foreach (string raceId in RaceTextureIndex.AllRaceIds)
        {
            foreach (string shipId in FleetGalleryLayout.AllShipIds)
            {
                float[] mesh = RaceShipMeshes.BuildForDefinition(shipId, raceId);
                Assert.True(mesh.Length >= 36, $"{raceId}/{shipId}");
            }

            foreach (string baseId in FleetGalleryLayout.AllBaseIds)
            {
                float[] mesh = RaceBuildingMeshes.Build(baseId, raceId);
                Assert.True(mesh.Length >= 36, $"{raceId}/{baseId}");
            }
        }
    }

    [Fact]
    public void ShipOffsets_are_unique_for_full_roster()
    {
        var offsets = new HashSet<Vector3>();
        for (int i = 0; i < FleetGalleryLayout.AllShipIds.Length; i++)
            Assert.True(offsets.Add(FleetGalleryLayout.ShipOffset(i)), $"duplicate offset at index {i}");
    }

    [Fact]
    public void Small_craft_remain_separated_from_capital_hulls()
    {
        RaceVisualSchema.Load();

        float minSeparation = FleetGalleryLayout.MinSmallCraftToCapitalSeparation();
        const float minimumCenterGap = 18f;
        Assert.True(minSeparation >= minimumCenterGap,
            $"small-to-capital center gap {minSeparation:0.##} < {minimumCenterGap}");
    }

    [Fact]
    public void ShipOffsets_respect_bounding_radius_clearance()
    {
        RaceVisualSchema.Load();
        const float shipScaleMultiplier = 3.5f;

        for (int a = 0; a < FleetGalleryLayout.AllShipIds.Length; a++)
        {
            for (int b = a + 1; b < FleetGalleryLayout.AllShipIds.Length; b++)
            {
                Assert.False(
                    FleetGalleryLayout.PositionsOverlap(a, b, shipScaleMultiplier),
                    $"overlap between {FleetGalleryLayout.AllShipIds[a]} ({a}) and {FleetGalleryLayout.AllShipIds[b]} ({b})");
            }
        }
    }

    [Fact]
    public void Capital_row_uses_wider_column_pitch_than_small_craft_row()
    {
        Assert.True(FleetGalleryLayout.ColumnSpacing(2) > FleetGalleryLayout.ColumnSpacing(0));
        Assert.True(FleetGalleryLayout.RowZ(2) > FleetGalleryLayout.RowZ(1) + FleetGalleryLayout.CapitalRowLead);
    }
}