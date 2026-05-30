using OpenTK.Mathematics;

namespace SharpOpenGl.Engine.ECS;

/// <summary>
/// Rendering data for an entity. Used by <see cref="RenderSystem"/> each frame.
/// </summary>
public sealed class RenderComponent
{
    /// <summary>
    /// Vertex array object handle returned by the renderer.
    /// A value of <c>-1</c> means "use default object".
    /// </summary>
    public int MeshId { get; set; } = -1;

    /// <summary>Number of vertices in the mesh.</summary>
    public int VertexCount { get; set; }

    /// <summary>
    /// RGBA tint color. An alpha of 0 means "use vertex colors unchanged".
    /// </summary>
    public Vector4 Color { get; set; } = new Vector4(1f, 1f, 1f, 1f);

    /// <summary>When <c>false</c> the entity is skipped during rendering.</summary>
    public bool Visible { get; set; } = true;

    /// <summary>OpenGL primitive type (e.g. GL_TRIANGLES = 4, GL_LINES = 1).</summary>
    public int PrimitiveType { get; set; } = 4; // GL_TRIANGLES
}
