namespace SharpOpenGl.Engine.Rendering;

/// <summary>Per-race surface substrate — drives UV scale, panel depth, and mesh micro-variation.</summary>
public sealed class RaceSubstrateProfile
{
    public string Pattern { get; init; } = "angular";
    public float UvScale { get; init; } = 0.12f;
    public float PanelDepth { get; init; } = 0.06f;
    public float Grit { get; init; } = 0.15f;
    public float AccentBoost { get; init; } = 0.2f;
    public float MicroFrequency { get; init; } = 2.4f;

    public static RaceSubstrateProfile ForRace(RaceVisualDefinition race)
    {
        if (race.Substrate != null)
        {
            return new RaceSubstrateProfile
            {
                Pattern = race.Substrate.Pattern ?? race.Style,
                UvScale = race.Substrate.UvScale > 0 ? race.Substrate.UvScale : 0.12f,
                PanelDepth = race.Substrate.PanelDepth,
                Grit = race.Substrate.Grit,
                AccentBoost = race.Substrate.AccentBoost,
                MicroFrequency = race.Substrate.MicroFrequency > 0 ? race.Substrate.MicroFrequency : 2.4f,
            };
        }

        return race.Style.ToLowerInvariant() switch
        {
            "sleek" => new() { Pattern = "sleek", UvScale = 0.09f, PanelDepth = 0.03f, Grit = 0.08f, MicroFrequency = 3.2f },
            "blocky" => new() { Pattern = "blocky", UvScale = 0.14f, PanelDepth = 0.1f, Grit = 0.28f, MicroFrequency = 1.8f },
            "truss" => new() { Pattern = "truss", UvScale = 0.09f, PanelDepth = 0.08f, Grit = 0.18f, MicroFrequency = 1.6f },
            "organic" => new() { Pattern = "organic", UvScale = 0.11f, PanelDepth = 0.05f, Grit = 0.12f, MicroFrequency = 2.8f },
            "asymmetric" => new() { Pattern = "asymmetric", UvScale = 0.13f, PanelDepth = 0.07f, Grit = 0.2f, MicroFrequency = 2.1f },
            "radiant" => new() { Pattern = "radiant", UvScale = 0.1f, PanelDepth = 0.04f, Grit = 0.1f, AccentBoost = 0.35f, MicroFrequency = 2.6f },
            "spiny" => new() { Pattern = "spiny", UvScale = 0.15f, PanelDepth = 0.08f, Grit = 0.32f, MicroFrequency = 3.5f },
            "crystalline" => new() { Pattern = "crystalline", UvScale = 0.16f, PanelDepth = 0.12f, Grit = 0.18f, MicroFrequency = 4f },
            _ => new() { Pattern = "angular", UvScale = 0.12f, PanelDepth = 0.06f, Grit = 0.15f, MicroFrequency = 2.4f },
        };
    }
}

public sealed class RaceSubstrateDefinition
{
    public string Pattern { get; set; } = "angular";
    public float UvScale { get; set; } = 0.12f;
    public float PanelDepth { get; set; } = 0.06f;
    public float Grit { get; set; } = 0.15f;
    public float AccentBoost { get; set; } = 0.2f;
    public float MicroFrequency { get; set; } = 2.4f;
}