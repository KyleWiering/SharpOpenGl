using OpenTK.Mathematics;
using SharpOpenGl.Engine.Config;
using SharpOpenGl.Engine.ECS;

namespace SharpOpenGl.Engine.UI.Widgets;

/// <summary>
/// Snapshot of stats shown for one selected unit in the info panel.
/// </summary>
public sealed class UnitInfo
{
    /// <summary>Display name (ship/unit class).</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>Current HP fraction 0–1.</summary>
    public float HPFraction { get; init; }

    /// <summary>Current shield fraction 0–1.</summary>
    public float ShieldFraction { get; init; }

    /// <summary>Current HP value (display only).</summary>
    public float CurrentHP { get; init; }

    /// <summary>Max HP.</summary>
    public float MaxHP { get; init; }

    /// <summary>Current shield value.</summary>
    public float CurrentShields { get; init; }

    /// <summary>Max shields.</summary>
    public float MaxShields { get; init; }

    /// <summary>Armour value.</summary>
    public float Armor { get; init; }

    /// <summary>HUD classification for label color.</summary>
    public EntityDisplayKind DisplayKind { get; init; } = EntityDisplayKind.Friendly;

    /// <summary>Optional subtitle (resource amount, faction, etc.).</summary>
    public string Subtitle { get; init; } = string.Empty;

    /// <summary>Race-tinted shield bar color. Null when no shields.</summary>
    public Vector4? ShieldBarColor { get; init; }

    /// <summary>Harvest mode label for miners (Drones, EVA, Tractor Beam).</summary>
    public string HarvestMode { get; init; } = string.Empty;

    /// <summary>Current cargo amount for resource collectors.</summary>
    public float CargoAmount { get; init; }

    /// <summary>Maximum cargo capacity for resource collectors.</summary>
    public float CargoCapacity { get; init; }

    /// <summary>Cargo fill fraction 0–1.</summary>
    public float CargoFraction =>
        CargoCapacity > 0f ? Math.Clamp(CargoAmount / CargoCapacity, 0f, 1f) : 0f;

    /// <summary>Build a <see cref="UnitInfo"/> from a <see cref="HealthComponent"/> and entity name.</summary>
    public static UnitInfo FromHealth(
        string name,
        HealthComponent health,
        EntityDisplayKind kind = EntityDisplayKind.Friendly,
        string? raceId = null) =>
        new()
        {
            Name = name,
            HPFraction = health.HPFraction,
            ShieldFraction = health.ShieldFraction,
            CurrentHP = health.CurrentHP,
            MaxHP = health.MaxHP,
            CurrentShields = health.CurrentShields,
            MaxShields = health.MaxShields,
            Armor = health.Armor,
            DisplayKind = kind,
            ShieldBarColor = health.MaxShields > 0f && !string.IsNullOrEmpty(raceId)
                ? RaceShieldSchema.ResolveShieldTint(raceId)
                : null,
        };
}