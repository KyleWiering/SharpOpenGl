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
        float innerW = Math.Max(0f, panelWidth - PadX * 2f);
        float y = PadTop;

        if (!string.IsNullOrEmpty(missionTitle))
            y += titleFontSize * UITextDrawing.LineHeightFactor + HeaderGap;

        y += DefaultHeaderFontSize * UITextDrawing.LineHeightFactor + HeaderGap;

        foreach (ObjectiveLine obj in objectives)
        {
            int lineCount = CountObjectiveLines(obj.Text, innerW, bodyFontSize);
            y += lineCount * bodyFontSize * UITextDrawing.LineHeightFactor + RowGap;
        }

        if (!string.IsNullOrEmpty(hintText))
            y += hintFontSize * UITextDrawing.LineHeightFactor + 10f;

        return y + PadBottom;
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
            string title = UITextDrawing.TruncateWithEllipsis(MissionTitle, innerW, TitleFontSize);
            renderer.DrawText(title, new Vector2(x, y), TitleFontSize,
                new Vector4(0.55f, 0.85f, 1f, 1f));
            y += TitleFontSize * UITextDrawing.LineHeightFactor + HeaderGap;
        }

        string header = string.IsNullOrEmpty(ProgressText)
            ? "OBJECTIVES"
            : $"OBJECTIVES — {ProgressText}";
        renderer.DrawText(header, new Vector2(x, y), HeaderFontSize,
            new Vector4(0.7f, 0.8f, 1f, 1f));
        y += HeaderFontSize * UITextDrawing.LineHeightFactor + HeaderGap;

        float hintReserve = string.IsNullOrEmpty(HintText)
            ? 0f
            : HintFontSize * UITextDrawing.LineHeightFactor + 12f;
        float objectiveBottom = position.Y + size.Y - hintReserve;

        foreach (ObjectiveLine obj in Objectives)
        {
            if (y >= objectiveBottom)
                break;

            string prefix = obj.IsCompleted ? "[✓] " : "[ ] ";
            var color = obj.IsCompleted
                ? new Vector4(0.45f, 0.9f, 0.55f, 1f)
                : new Vector4(0.92f, 0.92f, 0.95f, 1f);

            int lineCount = CountObjectiveLines(obj.Text, innerW, BodyFontSize);
            float rowHeight = lineCount * BodyFontSize * UITextDrawing.LineHeightFactor;
            UITextDrawing.DrawTextBlock(renderer, prefix + obj.Text, new Vector2(x, y),
                BodyFontSize, color, innerW, MaxObjectiveLinesPerRow, rowHeight);
            y += rowHeight + RowGap;
        }

        if (!string.IsNullOrEmpty(HintText))
        {
            float hintY = position.Y + size.Y - HintFontSize * UITextDrawing.LineHeightFactor - PadBottom;
            UITextDrawing.DrawTextBlock(renderer, HintText, new Vector2(x, hintY),
                HintFontSize, new Vector4(0.65f, 0.7f, 0.8f, 1f), innerW, maxLines: 2);
        }
    }

    private static int CountObjectiveLines(string text, float innerW, float bodyFontSize)
    {
        if (string.IsNullOrEmpty(text))
            return 1;

        return UITextDrawing.WrapTextLimited(text, innerW, bodyFontSize, MaxObjectiveLinesPerRow).Count;
    }
}