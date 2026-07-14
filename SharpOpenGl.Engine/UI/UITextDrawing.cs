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

    /// <summary>Usable wrap width inside a container after horizontal padding.</summary>
    public static float ContentWrapWidth(float containerWidth, float horizontalPadding = 16f) =>
        Math.Max(0f, containerWidth - horizontalPadding * 2f);

    /// <summary>Truncate <paramref name="text"/> to fit <paramref name="maxWidth"/> with an ellipsis suffix.</summary>
    public static string TruncateWithEllipsis(string text, float maxWidth, float fontSize, string ellipsis = "…")
    {
        if (string.IsNullOrEmpty(text) || maxWidth <= 0f)
            return string.Empty;

        if (UIFontMetrics.MeasureTextWidth(text, fontSize) <= maxWidth)
            return text;

        float ellipsisWidth = UIFontMetrics.MeasureTextWidth(ellipsis, fontSize);
        float budget = Math.Max(0f, maxWidth - ellipsisWidth);
        if (budget <= 0f)
            return ellipsis;

        var builder = new System.Text.StringBuilder();
        foreach (char ch in text)
        {
            string candidate = builder.ToString() + ch;
            if (UIFontMetrics.MeasureTextWidth(candidate, fontSize) > budget)
                break;
            builder.Append(ch);
        }

        string trimmed = builder.ToString().TrimEnd();
        return trimmed.Length == 0 ? ellipsis : trimmed + ellipsis;
    }

    /// <summary>Wrap text and cap the number of lines, truncating the last line with ellipsis when needed.</summary>
    public static IReadOnlyList<string> WrapTextLimited(
        string text, float maxWidth, float fontSize, int maxLines, string ellipsis = "…")
    {
        if (maxLines <= 0 || string.IsNullOrEmpty(text))
            return Array.Empty<string>();

        IReadOnlyList<string> wrapped = maxWidth > 0f
            ? WrapText(text, maxWidth, fontSize)
            : text.Split('\n');

        if (wrapped.Count <= maxLines)
            return wrapped;

        var lines = wrapped.Take(maxLines - 1).ToList();
        string lastLine = TruncateWithEllipsis(wrapped[maxLines - 1], maxWidth, fontSize, ellipsis);
        lines.Add(lastLine);
        return lines;
    }

    /// <summary>Draw wrapped text with an optional maximum line count.</summary>
    public static void DrawTextBlock(
        IUIRenderer renderer, string text, Vector2 position,
        float fontSize, Vector4 color, float maxWidth, int maxLines)
    {
        if (string.IsNullOrEmpty(text)) return;

        IReadOnlyList<string> lines = maxLines > 0 && maxWidth > 0f
            ? WrapTextLimited(text, maxWidth, fontSize, maxLines)
            : maxWidth > 0f
                ? WrapText(text, maxWidth, fontSize)
                : text.Split('\n');

        float lineHeight = fontSize * LineHeightFactor;
        for (int i = 0; i < lines.Count; i++)
            renderer.DrawText(lines[i], position + new Vector2(0f, i * lineHeight), fontSize, color);
    }
}