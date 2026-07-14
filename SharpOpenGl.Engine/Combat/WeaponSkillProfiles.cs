using SharpOpenGl.Engine.ECS;
using SharpOpenGl.Engine.Entities;

namespace SharpOpenGl.Engine.Combat;

/// <summary>Resolves <see cref="WeaponSkillDefinition"/> JSON into runtime combat modifiers.</summary>
public static class WeaponSkillProfiles
{
    /// <summary>Baseline skill when JSON omits <c>weaponSkill</c> or the ECS component is absent.</summary>
    public static WeaponSkillComponent Baseline() => new();

    /// <summary>Builds a component from GameData; unknown tiers fall back to medium.</summary>
    public static WeaponSkillComponent Resolve(WeaponSkillDefinition? def)
    {
        if (def == null)
            return Baseline();

        float reload = def.ReloadModifier > 0f ? def.ReloadModifier : 1f;
        return new WeaponSkillComponent
        {
            AccuracyBonus = def.AccuracyBonus,
            ReloadModifier = reload,
            RangeTier = ParseTier(def.RangeTier),
        };
    }

    /// <summary>Range multiplier for each engagement tier.</summary>
    public static float RangeMultiplier(WeaponRangeTier tier) => tier switch
    {
        WeaponRangeTier.Short => 0.85f,
        WeaponRangeTier.Long => 1.15f,
        _ => 1.00f,
    };

    private static WeaponRangeTier ParseTier(string? raw) => raw?.Trim().ToLowerInvariant() switch
    {
        "short" => WeaponRangeTier.Short,
        "long" => WeaponRangeTier.Long,
        _ => WeaponRangeTier.Medium,
    };
}