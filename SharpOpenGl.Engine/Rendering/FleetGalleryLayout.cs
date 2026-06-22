using OpenTK.Mathematics;
using SharpOpenGl.Engine.Grid;

namespace SharpOpenGl.Engine.Rendering;

/// <summary>
/// Spreads all 8 race fleets and station sets across the sector in grid formations.
/// </summary>
public static class FleetGalleryLayout
{
    public const int ShipColumns = 5;
    public const float ShipSpacing = 22f;
    public const int BaseColumns = 4;
    public const float BaseSpacing = 28f;
    public const float BaseRowOffset = 130f;

    public static readonly string[] AllShipIds =
    [
        "hero_default",
        "scout_light",
        "fighter_basic",
        "interceptor_mk2",
        "drone_swarm",
        "corvette_fast",
        "frigate_strike",
        "gunship_heavy",
        "bomber_heavy",
        "destroyer_assault",
        "cruiser_heavy",
        "carrier_command",
        "dreadnought",
        "miner_basic",
        "miner_eva",
        "miner_tractor",
        "transport_cargo",
        "freighter_bulk",
        "support_repair",
    ];

    public static readonly string[] AllBaseIds =
    [
        "command_center",
        "shipyard_small",
        "shipyard_medium",
        "shipyard_large",
        "defense_turret",
        "sensor_array",
        "resource_refinery",
        "repair_bay",
        "power_reactor",
        "supply_depot",
    ];

    /// <summary>Eight zone anchors on the 200×200 grid (spread north–south bands).</summary>
    public static readonly (int gridX, int gridY)[] ZoneAnchors =
    [
        (25, 25),
        (75, 25),
        (125, 25),
        (175, 25),
        (25, 125),
        (75, 125),
        (125, 125),
        (175, 125),
    ];

    public static Vector3 ZoneWorldCenter(int zoneIndex) =>
        MapCoordinates.GridToWorld(ZoneAnchors[zoneIndex].gridX, ZoneAnchors[zoneIndex].gridY);

    public static string RaceForZone(int zoneIndex) =>
        RaceTextureIndex.AllRaceIds[zoneIndex % RaceTextureIndex.AllRaceIds.Count];

    public static int PlayerIdForZone(int zoneIndex) => zoneIndex + 1;

    public static Vector3 ShipOffset(int index) => GalleryGridOffset(index, ShipColumns, ShipSpacing);

    public static Vector3 BaseOffset(int index) =>
        GalleryGridOffset(index, BaseColumns, BaseSpacing) + new Vector3(0f, 0f, BaseRowOffset);

    public static Vector3 GalleryGridOffset(int index, int columns, float spacing)
    {
        int col = index % columns;
        int row = index / columns;
        float x = (col - (columns - 1) * 0.5f) * spacing;
        float z = row * spacing;
        return new Vector3(x, 0f, z);
    }
}