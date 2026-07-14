namespace SharpOpenGl.Engine.ECS;

/// <summary>
/// Marks a structure that is still being built. Progress is advanced autonomously by
/// <see cref="ConstructionSystem"/> — no ongoing builder proximity check after placement.
/// </summary>
public sealed class UnderConstructionComponent
{
    /// <summary>Base JSON definition id (e.g. <c>power_reactor</c>).</summary>
    public string DefinitionId { get; set; } = string.Empty;

    /// <summary>Elapsed construction seconds.</summary>
    public float BuildProgress { get; set; }

    /// <summary>Total seconds required, copied from <see cref="Entities.EntityDefinition.BuildTime"/>.</summary>
    public float TotalBuildTime { get; set; }

    /// <summary>Owner player id.</summary>
    public int PlayerId { get; set; } = 1;
}