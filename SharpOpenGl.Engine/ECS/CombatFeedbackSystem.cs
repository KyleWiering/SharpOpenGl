using OpenTK.Mathematics;
using SharpOpenGl.Engine.Config;
using SharpOpenGl.Engine.Events;
using SharpOpenGl.Engine.Rendering;

namespace SharpOpenGl.Engine.ECS;

/// <summary>
/// Applies brief hit flashes on damaged entities and race-ultimate cast pulses via
/// <see cref="RenderComponent"/> color or <see cref="RenderComponent.TeamTint"/> — no geometry rebuild.
/// </summary>
public sealed class CombatFeedbackSystem : GameSystem
{
    /// <summary>Fraction of max HP that qualifies as a heavy hit for HUD pulse.</summary>
    public const float HeavyHitThresholdFraction = 0.15f;

    /// <summary>Minimum final damage to trigger HP bar pulse regardless of max HP.</summary>
    public const float HeavyHitMinDamage = 20f;

    private readonly EventBus _bus;
    private World? _world;

    public CombatFeedbackSystem(EventBus bus)
    {
        _bus = bus;
        _bus.Subscribe<DamageDealtEvent>(OnDamageDealt);
        _bus.Subscribe<AbilityActivatedEvent>(OnAbilityActivated);
    }

    /// <inheritdoc/>
    public override void Update(World world, float deltaTime)
    {
        _world = world;

        foreach (var (entity, flash) in world.Query<HitFlashComponent>())
        {
            flash.Remaining -= deltaTime;
            if (flash.Remaining <= 0f)
            {
                RestoreRender(world, entity, flash.BaseColor, flash.BaseTeamTint);
                world.RemoveComponent<HitFlashComponent>(entity);
                continue;
            }

            ApplyHitFlash(world, entity, flash);
        }

        foreach (var (entity, cast) in world.Query<CastFlashComponent>())
        {
            cast.Remaining -= deltaTime;
            if (cast.Remaining <= 0f)
            {
                RestoreRender(world, entity, cast.BaseColor, cast.BaseTeamTint);
                world.RemoveComponent<CastFlashComponent>(entity);
                continue;
            }

            ApplyCastFlash(world, entity, cast);
        }

        foreach (var (entity, pulse) in world.Query<HpBarPulseComponent>())
        {
            pulse.Remaining -= deltaTime;
            if (pulse.Remaining <= 0f)
                world.RemoveComponent<HpBarPulseComponent>(entity);
        }
    }

    private void OnDamageDealt(DamageDealtEvent e)
    {
        if (_world == null) return;
        if (!_world.TryGetEntityByIndex(e.TargetId, out Entity target)) return;
        TriggerHitFlash(_world, target);

        var health = _world.GetComponent<HealthComponent>(target);
        if (health != null && IsHeavyHit(e.FinalDamage, health))
            TriggerHpBarPulse(_world, target);
    }

    private void OnAbilityActivated(AbilityActivatedEvent e)
    {
        if (_world == null) return;
        if (!RaceUltimateSchema.TryGetByAbilityId(e.AbilityId, out _)) return;
        if (!_world.TryGetEntityByIndex(e.CasterId, out Entity caster)) return;

        string raceId = _world.GetComponent<RaceComponent>(caster)?.RaceId ?? string.Empty;
        Vector3 castTint = ResolveUltimateCastTint(raceId, e.AbilityId);
        TriggerCastFlash(_world, caster, castTint);
    }

    /// <summary>Returns true when <paramref name="damage"/> is a heavy hit for TTK feedback.</summary>
    public static bool IsHeavyHit(float damage, HealthComponent health) =>
        damage >= MathF.Max(health.MaxHP * HeavyHitThresholdFraction, HeavyHitMinDamage);

    /// <summary>Starts or refreshes an HP-bar pulse on <paramref name="target"/>.</summary>
    public static void TriggerHpBarPulse(World world, Entity target)
    {
        if (world.GetComponent<HealthComponent>(target) == null) return;

        if (!world.HasComponent<HpBarPulseComponent>(target))
        {
            world.AddComponent(target, new HpBarPulseComponent
            {
                Duration = 0.35f,
                Remaining = 0.35f,
            });
            return;
        }

        var pulse = world.GetComponent<HpBarPulseComponent>(target)!;
        pulse.Remaining = pulse.Duration;
    }

