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

        int pods = hull is "transport" or "freighter" or "miner"
            ? 1 + Rng(seed, 4) % 3
            : 0;

        return new ShipDesignVariant
        {
            LengthScale = tierLen * LerpF(0.92f, 1.08f, Rng(seed, 10)),
            WidthScale = tierWid * LerpF(0.94f, 1.08f, Rng(seed, 11)),
            HeightScale = LerpF(0.9f, 1.1f, Rng(seed, 12)),
            NoseExtension = LerpF(0f, 0.12f, Rng(seed, 13)),
            SternExtension = LerpF(0f, 0.1f, Rng(seed, 14)),
            WingSweepDelta = 0f,
            WingSpanDelta = 0f,
            ProtrusionDelta = 0f,
            AsymmetryDelta = 0f,
            ExtraEngines = Rng(seed, 19) % (spec.IsSpecial ? 3 : 2),
            HardpointCount = 0,
            AntennaCount = 0,
            CargoPodCount = pods,
            ArmorPlateCount = 0,
            SensorArrayCount = 0,
            FinClusterCount = 0,
            Nose = NoseProfile.Standard,
            Wings = WingProfile.Delta,
            AttachmentBundle = 0,
        };
    }

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