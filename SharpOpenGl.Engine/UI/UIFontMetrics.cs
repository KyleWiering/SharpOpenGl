namespace SharpOpenGl.Engine.UI;

/// <summary>Shared text layout metrics for UI widgets and renderers.</summary>
public static class UIFontMetrics
{
    /// <summary>Glyph body width relative to font size.</summary>
    public const float CharWidthFactor = 0.56f;

    /// <summary>Inter-character gap relative to glyph width.</summary>
    public const float SpacingFactor = 0.14f;

    /// <summary>Segment stroke thickness relative to font size.</summary>
    public const float LineThicknessFactor = 0.125f;

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

/// <summary>Segment-font glyph definitions shared by renderers and tests.</summary>
public static class UIFontGlyphSegments
{
    public enum Segment
    {
        Top, Bottom, Middle,
        TopLeft, TopRight, BottomLeft, BottomRight,
        LeftFull, RightFull, CenterVert, BottomCenterStem,
        TopHalfRight, BottomHalfLeft, Dot, DotLow,
        DiagTopRightToBottomLeft, DiagTopLeftToBottomRight,
        DiagMidLeftTopRight, DiagMidLeftBottomRight, DiagMidRightBottomRight,
        TopHalfLeft, BottomHalfRight,
        TopPeakLeft, TopPeakRight,
        BottomValleyLeft, BottomValleyRight,
        MiddleRight, TopTick
    }

