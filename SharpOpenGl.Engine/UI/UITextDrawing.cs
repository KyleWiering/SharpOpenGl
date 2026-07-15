using OpenTK.Mathematics;

namespace SharpOpenGl.Engine.UI;

/// <summary>Shared helpers for multiline and wrapped UI text.</summary>
public static class UITextDrawing
{
    /// <summary>Line height multiplier relative to font size.</summary>
    public const float LineHeightFactor = 1.55f;

    /// <summary>Horizontal bleed tolerance when comparing measured line width to wrap width.</summary>
    public const float WidthTolerance = 1f;

    /// <summary>Wrap <paramref name="text"/> to fit within <paramref name="maxWidth"/> pixels.</summary>
    public static IReadOnlyList<string> WrapText(string text, float maxWidth, float fontSize)
    {
        if (string.IsNullOrEmpty(text)) return Array.Empty<string>();
        if (maxWidth <= 0f) return text.Split('\n');

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
                if (FitsWidth(candidate, maxWidth, fontSize))
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

                if (FitsWidth(word, maxWidth, fontSize))
                    current.Append(word);
                else
                    BreakLongToken(word, maxWidth, fontSize, lines, current);
            }

            if (current.Length > 0)
                lines.Add(current.ToString());
        }

        GuardLineWidths(lines, maxWidth, fontSize);
        return lines;
    }

    /// <summary>Draw wrapped or multiline text using an <see cref="IUIRenderer"/>.</summary>
    public static void DrawTextBlock(
        IUIRenderer renderer, string text, Vector2 position,
        float fontSize, Vector4 color, float maxWidth)
    {
        DrawTextBlock(renderer, text, position, fontSize, color, maxWidth, maxLines: 0, maxHeight: 0f);
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

    /// <summary>Maximum drawable lines that fit within <paramref name="maxHeight"/>.</summary>
    public static int MaxLinesFromHeight(float maxHeight, float fontSize)
    {
        if (maxHeight <= 0f || fontSize <= 0f) return 0;

        float lineHeight = fontSize * LineHeightFactor;
        return Math.Max(1, (int)MathF.Floor(maxHeight / lineHeight + 0.001f));
    }

    /// <summary>Truncate <paramref name="text"/> to fit <paramref name="maxWidth"/> with an ellipsis suffix.</summary>
    public static string TruncateWithEllipsis(string text, float maxWidth, float fontSize, string ellipsis = "…")
    {
        if (string.IsNullOrEmpty(text) || maxWidth <= 0f)
            return string.Empty;

        if (FitsWidth(text, maxWidth, fontSize))
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
        lines.Add(CapLineWithEllipsis(wrapped[maxLines - 1], maxWidth, fontSize, ellipsis));
        return lines;
    }

    /// <summary>Draw wrapped text with an optional maximum line count.</summary>
    public static void DrawTextBlock(
        IUIRenderer renderer, string text, Vector2 position,
        float fontSize, Vector4 color, float maxWidth, int maxLines)
    {
        DrawTextBlock(renderer, text, position, fontSize, color, maxWidth, maxLines, maxHeight: 0f);
    }

    /// <summary>Draw wrapped text with optional line and height caps.</summary>
    public static void DrawTextBlock(
        IUIRenderer renderer, string text, Vector2 position,
        float fontSize, Vector4 color, float maxWidth, int maxLines, float maxHeight)
    {
        if (string.IsNullOrEmpty(text)) return;

        IReadOnlyList<string> lines = ResolveLines(text, maxWidth, fontSize, maxLines, maxHeight);
        float lineHeight = fontSize * LineHeightFactor;

        for (int i = 0; i < lines.Count; i++)
        {
            // Always render the first line; additional lines respect the height budget.
            if (maxHeight > 0f && i > 0 && i * lineHeight >= maxHeight + 0.001f)
                break;

            renderer.DrawText(lines[i], position + new Vector2(0f, i * lineHeight), fontSize, color);
        }
    }

    private static IReadOnlyList<string> ResolveLines(
        string text, float maxWidth, float fontSize, int maxLines, float maxHeight)
    {
        int effectiveMaxLines = maxLines;
        if (maxHeight > 0f)
        {
            int heightLines = MaxLinesFromHeight(maxHeight, fontSize);
            effectiveMaxLines = effectiveMaxLines > 0
                ? Math.Min(effectiveMaxLines, heightLines)
                : heightLines;
        }

        if (effectiveMaxLines > 0)
            return WrapTextLimited(text, maxWidth, fontSize, effectiveMaxLines);

        if (maxWidth > 0f)
            return WrapText(text, maxWidth, fontSize);

        return text.Split('\n');
    }

    private static bool FitsWidth(string text, float maxWidth, float fontSize) =>
        UIFontMetrics.MeasureTextWidth(text, fontSize) <= maxWidth + WidthTolerance;

    private static void BreakLongToken(
        string token, float maxWidth, float fontSize,
        List<string> lines, System.Text.StringBuilder current)
    {
        foreach (char ch in token)
        {
            string candidate = current.Length == 0 ? ch.ToString() : current.ToString() + ch;
            if (FitsWidth(candidate, maxWidth, fontSize))
            {
                current.Append(ch);
                continue;
            }

            if (current.Length > 0)
            {
                lines.Add(current.ToString());
                current.Clear();
            }

            string single = ch.ToString();
            if (FitsWidth(single, maxWidth, fontSize))
                current.Append(ch);
            else
                lines.Add(single);
        }
    }

    private static void GuardLineWidths(List<string> lines, float maxWidth, float fontSize)
    {
        for (int i = 0; i < lines.Count; i++)
        {
            if (!FitsWidth(lines[i], maxWidth, fontSize))
                lines[i] = TruncateWithEllipsis(lines[i], maxWidth, fontSize);
        }
    }

    private static string CapLineWithEllipsis(string line, float maxWidth, float fontSize, string ellipsis)
    {
        if (maxWidth <= 0f)
            return line + ellipsis;

        if (line.EndsWith(ellipsis, StringComparison.Ordinal))
            return FitsWidth(line, maxWidth, fontSize) ? line : TruncateWithEllipsis(line, maxWidth, fontSize, ellipsis);

        float ellipsisWidth = UIFontMetrics.MeasureTextWidth(ellipsis, fontSize);
        float bodyBudget = Math.Max(0f, maxWidth - ellipsisWidth);
        string body = bodyBudget > 0f
            ? TruncateWithEllipsis(line, bodyBudget, fontSize, string.Empty)
            : string.Empty;
        return body + ellipsis;
    }
}