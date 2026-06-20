using SharpOpenGl.Engine.Combat;

namespace SharpOpenGl.Engine.ECS;

/// <summary>Rendering hints for live weapon projectiles.</summary>
public sealed class ProjectileVisualComponent
{
    public WeaponVisualKind Visual { get; init; }
    public float Scale { get; init; } = 1f;
}