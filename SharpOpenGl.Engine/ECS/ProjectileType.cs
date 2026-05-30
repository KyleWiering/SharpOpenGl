namespace SharpOpenGl.Engine.ECS;

/// <summary>
/// Describes how a projectile travels and delivers its effect.
/// </summary>
public enum ProjectileType
{
    /// <summary>Instant hit — no travel time, damage applied immediately.</summary>
    Instant,

    /// <summary>Travels in a straight line at constant speed until it hits or expires.</summary>
    Linear,

    /// <summary>Homes in on its target entity, tracking its position each frame.</summary>
    Homing,

    /// <summary>Area-of-effect explosion on arrival; damages all enemies within blast radius.</summary>
    AoE,
}
