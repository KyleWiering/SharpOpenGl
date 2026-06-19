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
    public string MissionTitle { get; set; } = string.Empty;
    public IReadOnlyList<ObjectiveLine> Objectives { get; set; } = Array.Empty<ObjectiveLine>();
    public string HintText { get; set; } = "Select ships (LClick) · Move (RClick) · Attack enemies (RClick)";

    public Vector4 BackgroundColor { get; set; } = new Vector4(0.04f, 0.06f, 0.12f, 0.92f);
    public Vector4 BorderColor { get; set; } = new Vector4(0.35f, 0.55f, 0.85f, 1f);
    public float TitleFontSize { get; set; } = 22f;
    public float BodyFontSize { get; set; } = 18f;
    public float HintFontSize { get; set; } = 15f;

    protected override void OnDraw(IUIRenderer renderer, Vector2 position, Vector2 size)
    {
        renderer.DrawRect(position, size, BackgroundColor);
        renderer.DrawRectOutline(position, size, BorderColor);

        float x = position.X + 12f;
        float y = position.Y + 10f;
        float innerW = size.X - 24f;

        if (!string.IsNullOrEmpty(MissionTitle))
        {
            renderer.DrawText(MissionTitle, new Vector2(x, y), TitleFontSize,
                new Vector4(0.55f, 0.85f, 1f, 1f));
            y += TitleFontSize * UITextDrawing.LineHeightFactor + 4f;
        }

        foreach (var obj in Objectives)
        {
            string prefix = obj.IsCompleted ? "[X] " : "[ ] ";
            var color = obj.IsCompleted
                ? new Vector4(0.45f, 0.9f, 0.55f, 1f)
                : new Vector4(0.92f, 0.92f, 0.95f, 1f);
            renderer.DrawText(prefix + obj.Text, new Vector2(x, y), BodyFontSize, color);
            y += BodyFontSize * UITextDrawing.LineHeightFactor;
        }

        if (!string.IsNullOrEmpty(HintText) && y + HintFontSize < position.Y + size.Y)
        {
            float hintY = position.Y + size.Y - HintFontSize * UITextDrawing.LineHeightFactor - 8f;
            UITextDrawing.DrawTextBlock(renderer, HintText, new Vector2(x, hintY),
                HintFontSize, new Vector4(0.65f, 0.7f, 0.8f, 1f), innerW);
        }
    }
}