using OpenTK.Mathematics;
using SharpOpenGl.Engine.Grid;

namespace SharpOpenGl.Engine.Rendering;

/// <summary>
/// Spreads all 8 race fleets and station sets across the sector in grid formations.
/// </summary>
public static class FleetGalleryLayout
{
    public const int ShipColumns = 5;

    /// <summary>
    /// Legacy uniform pitch kept for callers that still expect a single spacing constant.
    /// Actual ship offsets use per-row <see cref="ColumnSpacing"/> / <see cref="RowDepth"/>.
    /// </summary>
    public const float ShipSpacing = 34f;

    public const int BaseColumns = 4;
    public const float BaseSpacing = 28f;

    /// <summary>Separates the station row from the deepest utility ship row (size-class-aware grid).</summary>
    public const float BaseRowOffset = 176f;

    /// <summary>Extra Z gap inserted after the medium row before the capital band (indices 9–12).</summary>
    public const float CapitalRowLead = 22f;

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
        "missile_battery",
        "fortress_core",
        "shield_emitter",
        "orbital_uplink",
        "comms_relay",
        "fabrication_hub",
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

    /// <summary>Small-craft hull indices (hero + strike craft) — tightest envelopes, row 0.</summary>
    public static readonly int[] SmallCraftIndices = [0, 1, 2, 3, 4];

    /// <summary>Capital hull indices — widest silhouettes; row 1 tail + row 2 core.</summary>
    public static readonly int[] CapitalIndices = [9, 10, 11, 12];

    public static Vector3 ZoneWorldCenter(int zoneIndex) =>
        MapCoordinates.GridToWorld(ZoneAnchors[zoneIndex].gridX, ZoneAnchors[zoneIndex].gridY);

    public static string RaceForZone(int zoneIndex) =>
        RaceTextureIndex.AllRaceIds[zoneIndex % RaceTextureIndex.AllRaceIds.Count];

    public static int PlayerIdForZone(int zoneIndex) => zoneIndex + 1;

    public static Vector3 ShipOffset(int index)
    {
        int col = index % ShipColumns;
        int row = index / ShipColumns;
        float spacing = ColumnSpacing(row);
        float x = (col - (ShipColumns - 1) * 0.5f) * spacing;
        float z = RowZ(row);
        return new Vector3(x, 0f, z);
    }

    public static Vector3 BaseOffset(int index) =>
        GalleryGridOffset(index, BaseColumns, BaseSpacing) + new Vector3(0f, 0f, BaseRowOffset);

    /// <summary>Horizontal pitch per gallery row — wider rows for larger hull envelopes at 3.5× scale.</summary>
    public static float ColumnSpacing(int row) => row switch
    {
        0 => 30f, // row 0: hero + strike craft (size ≤1.75) — tightest pitch
        1 => 36f, // row 1: corvette–destroyer band; +6f vs row 0 for 4.0-size destroyer tail
        2 => 42f, // row 2: cruiser/carrier/dreadnought; +12f for 6.3 dreadnought width ratio
        3 => 34f, // row 3: utility miners/transports — moderate bulk, narrower than capitals
        _ => ShipSpacing,
    };

    /// <summary>Vertical depth between row centers — deepest gap before capital band prevents Z bleed.</summary>
    public static float RowDepth(int row) => row switch
    {
        0 => 30f, // small-craft row: keeps indices 0–4 clear of row-1 destroyer_assault (index 9)
        1 => 36f, // medium combat row depth
        2 => 44f, // capital row: extra depth for dreadnought lengthRatio 1.3 at gallery taper 0.80
        3 => 32f, // utility row
        _ => ShipSpacing,
    };

    public static float RowZ(int row)
    {
        float z = 0f;
        for (int r = 0; r < row; r++)
        {
            z += RowDepth(r);
            if (r == 1)
                z += CapitalRowLead;
        }

        return z;
    }

