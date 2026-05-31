using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using SharpOpenGl.Engine.ECS;
using SharpOpenGl.Engine.Events;
using SharpOpenGl.Engine.Rendering;
using SharpOpenGl.Engine.Scenes;
using SharpOpenGl.Engine.UI;
using SharpOpenGl.Engine.UI.Screens;
using SharpOpenGl.Environment;
using StbImageWriteSharp;
using PixelFormat = OpenTK.Graphics.OpenGL4.PixelFormat;

namespace SharpOpenGl;

/// <summary>
/// Main RTS game window. Manages scene transitions between main menu and gameplay.
/// </summary>
public class EngineWindow : GameWindow
{
    private RTSCameraController _rtsCamera = null!;
    private EnvironmentController _environment = null!;
    private int _frameCount;
    private readonly bool _screenshotMode;
    private readonly string _screenshotPath;

    private ShaderManager _shaderManager = null!;
    private int _shaderProgram;
    private int _uniformProjection;
    private int _uniformView;
    private int _uniformModel;
    private int _uniformColor;

    // Scene & UI management
    private EventBus _eventBus = null!;
    private SceneManager _sceneManager = null!;
    private UIManager _uiManager = null!;
    private GLUIRenderer _uiRenderer = null!;

    // ECS (initialized when gameplay starts)
    private World? _world;
    private MovementSystem? _movementSystem;

    // Ship meshes (initialized when gameplay starts)
    private int _heroVao, _heroVbo, _heroVertCount;
    private int _fighterVao, _fighterVbo, _fighterVertCount;
    private int _selectionVao, _selectionVbo, _selectionVertCount;
    private int _moveTargetVao, _moveTargetVbo, _moveTargetVertCount;
    private int _gridVao, _gridVbo, _gridVertCount;
    private bool _gameplayMeshesLoaded;

    // Game entities
    private Entity _heroEntity;
    private readonly List<Entity> _fighterEntities = new();

    // Move target indicator
    private Vector3? _moveTargetPosition;
    private float _moveTargetTimer;

    // Input state for Escape key debounce
    private bool _escapeWasDown;

    public EngineWindow(GameWindowSettings gameSettings, NativeWindowSettings nativeSettings,
        bool screenshotMode = false, string screenshotPath = "screenshot.png")
        : base(gameSettings, nativeSettings)
    {
        _screenshotMode = screenshotMode;
        _screenshotPath = screenshotPath;
        _frameCount = 0;
    }

    protected override void OnLoad()
    {
        base.OnLoad();

        GL.ClearColor(0.0f, 0.0f, 0.05f, 1.0f);
        GL.Enable(EnableCap.DepthTest);
        GL.DepthFunc(DepthFunction.Less);
        GL.Enable(EnableCap.ProgramPointSize);
        GL.Enable(EnableCap.Blend);
        GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

        _shaderManager = new ShaderManager();
        _shaderProgram = _shaderManager.CreateProgram(VertexShaderSource, FragmentShaderSource);
        _uniformProjection = ShaderManager.GetUniform(_shaderProgram, "projection");
        _uniformView = ShaderManager.GetUniform(_shaderProgram, "view");
        _uniformModel = ShaderManager.GetUniform(_shaderProgram, "model");
        _uniformColor = ShaderManager.GetUniform(_shaderProgram, "overrideColor");

        // Environment (starfield background)
        _environment = new EnvironmentController();
        _environment.Initialize();

        // RTS Camera
        _rtsCamera = new RTSCameraController
        {
            Target = new Vector3(0f, 0f, 0f),
            Height = 80f,
            TiltAngle = 35f,
        };

        // UI Renderer
        _uiRenderer = new GLUIRenderer();
        _uiRenderer.Initialize(Size.X, Size.Y);

        // Event bus & managers
        _eventBus = new EventBus();
        _sceneManager = new SceneManager(_eventBus);
        _uiManager = new UIManager(_eventBus);

        // Register scenes
        _sceneManager.Register("MainMenu", () => new MainMenuScene(this));
        _sceneManager.Register("Gameplay", () => new GameplayScene(this));

        // Start at main menu (or skip to gameplay in screenshot mode)
        if (_screenshotMode)
        {
            _sceneManager.TransitionTo("Gameplay", GameState.Playing);
        }
        else
        {
            _sceneManager.TransitionTo("MainMenu", GameState.MainMenu);
        }

        Console.WriteLine("SharpOpenGL RTS Engine initialized.");
    }

