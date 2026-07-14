namespace SharpOpenGl;

/// <summary>CLI / headless launch flags for sandbox universe sessions.</summary>
public static class SandboxLaunchOptions
{
    public static bool Enabled { get; set; }
    public static string SeedText { get; set; } = "sandbox-headless";
}