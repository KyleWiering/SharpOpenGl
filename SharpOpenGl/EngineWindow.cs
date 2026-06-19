using System.Text.Json;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using SharpOpenGl.Engine.ECS;
using SharpOpenGl.Engine.Economy;
using SharpOpenGl.Engine.Entities;
using SharpOpenGl.Engine.Events;
using SharpOpenGl.Engine.Rendering;
using SharpOpenGl.Engine.Scenes;
using SharpOpenGl.Engine.UI;
using SharpOpenGl.Engine.UI.Screens;
using SharpOpenGl.Engine.UI.Widgets;
using SharpOpenGl.Environment;
using StbImageWriteSharp;
using PixelFormat = OpenTK.Graphics.OpenGL4.PixelFormat;

namespace SharpOpenGl;

/// <summary>
/// Main RTS game window. Manages scene transitions between main menu and gameplay.
/// </summary>
public partial class EngineWindow : GameWindow
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

    // Scene name constants
    private const string SceneMainMenu = "MainMenu";
    private const string SceneGameplay = "Gameplay";

    // ECS (initialized when gameplay starts)
    private World? _world;
    private MovementSystem? _movementSystem;
    private AIPlayerSystem? _aiSystem;
    private BuildSystem? _buildSystem;

    // Economy
    private ResourceManager? _resourceManager;

    // Entity definitions registry (loaded from GameData JSON)
    private readonly Dictionary<string, EntityDefinition> _definitions = new();
    private UnitFactory? _unitFactory;

    // Board size constants (100x original: was 20×20 cells @ 10f = 200 units, now 200×200 @ 10f = 2000 units)
    private const int GridColumns = 200;
    private const int GridRows = 200;
    private const float GridCellSize = 10f;
    private const float MapWorldSize = GridColumns * GridCellSize; // 2000 units

    // Ship meshes (initialized when gameplay starts)
    private int _heroVao, _heroVbo, _heroVertCount;
    private int _fighterVao, _fighterVbo, _fighterVertCount;
    private int _bomberVao, _bomberVbo, _bomberVertCount;
    private int _destroyerVao, _destroyerVbo, _destroyerVertCount;
    private int _carrierVao, _carrierVbo, _carrierVertCount;
    private int _engineTrailVao, _engineTrailVbo, _engineTrailVertCount;
    private int _selectionVao, _selectionVbo, _selectionVertCount;
    private int _moveTargetVao, _moveTargetVbo, _moveTargetVertCount;
    private int _gridVao, _gridVbo, _gridVertCount;
    private int _resourceNodeVao, _resourceNodeVbo, _resourceNodeVertCount;
    private int _minerVao, _minerVbo, _minerVertCount;
    private int _baseVao, _baseVbo, _baseVertCount;
    private bool _gameplayMeshesLoaded;

    // Game entities
    private Entity _heroEntity;
    private Entity _baseEntity;
    private readonly List<Entity> _fighterEntities = new();
    private readonly List<Entity> _minerEntities = new();
    private readonly List<Entity> _resourceNodeEntities = new();
    private readonly List<Entity> _aiEntities = new();

    // Move target indicator
    private Vector3? _moveTargetPosition;
    private float _moveTargetTimer;

    // Input state for Escape key debounce
    private bool _escapeWasDown;

    // Control groups (Ctrl+1-9 to assign, 1-9 to recall)
    private readonly Dictionary<int, List<Entity>> _controlGroups = new();

    // Input modes for attack-move and patrol
    private bool _attackMoveMode;
    private bool _patrolMode;

    // Building placement mode
    private string? _placementBuildingId;
    private SupplySystem? _supplySystem;

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

        // RTS Camera (adjusted for larger map)
        _rtsCamera = new RTSCameraController
        {
            Target = new Vector3(0f, 0f, 0f),
            Height = 200f,
            TiltAngle = 35f,
            PanSpeed = 300f,
            ZoomSpeed = 40f,
            MinHeight = 40f,
            MaxHeight = 1500f,
        };

        // UI Renderer
        _uiRenderer = new GLUIRenderer();
        _uiRenderer.Initialize(Size.X, Size.Y);

        // Event bus & managers
        _eventBus = new EventBus();
        _sceneManager = new SceneManager(_eventBus);
        _uiManager = new UIManager(_eventBus);
        _uiManager.Resize(new Vector2(Size.X, Size.Y));

        // Register scenes
        _sceneManager.Register(SceneMainMenu, () => new MainMenuScene(this));
        _sceneManager.Register(SceneGameplay, () => new GameplayScene(this));

        // Start at main menu (or skip to gameplay in screenshot mode)
        if (_screenshotMode)
        {
            _sceneManager.TransitionTo(SceneGameplay, GameState.Playing);
        }
        else
        {
            _sceneManager.TransitionTo(SceneMainMenu, GameState.MainMenu);
        }

        Console.WriteLine("SharpOpenGL RTS Engine initialized.");
    }

    // ── Scene transitions ─────────────────────────────────────────────────────

    internal void ShowMainMenu()
    {
        _uiManager.Clear();
        var menu = new MainMenuScreen();
        menu.NewGameRequested += ShowMissionSelect;
        menu.MultiplayerRequested += ShowMultiplayerSetup;
        menu.ContinueRequested += ContinueSavedGame;
        menu.ShipDesignerRequested += ShowShipDesigner;
        menu.SettingsRequested += ShowSettings;
        menu.QuitRequested += () => Close();
        _uiManager.Push(menu);
    }

    /// <summary>Show mission selection screen with available scenarios.</summary>
    internal void ShowMissionSelect()
    {
        var missionSelect = new MissionSelectScreen();

        // Load available missions from GameData/Missions
        var missions = LoadMissionEntries();
        missionSelect.SetMissions(missions);

        missionSelect.MissionStartRequested += missionId =>
        {
            Console.WriteLine($"[Mission] Selected: {missionId}");
            ShowMissionBriefing(missionId);
        };
        missionSelect.BackRequested += () =>
        {
            _uiManager.Pop();
        };
        _uiManager.Push(missionSelect);
    }

    private IEnumerable<MissionEntry> LoadMissionEntries()
    {
        string missionsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GameData", "Missions");
        // Try relative path from working directory if not found
        if (!Directory.Exists(missionsPath))
            missionsPath = Path.Combine(Directory.GetCurrentDirectory(), "GameData", "Missions");

        if (!Directory.Exists(missionsPath))
        {
            Console.WriteLine("[Mission] No missions directory found, using defaults");
            return new[]
            {
                new MissionEntry { Id = "tutorial_01", Title = "Tutorial - First Steps", Description = "Learn the basics of fleet command." },
                new MissionEntry { Id = "example_scenario", Title = "First Contact", Description = "Encounter unknown hostiles in Sector Alpha." },
                new MissionEntry { Id = "mission_02", Title = "Resource Rush", Description = "Secure critical resource nodes before the enemy." },
                new MissionEntry { Id = "mission_03", Title = "Defensive Stand", Description = "Hold your position against waves of enemies." },
                new MissionEntry { Id = "mission_04", Title = "Deep Strike", Description = "Strike behind enemy lines and destroy their base." },
                new MissionEntry { Id = "mission_05", Title = "Final Assault", Description = "Launch the decisive attack on the enemy homeworld." },
            };
        }

        var entries = new List<MissionEntry>();
        foreach (string file in Directory.GetFiles(missionsPath, "*.json"))
        {
            string filename = Path.GetFileNameWithoutExtension(file);
            if (filename.StartsWith('_')) continue;

            // Read display name from JSON if possible
            string title = filename.Replace('_', ' ');
            if (title.Length > 0)
                title = char.ToUpper(title[0]) + title[1..];

            entries.Add(new MissionEntry
            {
                Id = filename,
                Title = title,
                Description = $"Mission: {title}",
            });
        }
        return entries;
    }

    /// <summary>Show multiplayer setup screen with AI player option (issue #6).</summary>
    internal void ShowMultiplayerSetup()
    {
        var mpScreen = new MultiplayerSetupScreen();
        mpScreen.StartRequested += includeAI =>
        {
            Console.WriteLine($"[Multiplayer] Starting match, AI={includeAI}");
            _pendingMissionId = null;
            _sceneManager.TransitionTo(SceneGameplay, GameState.Playing);
        };
        mpScreen.BackRequested += () => _uiManager.Pop();
        _uiManager.Push(mpScreen);
    }

    internal void StartNewGame()
    {
        _sceneManager.TransitionTo(SceneGameplay, GameState.Playing);
    }

    internal void StartGameplay()
    {
        _uiManager.Clear();
        var hud = new GameplayHUD();
        hud.PauseRequested += ShowPauseMenu;
        hud.BuildPanel.BuildRequested += OnBuildPanelBuildRequested;
        hud.ShipControlBar.CommandActivated += HandleShipControlCommand;
        _uiManager.Push(hud);

        if (!_gameplayMeshesLoaded)
        {
            LoadGameplayMeshes();
        }
        InitializeWorld();
        Console.WriteLine("Game started! W/S/Q/E/Z/X/A/D=camera, M/S/P/A=commands, LClick=select, RClick=move");
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
            _sceneManager.TransitionTo(SceneMainMenu, GameState.MainMenu);
        };
        _uiManager.Push(pause);
    }

    private void LoadGameplayMeshes()
    {
        (_heroVao, _heroVbo, _heroVertCount) =
            ShipMeshBuilder.BuildShipMesh(new Vector3(0.2f, 0.8f, 1.0f), 3f);
        (_fighterVao, _fighterVbo, _fighterVertCount) =
            ShipMeshBuilder.BuildShipMesh(new Vector3(0.4f, 1.0f, 0.4f), 1.5f);
        (_bomberVao, _bomberVbo, _bomberVertCount) =
            ShipMeshBuilder.BuildBomberMesh(new Vector3(0.9f, 0.5f, 0.2f), 2.5f);
        (_destroyerVao, _destroyerVbo, _destroyerVertCount) =
            ShipMeshBuilder.BuildDestroyerMesh(new Vector3(0.7f, 0.2f, 0.9f), 3.5f);
        (_carrierVao, _carrierVbo, _carrierVertCount) =
            ShipMeshBuilder.BuildCarrierMesh(new Vector3(0.6f, 0.6f, 0.8f), 4f);
        (_engineTrailVao, _engineTrailVbo, _engineTrailVertCount) =
            ShipMeshBuilder.BuildEngineTrail(new Vector3(1.0f, 0.6f, 0.1f), 2.5f);
        (_selectionVao, _selectionVbo, _selectionVertCount) =
            ShipMeshBuilder.BuildSelectionRing(new Vector3(0f, 1f, 0f), 3f);
        (_moveTargetVao, _moveTargetVbo, _moveTargetVertCount) =
            ShipMeshBuilder.BuildMoveTarget(new Vector3(0f, 1f, 0.5f), 2f);
        (_gridVao, _gridVbo, _gridVertCount) =
            MeshBuilder.BuildGrid(GridColumns, GridRows, GridCellSize, new Vector3(0.15f, 0.15f, 0.25f));
        // Resource node marker (diamond shape using wireframe cube)
        (_resourceNodeVao, _resourceNodeVbo, _resourceNodeVertCount) =
            MeshBuilder.BuildWireframeCube(new Vector3(0.9f, 0.8f, 0.2f));
        // Miner ship (uses bomber mesh shape with different color — yellow/gold)
        (_minerVao, _minerVbo, _minerVertCount) =
            ShipMeshBuilder.BuildBomberMesh(new Vector3(0.9f, 0.8f, 0.2f), 2f);
        // Base structure (wireframe cube — larger)
        (_baseVao, _baseVbo, _baseVertCount) =
            MeshBuilder.BuildWireframeCube(new Vector3(0.3f, 0.7f, 1.0f));
        _gameplayMeshesLoaded = true;
    }

    private void LoadEntityDefinitions()
    {
        _definitions.Clear();
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
        };

        string[] searchDirs = ["GameData/Ships", "GameData/Units", "GameData/Bases"];
        foreach (string dir in searchDirs)
        {
            if (!Directory.Exists(dir)) continue;
            foreach (string file in Directory.GetFiles(dir, "*.json"))
            {
                if (Path.GetFileName(file).StartsWith("_")) continue;
                try
                {
                    string json = File.ReadAllText(file);
                    var def = JsonSerializer.Deserialize<EntityDefinition>(json, options);
                    if (def != null && !string.IsNullOrEmpty(def.Id))
                        _definitions[def.Id] = def;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[LoadDefs] Failed to load {file}: {ex.Message}");
                }
            }
        }

        Console.WriteLine($"[LoadDefs] Loaded {_definitions.Count} entity definitions.");
    }


    private void InitializeWorld()
    {
        string? missionId = _pendingMissionId;
        _pendingMissionId = null;
        InitializeWorldCore(missionId);
    }

    private void SpawnAIPlayer(Random rng)
    {
        if (_world == null) return;

        // Spawn AI fleet on opposite side of map
        float aiBaseX = 600f;
        float aiBaseZ = 600f;

        for (int i = 0; i < 4; i++)
        {
            float x = aiBaseX + (rng.NextSingle() - 0.5f) * 200f;
            float z = aiBaseZ + (rng.NextSingle() - 0.5f) * 200f;

            var aiShip = _world.CreateEntity();
            _world.AddComponent(aiShip, new TransformComponent
            {
                Position = new Vector3(x, 0f, z),
                Scale = Vector3.One,
            });
            _world.AddComponent(aiShip, new MovementComponent
            {
                Speed = 70f,
                Acceleration = 100f,
                TurnRate = 200f,
            });
            _world.AddComponent(aiShip, new RenderComponent
            {
                MeshId = _fighterVao,
                VertexCount = _fighterVertCount,
                Color = new Vector4(1.0f, 0.2f, 0.2f, 1f), // Red for enemy
                Visible = true,
                PrimitiveType = (int)PrimitiveType.Triangles,
            });
            _world.AddComponent(aiShip, new AIControlledComponent
            {
                PlayerId = 2,
                Aggressiveness = 0.5f,
            });
            _aiEntities.Add(aiShip);
        }
    }

    private void SpawnResourceNodes(Random rng)
    {
        if (_world == null) return;

        // Scatter resource nodes across the map
        var resourceTypes = new[] { ResourceType.Energy, ResourceType.Minerals, ResourceType.Data, ResourceType.Crew };
        var nodeColors = new Dictionary<ResourceType, Vector4>
        {
            [ResourceType.Energy] = new Vector4(0.9f, 0.8f, 0.2f, 1f),
            [ResourceType.Minerals] = new Vector4(0.4f, 0.8f, 1.0f, 1f),
            [ResourceType.Data] = new Vector4(0.6f, 1.0f, 0.6f, 1f),
            [ResourceType.Crew] = new Vector4(1.0f, 0.6f, 0.4f, 1f),
        };

        for (int i = 0; i < 16; i++)
        {
            float x = (rng.NextSingle() - 0.5f) * MapWorldSize * 0.8f;
            float z = (rng.NextSingle() - 0.5f) * MapWorldSize * 0.8f;
            var resType = resourceTypes[i % 4];

            var node = _world.CreateEntity();
            _world.AddComponent(node, new TransformComponent
            {
                Position = new Vector3(x, 0f, z),
                Scale = new Vector3(8f, 8f, 8f),
            });
            _world.AddComponent(node, new RenderComponent
            {
                MeshId = _resourceNodeVao,
                VertexCount = _resourceNodeVertCount,
                Color = nodeColors[resType],
                Visible = true,
                PrimitiveType = (int)PrimitiveType.Lines,
            });
            _world.AddComponent(node, new ResourceNodeComponent
            {
                ResourceType = resType,
                Amount = 5000f,
                MaxAmount = 5000f,
                HarvestRate = 10f,
            });
            _resourceNodeEntities.Add(node);
        }
    }

    private void SpawnPlayerBase()
    {
        if (_world == null) return;

        // Command Center
        _baseEntity = _world.CreateEntity();
        _world.AddComponent(_baseEntity, new TransformComponent
        {
            Position = new Vector3(-30f, 0f, -30f),
            Scale = new Vector3(15f, 15f, 15f),
        });
        _world.AddComponent(_baseEntity, new RenderComponent
        {
            MeshId = _baseVao,
            VertexCount = _baseVertCount,
            Color = new Vector4(0.3f, 0.7f, 1.0f, 1f),
            Visible = true,
            PrimitiveType = (int)PrimitiveType.Lines,
        });
        _world.AddComponent(_baseEntity, new BuildingComponent
        {
            BuildingType = "command_center",
            ProductionRate = 1f,
            Footprint = [2, 2],
            PlayerId = 1,
            RallyPoint = new Vector3(-30f, 0f, 0f),
            Producible = ["drone_worker", "miner_basic"],
        });
        _world.AddComponent(_baseEntity, new SelectionComponent
        {
            IsSelected = false,
            SelectionRadius = 15f,
        });
        _world.AddComponent(_baseEntity, new HealthComponent
        {
            MaxHP = 2000f,
            CurrentHP = 2000f,
            Armor = 100f,
        });

        // Shipyard
        var shipyard = _world.CreateEntity();
        _world.AddComponent(shipyard, new TransformComponent
        {
            Position = new Vector3(50f, 0f, -50f),
            Scale = new Vector3(18f, 18f, 18f),
        });
        _world.AddComponent(shipyard, new RenderComponent
        {
            MeshId = _baseVao,
            VertexCount = _baseVertCount,
            Color = new Vector4(0.8f, 0.5f, 0.2f, 1f),
            Visible = true,
            PrimitiveType = (int)PrimitiveType.Lines,
        });
        _world.AddComponent(shipyard, new BuildingComponent
        {
            BuildingType = "shipyard",
            ProductionRate = 1f,
            Footprint = [3, 3],
            PlayerId = 1,
            RallyPoint = new Vector3(80f, 0f, -50f),
            Producible = ["fighter_basic", "bomber_heavy", "destroyer_assault",
                          "scout_light", "miner_basic", "transport_cargo"],
        });
        _world.AddComponent(shipyard, new SelectionComponent
        {
            IsSelected = false,
            SelectionRadius = 18f,
        });
        _world.AddComponent(shipyard, new HealthComponent
        {
            MaxHP = 1500f,
            CurrentHP = 1500f,
            Armor = 75f,
        });
    }

    private void SpawnMiners(Random rng)
    {
        if (_world == null) return;

        for (int i = 0; i < 3; i++)
        {
            float x = -30f + (rng.NextSingle() - 0.5f) * 60f;
            float z = -30f + (rng.NextSingle() - 0.5f) * 60f;

            var miner = _world.CreateEntity();
            _world.AddComponent(miner, new TransformComponent
            {
                Position = new Vector3(x, 0f, z),
                Scale = Vector3.One,
            });
            _world.AddComponent(miner, new MovementComponent
            {
                Speed = 45f,
                Acceleration = 60f,
                TurnRate = 120f,
            });
            _world.AddComponent(miner, new SelectionComponent
            {
                IsSelected = false,
                SelectionRadius = 6f,
            });
            _world.AddComponent(miner, new RenderComponent
            {
                MeshId = _minerVao,
                VertexCount = _minerVertCount,
                Color = new Vector4(0.9f, 0.8f, 0.2f, 1f),
                Visible = true,
                PrimitiveType = (int)PrimitiveType.Triangles,
            });
            _world.AddComponent(miner, new ResourceCollectorComponent
            {
                PlayerId = 1,
                CarryCapacity = 100f,
                DepositTarget = _baseEntity,
            });
            _minerEntities.Add(miner);
        }
    }

    private void CleanupGameplay()
    {
        _world?.Dispose();
        _world = null;
        _movementSystem = null;
        _aiSystem = null;
        _resourceManager = null;
        _fighterEntities.Clear();
        _minerEntities.Clear();
        _resourceNodeEntities.Clear();
        _aiEntities.Clear();
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

        // Render grid (centered on origin for larger map)
        float halfGrid = MapWorldSize * 0.5f;
        var gridModel = Matrix4.CreateTranslation(-halfGrid, -0.5f, -halfGrid);
        GL.UniformMatrix4(_uniformModel, false, ref gridModel);
        GL.Uniform4(_uniformColor, new Vector4(0, 0, 0, 0));
        GL.BindVertexArray(_gridVao);
        GL.DrawArrays(PrimitiveType.Lines, 0, _gridVertCount);

        // Render all ships with engine trails
        foreach (var (entity, render) in _world!.Query<RenderComponent>())
        {
            if (!render.Visible) continue;
            var transform = _world.GetComponent<TransformComponent>(entity);
            if (transform == null) continue;

            // Render engine trail behind moving ships
            var movement = _world.GetComponent<MovementComponent>(entity);
            if (movement != null && movement.Velocity.LengthSquared > 1f)
            {
                float speed = movement.Velocity.Length;
                float trailScale = MathHelper.Clamp(speed / movement.Speed, 0.3f, 1.0f);
                // Trail sits behind the ship in its local space, then transforms to world
                Matrix4 trailModel = Matrix4.CreateScale(trailScale) *
                                     Matrix4.CreateTranslation(0f, 0f, -0.5f) *
                                     transform.GetModelMatrix();
                GL.UniformMatrix4(_uniformModel, false, ref trailModel);
                GL.Uniform4(_uniformColor, new Vector4(0, 0, 0, 0));
                GL.BindVertexArray(_engineTrailVao);
                GL.DrawArrays(PrimitiveType.Triangles, 0, _engineTrailVertCount);
            }

            Matrix4 model = transform.GetModelMatrix();
            GL.UniformMatrix4(_uniformModel, false, ref model);
            // Use entity color override for resource nodes and AI ships
            bool useOverride = _world.HasComponent<ResourceNodeComponent>(entity) ||
                               _world.HasComponent<AIControlledComponent>(entity);
            GL.Uniform4(_uniformColor, useOverride ? render.Color : new Vector4(0, 0, 0, 0));

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

        // Ctrl+A to select all friendly ships (issue #8)
        if (_sceneManager.State == GameState.Playing && _world != null &&
            (KeyboardState.IsKeyDown(Keys.LeftControl) || KeyboardState.IsKeyDown(Keys.RightControl)) &&
            KeyboardState.IsKeyDown(Keys.A))
        {
            foreach (var (entity, sel) in _world.Query<SelectionComponent>())
            {
                if (IsPlayerSelectable(entity))
                    sel.IsSelected = true;
            }
        }

        // Update scene
        _sceneManager.Update(dt);

        // Update UI
        _uiManager.Update(dt);

        // Gameplay-specific updates
        if (_sceneManager.State == GameState.Playing && _world != null)
        {
            UpdateCameraControls(dt);

            // Update ECS world (issue #1: ensures movement system runs)
            _world.Update(dt);

            // Tick economy (issue #4: resources on HUD)
            _resourceManager?.Tick(dt);
            BindResourceHUD();
            BindBuildPanel();
            BindUnitInfoPanel();
            BindShipControlBar();

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

        Vector3? worldPos = ScreenToWorldGround(MousePosition);
        if (worldPos == null) return;

        if (e.Button == MouseButton.Left)
        {
            if (_placementBuildingId != null)
            {
                HandlePlaceBuilding(worldPos.Value);
                _placementBuildingId = null;
            }
            else if (_attackMoveMode)
            {
                HandleAttackMoveCommand(worldPos.Value);
                _attackMoveMode = false;
            }
            else if (_patrolMode)
            {
                HandlePatrolCommand(worldPos.Value);
                _patrolMode = false;
            }
            else
            {
                HandleSelection(worldPos.Value);
            }
        }
        else if (e.Button == MouseButton.Right)
        {
            // Cancel special modes on right-click
            _attackMoveMode = false;
            _patrolMode = false;
            _placementBuildingId = null;
            HandleMoveCommand(worldPos.Value);
        }
    }

    protected override void OnKeyDown(KeyboardKeyEventArgs e)
    {
        base.OnKeyDown(e);

        if (_sceneManager.State != GameState.Playing || _world == null)
            return;

        bool ctrlHeld = KeyboardState.IsKeyDown(Keys.LeftControl) ||
                        KeyboardState.IsKeyDown(Keys.RightControl);

        if (TryHandleUnitShortcut(e.Key))
        {
            if (_uiManager.Current is GameplayHUD hudShortcut && hudShortcut.ShipControlBar.ActiveCommand != null)
                HandleShipControlCommand(hudShortcut.ShipControlBar.ActiveCommand);
            return;
        }

        switch (e.Key)
        {
            case Keys.X:
                if (HasSelectedUnits())
                    HandleStopCommand();
                break;

            // Attack-move mode (F key to avoid conflict with A for camera)
            case Keys.F:
                _attackMoveMode = true;
                _patrolMode = false;
                break;

            // Patrol mode (P key)
            case Keys.P:
                _patrolMode = true;
                _attackMoveMode = false;
                break;

            // Hold position (H key)
            case Keys.H:
                SetSelectedStance(Stance.Neutral);
                break;

            // Aggressive stance (G key)
            case Keys.G:
                SetSelectedStance(Stance.Aggressive);
                break;

            // Defensive stance (V key)
            case Keys.V:
                SetSelectedStance(Stance.Defensive);
                break;

            // Ability activation (1-4 without Ctrl — always abilities, not group recall)
            case Keys.D1 when !ctrlHeld:
                ActivateAbility(0);
                break;
            case Keys.D2 when !ctrlHeld:
                ActivateAbility(1);
                break;
            case Keys.D3 when !ctrlHeld:
                ActivateAbility(2);
                break;
            case Keys.D4 when !ctrlHeld:
                ActivateAbility(3);
                break;

            // Control groups: Ctrl+1-9 to assign, 5-9 to recall (1-4 are abilities)
            case Keys.D1 when ctrlHeld:
                AssignControlGroup(1);
                break;
            case Keys.D2 when ctrlHeld:
                AssignControlGroup(2);
                break;
            case Keys.D3 when ctrlHeld:
                AssignControlGroup(3);
                break;
            case Keys.D4 when ctrlHeld:
                AssignControlGroup(4);
                break;
            case Keys.D5 when ctrlHeld:
                AssignControlGroup(5);
                break;
            case Keys.D6 when ctrlHeld:
                AssignControlGroup(6);
                break;
            case Keys.D7 when ctrlHeld:
                AssignControlGroup(7);
                break;
            case Keys.D8 when ctrlHeld:
                AssignControlGroup(8);
                break;
            case Keys.D9 when ctrlHeld:
                AssignControlGroup(9);
                break;
            case Keys.D5 when !ctrlHeld:
                RecallControlGroup(5);
                break;
            case Keys.D6 when !ctrlHeld:
                RecallControlGroup(6);
                break;
            case Keys.D7 when !ctrlHeld:
                RecallControlGroup(7);
                break;
            case Keys.D8 when !ctrlHeld:
                RecallControlGroup(8);
                break;
            case Keys.D9 when !ctrlHeld:
                RecallControlGroup(9);
                break;

            // Build command (B key) — queue next producible item from selected building
            case Keys.B when !ctrlHeld:
                HandleBuildCommand();
                break;

            // Set rally point (R key + next right-click)
            case Keys.R:
                HandleSetRallyPoint();
                break;

            // Place building mode (N key cycles through available buildings)
            case Keys.N:
                EnterPlacementMode();
                break;
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
            if (!IsPlayerSelectable(entity)) continue;
            var transform = _world.GetComponent<TransformComponent>(entity);
            if (transform == null) continue;

            float dist = (transform.Position - worldPos).Length;
            // Use selection radius scaled by camera height for easier picking
            float effectiveRadius = sel.SelectionRadius * (_rtsCamera.Height / 100f + 1f);
            if (dist < effectiveRadius && dist < closestDist)
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

        // Check if right-click target is a resource node
        Entity? targetNode = FindResourceNodeAt(worldPos);

        for (int i = 0; i < selectedEntities.Count; i++)
        {
            var entity = selectedEntities[i];

            // If clicking a resource node and entity is a collector, assign to mine
            if (targetNode.HasValue)
            {
                var collector = _world.GetComponent<ResourceCollectorComponent>(entity);
                if (collector != null)
                {
                    AssignMinerToNode(entity, collector, targetNode.Value);
                    continue;
                }
            }

            var movement = _world.GetComponent<MovementComponent>(entity);
            if (movement == null) continue;

            Vector3 offset = Vector3.Zero;
            if (selectedEntities.Count > 1)
            {
                float angle = MathF.PI * 2f * i / selectedEntities.Count;
                float radius = 12f;
                offset = new Vector3(MathF.Cos(angle) * radius, 0f, MathF.Sin(angle) * radius);
            }

            movement.PathTarget = worldPos + offset;
        }

        _moveTargetPosition = worldPos;
        _moveTargetTimer = 2f;
    }

    /// <summary>Find a resource node entity near the given world position.</summary>
    private Entity? FindResourceNodeAt(Vector3 worldPos)
    {
        if (_world == null) return null;

        const float nodeClickRadius = 12f;
        Entity? closest = null;
        float closestDist = float.MaxValue;

        foreach (var (entity, node) in _world.Query<ResourceNodeComponent>())
        {
            if (node.IsDepleted) continue;
            var transform = _world.GetComponent<TransformComponent>(entity);
            if (transform == null) continue;

            float dist = (transform.Position - worldPos).Length;
            if (dist < nodeClickRadius && dist < closestDist)
            {
                closestDist = dist;
                closest = entity;
            }
        }

        return closest;
    }

    /// <summary>Assign a miner to harvest a resource node.</summary>
    private void AssignMinerToNode(Entity minerEntity, ResourceCollectorComponent collector, Entity nodeEntity)
    {
        if (_world == null) return;

        collector.AssignedNode = nodeEntity;
        collector.State = CollectorState.MovingToNode;
        collector.PlayerId = 1; // Player 1

        // Find nearest base as deposit target (or use hero position as fallback)
        Entity? depositTarget = FindNearestBase(minerEntity);
        collector.DepositTarget = depositTarget ?? _heroEntity;

        // Set movement toward the node
        var nodeTransform = _world.GetComponent<TransformComponent>(nodeEntity);
        var movement = _world.GetComponent<MovementComponent>(minerEntity);
        if (nodeTransform != null && movement != null)
        {
            movement.PathTarget = nodeTransform.Position;
        }
    }

    /// <summary>Find the nearest base/building entity for resource deposit.</summary>
    private Entity? FindNearestBase(Entity fromEntity)
    {
        if (_world == null) return null;

        var fromTransform = _world.GetComponent<TransformComponent>(fromEntity);
        if (fromTransform == null) return null;

        Entity? nearest = null;
        float nearestDist = float.MaxValue;

        foreach (var (entity, building) in _world.Query<BuildingComponent>())
        {
            var transform = _world.GetComponent<TransformComponent>(entity);
            if (transform == null) continue;

            float dist = (transform.Position - fromTransform.Position).Length;
            if (dist < nearestDist)
            {
                nearestDist = dist;
                nearest = entity;
            }
        }

        return nearest;
    }

    // ── Ship Control Commands ─────────────────────────────────────────────────

    /// <summary>Stop all selected ships (clear PathTarget and waypoints).</summary>
    private void HandleStopCommand()
    {
        if (_world == null) return;

        foreach (var (entity, sel) in _world.Query<SelectionComponent>())
        {
            if (!sel.IsSelected) continue;

            var movement = _world.GetComponent<MovementComponent>(entity);
            if (movement != null)
            {
                movement.PathTarget = null;
                movement.Velocity = Vector3.Zero;
            }

            var waypoints = _world.GetComponent<WaypointQueueComponent>(entity);
            if (waypoints != null)
            {
                waypoints.Waypoints.Clear();
                waypoints.CurrentIndex = 0;
                waypoints.Patrol = false;
            }
        }
    }

    /// <summary>Attack-move: move to position with aggressive auto-engage.</summary>
    private void HandleAttackMoveCommand(Vector3 worldPos)
    {
        if (_world == null) return;

        foreach (var (entity, sel) in _world.Query<SelectionComponent>())
        {
            if (!sel.IsSelected) continue;

            var movement = _world.GetComponent<MovementComponent>(entity);
            if (movement != null)
                movement.PathTarget = worldPos;

            // Ensure entity has combat and stance components for aggressive behavior
            var stance = _world.GetComponent<StanceComponent>(entity);
            if (stance != null)
                stance.CurrentStance = Stance.Aggressive;
            else
                _world.AddComponent(entity, new StanceComponent { CurrentStance = Stance.Aggressive });

            // Ensure combat target component exists
            if (!_world.HasComponent<CombatTargetComponent>(entity))
                _world.AddComponent(entity, new CombatTargetComponent { Faction = 1 });
        }

        _moveTargetPosition = worldPos;
        _moveTargetTimer = 2f;
    }

    /// <summary>Patrol: set waypoint loop between current position and target.</summary>
    private void HandlePatrolCommand(Vector3 worldPos)
    {
        if (_world == null) return;

        foreach (var (entity, sel) in _world.Query<SelectionComponent>())
        {
            if (!sel.IsSelected) continue;

            var transform = _world.GetComponent<TransformComponent>(entity);
            if (transform == null) continue;

            var waypoints = _world.GetComponent<WaypointQueueComponent>(entity);
            if (waypoints == null)
            {
                waypoints = new WaypointQueueComponent();
                _world.AddComponent(entity, waypoints);
            }

            waypoints.Waypoints.Clear();
            waypoints.Waypoints.Add(worldPos);
            waypoints.Waypoints.Add(transform.Position);
            waypoints.CurrentIndex = 0;
            waypoints.Patrol = true;

            // Start moving toward first waypoint
            var movement = _world.GetComponent<MovementComponent>(entity);
            if (movement != null)
                movement.PathTarget = worldPos;
        }

        _moveTargetPosition = worldPos;
        _moveTargetTimer = 2f;
    }

    /// <summary>Set combat stance for all selected entities.</summary>
    private void SetSelectedStance(Stance stance)
    {
        if (_world == null) return;

        foreach (var (entity, sel) in _world.Query<SelectionComponent>())
        {
            if (!sel.IsSelected) continue;

            var stanceComp = _world.GetComponent<StanceComponent>(entity);
            if (stanceComp != null)
                stanceComp.CurrentStance = stance;
            else
                _world.AddComponent(entity, new StanceComponent { CurrentStance = stance });
        }
    }

    /// <summary>Activate hero ability at given slot index.</summary>
    private void ActivateAbility(int slot)
    {
        if (_world == null) return;

        foreach (var (entity, sel) in _world.Query<SelectionComponent>())
        {
            if (!sel.IsSelected) continue;

            var abilities = _world.GetComponent<AbilityListComponent>(entity);
            if (abilities == null) continue;

            var ability = abilities.GetBySlot(slot);
            ability?.Activate();
        }
    }

    /// <summary>Assign currently selected entities to a control group.</summary>
    private void AssignControlGroup(int group)
    {
        if (_world == null) return;

        var entities = new List<Entity>();
        foreach (var (entity, sel) in _world.Query<SelectionComponent>())
        {
            if (sel.IsSelected)
                entities.Add(entity);
        }

        _controlGroups[group] = entities;
    }

    /// <summary>Recall a control group (select those entities).</summary>
    private void RecallControlGroup(int group)
    {
        if (_world == null) return;

        if (!_controlGroups.TryGetValue(group, out var entities))
            return;

        // Deselect all first
        foreach (var (_, sel) in _world.Query<SelectionComponent>())
            sel.IsSelected = false;

        // Select group members (skip dead entities)
        foreach (var entity in entities)
        {
            if (!_world.IsAlive(entity)) continue;
            var sel = _world.GetComponent<SelectionComponent>(entity);
            if (sel != null)
                sel.IsSelected = true;
        }
    }

    // ── Building/Production Commands ──────────────────────────────────────────

    /// <summary>
    /// Called when a build button is clicked in the BuildPanel UI.
    /// </summary>
    private void OnBuildPanelBuildRequested(string defId)
    {
        if (_world == null || _resourceManager == null) return;

        foreach (var (entity, sel) in _world.Query<SelectionComponent>())
        {
            if (!sel.IsSelected) continue;
            var building = _world.GetComponent<BuildingComponent>(entity);
            if (building == null || !building.Producible.Contains(defId)) continue;

            TryEnqueueBuild(building, defId);
            return;
        }
    }

    /// <summary>
    /// Queue the first producible item from any selected building.
    /// Deducts resources immediately via ResourceManager.
    /// </summary>
    private void HandleBuildCommand()
    {
        if (_world == null || _resourceManager == null) return;

        foreach (var (entity, sel) in _world.Query<SelectionComponent>())
        {
            if (!sel.IsSelected) continue;

            var building = _world.GetComponent<BuildingComponent>(entity);
            if (building == null || building.Producible.Count == 0) continue;

            // Build the first producible item (UI will allow choosing later)
            if (TryEnqueueBuild(building, building.Producible[0]))
                break;
        }
    }

    /// <summary>
    /// Shared logic: check supply + resources, deduct, and enqueue a build item.
    /// Returns true if successfully enqueued.
    /// </summary>
    private bool TryEnqueueBuild(BuildingComponent building, string defId)
    {
        if (_resourceManager == null) return false;
        if (!_definitions.TryGetValue(defId, out var def)) return false;

        int energy = def.Cost?.Energy ?? 0;
        int minerals = def.Cost?.Minerals ?? 0;
        int data = def.Cost?.Data ?? 0;
        int crew = def.Cost?.Crew ?? 0;

        if (_supplySystem != null && crew > 0 &&
            !_supplySystem.CanAffordSupply(building.PlayerId, crew))
        {
            Console.WriteLine($"[Build] Supply cap reached, cannot build {def.DisplayName}");
            return false;
        }

        if (!_resourceManager.TrySpendCost(building.PlayerId, energy, minerals, data, crew))
        {
            Console.WriteLine($"[Build] Cannot afford {def.DisplayName}");
            return false;
        }

        if (_supplySystem != null && crew > 0)
            _supplySystem.ConsumeSupply(building.PlayerId, crew);

        building.BuildQueue.Enqueue(defId);
        Console.WriteLine($"[Build] Queued {def.DisplayName} at {building.BuildingType} " +
                          $"(queue: {building.BuildQueue.Count})");
        return true;
    }

    /// <summary>Set rally point for selected building to current move target.</summary>
    private void HandleSetRallyPoint()
    {
        if (_world == null) return;

        // Set rally point to current mouse position
        Vector3? worldPos = ScreenToWorldGround(MousePosition);
        if (worldPos == null) return;

        foreach (var (entity, sel) in _world.Query<SelectionComponent>())
        {
            if (!sel.IsSelected) continue;

            var building = _world.GetComponent<BuildingComponent>(entity);
            if (building == null) continue;

            building.RallyPoint = worldPos.Value;
            Console.WriteLine($"[Rally] Set rally point for {building.BuildingType}");
        }
    }

    // ── Building Placement ────────────────────────────────────────────────────

    /// <summary>Available buildings the player can place.</summary>
    private static readonly string[] PlaceableBuildings =
        ["command_center", "shipyard"];

    private int _placementIndex;

    /// <summary>Enter building placement mode, cycling through available buildings.</summary>
    private void EnterPlacementMode()
    {
        _placementIndex = (_placementIndex + 1) % PlaceableBuildings.Length;
        _placementBuildingId = PlaceableBuildings[_placementIndex];
        _attackMoveMode = false;
        _patrolMode = false;
        Console.WriteLine($"[Place] Click to place: {_placementBuildingId} (press N to cycle, right-click to cancel)");
    }

    /// <summary>Place a building at the given world position.</summary>
    private void HandlePlaceBuilding(Vector3 worldPos)
    {
        if (_world == null || _resourceManager == null || _placementBuildingId == null) return;

        if (!_definitions.TryGetValue(_placementBuildingId, out var def))
        {
            Console.WriteLine($"[Place] Unknown building: {_placementBuildingId}");
            return;
        }

        // Check cost
        int energy = def.Cost?.Energy ?? 0;
        int minerals = def.Cost?.Minerals ?? 0;
        int data = def.Cost?.Data ?? 0;
        int crew = def.Cost?.Crew ?? 0;

        if (!_resourceManager.TrySpendCost(1, energy, minerals, data, crew))
        {
            Console.WriteLine($"[Place] Cannot afford {def.DisplayName}");
            return;
        }

        // Spawn building at position
        var building = _world.CreateEntity();
        _world.AddComponent(building, new TransformComponent
        {
            Position = worldPos,
            Scale = new Vector3(15f, 15f, 15f),
        });
        _world.AddComponent(building, new RenderComponent
        {
            MeshId = _baseVao,
            VertexCount = _baseVertCount,
            Color = new Vector4(0.3f, 0.7f, 1.0f, 1f),
            Visible = true,
            PrimitiveType = (int)PrimitiveType.Lines,
        });
        _world.AddComponent(building, new SelectionComponent
        {
            IsSelected = false,
            SelectionRadius = 15f,
        });

        var healthDef = def.Components?.Health;
        _world.AddComponent(building, new HealthComponent
        {
            MaxHP = healthDef?.MaxHP ?? 1000f,
            CurrentHP = healthDef?.MaxHP ?? 1000f,
            Armor = healthDef?.Armor ?? 50f,
        });

        var buildingDef = def.Components?.Building;
        _world.AddComponent(building, new BuildingComponent
        {
            BuildingType = buildingDef?.BuildingType ?? _placementBuildingId,
            ProductionRate = buildingDef?.ProductionRate ?? 1f,
            Footprint = buildingDef?.Footprint ?? [2, 2],
            PlayerId = 1,
            RallyPoint = worldPos + new Vector3(30f, 0f, 0f),
            Producible = def.Producible?.ToList() ?? new List<string>(),
        });

        Console.WriteLine($"[Place] Built {def.DisplayName} at ({worldPos.X:F0}, {worldPos.Z:F0})");
    }

    /// <summary>
    /// Bind current resource data to the HUD ResourceBar each frame.
    /// </summary>
    private void BindResourceHUD()
    {
        if (_resourceManager == null) return;
        if (_uiManager.Current is not GameplayHUD hud) return;

        var displays = new List<ResourceDisplay>
        {
            _resourceManager.GetDisplay(1, ResourceType.Energy),
            _resourceManager.GetDisplay(1, ResourceType.Minerals),
            _resourceManager.GetDisplay(1, ResourceType.Data),
            _resourceManager.GetDisplay(1, ResourceType.Crew),
        };
        hud.ResourceBar.Resources = displays;
    }

    /// <summary>Bind build panel data when a building is selected.</summary>
    private void BindBuildPanel()
    {
        if (_world == null || _resourceManager == null) return;
        if (_uiManager.Current is not GameplayHUD hud) return;

        // Find first selected building
        BuildingComponent? selectedBuilding = null;
        foreach (var (entity, sel) in _world.Query<SelectionComponent>())
        {
            if (!sel.IsSelected) continue;
            var building = _world.GetComponent<BuildingComponent>(entity);
            if (building != null && building.Producible.Count > 0)
            {
                selectedBuilding = building;
                break;
            }
        }

        if (selectedBuilding == null)
        {
            hud.BuildPanel.Visible = false;
            return;
        }

        hud.BuildPanel.Visible = true;
        hud.BuildPanel.BuildingName = selectedBuilding.BuildingType.Replace("_", " ");

        // Supply info
        if (_supplySystem != null)
        {
            int used = _supplySystem.GetUsed(selectedBuilding.PlayerId);
            int cap = _supplySystem.GetCap(selectedBuilding.PlayerId);
            hud.BuildPanel.SupplyText = $"{used} / {cap}";
        }

        // Available items
        var items = new List<BuildableItem>();
        foreach (string defId in selectedBuilding.Producible)
        {
            if (!_definitions.TryGetValue(defId, out var def)) continue;
            var playerRes = _resourceManager.GetPlayer(selectedBuilding.PlayerId);

            bool canAfford = playerRes != null &&
                playerRes.GetAmount(ResourceType.Energy) >= (def.Cost?.Energy ?? 0) &&
                playerRes.GetAmount(ResourceType.Minerals) >= (def.Cost?.Minerals ?? 0) &&
                playerRes.GetAmount(ResourceType.Data) >= (def.Cost?.Data ?? 0) &&
                playerRes.GetAmount(ResourceType.Crew) >= (def.Cost?.Crew ?? 0);

            items.Add(new BuildableItem
            {
                Id = def.Id,
                Name = def.DisplayName,
                EnergyCost = def.Cost?.Energy ?? 0,
                MineralsCost = def.Cost?.Minerals ?? 0,
                DataCost = def.Cost?.Data ?? 0,
                CrewCost = def.Cost?.Crew ?? 0,
                BuildTime = def.BuildTime,
                CanAfford = canAfford,
            });
        }
        hud.BuildPanel.AvailableItems = items;

        // Queue
        var queueItems = new List<QueuedItem>();
        int qIdx = 0;
        foreach (string qItem in selectedBuilding.BuildQueue)
        {
            float progress = 0f;
            if (qIdx == 0 && selectedBuilding.BuildProgress > 0)
            {
                var qDef = _definitions.GetValueOrDefault(qItem);
                float totalTime = qDef?.BuildTime ?? 1f;
                progress = selectedBuilding.BuildProgress / totalTime;
            }
            string name = _definitions.TryGetValue(qItem, out var d) ? d.DisplayName : qItem;
            queueItems.Add(new QueuedItem { Name = name, Progress = progress });
            qIdx++;
        }
        hud.BuildPanel.Queue = queueItems;
    }

    /// <summary>Bind unit info panel data for selected units (including miner cargo).</summary>
    private void BindUnitInfoPanel()
    {
        if (_world == null) return;
        if (_uiManager.Current is not GameplayHUD hud) return;

        var unitInfos = new List<UnitInfo>();
        foreach (var (entity, sel) in _world.Query<SelectionComponent>())
        {
            if (!sel.IsSelected) continue;
            if (unitInfos.Count >= 6) break;

            var health = _world.GetComponent<HealthComponent>(entity);
            string name = "Unit";

            // Determine name from components
            var building = _world.GetComponent<BuildingComponent>(entity);
            if (building != null)
                name = building.BuildingType.Replace("_", " ");
            else
            {
                var collector = _world.GetComponent<ResourceCollectorComponent>(entity);
                if (collector != null)
                {
                    string cargoInfo = collector.CarryAmount > 0
                        ? $" [{collector.CarryAmount:0}/{collector.CarryCapacity:0}]"
                        : "";
                    name = $"Miner{cargoInfo} ({collector.State})";
                }
                else
                {
                    var hero = _world.GetComponent<HeroComponent>(entity);
                    name = hero != null ? "Hero Ship" : "Ship";
                }
            }

            if (health != null)
                unitInfos.Add(UnitInfo.FromHealth(name, health));
        }

        hud.UnitInfoPanel.SelectedUnits = unitInfos;
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
        _uiManager?.Resize(new Vector2(e.Width, e.Height));
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
            MeshBuilder.DeleteMesh(_bomberVao, _bomberVbo);
            MeshBuilder.DeleteMesh(_destroyerVao, _destroyerVbo);
            MeshBuilder.DeleteMesh(_carrierVao, _carrierVbo);
            MeshBuilder.DeleteMesh(_engineTrailVao, _engineTrailVbo);
            MeshBuilder.DeleteMesh(_selectionVao, _selectionVbo);
            MeshBuilder.DeleteMesh(_moveTargetVao, _moveTargetVbo);
            MeshBuilder.DeleteMesh(_gridVao, _gridVbo);
            MeshBuilder.DeleteMesh(_resourceNodeVao, _resourceNodeVbo);
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

