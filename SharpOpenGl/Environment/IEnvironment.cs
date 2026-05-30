using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace SharpOpenGl.Environment;

/// <summary>
/// Interface for environment objects that can be initialized, rendered, and updated.
/// </summary>
public interface IEnvironment : IDisposable
{
    void Initialize();
    void Render(int shaderProgram, int modelUniform, int colorUniform);
    void Update(double elapsedTime);
}
