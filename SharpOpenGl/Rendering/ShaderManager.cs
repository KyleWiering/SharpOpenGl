using OpenTK.Graphics.OpenGL4;

namespace SharpOpenGl.Rendering;

/// <summary>
/// Compiles, links, and caches OpenGL shader programs.
/// </summary>
public class ShaderManager : IDisposable
{
    private readonly List<int> _programs = new();

    public int CreateProgram(string vertexSource, string fragmentSource)
    {
        int vs = CompileShader(ShaderType.VertexShader, vertexSource);
        int fs = CompileShader(ShaderType.FragmentShader, fragmentSource);

        int program = GL.CreateProgram();
        GL.AttachShader(program, vs);
        GL.AttachShader(program, fs);
        GL.LinkProgram(program);
        CheckProgramLink(program);

        GL.DeleteShader(vs);
        GL.DeleteShader(fs);

        _programs.Add(program);
        return program;
    }

    public static int GetUniform(int program, string name) =>
        GL.GetUniformLocation(program, name);

    private static int CompileShader(ShaderType type, string source)
    {
        int shader = GL.CreateShader(type);
        GL.ShaderSource(shader, source);
        GL.CompileShader(shader);

        GL.GetShader(shader, ShaderParameter.CompileStatus, out int ok);
        if (ok == 0)
        {
            string info = GL.GetShaderInfoLog(shader);
            GL.DeleteShader(shader);
            throw new InvalidOperationException($"Shader ({type}) compile failed: {info}");
        }

        return shader;
    }

    private static void CheckProgramLink(int program)
    {
        GL.GetProgram(program, GetProgramParameterName.LinkStatus, out int ok);
        if (ok == 0)
        {
            string info = GL.GetProgramInfoLog(program);
            throw new InvalidOperationException($"Program link failed: {info}");
        }
    }

    public void Dispose()
    {
        foreach (int p in _programs)
            GL.DeleteProgram(p);
        _programs.Clear();
    }
}