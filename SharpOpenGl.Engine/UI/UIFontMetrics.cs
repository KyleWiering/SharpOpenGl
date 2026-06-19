namespace SharpOpenGl.Engine.UI;

/// <summary>Shared text layout metrics for UI widgets and renderers.</summary>
public static class UIFontMetrics
{
    /// <summary>Estimate rendered text width for centering.</summary>
    public static float MeasureTextWidth(string text, float fontSize)
    {
        float charWidth = fontSize * 0.6f;
        float spacing = charWidth * 0.2f;
        float width = 0f;
        for (int i = 0; i < text.Length; i++)
        {
            if (text[i] == ' ') continue;
            width += charWidth + spacing;
        }
        return MathF.Max(0f, width - spacing);
    }
}