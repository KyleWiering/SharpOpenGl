namespace SharpOpenGl.Engine.Config;

/// <summary>Runtime visual tuning loaded from <c>GameData/Config/balance.json</c>.</summary>
public static class VisualBalance
{
    public static float ShipScaleMultiplier { get; private set; } = 3.5f;

    public static void Apply(VisualConfig? config)
    {
        if (config == null) return;
        if (config.GlobalShipScaleMultiplier > 0f)
            ShipScaleMultiplier = config.GlobalShipScaleMultiplier;
    }

    public static void ResetForTests()
    {
        ShipScaleMultiplier = 3.5f;
    }
}

public sealed class VisualConfig
{
    public float GlobalShipScaleMultiplier { get; set; } = 3.5f;
}