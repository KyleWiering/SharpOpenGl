namespace SharpOpenGl.Engine.Multiplayer;

/// <summary>Discriminates the type of a <see cref="IGameCommand"/>.</summary>
public enum CommandType
{
    /// <summary>Order one or more entities to move to a position.</summary>
    Move,

    /// <summary>Order one or more entities to attack a target.</summary>
    Attack,

    /// <summary>Order a building entity to start constructing a unit or structure.</summary>
    Build,

    /// <summary>Order an entity to stop all current actions.</summary>
    Stop,

    /// <summary>Activates an ability on a source entity, optionally targeting another entity or position.</summary>
    UseAbility,

    /// <summary>Orders repair-capable units to restore a friendly hull.</summary>
    Repair,
}
