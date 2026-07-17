using OpenTK.Mathematics;

namespace SharpOpenGl.Engine.UI;

/// <summary>Layout helpers for tooltip sizing, wrapping, and viewport-safe positioning.</summary>
public static class TooltipLayout
{
    public const float DefaultPointerOffset = 12f;
    public const float DefaultMaxTextWidth = 420f;
    public const float DefaultPadding = 12f;
    public const float DefaultFontSize = 18f;
    public const int DefaultScrollViewportMaxLines = 2;
    private const float ScrollbarGutter = 10f;

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

    /// <summary>
    /// Compute tooltip origin with an additional physical-viewport clamp so scaled gameplay
    /// draws (MinPhysicalFontSize) never bleed past the screen right/bottom edge.
    /// </summary>
    public static Vector2 ComputeBounds(
        Vector2 pointerPosition,
        Vector2 tooltipSize,
        Vector2 logicalViewportSize,
        float pointerOffset,
        IUIRenderer renderer)
    {
        Vector2 origin = ComputeBounds(
            pointerPosition, tooltipSize, logicalViewportSize, pointerOffset);

        float uniformScale = MathF.Max(renderer.ScaleToPhysical(1f), 0.001f);
        float physX = renderer.ScaleToPhysical(origin.X);
        float physY = renderer.ScaleToPhysical(origin.Y);
        float physW = renderer.ScaleToPhysical(tooltipSize.X);
        float physH = renderer.ScaleToPhysical(tooltipSize.Y);

        physX = Math.Clamp(physX, 0f, Math.Max(0f, renderer.ViewportSize.X - physW));
        physY = Math.Clamp(physY, 0f, Math.Max(0f, renderer.ViewportSize.Y - physH));

        return new Vector2(physX / uniformScale, physY / uniformScale);
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

    /// <summary>Line height used for tooltip text and scroll viewport caps.</summary>
    public static float LineHeight(float fontSize = DefaultFontSize) =>
        fontSize * UITextDrawing.LineHeightFactor;

    /// <summary>Maximum inner scroll viewport height before capping (~2 lines by default).</summary>
    public static float ScrollViewportCapHeight(
        float fontSize = DefaultFontSize,
        int maxVisibleLines = DefaultScrollViewportMaxLines) =>
        LineHeight(fontSize) * maxVisibleLines;

    /// <summary>Measure wrapped inner text width capped at <paramref name="maxTextWidth"/>.</summary>
    public static float MeasureInnerTextWidth(
        TooltipContent content,
        float maxTextWidth = DefaultMaxTextWidth,
        float fontSize = DefaultFontSize,
        IUIRenderer? renderer = null)
    {
        float effectiveMax = UITextDrawing.ScaleAwareWrapWidth(maxTextWidth, fontSize, renderer);
        float measureFont = renderer?.ResolveFontSize(fontSize) ?? fontSize;
        float measureScale = renderer != null
            ? MathF.Max(renderer.ScaleToPhysical(1f), 0.001f)
            : 1f;

        IReadOnlyList<string> wrapped = WrapLines(content.ToLines(), effectiveMax, fontSize);

        float contentWidth = 0f;
        foreach (string line in wrapped)
        {
            float lineWidth = UIFontMetrics.MeasureTextWidth(line, measureFont);
            contentWidth = Math.Max(contentWidth, lineWidth / measureScale);
        }

        return Math.Min(contentWidth, effectiveMax);
    }

    /// <summary>
    /// Measure tooltip box and inner scroll viewport using a capped height when content exceeds
    /// <paramref name="maxVisibleLines"/>.
    /// </summary>
    public static (Vector2 TooltipSize, Vector2 ScrollViewportSize, bool ScrollingEnabled) MeasureScrollViewport(
        float measuredContentHeight,
        float innerTextWidth,
        float fontSize = DefaultFontSize,
        float padding = DefaultPadding,
        int maxVisibleLines = DefaultScrollViewportMaxLines)
    {
        float viewportCap = ScrollViewportCapHeight(fontSize, maxVisibleLines);
        bool scrolling = measuredContentHeight > viewportCap + 0.01f;
        float scrollHeight = scrolling ? viewportCap : measuredContentHeight;
        float scrollWidth = innerTextWidth + (scrolling ? ScrollbarGutter : 0f);

        Vector2 scrollSize = new(scrollWidth, scrollHeight);
        Vector2 tooltipSize = new(scrollWidth + padding * 2f, scrollHeight + padding * 2f);
        return (tooltipSize, scrollSize, scrolling);
    }

    /// <summary>
    /// Wrap <paramref name="content"/> and measure tooltip box with scroll viewport cap applied.
    /// </summary>
    public static (Vector2 TooltipSize, Vector2 ScrollViewportSize, bool ScrollingEnabled) MeasureScrollViewport(
        TooltipContent content,
        float maxTextWidth = DefaultMaxTextWidth,
        float fontSize = DefaultFontSize,
        float padding = DefaultPadding,
        int maxVisibleLines = DefaultScrollViewportMaxLines)
    {
        float innerTextWidth = MeasureInnerTextWidth(content, maxTextWidth, fontSize);
        IReadOnlyList<string> wrapped = WrapLines(content.ToLines(), maxTextWidth, fontSize);
        float measuredHeight = wrapped.Count * LineHeight(fontSize);
        return MeasureScrollViewport(measuredHeight, innerTextWidth, fontSize, padding, maxVisibleLines);
    }
}