    // ── Scene transitions ─────────────────────────────────────────────────────

    internal void ShowMainMenu()
    {
        _uiManager.Clear();
        var menu = new MainMenuScreen();
        menu.NewGameRequested += StartNewGame;
        menu.QuitRequested += () => Close();
        _uiManager.Push(menu);
    }

    internal void StartNewGame()
    {
        _sceneManager.TransitionTo("Gameplay", GameState.Playing);
    }

    internal void StartGameplay()
    {
        _uiManager.Clear();
        var hud = new GameplayHUD();
        hud.PauseRequested += ShowPauseMenu;
        _uiManager.Push(hud);

        if (!_gameplayMeshesLoaded)
        {
            LoadGameplayMeshes();
        }
        InitializeWorld();
        Console.WriteLine("Game started! WASD=pan, Scroll=zoom, LClick=select, RClick=move, Esc=pause");
    }

    private void ShowPauseMenu()
    {
        if (_sceneManager.State != GameState.Playing) return;

        var pause = new PauseScreen();
        pause.ResumeRequested += () =>
        {
            _uiManager.Pop();
        };
        pause.QuitToMenuRequested += () =>
        {
            CleanupGameplay();
            _sceneManager.TransitionTo("MainMenu", GameState.MainMenu);
        };
        _uiManager.Push(pause);
    }

    private void LoadGameplayMeshes()
    {
        (_heroVao, _heroVbo, _heroVertCount) =
            ShipMeshBuilder.BuildShipMesh(new Vector3(0.2f, 0.8f, 1.0f), 3f);
        (_fighterVao, _fighterVbo, _fighterVertCount) =
            ShipMeshBuilder.BuildShipMesh(new Vector3(0.4f, 1.0f, 0.4f), 1.5f);
        (_selectionVao, _selectionVbo, _selectionVertCount) =
            ShipMeshBuilder.BuildSelectionRing(new Vector3(0f, 1f, 0f), 3f);
        (_moveTargetVao, _moveTargetVbo, _moveTargetVertCount) =
            ShipMeshBuilder.BuildMoveTarget(new Vector3(0f, 1f, 0.5f), 2f);
        (_gridVao, _gridVbo, _gridVertCount) =
            MeshBuilder.BuildGrid(20, 20, 10f, new Vector3(0.15f, 0.15f, 0.25f));
        _gameplayMeshesLoaded = true;
    }

    private void InitializeWorld()
    {
        _world?.Dispose();
        _fighterEntities.Clear();

        _world = new World();
        _movementSystem = new MovementSystem();
        _world.AddSystem(_movementSystem);

        // Spawn hero ship at center
        _heroEntity = _world.CreateEntity();
        _world.AddComponent(_heroEntity, new TransformComponent
        {
            Position = new Vector3(0f, 0f, 0f),
            Scale = Vector3.One
        });
        _world.AddComponent(_heroEntity, new MovementComponent
        {
            Speed = 25f,
            Acceleration = 40f,
            TurnRate = 180f,
        });
        _world.AddComponent(_heroEntity, new SelectionComponent
        {
            IsSelected = true,
            SelectionRadius = 4f,
        });
        _world.AddComponent(_heroEntity, new RenderComponent
        {
            MeshId = _heroVao,
            VertexCount = _heroVertCount,
            Color = new Vector4(0.2f, 0.8f, 1.0f, 1f),
            Visible = true,
            PrimitiveType = (int)PrimitiveType.Triangles,
        });

        // Spawn a squad of fighters
        var rng = new Random(123);
        for (int i = 0; i < 5; i++)
        {
            float x = (rng.NextSingle() - 0.5f) * 40f;
            float z = (rng.NextSingle() - 0.5f) * 40f;

            var fighter = _world.CreateEntity();
            _world.AddComponent(fighter, new TransformComponent
            {
                Position = new Vector3(x, 0f, z),
                Scale = Vector3.One,
            });
            _world.AddComponent(fighter, new MovementComponent
            {
                Speed = 35f,
                Acceleration = 50f,
                TurnRate = 220f,
            });
            _world.AddComponent(fighter, new SelectionComponent
            {
                IsSelected = false,
                SelectionRadius = 2.5f,
            });
            _world.AddComponent(fighter, new RenderComponent
            {
                MeshId = _fighterVao,
                VertexCount = _fighterVertCount,
                Color = new Vector4(0.4f, 1.0f, 0.4f, 1f),
                Visible = true,
                PrimitiveType = (int)PrimitiveType.Triangles,
            });
            _fighterEntities.Add(fighter);
        }
    }

