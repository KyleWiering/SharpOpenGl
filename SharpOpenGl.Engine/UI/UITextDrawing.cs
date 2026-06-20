using OpenTK.Mathematics;

namespace SharpOpenGl.Engine.UI;

/// <summary>Shared helpers for multiline and wrapped UI text.</summary>
public static class UITextDrawing
{
    /// <summary>Line height multiplier relative to font size.</summary>
    public const float LineHeightFactor = 1.55f;

    /// <summary>Wrap <paramref name="text"/> to fit within <paramref name="maxWidth"/> pixels.</summary>
    public static IReadOnlyList<string> WrapText(string text, float maxWidth, float fontSize)
    {
        if (string.IsNullOrEmpty(text)) return Array.Empty<string>();

        var lines = new List<string>();
        foreach (string paragraph in text.Split('\n'))
        {
            if (string.IsNullOrWhiteSpace(paragraph))
            {
                lines.Add(string.Empty);
                continue;
            }

            string[] words = paragraph.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var current = new System.Text.StringBuilder();

            foreach (string word in words)
            {
                string candidate = current.Length == 0 ? word : $"{current} {word}";
                if (UIFontMetrics.MeasureTextWidth(candidate, fontSize) <= maxWidth)
                {
                    if (current.Length > 0) current.Append(' ');
                    current.Append(word);
                    continue;
                }

                if (current.Length > 0)
                {
                    lines.Add(current.ToString());
                    current.Clear();
                }

                if (UIFontMetrics.MeasureTextWidth(word, fontSize) <= maxWidth)
                    current.Append(word);
                else
                    lines.Add(word);
            }

            if (current.Length > 0)
                lines.Add(current.ToString());
        }

        return lines;
    }

    /// <summary>Draw wrapped or multiline text using an <see cref="IUIRenderer"/>.</summary>
    public static void DrawTextBlock(
        IUIRenderer renderer, string text, Vector2 position,
        float fontSize, Vector4 color, float maxWidth)
    {
        if (string.IsNullOrEmpty(text)) return;

        IReadOnlyList<string> lines = maxWidth > 0f
            ? WrapText(text, maxWidth, fontSize)
            : text.Split('\n');

        float lineHeight = fontSize * LineHeightFactor;
        for (int i = 0; i < lines.Count; i++)
            renderer.DrawText(lines[i], position + new Vector2(0f, i * lineHeight), fontSize, color);
    }

    /// <summary>Estimate total height of a wrapped text block.</summary>
    public static float MeasureTextBlockHeight(string text, float fontSize, float maxWidth)
    {
        int lineCount = maxWidth > 0f
            ? WrapText(text, maxWidth, fontSize).Count
            : Math.Max(1, text.Split('\n').Length);
        return lineCount * fontSize * LineHeightFactor;
    }
}