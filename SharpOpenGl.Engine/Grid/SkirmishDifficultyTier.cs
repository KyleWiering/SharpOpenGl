namespace SharpOpenGl.Engine.Grid;

/// <summary>Skirmish AI difficulty tiers for multiplayer setup (stub — scales spawns and aggressiveness).</summary>
public enum SkirmishDifficultyTier
{
    Easy,
    Normal,
    Hard,
}

/// <summary>Tuning helpers for <see cref="SkirmishDifficultyTier"/>.</summary>
public static class SkirmishDifficultyTuning
{
    public static SkirmishDifficultyTier Cycle(SkirmishDifficultyTier current, int delta = 1)
    {
        int count = Enum.GetValues<SkirmishDifficultyTier>().Length;
        int index = ((int)current + delta) % count;
        return index < 0 ? (SkirmishDifficultyTier)(index + count) : (SkirmishDifficultyTier)index;
    }

    public static string DisplayName(SkirmishDifficultyTier tier) => tier switch
    {
        SkirmishDifficultyTier.Easy => "Easy",
        SkirmishDifficultyTier.Normal => "Normal",
        SkirmishDifficultyTier.Hard => "Hard",
        _ => "Normal",
    };

    public static float AiAggressiveness(SkirmishDifficultyTier tier) => tier switch
    {
        SkirmishDifficultyTier.Easy => 0.35f,
        SkirmishDifficultyTier.Normal => 0.6f,
        SkirmishDifficultyTier.Hard => 0.85f,
        _ => 0.6f,
    };

    public static int AiScoutSpawnCount(SkirmishDifficultyTier tier) => tier switch
    {
        SkirmishDifficultyTier.Easy => 2,
        SkirmishDifficultyTier.Normal => 4,
        SkirmishDifficultyTier.Hard => 6,
        _ => 4,
    };

    public static float AiBuildQueueIntervalSeconds(SkirmishDifficultyTier tier) => tier switch
    {
        SkirmishDifficultyTier.Easy => 22f,
        SkirmishDifficultyTier.Normal => 15f,
        SkirmishDifficultyTier.Hard => 10f,
        _ => 15f,
    };

    /// <summary>Skirmish starting resource bundle for AI factions.</summary>
    public readonly record struct SkirmishResourceAmounts(
        float Energy, float Minerals, float Data, float Crew);

    /// <summary>AI skirmish starting resources scaled by difficulty tier.</summary>
    public static SkirmishResourceAmounts AiStartingResources(SkirmishDifficultyTier tier) => tier switch
    {
        SkirmishDifficultyTier.Easy => new(300f, 180f, 60f, 30f),
        SkirmishDifficultyTier.Normal => new(500f, 300f, 100f, 50f),
        SkirmishDifficultyTier.Hard => new(800f, 500f, 160f, 80f),
        _ => new(500f, 300f, 100f, 50f),
    };
}