    private void CleanupGameplay()
    {
        _world?.Dispose();
        _world = null;
        _movementSystem = null;
        _fighterEntities.Clear();
        _moveTargetPosition = null;
        _moveTargetTimer = 0f;
    }

    // ── Rendering ─────────────────────────────────────────────────────────────

    protected override void OnRenderFrame(FrameEventArgs args)
    {
        base.OnRenderFrame(args);

        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        // Always render the starfield background
        var projection = Matrix4.CreatePerspectiveFieldOfView(
            MathHelper.DegreesToRadians(45.0f),
            (float)Size.X / Size.Y,
            0.1f,
            10000.0f);

        var view = _rtsCamera.GetViewMatrix();

        GL.UseProgram(_shaderProgram);
        GL.UniformMatrix4(_uniformProjection, false, ref projection);
        GL.UniformMatrix4(_uniformView, false, ref view);

        _environment.Render(_shaderProgram, _uniformModel, _uniformColor);

        // Render gameplay 3D content only when playing
        if (_sceneManager.State == GameState.Playing && _world != null)
        {
            RenderGameplay(projection, view);
        }

        // Render UI on top
        _uiRenderer.Begin();
        _uiManager.Draw(_uiRenderer);
        _uiRenderer.End();

        GL.BindVertexArray(0);
        SwapBuffers();

        _frameCount++;

        if (_screenshotMode && _frameCount >= 5)
        {
            CaptureScreenshot(_screenshotPath);
            Console.WriteLine($"Screenshot saved to: {_screenshotPath}");
            Close();
        }
    }

    private void RenderGameplay(Matrix4 projection, Matrix4 view)
    {
        GL.UseProgram(_shaderProgram);
        GL.UniformMatrix4(_uniformProjection, false, ref projection);
        GL.UniformMatrix4(_uniformView, false, ref view);

        // Render grid
        var gridModel = Matrix4.CreateTranslation(-100f, -0.5f, -100f);
        GL.UniformMatrix4(_uniformModel, false, ref gridModel);
        GL.Uniform4(_uniformColor, new Vector4(0, 0, 0, 0));
        GL.BindVertexArray(_gridVao);
        GL.DrawArrays(PrimitiveType.Lines, 0, _gridVertCount);

        // Render all ships
        foreach (var (entity, render) in _world!.Query<RenderComponent>())
        {
            if (!render.Visible) continue;
            var transform = _world.GetComponent<TransformComponent>(entity);
            if (transform == null) continue;

            Matrix4 model = transform.GetModelMatrix();
            GL.UniformMatrix4(_uniformModel, false, ref model);
            GL.Uniform4(_uniformColor, new Vector4(0, 0, 0, 0));

            GL.BindVertexArray(render.MeshId);
            GL.DrawArrays((PrimitiveType)render.PrimitiveType, 0, render.VertexCount);
        }

        // Render selection rings
        foreach (var (entity, sel) in _world.Query<SelectionComponent>())
        {
            if (!sel.IsSelected) continue;
            var transform = _world.GetComponent<TransformComponent>(entity);
            if (transform == null) continue;

            var ringModel = Matrix4.CreateTranslation(transform.Position);
            GL.UniformMatrix4(_uniformModel, false, ref ringModel);
            GL.Uniform4(_uniformColor, new Vector4(0, 1, 0, 1));
            GL.BindVertexArray(_selectionVao);
            GL.DrawArrays(PrimitiveType.Lines, 0, _selectionVertCount);
        }

        // Render move target indicator
        if (_moveTargetPosition.HasValue && _moveTargetTimer > 0f)
        {
            float pulse = 0.5f + 0.5f * MathF.Sin(_moveTargetTimer * 8f);
            var targetModel = Matrix4.CreateScale(pulse * 0.5f + 0.5f) *
                              Matrix4.CreateTranslation(_moveTargetPosition.Value);
            GL.UniformMatrix4(_uniformModel, false, ref targetModel);
            GL.Uniform4(_uniformColor, new Vector4(0, 1, 0.5f, 1));
            GL.BindVertexArray(_moveTargetVao);
            GL.DrawArrays(PrimitiveType.Lines, 0, _moveTargetVertCount);
        }
    }

    // ── Update ────────────────────────────────────────────────────────────────

