using SharpOpenGl.Engine.Combat;

namespace SharpOpenGl.Engine.ECS;

/// <summary>Engagement distance tier applied by hull weapon skill.</summary>
public enum WeaponRangeTier
{
    Short,
    Medium,
    Long,
}

/// <summary>
/// Entity-level combat modifiers derived from <c>components.weaponSkill</c> in GameData.
/// One per armed military hull; absent on engineering/political hulls (CombatSystem uses baseline).
/// </summary>
public sealed class WeaponSkillComponent
{
    /// <summary>Additive damage multiplier on hit (e.g. 0.10 → +10% effective damage).</summary>
    public float AccuracyBonus { get; set; }

    /// <summary>Fire-rate divisor; values below 1 increase fire rate, 1.0 is baseline.</summary>
    public float ReloadModifier { get; set; } = 1f;

    /// <summary>Range tier mapped to a multiplier via <see cref="WeaponSkillProfiles"/>.</summary>
    public WeaponRangeTier RangeTier { get; set; } = WeaponRangeTier.Medium;

    /// <summary>World-unit attack range after tier scaling.</summary>
    public float EffectiveRange(float baseRange)
        => baseRange * WeaponSkillProfiles.RangeMultiplier(RangeTier);

    /// <summary>Shots per second after reload modifier (higher = faster firing).</summary>
    public float EffectiveFireRate(float baseFireRate)
        => baseFireRate > 0f && ReloadModifier > 0f
            ? baseFireRate / ReloadModifier
            : baseFireRate;

    /// <summary>Damage per hit after accuracy bonus.</summary>
    public float EffectiveDamage(float baseDamage)
        => baseDamage * (1f + AccuracyBonus);
}