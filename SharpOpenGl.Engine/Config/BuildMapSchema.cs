namespace SharpOpenGl.Engine.Config;

/// <summary>Root document for <c>GameData/Config/build_map.json</c>.</summary>
public sealed class BuildMapConfig
{
    public List<BuildMapCategoryConfig> Categories { get; set; } = [];
}

/// <summary>A build-map tab grouping structures by role.</summary>
public sealed class BuildMapCategoryConfig
{
    public string Id { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public List<BuildMapEntryConfig> Buildings { get; set; } = [];
}

/// <summary>Single placeable structure with prerequisite chain.</summary>
public sealed class BuildMapEntryConfig
{
    public string Id { get; set; } = string.Empty;
    public List<string> Prerequisites { get; set; } = [];
}