using OpenTK.Mathematics;

namespace SharpOpenGl.Engine.UI.Widgets;

/// <summary>
/// A clickable button widget with a label and optional hover-state colour.
/// </summary>
public sealed class Button : Widget
{
    /// <summary>Text displayed on the button face.</summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>Background colour in the normal state.</summary>
    public Vector4 NormalColor { get; set; } = new Vector4(0.2f, 0.2f, 0.3f, 1f);

    /// <summary>Background colour when the pointer is hovering over the button.</summary>
    public Vector4 HoverColor { get; set; } = new Vector4(0.35f, 0.35f, 0.5f, 1f);

    /// <summary>Background colour while the button is being pressed.</summary>
    public Vector4 PressedColor { get; set; } = new Vector4(0.15f, 0.15f, 0.25f, 1f);

    /// <summary>Text colour.</summary>
    public Vector4 TextColor { get; set; } = new Vector4(1f, 1f, 1f, 1f);

    /// <summary>Font size in logical pixels.</summary>
    public float FontSize { get; set; } = 20f;

    /// <summary>Whether the pointer is currently hovering over this button.</summary>
    public bool IsHovered { get; private set; }

    /// <summary>Whether the button is currently in a pressed state.</summary>
    public bool IsPressed { get; private set; }

    /// <summary>Whether the button can receive input.</summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>Raised when the button is clicked (pointer tap within bounds).</summary>
    public event Action? Clicked;

    /// <inheritdoc/>
    public override bool HandlePointerTapped(
        Vector2 screenPoint, int button,
        Vector2 containerPosition, Vector2 containerSize)
    {
        if (!Visible || !IsEnabled) return false;

        var (pos, size) = Resolve(containerPosition, containerSize);
        bool inside = screenPoint.X >= pos.X && screenPoint.X < pos.X + size.X
                   && screenPoint.Y >= pos.Y && screenPoint.Y < pos.Y + size.Y;

        if (!inside) return false;

        IsPressed = true;
        Clicked?.Invoke();
        return true;
    }

    /// <summary>
    /// Update hover / press state based on current pointer position.
    /// </summary>
    public void UpdatePointerState(
        Vector2 pointerPosition, bool isPointerDown,
        Vector2 containerPosition, Vector2 containerSize)
    {
        var (pos, size) = Resolve(containerPosition, containerSize);
        IsHovered = pointerPosition.X >= pos.X && pointerPosition.X < pos.X + size.X
                 && pointerPosition.Y >= pos.Y && pointerPosition.Y < pos.Y + size.Y;
        IsPressed = IsHovered && isPointerDown;
    }

    /// <inheritdoc/>
    protected override void OnDraw(IUIRenderer renderer, Vector2 position, Vector2 size)
    {
        Vector4 bg = IsPressed ? PressedColor : (IsHovered ? HoverColor : NormalColor);
        renderer.DrawRect(position, size, bg);
        renderer.DrawRectOutline(position, size, new Vector4(0.6f, 0.6f, 0.8f, 1f));

        if (!string.IsNullOrEmpty(Label))
        {
            float textWidth = UIFontMetrics.MeasureTextWidth(Label, FontSize);
            Vector2 textPos = new(
                position.X + (size.X - textWidth) * 0.5f,
                position.Y + (size.Y - FontSize) * 0.5f);
            renderer.DrawText(Label, textPos, FontSize, TextColor);
        }
    }
}