    protected override void OnUpdateFrame(FrameEventArgs args)
    {
        base.OnUpdateFrame(args);

        if (!IsFocused && !_screenshotMode)
            return;

        float dt = (float)args.Time;

        // Handle Escape key with debounce
        bool escapeDown = KeyboardState.IsKeyDown(Keys.Escape);
        if (escapeDown && !_escapeWasDown)
        {
            HandleEscapePressed();
        }
        _escapeWasDown = escapeDown;

        // Update scene
        _sceneManager.Update(dt);

        // Update UI
        _uiManager.Update(dt);

        // Gameplay-specific updates
        if (_sceneManager.State == GameState.Playing && _world != null)
        {
            // Camera panning with WASD
            float panX = 0f, panZ = 0f;
            if (KeyboardState.IsKeyDown(Keys.W) || KeyboardState.IsKeyDown(Keys.Up))
                panZ = -1f;
            if (KeyboardState.IsKeyDown(Keys.S) || KeyboardState.IsKeyDown(Keys.Down))
                panZ = 1f;
            if (KeyboardState.IsKeyDown(Keys.A) || KeyboardState.IsKeyDown(Keys.Left))
                panX = -1f;
            if (KeyboardState.IsKeyDown(Keys.D) || KeyboardState.IsKeyDown(Keys.Right))
                panX = 1f;
            _rtsCamera.Pan(panX, panZ, dt);

            // Update ECS world
            _world.Update(dt);

            // Fade move target indicator
            if (_moveTargetTimer > 0f)
                _moveTargetTimer -= dt;
        }
    }

    private void HandleEscapePressed()
    {
        switch (_sceneManager.State)
        {
            case GameState.MainMenu:
                Close();
                break;
            case GameState.Playing:
                // Check if pause menu is already showing
                if (_uiManager.Current is PauseScreen)
                    _uiManager.Pop(); // Resume
                else
                    ShowPauseMenu();
                break;
        }
    }

    // ── Input ─────────────────────────────────────────────────────────────────

    protected override void OnMouseWheel(MouseWheelEventArgs e)
    {
        base.OnMouseWheel(e);
        if (_sceneManager.State == GameState.Playing)
            _rtsCamera.Zoom(e.OffsetY);
    }

    protected override void OnMouseDown(MouseButtonEventArgs e)
    {
        base.OnMouseDown(e);

        // Route click to UI first
        var screenPoint = new Vector2(MousePosition.X, MousePosition.Y);
        var viewportSize = new Vector2(Size.X, Size.Y);
        if (_uiManager.HandlePointerTapped(screenPoint, (int)e.Button, viewportSize))
            return;

        // If UI didn't consume, handle gameplay input
        if (_sceneManager.State != GameState.Playing || _world == null)
            return;

        Vector3? worldPos = ScreenToWorld(MousePosition);
        if (worldPos == null) return;

        if (e.Button == MouseButton.Left)
        {
            HandleSelection(worldPos.Value);
        }
        else if (e.Button == MouseButton.Right)
        {
            HandleMoveCommand(worldPos.Value);
        }
    }

    private void HandleSelection(Vector3 worldPos)
    {
        if (_world == null) return;

        bool shiftHeld = KeyboardState.IsKeyDown(Keys.LeftShift) ||
                         KeyboardState.IsKeyDown(Keys.RightShift);

        Entity? hitEntity = null;
        float closestDist = float.MaxValue;

        foreach (var (entity, sel) in _world.Query<SelectionComponent>())
        {
            var transform = _world.GetComponent<TransformComponent>(entity);
            if (transform == null) continue;

            float dist = (transform.Position - worldPos).Length;
            if (dist < sel.SelectionRadius && dist < closestDist)
            {
                closestDist = dist;
                hitEntity = entity;
            }
        }

        if (!shiftHeld)
        {
            foreach (var (_, sel) in _world.Query<SelectionComponent>())
                sel.IsSelected = false;
        }

        if (hitEntity.HasValue)
        {
            var sel = _world.GetComponent<SelectionComponent>(hitEntity.Value);
            if (sel != null)
                sel.IsSelected = shiftHeld ? !sel.IsSelected : true;
        }
    }

