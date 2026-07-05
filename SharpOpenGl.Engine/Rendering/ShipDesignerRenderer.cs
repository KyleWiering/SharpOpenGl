using OpenTK.Mathematics;
using SharpOpenGl.Engine.UI.Screens;

namespace SharpOpenGl.Engine.Rendering;

/// <summary>
/// Drives the 3-D ship preview in the <see cref="ShipDesignerScreen"/>.
/// Renders a rotating ship model tinted by the screen's current colour selections.
/// </summary>
/// <remarks>
/// Create one instance per designer session and call <see cref="Render"/> each frame
/// while the designer screen is active.  Pass the camera/projection matrices from
/// the engine window.
/// </remarks>
public sealed class ShipDesignerRenderer : IDisposable
{
    private readonly MeshRegistry _registry;
    private bool _disposed;

    /// <summary>Rotation speed in degrees per second for the auto-spin preview.</summary>
    public float AutoRotateSpeed { get; set; } = 30f;

    /// <summary>World-space position the preview model is centred on.</summary>
    public Vector3 PreviewOrigin { get; set; } = new Vector3(0f, 0f, 0f);

    /// <summary>Uniform scale factor applied to the preview model.</summary>
    public float PreviewScale { get; set; } = 1.5f;

    /// <param name="registry">
    /// Mesh registry used to resolve ship mesh keys.
    /// Must contain entries for the default fallback keys.
    /// </param>
    public ShipDesignerRenderer(MeshRegistry registry)
    {
        _registry = registry;
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// Render the ship preview using the current state of <paramref name="screen"/>.
    /// </summary>
    public void Render(
        ShipDesignerScreen screen,
        string meshKey,
        string fallbackKey,
        IRenderer renderer,
        Matrix4 projection,
        Matrix4 view,
        float? previewScale = null,
        int raceTextureIndex = -1,
        Vector3 teamTint = default)
    {
        if (_disposed) return;

        MeshRegistry.MeshEntry? mesh = _registry.GetOrFallback(meshKey, fallbackKey);
        if (mesh == null || mesh.Vao == 0) return;

        float scale = previewScale ?? PreviewScale;
        float yaw = MathHelper.DegreesToRadians(screen.RotationDegrees);
        Matrix4 model =
            Matrix4.CreateScale(scale) *
            Matrix4.CreateRotationY(yaw) *
            Matrix4.CreateTranslation(PreviewOrigin);

        Vector4 color = raceTextureIndex >= 0 ? Vector4.Zero : screen.PrimaryColor;

        renderer.BeginFrame(projection, view);
        renderer.DrawMesh(
            mesh.Vao,
            mesh.VertexCount,
            model,
            color,
            4 /* GL_TRIANGLES */,
            raceTextureIndex,
            teamTint);
        renderer.EndFrame();
    }

    /// <summary>
    /// Advance the auto-rotate angle (call each frame while in designer mode).
    /// This updates <see cref="ShipDesignerScreen.RotationDegrees"/> via <see cref="ShipDesignerScreen.Rotate"/>.
    /// </summary>
    public void AutoRotate(ShipDesignerScreen screen, float deltaTime)
    {
        screen.Rotate(AutoRotateSpeed * deltaTime);
    }

    /// <inheritdoc/>
    public void Dispose() => _disposed = true;
}