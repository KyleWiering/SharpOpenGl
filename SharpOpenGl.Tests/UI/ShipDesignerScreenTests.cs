using OpenTK.Mathematics;
using SharpOpenGl.Engine.Rendering;
using SharpOpenGl.Engine.UI;
using SharpOpenGl.Engine.UI.Screens;
using Xunit;

namespace SharpOpenGl.Tests.UI;

public class ShipDesignerScreenTests
{
    private static readonly Vector2 Viewport = UIScaler.ReferenceSize;

    [Fact]
    public void Default_mesh_key_resolves_ship_for_default_race()
    {
        var screen = new ShipDesignerScreen();

        Assert.Equal(MeshManifest.ShipKey(RaceShipMeshes.DefaultRace, "fighter_basic"), screen.MeshKey);
    }

    [Fact]
    public void CycleRace_updates_race_and_mesh_key()
    {
        var screen = new ShipDesignerScreen();
        screen.LoadShip("fighter_basic", "terran");

        screen.CycleRace();

        Assert.NotEqual("terran", screen.RaceId);
        Assert.Equal(MeshManifest.ShipKey(screen.RaceId, "fighter_basic"), screen.MeshKey);
    }

    [Fact]
    public void CycleModel_steps_through_ship_roster()
    {
        var screen = new ShipDesignerScreen();
        screen.LoadShip(FleetGalleryLayout.AllShipIds[0], "terran");

        screen.CycleModel();

        Assert.Equal(FleetGalleryLayout.AllShipIds[1], screen.ShipId);
    }

    [Fact]
    public void CycleModel_requests_preview_refresh()
    {
        var screen = new ShipDesignerScreen();
        screen.LoadShip("fighter_basic", "terran");
        screen.NotifyPreviewMeshReady(true);

        int previewRequests = 0;
        screen.PreviewRequested += () => previewRequests++;

        screen.CycleModel();

        Assert.Equal(1, previewRequests);
        Assert.False(screen.PreviewMeshReady);
    }

    [Fact]
    public void ToggleCategory_switches_to_station_mesh_key()
    {
        var screen = new ShipDesignerScreen();
        screen.LoadShip("fighter_basic", "korath");

        screen.ToggleCategory();

        Assert.Equal(DesignerAssetCategory.Station, screen.Category);
        Assert.Equal(FleetGalleryLayout.AllBaseIds[0], screen.ShipId);
        Assert.Equal(MeshManifest.StationKey("korath", screen.ShipId), screen.MeshKey);
    }

    [Fact]
    public void Race_and_model_picker_buttons_cycle_selection()
    {
        var screen = new ShipDesignerScreen();
        screen.LoadShip("fighter_basic", "terran");
        string raceBefore = screen.RaceId;
        string shipBefore = screen.ShipId;

        bool racePrev = screen.HandlePointerTapped(new Vector2(1152f, 74f), 0, Viewport);
        Assert.True(racePrev);
        Assert.NotEqual(raceBefore, screen.RaceId);

        bool modelNext = screen.HandlePointerTapped(new Vector2(1432f, 126f), 0, Viewport);
        Assert.True(modelNext);
        Assert.NotEqual(shipBefore, screen.ShipId);
    }

    [Fact]
    public void Category_toggle_button_switches_roster()
    {
        var screen = new ShipDesignerScreen();
        screen.LoadShip("fighter_basic", "terran");

        bool consumed = screen.HandlePointerTapped(new Vector2(1312f, 220f), 0, Viewport);

        Assert.True(consumed);
        Assert.Equal(DesignerAssetCategory.Station, screen.Category);
    }

    [Fact]
    public void Load_model_button_requests_preview()
    {
        var screen = new ShipDesignerScreen();
        screen.LoadShip("fighter_basic", "terran");

        int previewRequests = 0;
        screen.PreviewRequested += () => previewRequests++;

        bool consumed = screen.HandlePointerTapped(new Vector2(1312f, 172f), 0, Viewport);

        Assert.True(consumed);
        Assert.Equal(1, previewRequests);
        Assert.False(screen.PreviewMeshReady);
    }

    [Fact]
    public void NotifyPreviewMeshReady_updates_preview_state()
    {
        var screen = new ShipDesignerScreen();
        screen.LoadShip("cruiser_heavy", "korath");

        screen.NotifyPreviewMeshReady(true);

        Assert.True(screen.PreviewMeshReady);
    }
}