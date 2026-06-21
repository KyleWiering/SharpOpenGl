namespace SharpOpenGl.Engine.Rendering;

public enum NoseProfile { Standard, Needle, Blunt, TwinProng, Hammerhead, Forked }
public enum WingProfile { Delta, Swept, Canted, Ring, Stub, Wide }

/// <summary>Per-design procedural modifiers derived deterministically from catalog seed.</summary>
public readonly struct ShipDesignVariant
{
    public float LengthScale { get; init; }
    public float WidthScale { get; init; }
    public float HeightScale { get; init; }
    public float NoseExtension { get; init; }
    public float SternExtension { get; init; }
    public float WingSweepDelta { get; init; }
    public float WingSpanDelta { get; init; }
    public float ProtrusionDelta { get; init; }
    public float AsymmetryDelta { get; init; }
    public int ExtraEngines { get; init; }
    public int HardpointCount { get; init; }
    public int AntennaCount { get; init; }
    public int CargoPodCount { get; init; }
    public int ArmorPlateCount { get; init; }
    public int SensorArrayCount { get; init; }
    public int FinClusterCount { get; init; }
    public NoseProfile Nose { get; init; }
    public WingProfile Wings { get; init; }
    public int AttachmentBundle { get; init; }

    public static ShipDesignVariant FromSpec(ShipDesignSpec spec)
    {
        int seed = spec.Seed;
        string hull = spec.HullClass;

        float tierLen = spec.Tier switch { 0 => 0.96f, 1 => 1.0f, 2 => 1.06f, _ => 1.08f };
        float tierWid = spec.Tier switch { 0 => 0.94f, 1 => 1.0f, 2 => 1.04f, _ => 1.1f };

        int hardpoints = Rng(seed, 1) % 5 + (IsCombatHull(hull) ? 1 + Rng(seed, 2) % 3 : 0);
        if (spec.IsSpecial) hardpoints += 1 + Rng(seed, 3) % 3;

        int pods = hull is "transport" or "freighter" or "miner"
            ? 2 + Rng(seed, 4) % 5
            : Rng(seed, 5) % 4;

        return new ShipDesignVariant
        {
            LengthScale = tierLen * LerpF(0.88f, 1.12f, Rng(seed, 10)),
            WidthScale = tierWid * LerpF(0.9f, 1.14f, Rng(seed, 11)),
            HeightScale = LerpF(0.85f, 1.2f, Rng(seed, 12)),
            NoseExtension = LerpF(0f, 0.28f, Rng(seed, 13)) + (spec.IsSpecial ? 0.08f : 0f),
            SternExtension = LerpF(0f, 0.18f, Rng(seed, 14)),
            WingSweepDelta = LerpF(-0.15f, 0.2f, Rng(seed, 15)),
            WingSpanDelta = LerpF(-0.12f, 0.22f, Rng(seed, 16)),
            ProtrusionDelta = LerpF(0f, 0.25f, Rng(seed, 17)),
            AsymmetryDelta = LerpF(0f, 0.2f, Rng(seed, 18)),
            ExtraEngines = Rng(seed, 19) % (spec.IsSpecial ? 4 : 3),
            HardpointCount = hardpoints,
            AntennaCount = Rng(seed, 20) % 5,
            CargoPodCount = pods,
            ArmorPlateCount = 1 + Rng(seed, 21) % 5 + spec.Tier,
            SensorArrayCount = Rng(seed, 22) % 4 + (hull is "scout" or "frigate" ? 1 : 0),
            FinClusterCount = Rng(seed, 23) % 5,
            Nose = (NoseProfile)(Rng(seed, 24) % 6),
            Wings = (WingProfile)(Rng(seed, 25) % 6),
            AttachmentBundle = Rng(seed, 26) % 16,
        };
    }

    private static bool IsCombatHull(string hull)
        => hull is "fighter" or "interceptor" or "gunship" or "destroyer" or "cruiser" or "dreadnought" or "hero";

    private static int Rng(int seed, int salt)
    {
        unchecked
        {
            int s = seed ^ unchecked(salt * (int)0x9E3779B9);
            s ^= s << 13;
            s ^= s >> 17;
            s ^= s << 5;
            return Math.Abs(s);
        }
    }

    private static float LerpF(float a, float b, int hash)
        => a + (b - a) * (hash % 10_000 / 10_000f);
}