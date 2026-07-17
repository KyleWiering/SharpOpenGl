using OpenTK.Mathematics;

namespace SharpOpenGl.Engine.UI.Widgets;

/// <summary>One mission objective line for the in-game tracker.</summary>
public sealed class ObjectiveLine
{
    public string Text { get; init; } = string.Empty;
    public bool IsCompleted { get; init; }
    public bool IsPrimary { get; init; } = true;
}

/// <summary>
/// Top-center HUD panel showing the active mission and objective checklist.
/// </summary>
public sealed class ObjectivePanel : Widget
{
    private const float PadX = 12f;
    private const float PadTop = 10f;
    private const float PadBottom = 8f;
    private const float HeaderGap = 4f;
    private const float RowGap = 4f;
    private const float DefaultHeaderFontSize = 14f;
    private const int MaxObjectiveLinesPerRow = 2;
    private const float ObjectiveScrollStep = 36f;

    /// <summary>Maximum auto-grown panel height before objectives scroll.</summary>
    public const float MaxPanelHeight = 240f;

    private float _objectiveScrollOffsetY;
    private float _lastMaxObjectiveScrollOffset;

    public string MissionTitle { get; set; } = string.Empty;
    public IReadOnlyList<ObjectiveLine> Objectives { get; set; } = Array.Empty<ObjectiveLine>();
    public string ProgressText { get; set; } = string.Empty;
    public string HintText { get; set; } = "Select ships (LClick) · Move (RClick) · Attack enemies (RClick)";

    public Vector4 BackgroundColor { get; set; } = new Vector4(0.04f, 0.06f, 0.12f, 0.92f);
    public Vector4 BorderColor { get; set; } = new Vector4(0.35f, 0.55f, 0.85f, 1f);
    public float TitleFontSize { get; set; } = 22f;
    public float BodyFontSize { get; set; } = 18f;
    public float HintFontSize { get; set; } = 15f;
    public float HeaderFontSize { get; set; } = DefaultHeaderFontSize;

    /// <summary>Current vertical scroll offset for overflowing objective rows.</summary>
    public float ObjectiveScrollOffsetY => _objectiveScrollOffsetY;

    /// <summary>Maximum scroll offset from the last draw or scroll interaction.</summary>
    public float MaxObjectiveScrollOffset => _lastMaxObjectiveScrollOffset;

    /// <summary>Estimate panel height for the current title, objectives, and hint.</summary>
    public static float EstimateContentHeight(
        string missionTitle,
        IReadOnlyList<ObjectiveLine> objectives,
        string hintText,
        float titleFontSize,
        float bodyFontSize,
        float hintFontSize,
        float panelWidth)
    {
        float natural = EstimateNaturalContentHeight(
            missionTitle, objectives, hintText, titleFontSize, bodyFontSize, hintFontSize, panelWidth);
        return MathF.Min(natural, MaxPanelHeight);
    }

    /// <inheritdoc/>
    public override bool HandleScroll(
        Vector2 screenPoint, float deltaY,
        Vector2 containerPosition, Vector2 containerSize)
    {
        if (!Visible) return false;

        var (pos, size) = Resolve(containerPosition, containerSize);
        if (screenPoint.X < pos.X || screenPoint.X >= pos.X + size.X ||
            screenPoint.Y < pos.Y || screenPoint.Y >= pos.Y + size.Y)
            return false;

        if (_lastMaxObjectiveScrollOffset <= 0f) return false;

        _objectiveScrollOffsetY = Math.Clamp(
            _objectiveScrollOffsetY + (deltaY > 0f ? ObjectiveScrollStep : -ObjectiveScrollStep),
            0f,
            _lastMaxObjectiveScrollOffset);
        return true;
    }

