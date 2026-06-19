namespace SharpOpenGl.Engine.ECS;

/// <summary>
/// Human-readable label for an entity, shown in the unit info panel and tooltips.
/// </summary>
public sealed class EntityNameComponent
{
    /// <summary>Display name shown in UI (e.g. "Whisper Scout").</summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>Source definition id (e.g. "scout_light").</summary>
    public string DefinitionId { get; set; } = string.Empty;
}