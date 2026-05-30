using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using SharpOpenGl.Environment;
using StbImageWriteSharp;
using PixelFormat = OpenTK.Graphics.OpenGL4.PixelFormat;

namespace SharpOpenGl;

/// <summary>
/// Main game window using OpenTK 4.x GameWindow.
/// Replaces the old WinForms Form1 + GLControl approach.
/// </summary>
public class EngineWindow : GameWindow
{
    private Camera _camera;
    private InputHandler _inputHandler;
    private EnvironmentController _environment;
    private int _frameCount;
    private readonly bool _screenshotMode;
    private readonly string _screenshotPath;

    // Shader program
    private int _shaderProgram;
    private int _uniformProjection;
    private int _uniformView;
    private int _uniformModel;
    private int _uniformColor;

    public EngineWindow(GameWindowSettings gameSettings, NativeWindowSettings nativeSettings,
        bool screenshotMode = false, string screenshotPath = "screenshot.png")
        : base(gameSettings, nativeSettings)
    {
        _screenshotMode = screenshotMode;
        _screenshotPath = screenshotPath;
        _camera = new Camera();
        _inputHandler = new InputHandler();
        _environment = new EnvironmentController();
        _frameCount = 0;
    }

    protected override void OnLoad()
    {
        base.OnLoad();

        GL.ClearColor(0.0f, 0.0f, 0.05f, 1.0f);
        GL.Enable(EnableCap.DepthTest);
        GL.DepthFunc(DepthFunction.Less);
        GL.Enable(EnableCap.ProgramPointSize);

        InitShaders();
        _environment.Initialize();

        Console.WriteLine("SharpOpenGL Engine initialized.");
    }

    protected override void OnRenderFrame(FrameEventArgs args)
    {
        base.OnRenderFrame(args);

        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        // Set up matrices
        var projection = Matrix4.CreatePerspectiveFieldOfView(
            MathHelper.DegreesToRadians(45.0f),
            (float)Size.X / Size.Y,
            0.1f,
            10000.0f);

        var view = _camera.GetViewMatrix();

        GL.UseProgram(_shaderProgram);
        GL.UniformMatrix4(_uniformProjection, false, ref projection);
        GL.UniformMatrix4(_uniformView, false, ref view);

        _environment.Render(_shaderProgram, _uniformModel, _uniformColor);

        SwapBuffers();

        _frameCount++;

        // In screenshot mode, render a few frames then capture and exit
        if (_screenshotMode && _frameCount >= 5)
        {
            CaptureScreenshot(_screenshotPath);
            Console.WriteLine($"Screenshot saved to: {_screenshotPath}");
            Close();
        }
    }

    protected override void OnUpdateFrame(FrameEventArgs args)
    {
        base.OnUpdateFrame(args);

        if (!IsFocused && !_screenshotMode)
            return;

        _inputHandler.Update(KeyboardState);
        _camera.MoveXAxis(_inputHandler.AxisMovement.X * (float)args.Time * 50f);
        _camera.MoveYAxis(_inputHandler.AxisMovement.Y * (float)args.Time * 50f);
        _camera.MoveZAxis(_inputHandler.AxisMovement.Z * (float)args.Time * 50f);
        _camera.RotateX(_inputHandler.AxisRotation.X * (float)args.Time);
        _camera.RotateY(_inputHandler.AxisRotation.Y * (float)args.Time);

        _environment.Update(args.Time);

        if (KeyboardState.IsKeyDown(Keys.Escape))
            Close();
    }

    protected override void OnResize(ResizeEventArgs e)
    {
        base.OnResize(e);
        GL.Viewport(0, 0, e.Width, e.Height);
    }

    protected override void OnUnload()
    {
        base.OnUnload();
        _environment.Dispose();
        GL.DeleteProgram(_shaderProgram);
    }

    private void InitShaders()
    {
        string vertexShaderSource = @"
#version 330 core
layout(location = 0) in vec3 aPosition;
layout(location = 1) in vec3 aColor;

uniform mat4 projection;
uniform mat4 view;
uniform mat4 model;
uniform vec4 overrideColor;

out vec3 fragColor;

void main()
{
    gl_Position = projection * view * model * vec4(aPosition, 1.0);
    gl_PointSize = 2.0;
    if (overrideColor.a > 0.0)
        fragColor = overrideColor.rgb;
    else
        fragColor = aColor;
}
";

        string fragmentShaderSource = @"
#version 330 core
in vec3 fragColor;
out vec4 outputColor;

void main()
{
    outputColor = vec4(fragColor, 1.0);
}
";

        int vertexShader = GL.CreateShader(ShaderType.VertexShader);
        GL.ShaderSource(vertexShader, vertexShaderSource);
        GL.CompileShader(vertexShader);
        CheckShaderCompile(vertexShader);

        int fragmentShader = GL.CreateShader(ShaderType.FragmentShader);
        GL.ShaderSource(fragmentShader, fragmentShaderSource);
        GL.CompileShader(fragmentShader);
        CheckShaderCompile(fragmentShader);

        _shaderProgram = GL.CreateProgram();
        GL.AttachShader(_shaderProgram, vertexShader);
        GL.AttachShader(_shaderProgram, fragmentShader);
        GL.LinkProgram(_shaderProgram);
        CheckProgramLink(_shaderProgram);

        GL.DeleteShader(vertexShader);
        GL.DeleteShader(fragmentShader);

        _uniformProjection = GL.GetUniformLocation(_shaderProgram, "projection");
        _uniformView = GL.GetUniformLocation(_shaderProgram, "view");
        _uniformModel = GL.GetUniformLocation(_shaderProgram, "model");
        _uniformColor = GL.GetUniformLocation(_shaderProgram, "overrideColor");
    }

    private static void CheckShaderCompile(int shader)
    {
        GL.GetShader(shader, ShaderParameter.CompileStatus, out int success);
        if (success == 0)
        {
            string info = GL.GetShaderInfoLog(shader);
            throw new Exception($"Shader compilation failed: {info}");
        }
    }

    private static void CheckProgramLink(int program)
    {
        GL.GetProgram(program, GetProgramParameterName.LinkStatus, out int success);
        if (success == 0)
        {
            string info = GL.GetProgramInfoLog(program);
            throw new Exception($"Program linking failed: {info}");
        }
    }

    private void CaptureScreenshot(string path)
    {
        int width = Size.X;
        int height = Size.Y;

        byte[] pixels = new byte[width * height * 4];
        GL.ReadPixels(0, 0, width, height, PixelFormat.Rgba, PixelType.UnsignedByte, pixels);

        // OpenGL reads bottom-to-top, flip vertically
        byte[] flipped = new byte[width * height * 4];
        int stride = width * 4;
        for (int y = 0; y < height; y++)
        {
            Array.Copy(pixels, (height - 1 - y) * stride, flipped, y * stride, stride);
        }

        using var stream = File.OpenWrite(path);
        var writer = new ImageWriter();
        writer.WritePng(flipped, width, height, ColorComponents.RedGreenBlueAlpha, stream);
    }
}
