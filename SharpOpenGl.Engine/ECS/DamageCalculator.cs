namespace SharpOpenGl.Engine.ECS;

/// <summary>
/// Pure-static helper that applies the game's damage formula.
///
/// Formula (from balance.json):
///   <c>finalDamage = baseDamage × (100 / (100 + armor)) − shieldAbsorb</c>
///
/// Shields absorb damage first. Whatever pierces shields is then reduced by armor.
/// A minimum of 1 damage is always applied when an attack lands.
/// </summary>
public static class DamageCalculator
{
    /// <summary>
    /// Minimum damage per hit, regardless of armor and shields.
    /// </summary>
    public const float MinimumDamage = 1f;

    /// <summary>
    /// Compute the final damage value and apply it to <paramref name="health"/>.
    /// </summary>
    /// <param name="baseDamage">Raw damage from the weapon before mitigation.</param>
    /// <param name="health">Target's health component (mutated in-place).</param>
    /// <returns>The final damage value that was applied to HP.</returns>
    public static float Apply(float baseDamage, HealthComponent health)
    {
        if (health.IsDead) return 0f;

        float remaining = baseDamage;

        // 1. Shields absorb damage first.
        if (health.CurrentShields > 0f)
        {
            float absorbed = Math.Min(health.CurrentShields, remaining);
            health.CurrentShields -= absorbed;
            remaining -= absorbed;
        }

        if (remaining <= 0f) return 0f;

        // 2. Armor reduces whatever got through shields.
        float armorReduced = remaining * (100f / (100f + health.Armor));
        float finalDamage  = Math.Max(MinimumDamage, armorReduced);

        health.CurrentHP = Math.Max(0f, health.CurrentHP - finalDamage);
        return finalDamage;
    }

    /// <summary>
    /// Calculate how much damage would be dealt without mutating any component.
    /// Useful for targeting AI and preview UI.
    /// </summary>
    /// <param name="baseDamage">Raw weapon damage.</param>
    /// <param name="armor">Target armor value.</param>
    /// <param name="currentShields">Current shield strength of the target.</param>
    /// <returns>Predicted final HP damage.</returns>
    public static float Preview(float baseDamage, float armor, float currentShields)
    {
        float remaining = baseDamage - Math.Min(currentShields, baseDamage);
        if (remaining <= 0f) return 0f;
        return Math.Max(MinimumDamage, remaining * (100f / (100f + armor)));
    }
}
