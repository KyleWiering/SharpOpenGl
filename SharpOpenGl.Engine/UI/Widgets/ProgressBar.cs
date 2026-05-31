using OpenTK.Mathematics;

namespace SharpOpenGl.Engine.UI.Widgets;

/// <summary>
/// A horizontal progress bar that displays a fill fraction from 0 to 1.
/// Optionally shows a text label (e.g. percentage or status message) centred on the bar.
/// </summary>
public sealed class ProgressBar : Widget
{
    // ── Value ─────────────────────────────────────────────────────────────────

    private float _value;

    /// <summary>
    /// Fill level in the range [0, 1].  Values are clamped on assignment.
    /// </summary>
    public float Value
    {
        get => _value;
        set => _value = Math.Clamp(value, 0f, 1f);
    }

    // ── Appearance ────────────────────────────────────────────────────────────

    /// <summary>Colour of the empty track behind the fill bar.</summary>
    public Vector4 TrackColor { get; set; } = new Vector4(0.12f, 0.12f, 0.18f, 1f);

    /// <summary>Colour of the filled portion.</summary>
    public Vector4 FillColor { get; set; } = new Vector4(0.25f, 0.55f, 1.00f, 1f);

    /// <summary>Colour of the border drawn around the track.</summary>
    public Vector4 BorderColor { get; set; } = new Vector4(0.5f, 0.5f, 0.7f, 1f);

    /// <summary>Whether to draw a one-pixel border around the bar.</summary>
    public bool DrawBorder { get; set; } = true;

    // ── Label ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Optional text drawn centred over the bar.
    /// <c>null</c> or empty suppresses drawing.
    /// </summary>
    public string? Label { get; set; }

    /// <summary>Font size for the label text (logical pixels).</summary>
    public float FontSize { get; set; } = 16f;

    /// <summary>Colour used to render the label text.</summary>
    public Vector4 LabelColor { get; set; } = new Vector4(1f, 1f, 1f, 1f);

    /// <summary>Horizontal padding (logical pixels) between the bar edge and the label text.</summary>
    public float LabelPadding { get; set; } = 8f;

    // ── Drawing ───────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    protected override void OnDraw(IUIRenderer renderer, Vector2 position, Vector2 size)
    {
        // Track (background)
        renderer.DrawRect(position, size, TrackColor);

        // Fill
        if (_value > 0f)
        {
            var fillSize = new Vector2(size.X * _value, size.Y);
            renderer.DrawRect(position, fillSize, FillColor);
        }

        // Border
        if (DrawBorder)
            renderer.DrawRectOutline(position, size, BorderColor);

        // Label
        if (!string.IsNullOrEmpty(Label))
        {
            Vector2 textPos = new(position.X + LabelPadding, position.Y + (size.Y - FontSize) / 2f);
            renderer.DrawText(Label, textPos, FontSize, LabelColor);
        }
    }
}