    /// <summary>
    /// Gallery-only display scale taper for capitals/utility bulk so neighbors stay readable at 3.5× gameplay scale.
    /// </summary>
    public static float GalleryScaleMultiplier(string shipId)
    {
        HullClassProfile profile = RaceVisualSchema.ResolveHullProfile(shipId);
        return profile.Size switch
        {
            >= 6f => 0.80f,
            >= 4.5f => 0.84f,
            >= 4f => 0.88f,
            _ => 1f,
        };
    }

    /// <summary>Conservative XZ bounding radius for layout overlap checks (half-extent × scale × gallery taper).</summary>
    public static float GalleryBoundingRadius(string shipId, float shipScaleMultiplier = 3.5f)
    {
        HullClassProfile profile = RaceVisualSchema.ResolveHullProfile(shipId);
        float hullExtent = profile.Size * MathF.Max(profile.LengthRatio, profile.WidthRatio);
        return hullExtent * shipScaleMultiplier * GalleryScaleMultiplier(shipId) * 0.5f;
    }

    public static float PlanarDistance(int indexA, int indexB)
    {
        Vector3 a = ShipOffset(indexA);
        Vector3 b = ShipOffset(indexB);
        float dx = a.X - b.X;
        float dz = a.Z - b.Z;
        return MathF.Sqrt(dx * dx + dz * dz);
    }

    public static bool PositionsOverlap(int indexA, int indexB, float shipScaleMultiplier = 3.5f, float clearance = 1.5f)
    {
        float dist = PlanarDistance(indexA, indexB);
        float ra = GalleryBoundingRadius(AllShipIds[indexA], shipScaleMultiplier);
        float rb = GalleryBoundingRadius(AllShipIds[indexB], shipScaleMultiplier);
        return dist < ra + rb + clearance;
    }

    public static float MinSmallCraftToCapitalSeparation()
    {
        float min = float.MaxValue;
        foreach (int small in SmallCraftIndices)
        {
            foreach (int capital in CapitalIndices)
                min = MathF.Min(min, PlanarDistance(small, capital));
        }

        return min;
    }

    /// <summary>XZ centroid of the full 19-hull grid — used for zone-0 proof screenshots.</summary>
    public static Vector3 ZoneScreenshotCameraOffset()
    {
        Vector3 min = ShipOffset(0);
        Vector3 max = min;
        for (int i = 1; i < AllShipIds.Length; i++)
        {
            Vector3 p = ShipOffset(i);
            min = Vector3.ComponentMin(min, p);
            max = Vector3.ComponentMax(max, p);
        }

        return (min + max) * 0.5f;
    }

    /// <summary>RTS camera height that frames rows 0–3 without clipping capitals at 3.5× scale.</summary>
    public const float ZoneScreenshotCameraHeight = 132f;

    /// <summary>Medium combat row indices (corvette_fast … destroyer_assault) for Round 2 articulation captures.</summary>
    public const int MediumCombatRowFirstIndex = 5;

    /// <summary>Last medium combat hull index before the capital band.</summary>
    public const int MediumCombatRowLastIndex = 9;

    /// <summary>RTS camera height for Round 2 articulation gallery — tighter than zone-wide overview.</summary>
    public const float Round2ArticulationScreenshotCameraHeight = 100f;

    /// <summary>XZ centroid of zone-0 medium combat row + front station turret row.</summary>
    public static Vector3 Round2ArticulationScreenshotCameraOffset()
    {
        Vector3 shipCenter = (ShipOffset(MediumCombatRowFirstIndex) + ShipOffset(MediumCombatRowLastIndex)) * 0.5f;
        Vector3 baseCenter = (BaseOffset(0) + BaseOffset(BaseColumns - 1)) * 0.5f;
        return (shipCenter + baseCenter) * 0.5f;
    }

    public static Vector3 GalleryGridOffset(int index, int columns, float spacing)
    {
        int col = index % columns;
        int row = index / columns;
        float x = (col - (columns - 1) * 0.5f) * spacing;
        float z = row * spacing;
        return new Vector3(x, 0f, z);
    }
}