    protected override void OnDraw(IUIRenderer renderer, Vector2 position, Vector2 size)
    {
        renderer.DrawRect(position, size, BackgroundColor);
        renderer.DrawRectOutline(position, size, BorderColor);

        float x = position.X + PadX;
        float y = position.Y + PadTop;
        float innerW = size.X - PadX * 2f;

        if (!string.IsNullOrEmpty(MissionTitle))
        {
            DrawFittedText(renderer, MissionTitle, new Vector2(x, y), innerW, TitleFontSize,
                new Vector4(0.55f, 0.85f, 1f, 1f), minLogicalSize: 14f);
            y += TitleFontSize * UITextDrawing.LineHeightFactor + HeaderGap;
        }

        string header = string.IsNullOrEmpty(ProgressText)
            ? "OBJECTIVES"
            : $"OBJECTIVES — {ProgressText}";
        DrawFittedText(renderer, header, new Vector2(x, y), innerW, HeaderFontSize,
            new Vector4(0.7f, 0.8f, 1f, 1f), minLogicalSize: 11f);
        y += HeaderFontSize * UITextDrawing.LineHeightFactor + HeaderGap;

        float hintReserve = string.IsNullOrEmpty(HintText)
            ? 0f
            : HintFontSize * UITextDrawing.LineHeightFactor + 12f;
        float objectivesTop = y;
        float objectivesBottom = position.Y + size.Y - hintReserve;
        float objectivesViewportHeight = MathF.Max(0f, objectivesBottom - objectivesTop);

        float contentHeight = MeasureObjectivesContentHeight(renderer, innerW);
        _lastMaxObjectiveScrollOffset = MathF.Max(0f, contentHeight - objectivesViewportHeight);
        _objectiveScrollOffsetY = Math.Clamp(_objectiveScrollOffsetY, 0f, _lastMaxObjectiveScrollOffset);

        float drawY = objectivesTop - _objectiveScrollOffsetY;
        foreach (ObjectiveLine obj in Objectives)
        {
            string prefix = obj.IsCompleted ? "[✓] " : "[ ] ";
            string displayText = prefix + obj.Text;
            int lineCount = CountObjectiveLines(renderer, displayText, innerW);
            float rowHeight = lineCount * BodyFontSize * UITextDrawing.LineHeightFactor;
            if (drawY + rowHeight > objectivesTop && drawY < objectivesBottom)
            {
                var color = obj.IsCompleted
                    ? new Vector4(0.45f, 0.9f, 0.55f, 1f)
                    : new Vector4(0.92f, 0.92f, 0.95f, 1f);

                DrawFittedTextBlock(renderer, displayText, new Vector2(x, drawY),
                    innerW, BodyFontSize, color, MaxObjectiveLinesPerRow, minLogicalSize: 12f);
            }

            drawY += rowHeight + RowGap;
        }

        if (!string.IsNullOrEmpty(HintText))
        {
            float hintY = position.Y + size.Y - HintFontSize * UITextDrawing.LineHeightFactor - PadBottom;
            DrawFittedTextBlock(renderer, HintText, new Vector2(x, hintY),
                innerW, HintFontSize, new Vector4(0.65f, 0.7f, 0.8f, 1f),
                maxLines: 2, minLogicalSize: 11f);
        }
    }

    private float MeasureObjectivesContentHeight(IUIRenderer renderer, float innerW)
    {
        float height = 0f;
        for (int i = 0; i < Objectives.Count; i++)
        {
            if (i > 0) height += RowGap;
            string prefix = Objectives[i].IsCompleted ? "[✓] " : "[ ] ";
            int lineCount = CountObjectiveLines(renderer, prefix + Objectives[i].Text, innerW);
            height += lineCount * BodyFontSize * UITextDrawing.LineHeightFactor;
        }

        return height;
    }

    private int CountObjectiveLines(IUIRenderer renderer, string text, float innerW)
    {
        if (string.IsNullOrEmpty(text))
            return 1;

        var (lines, _) = FitWrappedLabel(
            renderer, text, innerW, BodyFontSize, MaxObjectiveLinesPerRow, minLogicalSize: 12f);
        return Math.Max(1, lines.Count);
    }

    private static float EstimateNaturalContentHeight(
        string missionTitle,
        IReadOnlyList<ObjectiveLine> objectives,
        string hintText,
        float titleFontSize,
        float bodyFontSize,
        float hintFontSize,
        float panelWidth)
    {
        float innerW = Math.Max(0f, panelWidth - PadX * 2f);
        float y = PadTop;

        if (!string.IsNullOrEmpty(missionTitle))
            y += titleFontSize * UITextDrawing.LineHeightFactor + HeaderGap;

        y += DefaultHeaderFontSize * UITextDrawing.LineHeightFactor + HeaderGap;

        foreach (ObjectiveLine obj in objectives)
        {
            string prefix = obj.IsCompleted ? "[✓] " : "[ ] ";
            int lineCount = CountObjectiveLinesStatic(prefix + obj.Text, innerW, bodyFontSize);
            y += lineCount * bodyFontSize * UITextDrawing.LineHeightFactor + RowGap;
        }

        if (!string.IsNullOrEmpty(hintText))
            y += hintFontSize * UITextDrawing.LineHeightFactor + 10f;

        return y + PadBottom;
    }

    private static int CountObjectiveLinesStatic(string text, float innerW, float bodyFontSize)
    {
        if (string.IsNullOrEmpty(text))
            return 1;

        return UITextDrawing.WrapTextLimited(text, innerW, bodyFontSize, MaxObjectiveLinesPerRow).Count;
    }

    private static float ResolvePhysicalWrapWidth(
        IUIRenderer renderer, float maxLogicalWidth, float preferredLogicalSize)
    {
        float physicalScale = MathF.Max(renderer.ScaleToPhysical(1f), 0.001f);
        float maxPhysicalWidth = MathF.Max(0f, renderer.ScaleToPhysical(maxLogicalWidth) - 2f);
        float preferredPhysical = renderer.ResolveFontSize(preferredLogicalSize);
        float minPhysical = MathF.Max(
            ScaledUIRenderer.MinPhysicalFontSize,
            renderer.ResolveFontSize(10f));
        float fontScaleRatio = preferredPhysical / MathF.Max(preferredLogicalSize, 0.001f);
        if (fontScaleRatio <= physicalScale + 0.001f)
            return maxPhysicalWidth;

        return maxPhysicalWidth * (physicalScale / fontScaleRatio);
    }

