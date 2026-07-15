using OpenTK.Mathematics;

namespace SharpOpenGl.Engine.UI;

/// <summary>Layout helpers for tooltip sizing, wrapping, and viewport-safe positioning.</summary>
public static class TooltipLayout
{
    public const float DefaultPointerOffset = 12f;
    public const float DefaultMaxTextWidth = 420f;
    public const float DefaultPadding = 12f;
    public const float DefaultFontSize = 18f;

    /// <summary>
    /// Compute top-left tooltip position anchored near <paramref name="pointerPosition"/>
    /// with flip and clamp so the full box stays inside <paramref name="viewportSize"/>.
    /// </summary>
    public static Vector2 ComputeBounds(
        Vector2 pointerPosition,
        Vector2 tooltipSize,
        Vector2 viewportSize,
        float pointerOffset = DefaultPointerOffset)
    {
        var offset = new Vector2(pointerOffset, pointerOffset);
        float x = pointerPosition.X + offset.X;
        float y = pointerPosition.Y + offset.Y;

        if (x + tooltipSize.X > viewportSize.X)
            x = pointerPosition.X - tooltipSize.X - offset.X;

        if (y + tooltipSize.Y > viewportSize.Y)
            y = pointerPosition.Y - tooltipSize.Y - offset.Y;

        x = Math.Clamp(x, 0f, Math.Max(0f, viewportSize.X - tooltipSize.X));
        y = Math.Clamp(y, 0f, Math.Max(0f, viewportSize.Y - tooltipSize.Y));

        return new Vector2(x, y);
    }

    /// <summary>Wrap each source line to fit within <paramref name="maxTextWidth"/>.</summary>
    public static IReadOnlyList<string> WrapLines(
        IReadOnlyList<string> sourceLines, float maxTextWidth, float fontSize)
    {
        var wrapped = new List<string>();
        foreach (string line in sourceLines)
        {
            if (string.IsNullOrEmpty(line))
            {
                wrapped.Add(string.Empty);
                continue;
            }

            wrapped.AddRange(UITextDrawing.WrapText(line, maxTextWidth, fontSize));
        }

        return wrapped;
    }

    /// <summary>Measure tooltip box size after wrapping lines at <paramref name="maxTextWidth"/>.</summary>
    public static Vector2 MeasureTooltipSize(
        IReadOnlyList<string> wrappedLines,
        float maxTextWidth,
        float fontSize,
        float padding = DefaultPadding)
    {
        float lineHeight = fontSize * UITextDrawing.LineHeightFactor;
        float height = wrappedLines.Count * lineHeight + padding * 2f;

        float contentWidth = 0f;
        foreach (string line in wrappedLines)
            contentWidth = Math.Max(contentWidth, UIFontMetrics.MeasureTextWidth(line, fontSize));

        contentWidth = Math.Min(contentWidth, maxTextWidth);
        float width = contentWidth + padding * 2f;

        return new Vector2(width, height);
    }

    /// <summary>Wrap <paramref name="content"/> and measure the resulting tooltip box.</summary>
    public static (IReadOnlyList<string> WrappedLines, Vector2 Size) MeasureContent(
        TooltipContent content,
        float maxTextWidth = DefaultMaxTextWidth,
        float fontSize = DefaultFontSize,
        float padding = DefaultPadding)
    {
        IReadOnlyList<string> wrapped = WrapLines(content.ToLines(), maxTextWidth, fontSize);
        Vector2 size = MeasureTooltipSize(wrapped, maxTextWidth, fontSize, padding);
        return (wrapped, size);
    }
}