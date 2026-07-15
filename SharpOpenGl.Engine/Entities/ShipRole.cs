using System.Text.Json;
using System.Text.Json.Serialization;

namespace SharpOpenGl.Engine.Entities;

/// <summary>High-level operational role for ship hulls (build-tree badges and UI).</summary>
public enum ShipRole
{
    /// <summary>Combat hulls (fighters, capitals, bombers).</summary>
    Military,

    /// <summary>Miners, repair, logistics, and cargo haulers.</summary>
    Engineering,

    /// <summary>Hero, command, and diplomat hulls.</summary>
    Political,
}

/// <summary>Resolves explicit or inferred <see cref="ShipRole"/> from entity definitions.</summary>
public static class ShipRoleResolver
{
    /// <summary>
    /// Returns the explicit <see cref="EntityDefinition.ShipRole"/> when set; otherwise infers from
    /// <see cref="EntityDefinition.Category"/>:
    /// miner, freighter, transport, support → Engineering; hero → Political; else Military.
    /// </summary>
    public static ShipRole Resolve(EntityDefinition definition)
    {
        if (definition.ShipRole.HasValue)
            return definition.ShipRole.Value;

        return InferFromCategory(definition.Category);
    }

    /// <summary>Category-based fallback when <c>shipRole</c> is omitted from JSON.</summary>
    public static ShipRole InferFromCategory(string? category) =>
        category?.Trim().ToLowerInvariant() switch
        {
            "miner" or "freighter" or "transport" or "support" => ShipRole.Engineering,
            "hero" => ShipRole.Political,
            _ => ShipRole.Military,
        };

    /// <summary>Display label for tooltips, e.g. <c>Military</c>.</summary>
    public static string DisplayName(ShipRole role) => role switch
    {
        ShipRole.Military => "Military",
        ShipRole.Engineering => "Engineering",
        ShipRole.Political => "Political",
        _ => role.ToString(),
    };
}

/// <summary>Case-insensitive JSON converter for optional <see cref="ShipRole"/> values.</summary>
public sealed class ShipRoleJsonConverter : JsonConverter<ShipRole?>
{
    /// <inheritdoc/>
    public override ShipRole? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return null;

        string? raw = reader.GetString();
        if (string.IsNullOrWhiteSpace(raw))
            return null;

        return Enum.TryParse(raw.Trim(), ignoreCase: true, out ShipRole role)
            ? role
            : null;
    }

    /// <inheritdoc/>
    public override void Write(Utf8JsonWriter writer, ShipRole? value, JsonSerializerOptions options)
    {
        if (!value.HasValue)
        {
            writer.WriteNullValue();
            return;
        }

        writer.WriteStringValue(value.Value.ToString().ToLowerInvariant());
    }
}