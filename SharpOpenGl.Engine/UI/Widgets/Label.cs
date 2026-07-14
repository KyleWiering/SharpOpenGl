using OpenTK.Mathematics;

namespace SharpOpenGl.Engine.UI.Widgets;

/// <summary>
/// Read-only text widget with optional word wrapping.
/// </summary>
public sealed class Label : Widget
{
    /// <summary>Text to display.</summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>Font size in logical pixels.</summary>
    public float FontSize { get; set; } = 20f;

    /// <summary>Text colour.</summary>
    public Vector4 TextColor { get; set; } = new Vector4(0.95f, 0.95f, 1f, 1f);

    /// <summary>When greater than zero, text wraps to this width in logical pixels.</summary>
    public float WrapWidth { get; set; }

    /// <summary>Maximum number of lines to render. Zero means unlimited.</summary>
    public int MaxLines { get; set; }

    /// <summary>Internal padding from the widget edge.</summary>
    public float Padding { get; set; } = 4f;

    /// <inheritdoc/>
    protected override void OnDraw(IUIRenderer renderer, Vector2 position, Vector2 size)
    {
        if (string.IsNullOrEmpty(Text)) return;

        float wrap = WrapWidth > 0f ? WrapWidth : UITextDrawing.ContentWrapWidth(size.X, Padding);
        Vector2 textPos = position + new Vector2(Padding, Padding);

        if (MaxLines > 0)
        {
            UITextDrawing.DrawTextBlock(renderer, Text, textPos, FontSize, TextColor, wrap, MaxLines);
            return;
        }

        UITextDrawing.DrawTextBlock(renderer, Text, textPos, FontSize, TextColor, wrap);
    }
}