using OpenTK.Mathematics;

namespace SharpOpenGl;

public partial class EngineWindow
{
    /// <summary>
    /// UI viewport in client pixels. MousePosition and the 2D ortho projection use this space.
    /// </summary>
    private Vector2 UiViewportSize
    {
        get
        {
            // ClientSize is the drawable area; Size includes the window frame/title bar.
            int w = ClientSize.X > 0 ? ClientSize.X : Size.X;
            int h = ClientSize.Y > 0 ? ClientSize.Y : Size.Y;
            return new Vector2(w, h);
        }
    }

    /// <summary>Pointer position in the same client-pixel space as <see cref="UiViewportSize"/>.</summary>
    private Vector2 UiMousePosition => new(MousePosition.X, MousePosition.Y);
}