using OpenTK.Mathematics;

namespace SharpOpenGl.Engine.ECS;

/// <summary>
/// Rendering data for an entity. Used by <see cref="RenderSystem"/> each frame.
/// </summary>
public sealed class RenderComponent
{
    /// <summary>
    /// Asset key for the mesh to render (e.g. "meshes/hero_vanguard.obj").
    /// The renderer resolves this key to a GPU buffer on first render.
    /// An empty string means "use the default fallback object".
    /// </summary>
    public string MeshKey { get; set; } = string.Empty;

    /// <summary>
    /// GPU vertex-array-object handle assigned by the renderer at load time.
    /// A value of <c>-1</c> means the mesh has not been uploaded yet.
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

    /// <summary>Procedural race texture shader index (0–7), or -1 to skip race plating.</summary>
    public int RaceTextureIndex { get; set; } = -1;

    /// <summary>Per-player faction tint multiplied with race texture in the shader.</summary>
    public Vector3 TeamTint { get; set; } = Vector3.One;
}