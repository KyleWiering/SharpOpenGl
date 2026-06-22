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
        Assert.Equal(10, FleetGalleryLayout.AllBaseIds.Length);
        Assert.Contains("command_center", FleetGalleryLayout.AllBaseIds);
        Assert.Contains("shipyard_large", FleetGalleryLayout.AllBaseIds);
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
}