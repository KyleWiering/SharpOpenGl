using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using SharpOpenGl.Engine.Config;
using SharpOpenGl.Engine.ECS;
using SharpOpenGl.Engine.Input;
using SharpOpenGl.Engine.UI;

namespace SharpOpenGl;

public partial class EngineWindow
{
    /// <summary>Selection ring mesh is authored at radius 3; hover ring stays slightly larger.</summary>
    private const float SelectionRingMeshRadius = 3f;

    private Entity? _attackHoverEntity;
    private float _attackHoverPulse;

    private void UpdateAttackHoverTarget(Vector2 screenPoint)
    {
        if (_world == null || !HasSelectedUnits())
        {
            _attackHoverEntity = null;
            return;
        }

        _attackHoverEntity = FindHostileAtScreen(screenPoint);
    }

    private Entity? FindRepairTargetAtScreen(Vector2 screenPoint)
    {
        if (_world == null) return null;

        var viewport = new Vector2(Size.X, Size.Y);
        var projection = Matrix4.CreatePerspectiveFieldOfView(
            MathHelper.DegreesToRadians(45f),
            viewport.X / viewport.Y,
            0.1f,
            10000f);
        var view = _rtsCamera.GetViewMatrix();

        Entity? closest = null;
        float closestDist = float.MaxValue;

        foreach (var (entity, health) in _world.Query<HealthComponent>())
        {
            if (health.IsDead || health.CurrentHP >= health.MaxHP) continue;
            if (GameplayEntityDisplay.IsHostileToPlayer(_world, entity)) continue;
            if (_world.HasComponent<ResourceNodeComponent>(entity)) continue;
            if (_world.HasComponent<BuildingComponent>(entity)) continue;

            var transform = _world.GetComponent<TransformComponent>(entity);
            if (transform == null) continue;
            if (!IsVisibleToPlayer(transform.Position)) continue;

            if (!GroundPlaneRaycaster.TryWorldToScreen(
                    transform.Position, viewport, projection, view, out Vector2 screen))
                continue;

            float pickRadius = ResolveHostilePickRadiusPx(entity);
            float dist = (screen - screenPoint).Length;
            if (dist <= pickRadius && dist < closestDist)
            {
                closestDist = dist;
                closest = entity;
            }
        }

        return closest;
    }

    private Entity? FindHostileAtScreen(Vector2 screenPoint)
    {
        if (_world == null) return null;

        var viewport = new Vector2(Size.X, Size.Y);
        var projection = Matrix4.CreatePerspectiveFieldOfView(
            MathHelper.DegreesToRadians(45f),
            viewport.X / viewport.Y,
            0.1f,
            10000f);
        var view = _rtsCamera.GetViewMatrix();

        Entity? closest = null;
        float closestDist = float.MaxValue;

        foreach (var (entity, health) in _world.Query<HealthComponent>())
        {
            if (health.IsDead || !GameplayEntityDisplay.IsHostileToPlayer(_world, entity))
                continue;

            var transform = _world.GetComponent<TransformComponent>(entity);
            if (transform == null) continue;
            if (!IsVisibleToPlayer(transform.Position)) continue;

            if (!GroundPlaneRaycaster.TryWorldToScreen(
                    transform.Position, viewport, projection, view, out Vector2 screen))
                continue;

            float pickRadius = ResolveHostilePickRadiusPx(entity);
            float dist = (screen - screenPoint).Length;
            if (dist <= pickRadius && dist < closestDist)
            {
                closestDist = dist;
                closest = entity;
            }
        }

        return closest;
    }

    private Entity? FindHostileAt(Vector3 worldPos)
    {
        if (_world == null) return null;

        Entity? closest = null;
        float closestDist = float.MaxValue;

        foreach (var (entity, health) in _world.Query<HealthComponent>())
        {
            if (health.IsDead || !GameplayEntityDisplay.IsHostileToPlayer(_world, entity))
                continue;

            var transform = _world.GetComponent<TransformComponent>(entity);
            if (transform == null) continue;
            if (!IsVisibleToPlayer(transform.Position)) continue;

            float pickRadius = ResolveHostilePickRadiusWorld(entity);
            float dist = HorizontalDistance(transform.Position, worldPos);
            if (dist <= pickRadius && dist < closestDist)
            {
                closestDist = dist;
                closest = entity;
            }
        }

        return closest;
    }

    private Entity? ResolveAttackTargetAt(Vector2 screenPoint, bool preferHover = true)
    {
        if (preferHover && _attackHoverEntity.HasValue && _world != null &&
            _world.IsAlive(_attackHoverEntity.Value))
            return _attackHoverEntity;

        return FindHostileAtScreen(screenPoint);
    }

    private float ResolveHostilePickRadiusWorld(Entity entity)
    {
        float radius = _world?.GetComponent<SelectionComponent>(entity)?.SelectionRadius ?? 8f;
        return MathF.Max(10f, radius * 1.1f);
    }

    private float ResolveHostilePickRadiusPx(Entity entity)
    {
        float worldRadius = ResolveHostilePickRadiusWorld(entity);
        float zoomFactor = 90f / MathF.Max(_rtsCamera.Height, 20f);
        return Math.Clamp(worldRadius * zoomFactor, 28f, 72f);
    }

    private void RenderAttackHoverRing()
    {
        if (_world == null || !_attackHoverEntity.HasValue || !HasSelectedUnits())
            return;

        Entity hover = _attackHoverEntity.Value;
        if (!_world.IsAlive(hover))
            return;

        var transform = _world.GetComponent<TransformComponent>(hover);
        if (transform == null || !IsVisibleToPlayer(transform.Position))
            return;

        float pulse = 0.92f + 0.08f * MathF.Sin(_attackHoverPulse * 6f);
        float selRadius = _world.GetComponent<SelectionComponent>(hover)?.SelectionRadius ?? 7f;
        float ringScale = (selRadius / SelectionRingMeshRadius) * 1.08f * pulse;
        var ringModel = Matrix4.CreateScale(ringScale) *
                        Matrix4.CreateTranslation(transform.Position with { Y = 0.25f });
        GL.UniformMatrix4(_uniformModel, false, ref ringModel);
        GL.Uniform4(_uniformColor, GameplayEntityDisplay.AttackHoverRingColor with { W = pulse });
        GL.BindVertexArray(_selectionVao);
        GL.DrawArrays(PrimitiveType.Lines, 0, _selectionVertCount);
    }
}