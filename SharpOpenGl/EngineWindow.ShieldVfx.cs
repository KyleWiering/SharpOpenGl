using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using SharpOpenGl.Engine.Config;
using SharpOpenGl.Engine.ECS;
using SharpOpenGl.Engine.UI;

namespace SharpOpenGl;

public partial class EngineWindow
{
    private float _shieldRingPulse;

    private void RenderShieldCombatRings()
    {
        if (_world == null) return;

        foreach (var (entity, health) in _world.Query<HealthComponent>())
        {
            if (health.MaxShields <= 0f || health.IsDead)
                continue;
            if (!CombatState.IsInCombat(_world, entity))
                continue;

            var transform = _world.GetComponent<TransformComponent>(entity);
            if (transform == null) continue;
            if (_world.HasComponent<AIControlledComponent>(entity) && !IsVisibleToPlayer(transform.Position))
                continue;

            string raceId = _world.GetComponent<RaceComponent>(entity)?.RaceId ?? string.Empty;
            Vector4 tint = string.IsNullOrEmpty(raceId)
                ? RaceShieldSchema.DefaultShieldTint
                : RaceShieldSchema.ResolveShieldTint(raceId);

            float pulse = 0.55f + 0.15f * MathF.Sin(_shieldRingPulse * 4f + entity.Index);
            float selRadius = _world.GetComponent<SelectionComponent>(entity)?.SelectionRadius ?? 7f;
            float ringScale = (selRadius / SelectionRingMeshRadius) * 1.22f * pulse;
            var ringModel = Matrix4.CreateScale(ringScale) *
                            Matrix4.CreateTranslation(transform.Position with { Y = 0.35f });
            GL.UniformMatrix4(_uniformModel, false, ref ringModel);
            GL.Uniform4(_uniformColor, tint with { W = pulse * 0.45f });
            GL.BindVertexArray(_selectionVao);
            GL.DrawArrays(PrimitiveType.Lines, 0, _selectionVertCount);
        }
    }

    /// <summary>
    /// Shimmer ring for shields regenerating out of combat — faster pulse than combat hold ring.
    /// </summary>
    private void RenderShieldRegenShimmerRings()
    {
        if (_world == null) return;

        foreach (var (entity, health) in _world.Query<HealthComponent>())
        {
            if (health.MaxShields <= 0f || health.ShieldRegenPerSecond <= 0f || health.IsDead)
                continue;
            if (health.CurrentShields >= health.MaxShields)
                continue;
            if (CombatState.IsInCombat(_world, entity))
                continue;

            var transform = _world.GetComponent<TransformComponent>(entity);
            if (transform == null) continue;
            if (_world.HasComponent<AIControlledComponent>(entity) && !IsVisibleToPlayer(transform.Position))
                continue;

            string raceId = _world.GetComponent<RaceComponent>(entity)?.RaceId ?? string.Empty;
            Vector4 tint = string.IsNullOrEmpty(raceId)
                ? RaceShieldSchema.DefaultShieldTint
                : RaceShieldSchema.ResolveShieldTint(raceId);

            float phase = _shieldRingPulse * 7.5f + entity.Index * 0.7f;
            float shimmer = 0.5f + 0.5f * MathF.Sin(phase);
            float fill = health.ShieldFraction;
            float selRadius = _world.GetComponent<SelectionComponent>(entity)?.SelectionRadius ?? 7f;
            float ringScale = (selRadius / SelectionRingMeshRadius) * (1.05f + fill * 0.12f) *
                              (0.92f + shimmer * 0.14f);
            var ringModel = Matrix4.CreateScale(ringScale) *
                            Matrix4.CreateTranslation(transform.Position with { Y = 0.32f });
            GL.UniformMatrix4(_uniformModel, false, ref ringModel);
            GL.Uniform4(_uniformColor, tint with { W = (0.28f + shimmer * 0.38f) * (0.65f + fill * 0.35f) });
            GL.BindVertexArray(_selectionVao);
            GL.DrawArrays(PrimitiveType.Lines, 0, _selectionVertCount);
        }
    }
}