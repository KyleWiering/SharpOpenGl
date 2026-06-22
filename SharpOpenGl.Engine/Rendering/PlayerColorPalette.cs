using OpenTK.Mathematics;

namespace SharpOpenGl.Engine.Rendering;

/// <summary>
/// Distinct faction colors for up to 8 multiplayer slots.
/// Applied as hull insignia and ground aura — not full-hull texture tints.
/// </summary>
public static class PlayerColorPalette
{
    public const int MaxPlayers = 8;
    public const float AuraFillAlpha = 0.46f;
    public const float AuraRingAlpha = 0.72f;
    public const float AuraOuterAlpha = 0.28f;

    private static readonly Vector3[] Tints =
    [
        new(0.35f, 0.72f, 1.00f), // P1 blue
        new(1.00f, 0.32f, 0.28f), // P2 red
        new(0.38f, 0.95f, 0.42f), // P3 green
        new(1.00f, 0.82f, 0.22f), // P4 gold
        new(0.78f, 0.42f, 1.00f), // P5 violet
        new(1.00f, 0.52f, 0.18f), // P6 orange
        new(0.28f, 0.92f, 0.92f), // P7 cyan
        new(0.95f, 0.45f, 0.78f), // P8 magenta
    ];

    public static Vector3 GetTint(int playerId)
    {
        if (playerId < 1) return Vector3.One;
        return Tints[(playerId - 1) % MaxPlayers];
    }

    public static Vector4 GetTintVector4(int playerId)
    {
        Vector3 tint = GetTint(playerId);
        return new Vector4(tint.X, tint.Y, tint.Z, 1f);
    }

    public static Vector4 GetAuraColor(int playerId, float pulse = 1f)
    {
        Vector3 tint = GetTint(playerId);
        float alpha = AuraFillAlpha * Math.Clamp(pulse, 0.5f, 1f);
        return new Vector4(tint.X, tint.Y, tint.Z, alpha);
    }

    public static Vector4 GetAuraRingColor(int playerId, float pulse = 1f)
    {
        Vector3 tint = GetTint(playerId);
        float alpha = AuraRingAlpha * Math.Clamp(pulse, 0.5f, 1f);
        return new Vector4(
            MathF.Min(tint.X * 1.15f, 1f),
            MathF.Min(tint.Y * 1.15f, 1f),
            MathF.Min(tint.Z * 1.15f, 1f),
            alpha);
    }

    public static Vector4 GetAuraOuterColor(int playerId, float pulse = 1f)
    {
        Vector3 tint = GetTint(playerId);
        float alpha = AuraOuterAlpha * Math.Clamp(pulse, 0.45f, 1f);
        return new Vector4(tint.X * 0.85f, tint.Y * 0.85f, tint.Z * 0.85f, alpha);
    }
}