    public static Segment[] GetSegments(char c) => char.ToUpperInvariant(c) switch
    {
        'A' => [Segment.Top, Segment.TopLeft, Segment.TopRight, Segment.Middle, Segment.BottomLeft, Segment.BottomRight],
        'B' => [Segment.Top, Segment.Bottom, Segment.Middle, Segment.LeftFull, Segment.TopRight, Segment.BottomRight],
        'C' => [Segment.Top, Segment.Bottom, Segment.TopLeft, Segment.BottomLeft],
        'D' => [Segment.Top, Segment.Bottom, Segment.TopRight, Segment.BottomRight, Segment.LeftFull],
        'E' => [Segment.Top, Segment.Bottom, Segment.Middle, Segment.TopLeft, Segment.BottomLeft],
        'F' => [Segment.Top, Segment.Middle, Segment.TopLeft, Segment.BottomLeft],
        'G' => [Segment.Top, Segment.Bottom, Segment.TopLeft, Segment.BottomLeft, Segment.BottomRight, Segment.MiddleRight],
        'H' => [Segment.LeftFull, Segment.RightFull, Segment.Middle],
        'I' => [Segment.Top, Segment.Bottom, Segment.CenterVert],
        'J' => [Segment.Top, Segment.TopRight, Segment.BottomRight, Segment.Bottom, Segment.BottomLeft],
        'K' => [Segment.LeftFull, Segment.DiagMidLeftTopRight, Segment.DiagMidLeftBottomRight],
        'L' => [Segment.TopLeft, Segment.BottomLeft, Segment.Bottom],
        'M' => [Segment.LeftFull, Segment.RightFull, Segment.TopPeakLeft, Segment.TopPeakRight],
        'N' => [Segment.LeftFull, Segment.RightFull, Segment.DiagTopRightToBottomLeft],
        'O' => [Segment.Top, Segment.Bottom, Segment.TopLeft, Segment.TopRight, Segment.BottomLeft, Segment.BottomRight],
        'P' => [Segment.Top, Segment.Middle, Segment.TopLeft, Segment.TopRight, Segment.BottomLeft],
        'Q' => [Segment.Top, Segment.Bottom, Segment.TopLeft, Segment.TopRight, Segment.BottomLeft, Segment.BottomRight, Segment.DiagMidRightBottomRight],
        'R' => [Segment.Top, Segment.Middle, Segment.TopLeft, Segment.TopRight, Segment.BottomLeft, Segment.DiagMidRightBottomRight],
        'S' => [Segment.TopHalfLeft, Segment.TopLeft, Segment.Middle, Segment.BottomRight, Segment.BottomHalfLeft, Segment.Bottom],
        'T' => [Segment.Top, Segment.CenterVert],
        'U' => [Segment.LeftFull, Segment.RightFull, Segment.Bottom],
        'V' => [Segment.TopLeft, Segment.TopRight, Segment.BottomLeft, Segment.BottomRight, Segment.Bottom],
        'W' => [Segment.LeftFull, Segment.RightFull, Segment.BottomValleyLeft, Segment.BottomValleyRight],
        'X' => [Segment.DiagTopLeftToBottomRight, Segment.DiagTopRightToBottomLeft],
        'Y' => [Segment.TopLeft, Segment.TopRight, Segment.BottomCenterStem],
        'Z' => [Segment.Top, Segment.Bottom, Segment.DiagTopRightToBottomLeft],
        '0' => [Segment.Top, Segment.Bottom, Segment.TopLeft, Segment.TopRight, Segment.BottomLeft, Segment.BottomRight, Segment.DiagTopRightToBottomLeft],
        '1' => [Segment.TopRight, Segment.BottomRight, Segment.Bottom, Segment.TopTick],
        '2' => [Segment.Top, Segment.TopRight, Segment.Middle, Segment.BottomLeft, Segment.Bottom],
        '3' => [Segment.Top, Segment.Middle, Segment.Bottom, Segment.TopRight, Segment.BottomRight],
        '4' => [Segment.TopLeft, Segment.Middle, Segment.TopRight, Segment.BottomRight],
        '5' => [Segment.Top, Segment.TopLeft, Segment.Middle, Segment.BottomRight, Segment.Bottom],
        '6' => [Segment.Top, Segment.TopLeft, Segment.Middle, Segment.BottomLeft, Segment.BottomRight, Segment.Bottom],
        '7' => [Segment.Top, Segment.TopRight, Segment.BottomRight],
        '8' => [Segment.Top, Segment.Bottom, Segment.Middle, Segment.TopLeft, Segment.TopRight, Segment.BottomLeft, Segment.BottomRight],
        '9' => [Segment.Top, Segment.Bottom, Segment.Middle, Segment.TopLeft, Segment.TopRight, Segment.BottomRight],
        '.' => [Segment.Dot],
        ':' => [Segment.Top, Segment.Bottom],
        '-' => [Segment.Middle],
        '+' => [Segment.Middle, Segment.CenterVert],
        '/' => [Segment.DiagTopRightToBottomLeft],
        '(' => [Segment.Top, Segment.TopLeft, Segment.BottomLeft, Segment.Bottom],
        ')' => [Segment.Top, Segment.TopRight, Segment.BottomRight, Segment.Bottom],
        '=' => [Segment.Middle, Segment.Bottom],
        '_' => [Segment.Bottom],
        '>' => [Segment.TopLeft, Segment.Middle, Segment.BottomLeft],
        '<' => [Segment.TopRight, Segment.Middle, Segment.BottomRight],
        '#' => [Segment.Middle, Segment.CenterVert, Segment.Top, Segment.Bottom],
        '%' => [Segment.DiagTopLeftToBottomRight, Segment.Dot, Segment.DotLow],
        ',' => [Segment.DotLow],
        '\'' => [Segment.TopTick],
        '!' => [Segment.CenterVert, Segment.Dot],
        '?' => [Segment.TopHalfLeft, Segment.TopHalfRight, Segment.TopRight, Segment.BottomCenterStem, Segment.Dot],
        '•' => [Segment.Dot],
        '·' => [Segment.Dot],
        _ => [Segment.Middle],
    };

    /// <summary>Sorted comma-separated segment names for regression comparisons.</summary>
    public static string GetSignature(char c)
    {
        var segments = GetSegments(c);
        var names = new string[segments.Length];
        for (int i = 0; i < segments.Length; i++)
            names[i] = segments[i].ToString();

        Array.Sort(names, StringComparer.Ordinal);
        return string.Join(",", names);
    }
}