    private void HandleMoveCommand(Vector3 worldPos)
    {
        if (_world == null) return;

        var selectedEntities = new List<Entity>();
        foreach (var (entity, sel) in _world.Query<SelectionComponent>())
        {
            if (sel.IsSelected)
                selectedEntities.Add(entity);
        }

        if (selectedEntities.Count == 0) return;

        for (int i = 0; i < selectedEntities.Count; i++)
        {
            var movement = _world.GetComponent<MovementComponent>(selectedEntities[i]);
            if (movement == null) continue;

            Vector3 offset = Vector3.Zero;
            if (selectedEntities.Count > 1)
            {
                float angle = MathF.PI * 2f * i / selectedEntities.Count;
                float radius = 4f;
                offset = new Vector3(MathF.Cos(angle) * radius, 0f, MathF.Sin(angle) * radius);
            }

            movement.PathTarget = worldPos + offset;
        }

        _moveTargetPosition = worldPos;
        _moveTargetTimer = 2f;
    }

    /// <summary>
    /// Convert screen coordinates to world XZ plane (Y=0) position using raycasting.
    /// </summary>
    private Vector3? ScreenToWorld(Vector2 screenPos)
    {
        float ndcX = (2f * screenPos.X / Size.X) - 1f;
        float ndcY = 1f - (2f * screenPos.Y / Size.Y);

        var projection = Matrix4.CreatePerspectiveFieldOfView(
            MathHelper.DegreesToRadians(45.0f),
            (float)Size.X / Size.Y,
            0.1f,
            10000.0f);

        var view = _rtsCamera.GetViewMatrix();

        var invProj = Matrix4.Invert(projection);
        var invView = Matrix4.Invert(view);

        var nearPoint = new Vector4(ndcX, ndcY, -1f, 1f) * invProj * invView;
        var farPoint = new Vector4(ndcX, ndcY, 1f, 1f) * invProj * invView;

        if (MathF.Abs(nearPoint.W) < 0.0001f || MathF.Abs(farPoint.W) < 0.0001f)
            return null;

        Vector3 near = new Vector3(nearPoint.X, nearPoint.Y, nearPoint.Z) / nearPoint.W;
        Vector3 far = new Vector3(farPoint.X, farPoint.Y, farPoint.Z) / farPoint.W;

        Vector3 dir = far - near;

        if (MathF.Abs(dir.Y) < 0.0001f)
            return null;

        float t = -near.Y / dir.Y;
        if (t < 0f) return null;

        return near + dir * t;
    }

    protected override void OnResize(ResizeEventArgs e)
    {
        base.OnResize(e);
        GL.Viewport(0, 0, e.Width, e.Height);
        _uiRenderer?.UpdateViewport(e.Width, e.Height);
    }

    protected override void OnUnload()
    {
        base.OnUnload();
        CleanupGameplay();
        _environment.Dispose();
        _shaderManager.Dispose();
        _uiRenderer.Dispose();
        _sceneManager.Dispose();

        if (_gameplayMeshesLoaded)
        {
            MeshBuilder.DeleteMesh(_heroVao, _heroVbo);
            MeshBuilder.DeleteMesh(_fighterVao, _fighterVbo);
            MeshBuilder.DeleteMesh(_selectionVao, _selectionVbo);
            MeshBuilder.DeleteMesh(_moveTargetVao, _moveTargetVbo);
            MeshBuilder.DeleteMesh(_gridVao, _gridVbo);
        }
    }

    private void CaptureScreenshot(string path)
    {
        int width = Size.X;
        int height = Size.Y;

        byte[] pixels = new byte[width * height * 4];
        GL.ReadPixels(0, 0, width, height, PixelFormat.Rgba, PixelType.UnsignedByte, pixels);

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

    // ── Shader sources ────────────────────────────────────────────────────────

    private const string VertexShaderSource = @"
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

    private const string FragmentShaderSource = @"
#version 330 core
in vec3 fragColor;
out vec4 outputColor;

void main()
{
    outputColor = vec4(fragColor, 1.0);
}
";
}

// ── Scene implementations ─────────────────────────────────────────────────

/// <summary>Main menu scene - shows the menu UI.</summary>
internal sealed class MainMenuScene : IScene
{
    private readonly EngineWindow _window;

    public MainMenuScene(EngineWindow window) => _window = window;

    public void Load() => _window.ShowMainMenu();
    public void Update(float deltaTime) { }
    public void Unload() { }
}

/// <summary>Gameplay scene - RTS gameplay with ships and commands.</summary>
internal sealed class GameplayScene : IScene
{
    private readonly EngineWindow _window;

    public GameplayScene(EngineWindow window) => _window = window;

    public void Load() => _window.StartGameplay();
    public void Update(float deltaTime) { }
    public void Unload() { }
}

