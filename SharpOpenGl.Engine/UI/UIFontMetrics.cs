namespace SharpOpenGl.Engine.UI;

/// <summary>Shared text layout metrics for UI widgets and renderers.</summary>
public static class UIFontMetrics
{
    /// <summary>Glyph body width relative to font size.</summary>
    public const float CharWidthFactor = 0.56f;

    /// <summary>Inter-character gap relative to glyph width.</summary>
    public const float SpacingFactor = 0.15f;

    /// <summary>Segment stroke thickness relative to font size.</summary>
    public const float LineThicknessFactor = 0.11f;

    /// <summary>Rendered glyph height relative to font size (leaves vertical padding).</summary>
    public const float GlyphHeightFactor = 0.78f;

    /// <summary>Top inset before glyph body, relative to font size.</summary>
    public const float GlyphPadTopFactor = 0.1f;

    /// <summary>Minimum segment stroke thickness in pixels.</summary>
    public const float MinLineThickness = 1.5f;

    public static float GetCharWidth(float fontSize) => fontSize * CharWidthFactor;

    public static float GetCharSpacing(float fontSize) => GetCharWidth(fontSize) * SpacingFactor;

    public static float GetGlyphHeight(float fontSize) => fontSize * GlyphHeightFactor;

    public static float GetGlyphPadTop(float fontSize) => fontSize * GlyphPadTopFactor;

    public static float GetLineThickness(float fontSize) =>
        MathF.Max(MinLineThickness, fontSize * LineThicknessFactor);

    /// <summary>Shrink font size until <paramref name="text"/> fits <paramref name="maxWidth"/>.</summary>
    public static float FitFontSize(string text, float preferredSize, float maxWidth, float minSize = 10f)
    {
        if (string.IsNullOrEmpty(text) || maxWidth <= 0f) return preferredSize;

        float size = preferredSize;
        while (size > minSize && MeasureTextWidth(text, size) > maxWidth)
            size -= 1f;

        return size;
    }

    /// <summary>Estimate rendered text width for centering.</summary>
    public static float MeasureTextWidth(string text, float fontSize)
    {
        float charWidth = GetCharWidth(fontSize);
        float spacing = GetCharSpacing(fontSize);
        int charCount = 0;
        for (int i = 0; i < text.Length; i++)
        {
            if (text[i] != ' ' && text[i] != '\n')
                charCount++;
        }

        if (charCount == 0) return 0f;
        return charCount * charWidth + (charCount - 1) * spacing;
    }
}