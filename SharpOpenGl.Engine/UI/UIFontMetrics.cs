namespace SharpOpenGl.Engine.UI;

/// <summary>Shared text layout metrics for UI widgets and renderers.</summary>
public static class UIFontMetrics
{
    /// <summary>Glyph body width relative to font size.</summary>
    public const float CharWidthFactor = 0.56f;

    /// <summary>Inter-character gap relative to glyph width.</summary>
    public const float SpacingFactor = 0.15f;

    /// <summary>Segment stroke thickness relative to font size.</summary>
    public const float LineThicknessFactor = 0.14f;

    /// <summary>Minimum segment stroke thickness in pixels.</summary>
    public const float MinLineThickness = 2f;

    public static float GetCharWidth(float fontSize) => fontSize * CharWidthFactor;

    public static float GetCharSpacing(float fontSize) => GetCharWidth(fontSize) * SpacingFactor;

    public static float GetLineThickness(float fontSize) =>
        MathF.Max(MinLineThickness, fontSize * LineThicknessFactor);

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