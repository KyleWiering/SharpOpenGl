using OpenTK.Mathematics;
using SharpOpenGl.Engine.Persistence;

namespace SharpOpenGl.Engine.Rendering;

/// <summary>Applies colorblind-safe transforms to team/UI colors (P11-D03/D04).</summary>
public static class ColorblindFilter
{
    private static ColorblindMode _mode = ColorblindMode.None;

    /// <summary>Active filter from persisted settings.</summary>
    public static ColorblindMode Mode
    {
        get => _mode;
        set => _mode = value;
    }

    public static void ApplyFromSettings(GameSettings? settings) =>
        _mode = settings?.Accessibility.ColorblindMode ?? ColorblindMode.None;

    public static Vector3 Filter(Vector3 color) => Filter(color, _mode);

    public static Vector4 Filter(Vector4 color)
    {
        Vector3 rgb = Filter(new Vector3(color.X, color.Y, color.Z), _mode);
        return new Vector4(rgb.X, rgb.Y, rgb.Z, color.W);
    }

    public static Vector3 Filter(Vector3 color, ColorblindMode mode) => mode switch
    {
        ColorblindMode.RedGreen => ApplyRedGreen(color),
        ColorblindMode.BlueYellow => ApplyBlueYellow(color),
        ColorblindMode.Monochrome => ApplyMonochrome(color),
        _ => color,
    };

    private static Vector3 ApplyRedGreen(Vector3 c)
    {
        float gray = 0.299f * c.X + 0.587f * c.Y + 0.114f * c.Z;
        return new Vector3(
            MathF.Min(1f, gray + 0.45f * (c.X - c.Y)),
            MathF.Min(1f, gray + 0.25f * (c.Y - c.X)),
            MathF.Min(1f, c.Z * 0.92f + gray * 0.08f));
    }

    private static Vector3 ApplyBlueYellow(Vector3 c)
    {
        float gray = 0.299f * c.X + 0.587f * c.Y + 0.114f * c.Z;
        return new Vector3(
            MathF.Min(1f, c.X * 0.95f + gray * 0.05f),
            MathF.Min(1f, c.Y * 0.95f + gray * 0.05f),
            MathF.Min(1f, gray + 0.35f * (c.Z - gray)));
    }

    private static Vector3 ApplyMonochrome(Vector3 c)
    {
        float gray = 0.299f * c.X + 0.587f * c.Y + 0.114f * c.Z;
        return new Vector3(gray, gray, gray);
    }
}