    /// <summary>Starts or refreshes a hit flash on <paramref name="target"/>.</summary>
    public static void TriggerHitFlash(World world, Entity target)
    {
        var render = world.GetComponent<RenderComponent>(target);
        if (render == null) return;

        if (!world.HasComponent<HitFlashComponent>(target))
        {
            world.AddComponent(target, new HitFlashComponent
            {
                Duration = 0.18f,
                Remaining = 0.18f,
                BaseColor = render.Color,
                BaseTeamTint = render.TeamTint,
                UsesRaceTexture = render.RaceTextureIndex >= 0,
            });
        }
        else
        {
            var flash = world.GetComponent<HitFlashComponent>(target)!;
            flash.Remaining = flash.Duration;
        }
    }

    /// <summary>Starts or refreshes a race-ultimate cast pulse on <paramref name="caster"/>.</summary>
    public static void TriggerCastFlash(World world, Entity caster, Vector3 castTint)
    {
        var render = world.GetComponent<RenderComponent>(caster);
        if (render == null) return;

        if (!world.HasComponent<CastFlashComponent>(caster))
        {
            world.AddComponent(caster, new CastFlashComponent
            {
                Duration = 0.36f,
                Remaining = 0.36f,
                BaseColor = render.Color,
                BaseTeamTint = render.TeamTint,
                CastTint = castTint,
                UsesRaceTexture = render.RaceTextureIndex >= 0,
            });
        }
        else
        {
            var cast = world.GetComponent<CastFlashComponent>(caster)!;
            cast.CastTint = castTint;
            cast.Remaining = cast.Duration;
        }
    }

    /// <summary>Resolves per-ultimate cast tint from <c>race_ultimates.json</c>.</summary>
    public static Vector3 ResolveUltimateCastTint(string raceId, string? abilityId = null) =>
        RaceUltimateSchema.ResolveCastTint(raceId, abilityId);

    private static void ApplyHitFlash(World world, Entity entity, HitFlashComponent flash)
    {
        var render = world.GetComponent<RenderComponent>(entity);
        if (render == null) return;

        float progress = 1f - MathF.Max(0f, flash.Remaining / flash.Duration);
        float intensity = MathF.Sin(progress * MathF.PI);

        if (flash.UsesRaceTexture)
        {
            var hot = new Vector3(1.45f, 0.55f, 0.4f);
            render.TeamTint = Vector3.Lerp(flash.BaseTeamTint, hot, intensity * 0.9f);
            return;
        }

        var flashColor = new Vector4(1f, 0.38f, 0.28f, 1f);
        render.Color = Vector4.Lerp(flash.BaseColor, flashColor, intensity);
        if (render.Color.W <= 0f)
            render.Color = flashColor with { W = intensity };
    }

    private static void ApplyCastFlash(World world, Entity entity, CastFlashComponent cast)
    {
        var render = world.GetComponent<RenderComponent>(entity);
        if (render == null) return;

        float progress = 1f - MathF.Max(0f, cast.Remaining / cast.Duration);
        float intensity = MathF.Sin(progress * MathF.PI);

        if (cast.UsesRaceTexture)
        {
            render.TeamTint = Vector3.Lerp(cast.BaseTeamTint, cast.CastTint * 1.35f, intensity * 0.95f);
            return;
        }

        var castColor = new Vector4(cast.CastTint, 1f);
        render.Color = Vector4.Lerp(cast.BaseColor, castColor, intensity * 0.85f);
        if (render.Color.W <= 0f)
            render.Color = castColor with { W = intensity };
    }

    private static void RestoreRender(
        World world, Entity entity, Vector4 baseColor, Vector3 baseTeamTint)
    {
        var render = world.GetComponent<RenderComponent>(entity);
        if (render == null) return;

        render.Color = baseColor;
        render.TeamTint = baseTeamTint;
    }
}