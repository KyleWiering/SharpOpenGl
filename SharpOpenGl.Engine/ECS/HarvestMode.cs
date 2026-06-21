namespace SharpOpenGl.Engine.ECS;

/// <summary>How a resource collector extracts ore from nodes.</summary>
public enum HarvestMode
{
    /// <summary>Small drones shuttle cargo between node and barge.</summary>
    Drones,

    /// <summary>EVA crew chip away on asteroid/planet surfaces at orbit range.</summary>
    Eva,

    /// <summary>Tractor beam pulls resources from range without docking.</summary>
    TractorBeam,
}

/// <summary>Mode-specific defaults for range, rate, and capacity.</summary>
public static class HarvestModeDefaults
{
    public static float DefaultRange(HarvestMode mode) => mode switch
    {
        HarvestMode.Drones => 28f,
        HarvestMode.Eva => 35f,
        HarvestMode.TractorBeam => 55f,
        _ => 28f,
    };

    public static float DefaultRateMultiplier(HarvestMode mode) => mode switch
    {
        HarvestMode.Drones => 1.0f,
        HarvestMode.Eva => 0.9f,
        HarvestMode.TractorBeam => 0.8f,
        _ => 1.0f,
    };

    public static float DefaultCarryCapacity(HarvestMode mode, float baseCapacity) => mode switch
    {
        HarvestMode.Drones => baseCapacity,
        HarvestMode.Eva => baseCapacity * 0.9f,
        HarvestMode.TractorBeam => baseCapacity * 1.15f,
        _ => baseCapacity,
    };

    /// <summary>Parse JSON harvest mode strings (drones, eva, tractor_beam).</summary>
    public static HarvestMode Parse(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return HarvestMode.Drones;

        return value.Trim().ToLowerInvariant() switch
        {
            "drones" or "drone" => HarvestMode.Drones,
            "eva" => HarvestMode.Eva,
            "tractor_beam" or "tractor" or "beam" => HarvestMode.TractorBeam,
            _ => HarvestMode.Drones,
        };
    }

    public static string ToLabel(HarvestMode mode) => mode switch
    {
        HarvestMode.Drones => "Drones",
        HarvestMode.Eva => "EVA",
        HarvestMode.TractorBeam => "Tractor Beam",
        _ => "Unknown",
    };
}