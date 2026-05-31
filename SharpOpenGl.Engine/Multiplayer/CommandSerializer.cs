using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace SharpOpenGl.Engine.Multiplayer;

/// <summary>
/// Serialises and deserialises <see cref="IGameCommand"/> instances to/from JSON.
/// The discriminator field <c>"type"</c> is written first so that deserialisation
/// can dispatch to the correct concrete class without a second pass.
/// </summary>
public static class CommandSerializer
{
    private static readonly JsonSerializerOptions _options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

    /// <summary>Serialise any <see cref="IGameCommand"/> to a JSON string.</summary>
    public static string Serialize(IGameCommand command)
    {
        return command.Type switch
        {
            CommandType.Move       => JsonSerializer.Serialize((MoveCommand)command,       _options),
            CommandType.Attack     => JsonSerializer.Serialize((AttackCommand)command,     _options),
            CommandType.Build      => JsonSerializer.Serialize((BuildCommand)command,      _options),
            CommandType.Stop       => JsonSerializer.Serialize((StopCommand)command,       _options),
            CommandType.UseAbility => JsonSerializer.Serialize((UseAbilityCommand)command, _options),
            _ => throw new NotSupportedException($"Unknown command type: {command.Type}")
        };
    }

    /// <summary>
    /// Deserialise a JSON string back into the appropriate <see cref="IGameCommand"/> subtype.
    /// Reads the <c>"type"</c> field first to determine the concrete class.
    /// </summary>
    /// <exception cref="JsonException">Thrown when the JSON is malformed or missing the type field.</exception>
    public static IGameCommand Deserialize(string json)
    {
        var node = JsonNode.Parse(json)
            ?? throw new JsonException("JSON deserialised to null.");

        string? typeName = node["type"]?.GetValue<string>();
        if (typeName is null)
            throw new JsonException("Command JSON is missing the 'type' field.");

        if (!Enum.TryParse<CommandType>(typeName, ignoreCase: true, out CommandType type))
            throw new JsonException($"Unknown command type: '{typeName}'.");

        return type switch
        {
            CommandType.Move       => JsonSerializer.Deserialize<MoveCommand>(json, _options)!,
            CommandType.Attack     => JsonSerializer.Deserialize<AttackCommand>(json, _options)!,
            CommandType.Build      => JsonSerializer.Deserialize<BuildCommand>(json, _options)!,
            CommandType.Stop       => JsonSerializer.Deserialize<StopCommand>(json, _options)!,
            CommandType.UseAbility => JsonSerializer.Deserialize<UseAbilityCommand>(json, _options)!,
            _ => throw new NotSupportedException($"Unknown command type: {type}")
        };
    }
}
