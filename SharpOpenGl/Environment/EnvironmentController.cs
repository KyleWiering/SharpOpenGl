using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace SharpOpenGl.Environment;

/// <summary>
/// Manages all environment objects (spacefield, model).
/// Ported from original EnvironmentController.cs.
/// </summary>
public class EnvironmentController : IDisposable
{
    private Spacefield? _spacefield;
    private RotatingModel? _model;

    public void Initialize()
    {
        _spacefield = new Spacefield();
        _spacefield.Initialize();

        _model = new RotatingModel();
        _model.Initialize();
    }

    public void Render(int shaderProgram, int modelUniform, int colorUniform)
    {
        _spacefield?.Render(shaderProgram, modelUniform, colorUniform);
        _model?.Render(shaderProgram, modelUniform, colorUniform);
    }

    public void Update(double elapsedTime)
    {
        _model?.Update(elapsedTime);
    }

    public void Dispose()
    {
        _spacefield?.Dispose();
        _model?.Dispose();
    }
}
