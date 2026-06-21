namespace SharpOpenGl.Engine.Config;

/// <summary>Runtime movement tuning loaded from <c>GameData/Config/balance.json</c>.</summary>
public static class MovementBalance
{
    public static float SpeedMultiplier { get; private set; } = 0.2f;
    public static float AccelerationMultiplier { get; private set; } = 0.3f;

    public static void Apply(MovementConfig? config)
    {
        if (config == null) return;
        if (config.GlobalSpeedMultiplier > 0f)
            SpeedMultiplier = config.GlobalSpeedMultiplier;
        if (config.GlobalAccelerationMultiplier > 0f)
            AccelerationMultiplier = config.GlobalAccelerationMultiplier;
    }

    public static void ResetForTests()
    {
        SpeedMultiplier = 0.2f;
        AccelerationMultiplier = 0.3f;
    }
}

public sealed class BalanceConfig
{
    public MovementConfig? Movement { get; set; }
    public CombatTuningConfig? Combat { get; set; }
    public VisualConfig? Visual { get; set; }
}

public sealed class MovementConfig
{
    public float GlobalSpeedMultiplier { get; set; } = 0.2f;
    public float GlobalAccelerationMultiplier { get; set; } = 0.3f;
}