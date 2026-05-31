namespace SharpOpenGl.Engine.Multiplayer;

/// <summary>
/// Represents a deterministic, serialisable player action.
/// All mutations to game state must be expressed as an <see cref="IGameCommand"/>
/// so they can be recorded, replayed, and transmitted over the network.
/// </summary>
public interface IGameCommand
{
    /// <summary>Discriminator used for polymorphic serialisation.</summary>
    CommandType Type { get; }

    /// <summary>
    /// Zero-based index of the player that issued the command.
    /// Used to validate authority and apply team ownership rules.
    /// </summary>
    int PlayerId { get; }

    /// <summary>
    /// Simulation tick number at which this command was issued.
    /// The deterministic game loop executes commands at their exact tick.
    /// </summary>
    long Tick { get; }
}
