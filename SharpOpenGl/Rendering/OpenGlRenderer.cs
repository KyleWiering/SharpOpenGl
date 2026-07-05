using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using SharpOpenGl.Engine.Rendering;

namespace SharpOpenGl.Rendering;

/// <summary>Desktop OpenGL implementation of <see cref="IRenderer"/>.</summary>
public sealed class OpenGlRenderer : IRenderer
{
    private readonly int _program;
    private readonly int _uniformProjection;
    private readonly int _uniformView;
    private readonly int _uniformModel;
    private readonly int _uniformColor;
    private readonly int _uniformRaceTextureIndex;
    private readonly int _uniformTeamTint;

    public OpenGlRenderer(
        int program,
        int uniformProjection,
        int uniformView,
        int uniformModel,
        int uniformColor,
        int uniformRaceTextureIndex,
        int uniformTeamTint)
    {
        _program = program;
        _uniformProjection = uniformProjection;
        _uniformView = uniformView;
        _uniformModel = uniformModel;
        _uniformColor = uniformColor;
        _uniformRaceTextureIndex = uniformRaceTextureIndex;
        _uniformTeamTint = uniformTeamTint;
    }

    public void BeginFrame(Matrix4 projection, Matrix4 view)
    {
        GL.UseProgram(_program);
        GL.Enable(EnableCap.DepthTest);
        GL.UniformMatrix4(_uniformProjection, false, ref projection);
        GL.UniformMatrix4(_uniformView, false, ref view);
    }

    public void DrawMesh(
        int vao, int vertexCount, Matrix4 model, Vector4 color, int primitiveType,
        int raceTextureIndex = -1, Vector3 teamTint = default)
    {
        if (vao <= 0 || vertexCount <= 0) return;

        GL.UniformMatrix4(_uniformModel, false, ref model);
        GL.Uniform4(_uniformColor, color);
        GL.Uniform1(_uniformRaceTextureIndex, raceTextureIndex);
        GL.Uniform3(_uniformTeamTint, teamTint);
        GL.BindVertexArray(vao);
        GL.DrawArrays((PrimitiveType)primitiveType, 0, vertexCount);
    }

    public void EndFrame() => GL.BindVertexArray(0);

    public void Resize(int width, int height) { }
}