using OpenTK.Mathematics;

namespace SharpOpenGl.Engine.Rendering;

/// <summary>
/// Platform-agnostic rendering interface. Implementations provide
/// OpenGL (desktop) or WebGL2 (browser) backends.
/// </summary>
public interface IRenderer
{
    /// <summary>Begin a render frame with the given projection and view matrices.</summary>
    void BeginFrame(Matrix4 projection, Matrix4 view);

    /// <summary>Draw a mesh with the given model transform and color override.</summary>
    /// <param name="vao">Vertex array object handle.</param>
    /// <param name="vertexCount">Number of vertices to draw.</param>
    /// <param name="model">Model transform matrix.</param>
    /// <param name="color">Override color (alpha 0 = use vertex colors).</param>
    /// <param name="primitiveType">OpenGL primitive type.</param>
    void DrawMesh(int vao, int vertexCount, Matrix4 model, Vector4 color, int primitiveType);

    /// <summary>End the current render frame and present to screen.</summary>
    void EndFrame();

    /// <summary>Called when the viewport is resized.</summary>
    void Resize(int width, int height);
}
