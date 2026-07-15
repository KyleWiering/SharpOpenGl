using OpenTK.Mathematics;
using SharpOpenGl.Engine.Rendering;

namespace SharpOpenGl.Engine.Entities;

/// <summary>
/// Per-hull turret pivot offsets and angle limits for armed ship spawn articulation.
/// </summary>
internal readonly record struct ShipTurretDef(
    Vector3 HullPivotOffset,
    Vector3 PitchPivotOnYaw,
    Vector3 YawMeshOffset,
    Vector3 PitchMeshOffset,
    float YawMin,
    float YawMax,
    float PitchMin,
    float PitchMax);

/// <summary>
/// Static lookup for ship turret articulation on weapon-bearing corvette/gunship/destroyer hulls.
/// Fallback only — prefer JSON <c>articulation</c> blocks in <c>GameData/Ships/*.json</c>.
/// </summary>
[Obsolete("Fallback only — prefer JSON articulation on EntityDefinition.")]
internal static class ShipTurretArticulationDefs
{
    private const float DefaultYawMin = -120f;
    private const float DefaultYawMax = 120f;
    private const float DefaultPitchMin = -10f;
    private const float DefaultPitchMax = 45f;

    private static readonly Dictionary<string, ShipTurretDef> Table =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["corvette_fast"] = new(
                new Vector3(0f, 0.4f, 0.6f),
                new Vector3(0f, 0.15f, 0.2f),
                Vector3.Zero,
                Vector3.Zero,
                DefaultYawMin,
                DefaultYawMax,
                DefaultPitchMin,
                DefaultPitchMax),
            ["gunship_heavy"] = new(
                new Vector3(0f, 0.35f, 0.9f),
                new Vector3(0f, 0.2f, 0.25f),
                Vector3.Zero,
                Vector3.Zero,
                DefaultYawMin,
                DefaultYawMax,
                DefaultPitchMin,
                DefaultPitchMax),
            ["destroyer_assault"] = new(
                new Vector3(0f, 0.5f, 1.1f),
                new Vector3(0f, 0.25f, 0.3f),
                Vector3.Zero,
                Vector3.Zero,
                DefaultYawMin,
                DefaultYawMax,
                DefaultPitchMin,
                DefaultPitchMax),
        };

    internal static string ResolveHullKey(EntityDefinition def)
    {
        if (!string.IsNullOrWhiteSpace(def.Id))
            return NormalizeHullKey(def.Id);

        return NormalizeHullKey(def.Category);
    }

    internal static bool TryGet(string hullOrId, out ShipTurretDef def)
    {
        string key = NormalizeHullKey(hullOrId);
        return Table.TryGetValue(key, out def);
    }

    /// <summary>
    /// Builds an <see cref="ArticulationDefinition"/> equivalent to the static turret table entry.
    /// </summary>
    internal static ArticulationDefinition? TryToArticulationDefinition(string hullKey)
    {
        string normalized = NormalizeHullKey(hullKey);
        if (!TryGet(normalized, out ShipTurretDef turretDef))
            return null;

        return new ArticulationDefinition
        {
            Parts =
            [
                new ArticulationPartDefinition
                {
                    Id = "turret_yaw",
                    PartType = "TurretYaw",
                    LocalPivot =
                    [
                        turretDef.HullPivotOffset.X,
                        turretDef.HullPivotOffset.Y,
                        turretDef.HullPivotOffset.Z,
                    ],
                    YawMin = turretDef.YawMin,
                    YawMax = turretDef.YawMax,
                    PitchMin = turretDef.PitchMin,
                    PitchMax = turretDef.PitchMax,
                    IdleSweep = true,
                    IdleSweepSpeed = 8f,
                    SlewRate = 90f,
                    MeshKey = ArticulatedShipPartMeshes.BuildPartKey("turret_yaw", normalized),
                },
                new ArticulationPartDefinition
                {
                    Id = "turret_pitch",
                    PartType = "TurretPitch",
                    OwnerPartId = "turret_yaw",
                    LocalPivot =
                    [
                        turretDef.PitchPivotOnYaw.X,
                        turretDef.PitchPivotOnYaw.Y,
                        turretDef.PitchPivotOnYaw.Z,
                    ],
                    YawMin = turretDef.YawMin,
                    YawMax = turretDef.YawMax,
                    PitchMin = turretDef.PitchMin,
                    PitchMax = turretDef.PitchMax,
                    SlewRate = 90f,
                    MeshKey = ArticulatedShipPartMeshes.BuildPartKey("turret_pitch", normalized),
                },
            ],
        };
    }

    private static string NormalizeHullKey(string hullKey) => hullKey.ToLowerInvariant() switch
    {
        "corvette" => "corvette_fast",
        "gunship" => "gunship_heavy",
        "destroyer" => "destroyer_assault",
        _ => hullKey,
    };
}