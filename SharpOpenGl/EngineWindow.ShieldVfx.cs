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
}