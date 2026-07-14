using OpenTK.Mathematics;
using SharpOpenGl.Engine.ECS;
using SharpOpenGl.Engine.Rendering;

namespace SharpOpenGl.Engine.Entities;

/// <summary>
/// Static fallback pivot/limit table for special-hull articulated parts (mirrors JSON from wo-sh-01-01).
/// Used when <see cref="EntityDefinition.Articulation"/> is absent or lacks special part entries.
/// </summary>
internal readonly record struct SpecialHullPartDef(
    string Id,
    ArticulatedPartType PartType,
    Vector3 LocalPivotOffset,
    Vector3 MeshLocalOffset,
    float YawMin,
    float YawMax,
    float PitchMin,
    float PitchMax,
    string MeshKey,
    bool IdleSweepEnabled,
    float IdleSweepSpeed,
    float SlewRate);

/// <summary>Per-hull special articulation definitions for factory-time spawn fallback.</summary>
internal static class SpecialHullArticulationDefs
{
    private const float DefaultSlewRate = 60f;
    private const float SensorSlewRate = 90f;
    private const float SensorIdleSweepSpeed = 25f;

    private static readonly HashSet<string> TargetHullIds = new(StringComparer.OrdinalIgnoreCase)
    {
        "bomber_heavy",
        "carrier_command",
        "drone_swarm",
        "scout_light",
        "fighter_basic",
        "interceptor_mk2",
    };

    private static readonly Dictionary<string, SpecialHullPartDef[]> Table =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["bomber_heavy"] =
            [
                new(
                    "bay_port",
                    ArticulatedPartType.BayDoor,
                    new Vector3(-0.35f, 0.15f, 0.25f),
                    Vector3.Zero,
                    0f, 0f, 0f, 90f,
                    ArticulatedShipPartMeshes.BuildBayDoorKey("bomber_heavy", port: true),
                    IdleSweepEnabled: false,
                    IdleSweepSpeed: 0f,
                    DefaultSlewRate),
                new(
                    "bay_starboard",
                    ArticulatedPartType.BayDoor,
                    new Vector3(0.35f, 0.15f, 0.25f),
                    Vector3.Zero,
                    0f, 0f, 0f, 90f,
                    ArticulatedShipPartMeshes.BuildBayDoorKey("bomber_heavy", port: false),
                    IdleSweepEnabled: false,
                    IdleSweepSpeed: 0f,
                    DefaultSlewRate),
            ],
            ["carrier_command"] =
            [
                new(
                    "deck_elevator",
                    ArticulatedPartType.BayDoor,
                    new Vector3(0f, 0.2f, -0.8f),
                    Vector3.Zero,
                    0f, 0f, 0f, 60f,
                    ArticulatedShipPartMeshes.BuildPartKey("deck_segment", "carrier_command"),
                    IdleSweepEnabled: false,
                    IdleSweepSpeed: 0f,
                    DefaultSlewRate),
            ],
            ["drone_swarm"] =
            [
                new(
                    "launcher_pod",
                    ArticulatedPartType.LauncherPod,
                    new Vector3(0f, 0.1f, -0.35f),
                    Vector3.Zero,
                    -90f, 90f, 0f, 0f,
                    ArticulatedShipPartMeshes.BuildPartKey("launcher_pod", "drone_swarm"),
                    IdleSweepEnabled: false,
                    IdleSweepSpeed: 0f,
                    DefaultSlewRate),
            ],
            ["scout_light"] =
            [
                new(
                    "sensor_dish",
                    ArticulatedPartType.SensorDish,
                    new Vector3(0f, 0.35f, 0.45f),
                    Vector3.Zero,
                    -120f, 120f, -15f, 45f,
                    ArticulatedShipPartMeshes.BuildPartKey("sensor_dish", "scout_light"),
                    IdleSweepEnabled: true,
                    IdleSweepSpeed: SensorIdleSweepSpeed,
                    SensorSlewRate),
            ],
            ["fighter_basic"] =
            [
                new(
                    "wing_flap_left",
                    ArticulatedPartType.EngineGimbal,
                    new Vector3(-0.42f, 0.05f, -0.15f),
                    Vector3.Zero,
                    0f, 0f, -5f, 25f,
                    ArticulatedShipPartMeshes.BuildWingFlapKey("fighter_basic", left: true),
                    IdleSweepEnabled: false,
                    IdleSweepSpeed: 0f,
                    DefaultSlewRate),
                new(
                    "wing_flap_right",
                    ArticulatedPartType.EngineGimbal,
                    new Vector3(0.42f, 0.05f, -0.15f),
                    Vector3.Zero,
                    0f, 0f, -5f, 25f,
                    ArticulatedShipPartMeshes.BuildWingFlapKey("fighter_basic", left: false),
                    IdleSweepEnabled: false,
                    IdleSweepSpeed: 0f,
                    DefaultSlewRate),
            ],
            ["interceptor_mk2"] =
            [
                new(
                    "wing_flap_left",
                    ArticulatedPartType.EngineGimbal,
                    new Vector3(-0.45f, 0.06f, -0.12f),
                    Vector3.Zero,
                    0f, 0f, -8f, 30f,
                    ArticulatedShipPartMeshes.BuildWingFlapKey("interceptor_mk2", left: true),
                    IdleSweepEnabled: false,
                    IdleSweepSpeed: 0f,
                    DefaultSlewRate),
                new(
                    "wing_flap_right",
                    ArticulatedPartType.EngineGimbal,
                    new Vector3(0.45f, 0.06f, -0.12f),
                    Vector3.Zero,
                    0f, 0f, -8f, 30f,
                    ArticulatedShipPartMeshes.BuildWingFlapKey("interceptor_mk2", left: false),
                    IdleSweepEnabled: false,
                    IdleSweepSpeed: 0f,
                    DefaultSlewRate),
            ],
        };

    internal static bool IsTargetHull(string definitionId) =>
        TargetHullIds.Contains(definitionId);

    internal static bool TryGetParts(string hullKey, out SpecialHullPartDef[] parts) =>
        Table.TryGetValue(NormalizeHullKey(hullKey), out parts!);

    internal static Vector4 ResolveAccentTint(string hullKey) => NormalizeHullKey(hullKey) switch
    {
        "bomber_heavy" => new Vector4(0.95f, 0.55f, 0.22f, 1f),
        "carrier_command" => new Vector4(0.42f, 0.72f, 0.95f, 1f),
        "drone_swarm" => new Vector4(0.55f, 0.88f, 0.42f, 1f),
        "scout_light" => new Vector4(0.72f, 0.82f, 0.95f, 1f),
        "fighter_basic" => new Vector4(0.88f, 0.32f, 0.28f, 1f),
        "interceptor_mk2" => new Vector4(0.92f, 0.38f, 0.32f, 1f),
        _ => new Vector4(0.72f, 0.78f, 0.88f, 1f),
    };

    private static string NormalizeHullKey(string hullKey) => hullKey.ToLowerInvariant();
}