    private static (string Text, float LogicalDrawSize) FitLabel(
        IUIRenderer renderer, string text, float maxLogicalWidth,
        float preferredLogicalSize, float minLogicalSize = 10f)
    {
        float physicalScale = MathF.Max(renderer.ScaleToPhysical(1f), 0.001f);
        float maxPhysicalWidth = MathF.Max(0f, renderer.ScaleToPhysical(maxLogicalWidth) - 2f);
        float preferredPhysical = renderer.ResolveFontSize(preferredLogicalSize);
        float minPhysical = MathF.Max(
            ScaledUIRenderer.MinPhysicalFontSize,
            renderer.ResolveFontSize(minLogicalSize));
        float fittedPhysical = UIFontMetrics.FitFontSize(
            text, preferredPhysical, maxPhysicalWidth, minPhysical);
        string display = UITextDrawing.TruncateWithEllipsis(text, maxPhysicalWidth, fittedPhysical);
        float logicalDrawSize = fittedPhysical / physicalScale;
        return (display, logicalDrawSize);
    }

    private static void DrawFittedText(
        IUIRenderer renderer, string text, Vector2 position, float maxLogicalWidth,
        float preferredLogicalSize, Vector4 color, float minLogicalSize = 10f)
    {
        var (display, logicalSize) = FitLabel(
            renderer, text, maxLogicalWidth, preferredLogicalSize, minLogicalSize);
        renderer.DrawText(display, position, logicalSize, color);
    }

    private static void DrawFittedTextBlock(
        IUIRenderer renderer, string text, Vector2 position, float maxLogicalWidth,
        float preferredLogicalSize, Vector4 color, int maxLines, float maxHeightLogical = 0f,
        float minLogicalSize = 10f)
    {
        if (string.IsNullOrEmpty(text)) return;

        var (lines, logicalDrawSize) = FitWrappedLabel(
            renderer, text, maxLogicalWidth, preferredLogicalSize, maxLines, maxHeightLogical, minLogicalSize);

        float lineHeight = logicalDrawSize * UITextDrawing.LineHeightFactor;
        for (int i = 0; i < lines.Count; i++)
        {
            if (maxHeightLogical > 0f && i > 0 && i * lineHeight >= maxHeightLogical + 0.001f)
                break;

            renderer.DrawText(lines[i], position + new Vector2(0f, i * lineHeight), logicalDrawSize, color);
        }
    }

    private static (IReadOnlyList<string> Lines, float LogicalDrawSize) FitWrappedLabel(
        IUIRenderer renderer, string text, float maxLogicalWidth,
        float preferredLogicalSize, int maxLines, float maxHeightLogical = 0f,
        float minLogicalSize = 10f)
    {
        float physicalScale = MathF.Max(renderer.ScaleToPhysical(1f), 0.001f);
        float maxPhysicalWidth = ResolvePhysicalWrapWidth(renderer, maxLogicalWidth, preferredLogicalSize);
        float preferredPhysical = renderer.ResolveFontSize(preferredLogicalSize);
        float minPhysical = MathF.Max(
            ScaledUIRenderer.MinPhysicalFontSize,
            renderer.ResolveFontSize(minLogicalSize));

        float fittedPhysical = preferredPhysical;
        int effectiveMaxLines = maxLines;
        if (maxHeightLogical > 0f)
        {
            float logicalDrawSize = fittedPhysical / physicalScale;
            int heightLines = UITextDrawing.MaxLinesFromHeight(maxHeightLogical, logicalDrawSize);
            effectiveMaxLines = effectiveMaxLines > 0
                ? Math.Min(effectiveMaxLines, heightLines)
                : heightLines;
        }

        IReadOnlyList<string> lines = effectiveMaxLines > 0
            ? UITextDrawing.WrapTextLimited(text, maxPhysicalWidth, fittedPhysical, effectiveMaxLines)
            : UITextDrawing.WrapText(text, maxPhysicalWidth, fittedPhysical);
        float widestLine = lines.Count == 0
            ? 0f
            : lines.Max(line => UIFontMetrics.MeasureTextWidth(line, fittedPhysical));

        while (fittedPhysical > minPhysical && widestLine > maxPhysicalWidth + UITextDrawing.WidthTolerance)
        {
            fittedPhysical -= 1f;
            lines = effectiveMaxLines > 0
                ? UITextDrawing.WrapTextLimited(text, maxPhysicalWidth, fittedPhysical, effectiveMaxLines)
                : UITextDrawing.WrapText(text, maxPhysicalWidth, fittedPhysical);
            widestLine = lines.Max(line => UIFontMetrics.MeasureTextWidth(line, fittedPhysical));
        }

        return (lines, fittedPhysical / physicalScale);
    }
}