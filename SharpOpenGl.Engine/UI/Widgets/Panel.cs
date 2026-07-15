using OpenTK.Mathematics;

namespace SharpOpenGl.Engine.UI.Widgets;

/// <summary>
/// A rectangular container widget that draws a background and lays out children.
/// </summary>
/// <remarks>
/// Children are positioned according to their own <see cref="Widget.Anchor"/> and
/// <see cref="Widget.Position"/> values relative to this panel's resolved bounds.
/// </remarks>
public class Panel : Widget
{
    // ── Visual ────────────────────────────────────────────────────────────────

    /// <summary>Background fill colour.  Use alpha = 0 for a transparent panel.</summary>
    public Vector4 BackgroundColor { get; set; } = new Vector4(0.1f, 0.1f, 0.15f, 0.85f);

    /// <summary>Border colour.  Use alpha = 0 to hide the border.</summary>
    public Vector4 BorderColor { get; set; } = new Vector4(0.4f, 0.4f, 0.6f, 1f);

    /// <summary>Whether to draw a 1-pixel border around the panel.</summary>
    public bool DrawBorder { get; set; } = true;

    // ── Drawing ───────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    protected override void OnDraw(IUIRenderer renderer, Vector2 position, Vector2 size)
    {
        if (BackgroundColor.W > 0f)
            renderer.DrawRect(position, size, BackgroundColor);

        if (DrawBorder && BorderColor.W > 0f)
            renderer.DrawRectOutline(position, size, BorderColor);
    }
}
