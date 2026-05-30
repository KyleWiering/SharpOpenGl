using SharpOpenGl.Engine.Events;

namespace SharpOpenGl.Engine.ECS;

/// <summary>
/// Ticks cooldowns on all <see cref="AbilityListComponent"/> entities each frame
/// and processes ability activations requested via <see cref="ActivateAbility"/>.
/// </summary>
/// <remarks>
/// Ability types implemented:
/// <list type="bullet">
///   <item><b>shield_boost</b> — instantly restores 50 % of the hero's max shields.</item>
///   <item><b>emp_burst</b>    — disables the hero's target for 3 s (clears its current target).</item>
///   <item>Any other ability key — no-op effect placeholder (still triggers cooldown).</item>
/// </list>
/// </remarks>
public sealed class AbilitySystem : GameSystem
{
    private readonly EventBus _bus;

    public AbilitySystem(EventBus bus) => _bus = bus;

    // Pending activation requests queued externally (e.g. from input handler).
    private readonly Queue<(Entity caster, int slot)> _pending = new();

    /// <summary>
    /// Request an ability activation for <paramref name="caster"/> on slot <paramref name="slot"/>.
    /// The activation is processed on the next <see cref="Update"/> call.
    /// </summary>
    public void ActivateAbility(Entity caster, int slot) =>
        _pending.Enqueue((caster, slot));

    /// <inheritdoc/>
    public override void Update(World world, float deltaTime)
    {
        TickCooldowns(world, deltaTime);
        ProcessPending(world);
    }

    // ── Cooldown tick ─────────────────────────────────────────────────────────

    private static void TickCooldowns(World world, float deltaTime)
    {
        foreach (var (_, al) in world.Query<AbilityListComponent>())
        {
            foreach (var ability in al.Abilities)
            {
                if (ability.CurrentCooldown > 0f)
                    ability.CurrentCooldown = Math.Max(0f, ability.CurrentCooldown - deltaTime);
            }
        }
    }

    // ── Activation ────────────────────────────────────────────────────────────

    private void ProcessPending(World world)
    {
        while (_pending.TryDequeue(out var req))
        {
            var (caster, slot) = req;
            if (!world.IsAlive(caster)) continue;

            var al = world.GetComponent<AbilityListComponent>(caster);
            var ability = al?.GetBySlot(slot);
            if (ability == null || !ability.IsReady) continue;

            ability.Activate();
            ApplyAbilityEffect(world, caster, ability);
            _bus.Publish(new AbilityActivatedEvent(caster.Index, slot, ability.Id));
        }
    }

    // ── Effect dispatch ───────────────────────────────────────────────────────

    private static void ApplyAbilityEffect(World world, Entity caster, AbilityComponent ability)
    {
        switch (ability.Id)
        {
            case "shield_boost":
                ApplyShieldBoost(world, caster);
                break;

            case "emp_burst":
                ApplyEmpBurst(world, caster);
                break;

            // Additional ability IDs handled in future phases — placeholder no-op.
            default:
                break;
        }
    }

    // ── Ability implementations ───────────────────────────────────────────────

    /// <summary>Restores 50 % of max shields to the caster.</summary>
    private static void ApplyShieldBoost(World world, Entity caster)
    {
        var health = world.GetComponent<HealthComponent>(caster);
        if (health == null) return;

        float restore = health.MaxShields * 0.5f;
        health.CurrentShields = Math.Min(health.MaxShields, health.CurrentShields + restore);
    }

    /// <summary>
    /// Clears the current target on the caster's current enemy, interrupting its attack
    /// for the duration (simulated by resetting its <see cref="CombatTargetComponent.CurrentTarget"/>).
    /// </summary>
    private static void ApplyEmpBurst(World world, Entity caster)
    {
        var ct = world.GetComponent<CombatTargetComponent>(caster);
        if (ct == null || !world.IsAlive(ct.CurrentTarget)) return;

        var targetCt = world.GetComponent<CombatTargetComponent>(ct.CurrentTarget);
        if (targetCt != null)
            targetCt.CurrentTarget = Entity.Null;
    }
}
