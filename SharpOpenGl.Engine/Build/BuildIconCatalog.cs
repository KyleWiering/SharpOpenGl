using OpenTK.Mathematics;

namespace SharpOpenGl.Engine.Build;

/// <summary>Procedural silhouette family for build-menu thumbnails.</summary>
public enum BuildIconShape
{
    CommandCenter,
    Shipyard,
    Reactor,
    Turret,
    Sensor,
    Depot,
    Capstone,
}

/// <summary>Color and shape metadata for a structure build icon.</summary>
public readonly struct BuildIconDescriptor
{
    public Vector4 PrimaryTint { get; init; }
    public Vector4 AccentTint { get; init; }
    public BuildIconShape Shape { get; init; }
    public string BuildingType { get; init; }
}

/// <summary>
/// Maps structure ids to procedural icon descriptors for the build-map UI.
/// Icons use category-inspired tints without requiring texture assets.
/// </summary>
public static class BuildIconCatalog
{
    private static readonly Dictionary<string, BuildIconDescriptor> Icons =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["command_center"] = Desc("command_center", BuildIconShape.CommandCenter,
                new Vector4(0.32f, 0.38f, 0.52f, 1f), new Vector4(0.55f, 0.68f, 0.88f, 1f)),

            ["shipyard_small"] = Desc("shipyard_small", BuildIconShape.Shipyard,
                new Vector4(0.28f, 0.36f, 0.48f, 1f), new Vector4(0.42f, 0.72f, 0.95f, 1f)),
            ["shipyard_medium"] = Desc("shipyard_medium", BuildIconShape.Shipyard,
                new Vector4(0.24f, 0.34f, 0.50f, 1f), new Vector4(0.38f, 0.66f, 0.90f, 1f)),
            ["shipyard_large"] = Desc("shipyard_large", BuildIconShape.Shipyard,
                new Vector4(0.20f, 0.30f, 0.46f, 1f), new Vector4(0.34f, 0.60f, 0.86f, 1f)),

            ["power_reactor"] = Desc("power_reactor", BuildIconShape.Reactor,
                new Vector4(0.18f, 0.42f, 0.36f, 1f), new Vector4(0.35f, 0.95f, 0.72f, 1f)),
            ["resource_refinery"] = Desc("resource_refinery", BuildIconShape.Depot,
                new Vector4(0.46f, 0.34f, 0.22f, 1f), new Vector4(0.92f, 0.62f, 0.28f, 1f)),
            ["supply_depot"] = Desc("supply_depot", BuildIconShape.Depot,
                new Vector4(0.38f, 0.40f, 0.28f, 1f), new Vector4(0.82f, 0.78f, 0.42f, 1f)),
            ["fabrication_hub"] = Desc("fabrication_hub", BuildIconShape.Depot,
                new Vector4(0.42f, 0.30f, 0.26f, 1f), new Vector4(0.88f, 0.55f, 0.32f, 1f)),

            ["sensor_array"] = Desc("sensor_array", BuildIconShape.Sensor,
                new Vector4(0.22f, 0.34f, 0.52f, 1f), new Vector4(0.45f, 0.78f, 1.00f, 1f)),
            ["defense_turret"] = Desc("defense_turret", BuildIconShape.Turret,
                new Vector4(0.42f, 0.28f, 0.26f, 1f), new Vector4(0.88f, 0.38f, 0.32f, 1f)),
            ["shield_emitter"] = Desc("shield_emitter", BuildIconShape.Reactor,
                new Vector4(0.20f, 0.32f, 0.58f, 1f), new Vector4(0.45f, 0.72f, 1.00f, 1f)),
            ["missile_battery"] = Desc("missile_battery", BuildIconShape.Turret,
                new Vector4(0.36f, 0.26f, 0.24f, 1f), new Vector4(0.78f, 0.48f, 0.28f, 1f)),

            ["repair_bay"] = Desc("repair_bay", BuildIconShape.Shipyard,
                new Vector4(0.26f, 0.40f, 0.38f, 1f), new Vector4(0.42f, 0.88f, 0.62f, 1f)),
            ["comms_relay"] = Desc("comms_relay", BuildIconShape.Sensor,
                new Vector4(0.30f, 0.28f, 0.48f, 1f), new Vector4(0.72f, 0.55f, 0.95f, 1f)),

            ["orbital_uplink"] = Desc("orbital_uplink", BuildIconShape.Capstone,
                new Vector4(0.38f, 0.32f, 0.52f, 1f), new Vector4(0.95f, 0.82f, 0.38f, 1f)),
            ["fortress_core"] = Desc("fortress_core", BuildIconShape.Capstone,
                new Vector4(0.34f, 0.26f, 0.30f, 1f), new Vector4(0.92f, 0.55f, 0.35f, 1f)),
        };

    private static readonly BuildIconDescriptor Fallback = Desc("command_center", BuildIconShape.CommandCenter,
        new Vector4(0.32f, 0.38f, 0.52f, 1f), new Vector4(0.55f, 0.68f, 0.88f, 1f));

    private static readonly HashSet<string> LoggedUnknownIds = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>All structure ids with catalog entries.</summary>
    public static IReadOnlyCollection<string> KnownBuildingIds => Icons.Keys;

    /// <summary>Resolve an icon descriptor, falling back to command center for unknown ids.</summary>
    public static BuildIconDescriptor Get(string buildingId)
    {
        TryGetIcon(buildingId, out BuildIconDescriptor icon);
        return icon;
    }

    /// <summary>Case-insensitive lookup with command-center fallback for unknown ids.</summary>
    public static bool TryGetIcon(string buildingId, out BuildIconDescriptor icon)
    {
        if (!string.IsNullOrWhiteSpace(buildingId) && Icons.TryGetValue(buildingId, out icon))
            return true;

        icon = Fallback with { BuildingType = buildingId ?? string.Empty };

        if (!string.IsNullOrWhiteSpace(buildingId) && LoggedUnknownIds.Add(buildingId))
            Console.WriteLine($"[BuildIconCatalog] Unknown building id '{buildingId}' — using CommandCenter fallback.");

        return false;
    }

    private static BuildIconDescriptor Desc(string buildingType, BuildIconShape shape, Vector4 primary, Vector4 accent) =>
        new()
        {
            BuildingType = buildingType,
            Shape = shape,
            PrimaryTint = primary,
            AccentTint = accent,
        };
}