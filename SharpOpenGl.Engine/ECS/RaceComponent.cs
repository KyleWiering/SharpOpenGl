namespace SharpOpenGl.Engine.ECS;

/// <summary>Faction race identifier attached at spawn (e.g. terran, korath).</summary>
public sealed class RaceComponent
{
    public string RaceId { get; set; } = string.Empty;
}