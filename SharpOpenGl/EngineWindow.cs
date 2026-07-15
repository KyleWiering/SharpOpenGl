using System.Text.Json;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using SharpOpenGl.Engine.Audio;
using SharpOpenGl.Engine.Build;

using SharpOpenGl.Engine.ECS;

using SharpOpenGl.Engine.Economy;
using SharpOpenGl.Engine.Entities;
using SharpOpenGl.Engine.Events;
using SharpOpenGl.Engine.Grid;
using SharpOpenGl.Engine.Multiplayer;
using SharpOpenGl.Engine.Persistence;
using SharpOpenGl.Engine.Rendering;
using SharpOpenGl.Rendering;
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
    private RtsCameraController _rtsCamera = null!;
    private EnvironmentController _environment = null!;
    private int _frameCount;
    private readonly bool _screenshotMode;
    private readonly string _screenshotPath;
    private readonly bool _demoRecordingMode;
    private readonly string _demoMissionId;
    private readonly string _demoVideoPath;

    private ShaderManager _shaderManager = null!;
    private int _shaderProgram;
    private int _uniformProjection;
    private int _uniformView;
    private int _uniformModel;
    private int _uniformColor;
    private int _uniformRaceTextureIndex;
    private int _uniformTeamTint;
    private int _uniformComponentTextureIndex;

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
    private ConstructionSystem? _constructionSystem;
    private StructureConstructionVisualSystem? _structureConstructionVisualSystem;
    private MiningVisualSystem? _miningVisualSystem;
    private ArticulationSystem? _articulationSystem;
    private AbilitySystem? _abilitySystem;
    private SquadSystem? _squadSystem;

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

    /// <summary>True when sandbox was started from setup screen — uses chunked procedural grid.</summary>
    private bool _sandboxChunkedMode;
    private int _sandboxChunkLoadRadius = 2;
    private SandboxChunkGrid? _sandboxChunkGrid;
    private PathFollowingSystem? _pathFollowingSystem;
    private FogOfWarSystem? _fogOfWarSystem;

    // Ship meshes (initialized when gameplay starts)
    private int _heroVao, _heroVbo, _heroVertCount;
    private int _fighterVao, _fighterVbo, _fighterVertCount;
    private int _bomberVao, _bomberVbo, _bomberVertCount;
    private int _destroyerVao, _destroyerVbo, _destroyerVertCount;
    private int _carrierVao, _carrierVbo, _carrierVertCount;
    private int _engineTrailVao, _engineTrailVbo, _engineTrailVertCount;
    private int _selectionVao, _selectionVbo, _selectionVertCount;
    private int _moveTargetVao, _moveTargetVbo, _moveTargetVertCount;
    private int _routePreviewVao, _routePreviewVbo, _routePreviewVertCount;
    private int _gridVao, _gridVbo, _gridVertCount;
    private int _gridLineStep = 1;
    private int _resourceNodeVao, _resourceNodeVbo, _resourceNodeVertCount;
    private int _minerVao, _minerVbo, _minerVertCount;
    private int _miningDroneVao, _miningDroneVbo, _miningDroneVertCount;
    private int _evaCrewVao, _evaCrewVbo, _evaCrewVertCount;
    private int _tractorBeamVao, _tractorBeamVbo, _tractorBeamVertCount;
    private int _commandCenterVao, _commandCenterVbo, _commandCenterVertCount;
    private int _shipyardVao, _shipyardVbo, _shipyardVertCount;
    private int _shipyardSmallVao, _shipyardSmallVbo, _shipyardSmallVertCount;
    private int _shipyardMediumVao, _shipyardMediumVbo, _shipyardMediumVertCount;
    private int _shipyardLargeVao, _shipyardLargeVbo, _shipyardLargeVertCount;
    private int _scoutVao, _scoutVbo, _scoutVertCount;
    private int _droneVao, _droneVbo, _droneVertCount;
    private int _corvetteVao, _corvetteVbo, _corvetteVertCount;
    private int _frigateVao, _frigateVbo, _frigateVertCount;
    private int _gunshipVao, _gunshipVbo, _gunshipVertCount;
    private int _cruiserVao, _cruiserVbo, _cruiserVertCount;
    private int _transportVao, _transportVbo, _transportVertCount;
    private int _dreadnoughtVao, _dreadnoughtVbo, _dreadnoughtVertCount;
    private bool _gameplayMeshesLoaded;

    private IAudioManager _audio = new NullAudioManager();

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
    private bool _attackMode;
    private bool _patrolMode;

    // Building placement mode
    private string? _placementBuildingId;
    private Entity? _placementBuilderEntity;
    private Entity? _builderPickEntity;
    private SupplySystem? _supplySystem;

    public EngineWindow(
        GameWindowSettings gameSettings,
        NativeWindowSettings nativeSettings,
        bool screenshotMode = false,
        string screenshotPath = "screenshot.png",
        bool demoRecordingMode = false,
        string demoMissionId = "example_scenario",
        string? demoVideoPath = null)
        : base(gameSettings, nativeSettings)
    {
        _screenshotMode = screenshotMode;
        _screenshotPath = screenshotPath;
        _demoRecordingMode = demoRecordingMode;
        _demoMissionId = demoMissionId;
        _demoVideoPath = demoVideoPath ?? ResolveDemoVideoPath();
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
        _shaderProgram = _shaderManager.CreateProgram(
            GameShaders.DesktopVertex, GameShaders.DesktopFragment);
        _uniformProjection = ShaderManager.GetUniform(_shaderProgram, "projection");
        _uniformView = ShaderManager.GetUniform(_shaderProgram, "view");
        _uniformModel = ShaderManager.GetUniform(_shaderProgram, "model");
        _uniformColor = ShaderManager.GetUniform(_shaderProgram, "overrideColor");
        _uniformRaceTextureIndex = ShaderManager.GetUniform(_shaderProgram, "raceTextureIndex");
        _uniformTeamTint = ShaderManager.GetUniform(_shaderProgram, "teamTint");
        _uniformComponentTextureIndex = ShaderManager.GetUniform(_shaderProgram, "componentTextureIndex");



        // Environment (starfield background)
        _environment = new EnvironmentController();
        _environment.Initialize();

        // RTS Camera (adjusted for larger map)
        _rtsCamera = new RtsCameraController
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
        var uiViewport = UiViewportSize;
        _uiRenderer.Initialize((int)uiViewport.X, (int)uiViewport.Y);

        // Event bus & managers
        _eventBus = new EventBus();

        InitializeExplosionVfx();
        InitializeCombatRingOverlays();
        InitializeAudio();

        _sceneManager = new SceneManager(_eventBus);
        _uiManager = new UIManager(_eventBus);
        _uiManager.Resize(UiViewportSize);

        // Register scenes
        _sceneManager.Register(SceneMainMenu, () => new MainMenuScene(this));
        _sceneManager.Register(SceneGameplay, () => new GameplayScene(this));

                if (_meshPreviewMode)
            InitializeMeshPreview();
        else if (SandboxLaunchOptions.Enabled)
        {
            _pendingSandboxSetup = new SandboxSetupResult(
                SandboxLaunchOptions.SeedText,
                ProceduralSeedHelper.ParseSeed(SandboxLaunchOptions.SeedText));
            Console.WriteLine($"[Sandbox] Headless launch seed='{SandboxLaunchOptions.SeedText}'");
            _sceneManager.TransitionTo(SceneGameplay, GameState.Playing);
        }
        else if (_screenshotMode || _demoRecordingMode)
        {
            if (_demoRecordingMode)
                InitializeDemoRecording();
            else
                InitializeScreenshotCapture();
            _sceneManager.TransitionTo(SceneGameplay, GameState.Playing);
        }
        else
            _sceneManager.TransitionTo(SceneMainMenu, GameState.MainMenu);

        Console.WriteLine("SharpOpenGL RTS Engine initialized.");
    }

    // ── Scene transitions ─────────────────────────────────────────────────────

    internal void ShowMainMenu()
    {
        _uiManager.Clear();
        EnsurePersistence();
        bool hasSave = _saveManager!.ListSaveFiles().Length > 0;
        var menu = new MainMenuScreen(hasSave);
        menu.NewGameRequested += ShowMissionSelect;
        menu.SandboxRequested += ShowSandboxSetup;
        menu.MultiplayerRequested += ShowMultiplayerSetup;
        menu.ContinueRequested += ContinueSavedGame;
        menu.LoadGameRequested += ShowLoadGameScreen;
        menu.ShipDesignerRequested += ShowShipDesigner;
        menu.SettingsRequested += ShowSettings;
        menu.QuitRequested += () => Close();
        _uiManager.Push(menu);
    }

    /// <summary>Show mission selection screen with available scenarios.</summary>
    internal void ShowMissionSelect()
    {
        EnsurePersistence();
        var missionSelect = new MissionSelectScreen();

        // Load available missions from GameData/Missions
        var missions = LoadMissionEntries();
        missionSelect.SetMissions(missions, _campaignProgressManager!.CompletedMissionIds);

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

    private IEnumerable<MissionEntry> LoadMissionEntries() => LoadMissionEntriesFromData();

    /// <summary>Show sandbox setup screen with optional world seed.</summary>
    internal void ShowSandboxSetup()
    {
        var sandboxScreen = new SandboxSetupScreen();
        sandboxScreen.StartRequested += result =>
        {
            _pendingSandboxSetup = result;
            _pendingMissionId = null;
            _pendingSkirmishSetup = null;
            Console.WriteLine($"[Sandbox] Starting with seed text '{result.SeedText}' (parsed {result.ParsedSeed})");
            _sceneManager.TransitionTo(SceneGameplay, GameState.Playing);
        };
        sandboxScreen.BackRequested += () => _uiManager.Pop();
        _uiManager.Push(sandboxScreen);
    }

    /// <summary>Show multiplayer setup screen with AI player option (issue #6).</summary>
    internal void ShowMultiplayerSetup()
    {
        var mpScreen = new MultiplayerSetupScreen();
        mpScreen.StartRequested += result =>
        {
            _pendingSkirmishSetup = result;
            _pendingMissionId = null;
            _pendingSandboxSetup = null;
            Console.WriteLine(
                $"[Multiplayer] Starting skirmish on '{result.MapId}' with {result.ActivePlayerCount} factions " +
                $"({result.HumanCount} human, {result.AiCount} AI)");
            foreach (var player in result.Players)
            {
                Console.WriteLine(
                    $"  Slot {player.SlotIndex + 1}: {(player.IsHuman ? "Human" : "AI")} — {player.RaceId}");
            }

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
        hud.BuildPanel.QueueCancelRequested += OnBuildPanelQueueCancelRequested;
        WireBuildMapHud(hud);
        hud.ShipControlBar.CommandActivated += HandleShipControlCommand;
        hud.ShipControlBar.StanceToggled += CycleSelectedStance;
        hud.ShipControlBar.FormationCycled += CycleSelectedFormation;
        hud.MinimapClicked += PanCameraToMinimap;
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

        EnsurePersistence();
        bool hasSave = _saveManager!.ListSaveFiles().Length > 0;
        var pause = new PauseScreen(hasSave);
        pause.ResumeRequested += () =>
        {
            _uiManager.Pop();
        };
        pause.SaveGameRequested += ShowSaveGameScreen;
        pause.LoadGameRequested += ShowLoadGameScreen;
        pause.SettingsRequested += ShowSettings;
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
            ShipMeshExtensions.BuildHeroMesh(new Vector3(0.2f, 0.8f, 1.0f), 3f);
        (_fighterVao, _fighterVbo, _fighterVertCount) =
            ShipMeshExtensions.BuildFighterMesh(new Vector3(0.4f, 1.0f, 0.4f), 1.75f);
        (_scoutVao, _scoutVbo, _scoutVertCount) =
            ShipMeshExtensions.BuildScoutMesh(new Vector3(0.5f, 0.95f, 1f), 1.4f);
        (_droneVao, _droneVbo, _droneVertCount) =
            ShipMeshExtensions.BuildDroneMesh(new Vector3(0.7f, 0.85f, 0.95f), 0.9f);
        (_corvetteVao, _corvetteVbo, _corvetteVertCount) =
            ShipMeshExtensions.BuildCorvetteMesh(new Vector3(0.45f, 0.75f, 1f), 2.2f);
        (_frigateVao, _frigateVbo, _frigateVertCount) =
            ShipMeshExtensions.BuildFrigateMesh(new Vector3(0.55f, 0.65f, 0.95f), 3f);
        (_gunshipVao, _gunshipVbo, _gunshipVertCount) =
            ShipMeshExtensions.BuildGunshipMesh(new Vector3(0.85f, 0.45f, 0.35f), 3.2f);
        (_cruiserVao, _cruiserVbo, _cruiserVertCount) =
            ShipMeshExtensions.BuildCruiserMesh(new Vector3(0.6f, 0.55f, 0.85f), 4.8f);
        (_transportVao, _transportVbo, _transportVertCount) =
            ShipMeshExtensions.BuildTransportMesh(new Vector3(0.55f, 0.75f, 0.95f), 3.8f);
        (_dreadnoughtVao, _dreadnoughtVbo, _dreadnoughtVertCount) =
            ShipMeshExtensions.BuildDreadnoughtMesh(new Vector3(0.75f, 0.35f, 0.55f), 6.3f);
        (_bomberVao, _bomberVbo, _bomberVertCount) =
            ShipMeshExtensions.BuildBomberMesh(new Vector3(0.9f, 0.5f, 0.2f), 2.5f);
        (_destroyerVao, _destroyerVbo, _destroyerVertCount) =
            ShipMeshExtensions.BuildDestroyerMesh(new Vector3(0.7f, 0.2f, 0.9f), 4.0f);
        (_carrierVao, _carrierVbo, _carrierVertCount) =
            ShipMeshExtensions.BuildCarrierMesh(new Vector3(0.6f, 0.6f, 0.8f), 4.6f);
        (_engineTrailVao, _engineTrailVbo, _engineTrailVertCount) =
            ShipMeshBuilder.BuildEngineTrail(new Vector3(1.0f, 0.6f, 0.1f), 2.5f);
        (_selectionVao, _selectionVbo, _selectionVertCount) =
            ShipMeshBuilder.BuildSelectionRing(new Vector3(0f, 1f, 0f), 3f);
        LoadTeamAuraMesh();
        (_moveTargetVao, _moveTargetVbo, _moveTargetVertCount) =
            ShipMeshBuilder.BuildMoveTarget(new Vector3(0f, 1f, 0.5f), 2f);
        (_gridVao, _gridVbo, _gridVertCount) =
            MeshBuilder.BuildGrid(GridColumns, GridRows, GridCellSize, new Vector3(0.15f, 0.15f, 0.25f));
        // Resource node marker (diamond shape using wireframe cube)
        (_resourceNodeVao, _resourceNodeVbo, _resourceNodeVertCount) =
            MeshBuilder.BuildResourceNodeMarker(new Vector3(0.55f, 0.85f, 1f), 3f);
        // Miner ship (uses bomber mesh shape with different color — yellow/gold)
        (_minerVao, _minerVbo, _minerVertCount) =
            ShipMeshExtensions.BuildMinerMesh(new Vector3(0.9f, 0.8f, 0.2f), 2.2f);
        (_miningDroneVao, _miningDroneVbo, _miningDroneVertCount) =
            MeshBuilder.UploadProcedural(ProceduralMeshes.BuildMiningDrone(new Vector3(0.9f, 0.8f, 0.25f)));
        (_evaCrewVao, _evaCrewVbo, _evaCrewVertCount) =
            MeshBuilder.UploadProcedural(ProceduralMeshes.BuildEvaAstronaut(new Vector3(0.92f, 0.94f, 0.98f)));
        (_tractorBeamVao, _tractorBeamVbo, _tractorBeamVertCount) =
            MeshBuilder.UploadProcedural(ProceduralMeshes.BuildBeamStreak(new Vector3(0.45f, 0.85f, 1f), 4f));
        (_commandCenterVao, _commandCenterVbo, _commandCenterVertCount) =
            ShipMeshBuilder.BuildCommandCenterStation(8f);
        (_shipyardSmallVao, _shipyardSmallVbo, _shipyardSmallVertCount) =
            ShipMeshExtensions.BuildShipyardSmall(6f);
        (_shipyardMediumVao, _shipyardMediumVbo, _shipyardMediumVertCount) =
            ShipMeshExtensions.BuildShipyardMedium(9f);
        (_shipyardLargeVao, _shipyardLargeVbo, _shipyardLargeVertCount) =
            ShipMeshExtensions.BuildShipyardLarge(12f);
        (_shipyardVao, _shipyardVbo, _shipyardVertCount) =
            (_shipyardMediumVao, _shipyardMediumVbo, _shipyardMediumVertCount);
        LoadStructureMeshes();
        LoadMapFeatureMeshes();
        LoadProjectileMeshes();
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
        LoadBuildMapCatalog();
    }


    private void InitializeWorld()
    {
        SaveData? saveData = _pendingSaveData;
        _pendingSaveData = null;

        MultiplayerSetupResult? skirmishSetup = _pendingSkirmishSetup;

        if (saveData != null && saveData.IsSandboxSession)
        {
            _isSandboxSession = true;
            _sandboxChunkedMode = true;
            _proceduralMapSeed = saveData.ProceduralMapSeed;
            _lastSandboxSeedText = saveData.SandboxSeedText;
        }
        else
        {
            _isSandboxSession = _pendingSandboxSetup != null;
        }

        SandboxSetupResult? sandboxSetup = saveData?.IsSandboxSession == true
            ? new SandboxSetupResult(saveData.SandboxSeedText, saveData.ProceduralMapSeed)
            : _pendingSandboxSetup;

        string? missionId = saveData?.MissionId;
        if (string.IsNullOrWhiteSpace(missionId))
            missionId = _isSandboxSession ? null : _pendingMissionId;
        _pendingMissionId = null;
        _pendingSkirmishSetup = null;
        _pendingSandboxSetup = null;

        if (saveData != null && !string.IsNullOrWhiteSpace(saveData.MissionId) && !saveData.IsSandboxSession)
        {
            EnsureAssets();
            _missionController!.StartMission(saveData.MissionId);
        }

        InitializeWorldCore(
            missionId,
            skipWorldSpawn: saveData != null,
            sandboxSetup: sandboxSetup,
            skirmishSetup: skirmishSetup);

        if (saveData != null)
        {
            if (saveData.IsSandboxSession)
            {
                var cameraPos = new Vector3(saveData.CameraX, 0f, saveData.CameraY);
                var entityPositions = saveData.Entities
                    .Select(e => new Vector3(e.X, 0f, e.Y))
                    .ToList();
                EnsureSandboxChunksAround(cameraPos, entityPositions);
            }

            ApplySaveData(saveData);

            if (saveData.IsSandboxSession)
            {
                BindSandboxSessionHud(new SandboxSetupResult(
                    _lastSandboxSeedText,
                    _proceduralMapSeed));
            }
        }
    }

    private void SpawnAIPlayer(Random rng)
    {
        if (_world == null) return;
        SpawnSkirmishAiFaction(2, new Vector3(600f, 0f, 600f));
    }

    private void SpawnResourceNodes(int seed)
    {
        if (_world == null) return;

        var existing = new HashSet<Entity>(
            _world.Query<ResourceNodeComponent>().Select(q => q.Entity));

        var map = new MapGenerator(seed).Generate(new MapGeneratorConfig
        {
            Width = GridColumns,
            Height = GridRows,
            CellSize = GridCellSize,
            ResourceNodeCount = 16,
            SpawnPointCount = 2,
        });

        MapFeatureSpawner.SpawnAll(_world, map, BuildMapFeatureMeshes(), RevealAreaAt);

        foreach (var (entity, _) in _world.Query<ResourceNodeComponent>())
        {
            if (!existing.Contains(entity))
                _resourceNodeEntities.Add(entity);
        }
    }

    private void SpawnPlayerBase(bool includeDefaultShipyard = true, Stance stationStance = Stance.Defensive)
    {
        if (_world == null) return;

        var (ccMesh, ccCount, ccScale) = ResolveBuildingMesh("command_center", 1);
        _baseEntity = _world.CreateEntity();
        _world.AddComponent(_baseEntity, new TransformComponent { Position = new Vector3(-30f, 0f, -30f), Scale = ccScale });
        var baseRender = new RenderComponent { MeshId = ccMesh, VertexCount = ccCount, Visible = true, PrimitiveType = (int)PrimitiveType.Triangles };
        ApplyRaceTexturing(baseRender, _playerRaceId, 1);
        _world.AddComponent(_baseEntity, baseRender);
        _definitions.TryGetValue("command_center", out var commandCenterDef);
        var commandCenterBuilding = commandCenterDef?.Components?.Building;
        _world.AddComponent(_baseEntity, new BuildingComponent
        {
            BuildingType = "command_center",
            ProductionRate = 1f,
            Footprint = commandCenterBuilding?.Footprint ?? [2, 2],
            Rotates = commandCenterBuilding?.Rotates ?? false,
            PlayerId = 1,
            RallyPoint = new Vector3(-30f, 0f, 0f),
            Producible = ["drone_worker", "miner_basic"],
        });
        _world.AddComponent(_baseEntity, new SelectionComponent { IsSelected = false, SelectionRadius = 15f });
        _world.AddComponent(_baseEntity, new HealthComponent { MaxHP = 2000f, CurrentHP = 2000f, Armor = 100f });
        _world.AddComponent(_baseEntity, new EntityNameComponent { DisplayName = "Command Center", DefinitionId = "command_center" });
        RegisterExistingBuildingOccupancy(_baseEntity, new Vector3(-30f, 0f, -30f),
            _world.GetComponent<BuildingComponent>(_baseEntity)!);
        AttachStationWeapons(_baseEntity, stationStance,
            ("beam", 45f, 480f, 1.2f),
            ("missile", 95f, 620f, 0.35f));

        if (!includeDefaultShipyard)
            return;

        var shipyard = _world.CreateEntity();
        var (syMesh, syCount, syScale) = ResolveBuildingMesh("shipyard_medium", 1);
        _world.AddComponent(shipyard, new TransformComponent { Position = new Vector3(50f, 0f, -50f), Scale = syScale });
        var yardRender = new RenderComponent { MeshId = syMesh, VertexCount = syCount, Visible = true, PrimitiveType = (int)PrimitiveType.Triangles };
        ApplyRaceTexturing(yardRender, _playerRaceId, 1);
        _world.AddComponent(shipyard, yardRender);
        _definitions.TryGetValue("shipyard_medium", out var shipyardDef);
        var shipyardBuilding = shipyardDef?.Components?.Building;
        _world.AddComponent(shipyard, new BuildingComponent
        {
            BuildingType = "shipyard_medium",
            ProductionRate = 1f,
            Footprint = shipyardBuilding?.Footprint ?? [3, 3],
            Rotates = shipyardBuilding?.Rotates ?? false,
            PlayerId = 1,
            RallyPoint = new Vector3(80f, 0f, -50f),
            Producible = GetDefaultProducible("shipyard_medium"),
        });
        _world.AddComponent(shipyard, new SelectionComponent { IsSelected = false, SelectionRadius = 18f });
        _world.AddComponent(shipyard, new HealthComponent { MaxHP = 1500f, CurrentHP = 1500f, Armor = 75f });
        _world.AddComponent(shipyard, new EntityNameComponent { DisplayName = "Medium Shipyard", DefinitionId = "shipyard_medium" });
        RegisterExistingBuildingOccupancy(shipyard, new Vector3(50f, 0f, -50f),
            _world.GetComponent<BuildingComponent>(shipyard)!);
        AttachStationWeapons(shipyard, stationStance,
            ("laser", 28f, 360f, 2.5f),
            ("cannon", 65f, 420f, 0.55f));
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
                Position = new Vector3(x, 1f, z),
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
                HarvestMode = HarvestMode.Drones,
                HarvestRange = 28f,
                HarvestRate = 15f,
                CarryCapacity = 100f,
                DepositTarget = _baseEntity,
            });
            _minerEntities.Add(miner);
        }
    }

    private void CleanupGameplay()
    {
        _isSandboxSession = false;
        _sandboxChunkedMode = false;
        _sandboxChunkGrid = null;
        _gridSystem = null;
        _fogOfWar = null;
        _pathFollowingSystem = null;
        _fogOfWarSystem = null;
        _fogNebulaOverlay?.Clear();
        _world?.Dispose();
        _world = null;
        _movementSystem = null;
        _miningVisualSystem = null;
        _aiSystem = null;
        _resourceManager = null;
        _fighterEntities.Clear();
        _minerEntities.Clear();
        _resourceNodeEntities.Clear();
        _aiEntities.Clear();
        _moveTargetPosition = null;
        _moveTargetTimer = 0f;
        CancelSelectionDrag();
    }

    // ── Rendering ─────────────────────────────────────────────────────────────

    protected override void OnRenderFrame(FrameEventArgs args)
    {
        base.OnRenderFrame(args);

        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        // Always render the starfield background
        var projection = Matrix4.CreatePerspectiveFieldOfView(
            MathHelper.DegreesToRadians(45.0f),
            Size.Y > 0 ? (float)Size.X / Size.Y : 4f / 3f,
            0.1f,
            10000.0f);

        var view = _rtsCamera.GetViewMatrix();

        GL.UseProgram(_shaderProgram);
        GL.UniformMatrix4(_uniformProjection, false, ref projection);
        GL.UniformMatrix4(_uniformView, false, ref view);

        if (_meshPreviewMode || _sceneManager.State != GameState.Playing)
            _environment.Render(_shaderProgram, _uniformModel, _uniformColor);

        if (_meshPreviewMode)
        {
            RenderMeshPreview(projection, (float)args.Time);
        }
        else
        {
            if (_uiManager.Current is ShipDesignerScreen designerScreen)
                RenderShipDesignerPreview(designerScreen, projection, (float)args.Time);

            if (_sceneManager.State == GameState.Playing && _world != null)
                RenderGameplay(projection, view);
        }

        if (!_meshPreviewMode)
        {
            _uiRenderer.Begin();
            _uiManager.Draw(_uiRenderer);
            if (_selectionBoxVisible)
                DrawSelectionBox(_uiRenderer);
            _uiRenderer.End();
        }

        GL.BindVertexArray(0);
        SwapBuffers();

        _frameCount++;

        if (_demoRecordingMode && _demoRecorder != null && _frameCount >= 2)
            _demoRecorder.CaptureFrame(Size.X, Size.Y, _frameCount);

        if ((SandboxLaunchOptions.Enabled || _screenshotMode || _meshPreviewMode) && _frameCount >= 5)
        {
            CaptureScreenshot(_screenshotPath);
            Console.WriteLine($"Screenshot saved to: {_screenshotPath}");
            Close();
        }

        if (_demoRecordingMode && _demoFinalizePending && _demoRecorder != null)
        {
            FinalizeDemoRecording();
            Close();
        }
    }

    private void RenderGameplay(Matrix4 projection, Matrix4 view)
    {
        GL.UseProgram(_shaderProgram);
        GL.UniformMatrix4(_uniformProjection, false, ref projection);
        GL.UniformMatrix4(_uniformView, false, ref view);
        GL.Uniform1(_uniformPointSize, 2f);

        UpdateGridMeshLod();

        // Render grid aligned to active grid origin
        Vector2 gridOrigin = _gridSystem!.Origin;
        var gridModel = Matrix4.CreateTranslation(gridOrigin.X, -0.5f, gridOrigin.Y);
        GL.UniformMatrix4(_uniformModel, false, ref gridModel);
        GL.Uniform4(_uniformColor, new Vector4(0, 0, 0, 0));
        GL.BindVertexArray(_gridVao);
        GL.DrawArrays(PrimitiveType.Lines, 0, _gridVertCount);

        RenderTerrainReadabilityOverlay();

        ResolveProjectileMeshes();
        ResolveArticulatedPartMeshes();
        RenderTeamAuras();

        // Render all ships with engine trails
        foreach (var (entity, render) in _world!.Query<RenderComponent>())
        {
            if (!render.Visible || render.MeshId < 0) continue;
            var transform = _world.GetComponent<TransformComponent>(entity);
            if (transform == null) continue;

            Vector3 visibilityPos = ArticulationDrawHelper.GetVisibilityPosition(_world, entity, transform.Position);
            if (!ShouldRenderEntity(entity, visibilityPos)) continue;

            bool isProjectile = _world.HasComponent<ProjectileComponent>(entity);
            bool isArticulatedPart = ArticulationDrawHelper.IsArticulatedPartChild(_world, entity);

            // Legacy mesh trail — skip when Terran particle nozzles are wired
            if (!isProjectile && !isArticulatedPart && !EntityHasShipEngineNozzles(_world, entity))
            {
                var movement = _world.GetComponent<MovementComponent>(entity);
                if (movement != null && movement.Velocity.LengthSquared > 1f)
                {
                    float speed = movement.Velocity.Length;
                    float trailScale = MathHelper.Clamp(speed / movement.Speed, 0.3f, 1.0f);
                    Matrix4 trailModel = Matrix4.CreateScale(trailScale) *
                                         Matrix4.CreateTranslation(0f, 0f, -0.5f) *
                                         transform.GetModelMatrix();
                    GL.UniformMatrix4(_uniformModel, false, ref trailModel);
                    GL.Uniform1(_uniformRaceTextureIndex, -1);
                    GL.Uniform1(_uniformComponentTextureIndex, ComponentTextureIndex.Engine);
                    GL.Uniform4(_uniformColor, Vector4.Zero);
                    GL.BindVertexArray(_engineTrailVao);
                    GL.DrawArrays(PrimitiveType.Triangles, 0, _engineTrailVertCount);
                }
            }

            var projectile = _world.GetComponent<ProjectileComponent>(entity);
            var visual = _world.GetComponent<ProjectileVisualComponent>(entity);
            Matrix4 model;
            if (!ArticulationDrawHelper.TryGetArticulatedModelMatrix(_world, entity, transform, out model))
                model = isProjectile
                    ? BuildProjectileModelMatrix(transform, projectile, visual)
                    : transform.GetModelMatrix();
            GL.UniformMatrix4(_uniformModel, false, ref model);

            if (render.RaceTextureIndex >= 0)
            {
                GL.Uniform1(_uniformRaceTextureIndex, render.RaceTextureIndex);
                GL.Uniform1(_uniformComponentTextureIndex, -1);
                GL.Uniform3(_uniformTeamTint, render.TeamTint);
                GL.Uniform4(_uniformColor, Vector4.Zero);
            }
            else if (isProjectile)
            {
                GL.Uniform1(_uniformRaceTextureIndex, -1);
                GL.Uniform1(_uniformComponentTextureIndex, ComponentTextureIndex.Weapon);
                GL.Uniform4(_uniformColor, Vector4.Zero);
            }
            else if (render.ComponentTextureIndex >= 0)
            {
                GL.Uniform1(_uniformRaceTextureIndex, -1);
                GL.Uniform1(_uniformComponentTextureIndex, render.ComponentTextureIndex);
                GL.Uniform4(_uniformColor, Vector4.Zero);
            }
            else
            {
                GL.Uniform1(_uniformRaceTextureIndex, -1);
                GL.Uniform1(_uniformComponentTextureIndex, -1);
                var displayKind = GameplayEntityDisplay.Classify(_world, entity);
                Vector4 tint = GameplayEntityDisplay.WorldTintColor(displayKind);
                bool useOverride = tint.W > 0f || render.Color.W > 0f;
                GL.Uniform4(_uniformColor, useOverride ? (tint.W > 0f ? tint : render.Color) : new Vector4(0, 0, 0, 0));
            }

            GL.BindVertexArray(render.MeshId);
            GL.DrawArrays((PrimitiveType)render.PrimitiveType, 0, render.VertexCount);
        }

        RenderFogOverlay(projection, view);
        RenderObjectiveMarkers(projection, view);

        // Render selection rings
        foreach (var (entity, sel) in _world.Query<SelectionComponent>())
        {
            if (!sel.IsSelected) continue;
            var transform = _world.GetComponent<TransformComponent>(entity);
            if (transform == null) continue;

            var ringModel = Matrix4.CreateTranslation(transform.Position);
            GL.UniformMatrix4(_uniformModel, false, ref ringModel);
            var ringKind = GameplayEntityDisplay.Classify(_world, entity);
            GL.Uniform4(_uniformColor, GameplayEntityDisplay.SelectionRingColor(_world, entity, ringKind));
            GL.BindVertexArray(_selectionVao);
            GL.DrawArrays(PrimitiveType.Lines, 0, _selectionVertCount);
        }

        RenderAttackHoverRing();
        RenderShieldCombatRings();
        RenderShieldRegenShimmerRings();
        RenderCombatRingOverlays();
        RenderMiningVfx();
        RenderEngineTrailParticles();
        RenderExplosionVfx();
        RenderRoutePreviews();
        RenderPlacementPreview();

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

    private Vector3 GetGameplayCameraPosition()
    {
        float tiltRad = MathHelper.DegreesToRadians(_rtsCamera.TiltAngle);
        float offsetZ = _rtsCamera.Height * MathF.Tan(tiltRad);
        return _rtsCamera.Target + new Vector3(0f, _rtsCamera.Height, offsetZ);
    }

    protected override void OnUpdateFrame(FrameEventArgs args)
    {
        base.OnUpdateFrame(args);

        if (!IsFocused && !_screenshotMode && !_demoRecordingMode)
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

        if (_sceneManager.State != GameState.Playing)
            _environment.Update(dt);

        // Update UI
        _uiManager.Update(dt);

        // Gameplay-specific updates (paused while mission result overlay is visible)
        if (_sceneManager.State == GameState.Playing && _world != null && !IsMissionResultOverlayActive())
        {
            float gameplayDt = _demoRecordingMode ? dt * DemoSimulationTimeScale : dt;

            if (_demoRecordingMode)
                UpdateDemoRecording(gameplayDt, dt);
            else
                UpdateCameraControls(dt);
            UpdateSandboxChunks();
            _attackHoverPulse += gameplayDt;
            _shieldRingPulse += gameplayDt;

            if (_articulationSystem != null)
                _articulationSystem.CameraPosition = GetGameplayCameraPosition();

            // Update ECS world (issue #1: ensures movement system runs)
            _world.Update(gameplayDt);
            StationRotationSystem.UpdateStationRotations(_world, gameplayDt);
            UpdateExplosionVfx(gameplayDt);
            UpdateCombatRingOverlays(gameplayDt);
            UpdateFogNebulaOverlay(gameplayDt);

            // Tick economy (issue #4: resources on HUD)
            _resourceManager?.Tick(gameplayDt);
            BindResourceHUD();
            BindBuildPanel();
            BindBuildMapPanel();
            TickPlacementHintFlash(dt);
            UpdatePlacementPreview();
            BindUnitInfoPanelExtended();
            BindObjectivePanel();
            BindShipControlBar();
            BindMinimap();



            UpdateAudioListener();

            _audio.Update(dt);



            // Fade move target indicator
            if (_moveTargetTimer > 0f)
                _moveTargetTimer -= dt;
        }
    }

    private void HandleEscapePressed()
    {
        if (_sceneManager.State == GameState.Playing &&
            (_placementBuildingId != null || _harvestCommandMode))
        {
            CancelPlacementMode();
            _harvestCommandMode = false;
            if (_uiManager.Current is GameplayHUD placementHud)
                placementHud.ShipControlBar.ClearActiveCommand();
            return;
        }

        switch (_sceneManager.State)
        {
            case GameState.MainMenu:
                if (_uiManager.ScreenCount > 1)
                    _uiManager.Pop();
                else
                    Close();
                break;
            case GameState.Playing:
                if (IsMissionResultOverlayActive())
                    return;

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
        {
            EnsurePersistence();
            var settings = _settingsManager!.Current.Clamped();
            _rtsCamera.ZoomSpeedMultiplier = settings.CameraZoomSpeed;
            _rtsCamera.ZoomTowardScreenPoint(e.OffsetY, UiMousePosition, UiViewportSize);
            ClampCameraTarget();
        }
    }

    protected override void OnMouseDown(MouseButtonEventArgs e)
    {
        base.OnMouseDown(e);

        var screenPoint = UiMousePosition;
        if (_uiManager.HandlePointerTapped(screenPoint, (int)e.Button, UiViewportSize))

        {

            PlayUiClick();

            return;

        }

        if (_sceneManager.State != GameState.Playing || _world == null)
            return;

        if (e.Button == MouseButton.Left)
        {
            if (_placementBuildingId != null || _attackMode || _attackMoveMode || _patrolMode ||
                _moveCommandMode || _harvestCommandMode)
            {
                Vector3? commandPos = ScreenToWorldGround(MousePosition);
                var clickPoint = new Vector2(MousePosition.X, MousePosition.Y);

                if (_placementBuildingId != null)
                {
                    if (commandPos == null) return;
                    if (HandlePlaceBuilding(commandPos.Value))
                        CancelPlacementMode(keepSuccessToast: true);
                }
                else if (_harvestCommandMode)
                {
                    if (commandPos == null) return;
                    if (HandleHarvestCommand(commandPos.Value))
                    {
                        _harvestCommandMode = false;
                        if (_uiManager.Current is GameplayHUD harvestHud)
                            harvestHud.ShipControlBar.ClearActiveCommand();
                    }
                }
                else if (_attackMode)
                {
                    Entity? enemy = ResolveAttackTargetAt(clickPoint);
                    if (!enemy.HasValue && commandPos != null)
                        enemy = FindHostileAt(commandPos.Value);
                    if (enemy.HasValue)
                        HandleAttackCommand(enemy.Value);
                    _attackMode = false;
                    if (_uiManager.Current is GameplayHUD attackHud)
                        attackHud.ShipControlBar.ClearActiveCommand();
                }
                else if (_attackMoveMode)
                {
                    if (commandPos == null) return;
                    HandleAttackMoveCommand(commandPos.Value);
                    _attackMoveMode = false;
                }
                else if (_patrolMode)
                {
                    if (commandPos == null) return;
                    HandlePatrolCommand(commandPos.Value);
                    _patrolMode = false;
                }
                else if (_moveCommandMode)
                {
                    if (commandPos == null) return;
                    HandleMoveCommand(commandPos.Value);
                    _moveCommandMode = false;
                    if (_uiManager.Current is GameplayHUD moveHud)
                        moveHud.ShipControlBar.ClearActiveCommand();
                }
            }
            else
            {
                BeginSelectionDrag(screenPoint);
            }
        }
        else if (e.Button == MouseButton.Right)
        {
            ProcessMouseDownRight();
        }
    }

    protected override void OnKeyDown(KeyboardKeyEventArgs e)
    {
        base.OnKeyDown(e);

        if (TryHandleMenuKey(e.Key))
            return;

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

            // Build map panel (B key)
            case Keys.B when !ctrlHeld:
                ToggleBuildMapPanel();
                break;

            // Set rally point (R key + next right-click)
            case Keys.R:
                HandleSetRallyPoint();
                break;

            // Cycle squad formation (G key)
            case Keys.G:
                CycleSelectedFormation();
                break;
        }
    }

    protected override void OnTextInput(TextInputEventArgs e)
    {
        base.OnTextInput(e);

        if (string.IsNullOrEmpty(e.AsString))
            return;

        _uiManager.HandleChar(e.AsString[0]);
    }

    private void HandleMoveCommand(Vector3 worldPos, bool appendWaypoint = false)
    {
        if (_world == null) return;

        var selectedEntities = new List<Entity>();
        foreach (var (entity, sel) in _world.Query<SelectionComponent>())
        {
            if (sel.IsSelected && IsPlayerSelectable(entity))
                selectedEntities.Add(entity);
        }

        if (selectedEntities.Count == 0) return;

        Entity? targetEnemy = FindHostileAt(worldPos);
        if (targetEnemy.HasValue)
        {
            HandleAttackCommand(targetEnemy.Value);
            return;
        }

        Entity? targetNode = FindResourceNodeAt(worldPos);
        var movable = new List<Entity>();

        foreach (var entity in selectedEntities)
        {
            if (targetNode.HasValue)
            {
                var collector = _world.GetComponent<ResourceCollectorComponent>(entity);
                if (collector != null)
                {
                    AssignMinerToNode(entity, collector, targetNode.Value);
                    continue;
                }
            }

            if (_world.GetComponent<MovementComponent>(entity) == null) continue;
            movable.Add(entity);
        }
        foreach (var entity in movable)
        {
            var ct = _world.GetComponent<CombatTargetComponent>(entity);
            if (ct != null)
            {
                ct.CurrentTarget = Entity.Null;
                ct.ManualTarget = false;
            }
        }


        if (movable.Count > 1 && _squadSystem != null)
            _squadSystem.AssignMoveRoutes(_world, movable, worldPos, appendWaypoint);
        else
        {
            foreach (var entity in movable)
                RouteCommands.AssignDestination(_world, entity, worldPos, appendWaypoint);
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

    private bool HandleHarvestCommand(Vector3 worldPos)
    {
        if (_world == null) return false;

        Entity? targetNode = FindResourceNodeAt(worldPos);
        if (!targetNode.HasValue) return false;

        bool assigned = false;
        foreach (var (entity, sel) in _world.Query<SelectionComponent>())
        {
            if (!sel.IsSelected || !IsPlayerSelectable(entity)) continue;

            var collector = _world.GetComponent<ResourceCollectorComponent>(entity);
            if (collector == null) continue;

            AssignMinerToNode(entity, collector, targetNode.Value);
            assigned = true;
        }

        return assigned;
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

        RouteCommands.ClearRoute(_world, minerEntity);
        collector.OrbitAngle = HarvestOrbitHelper.AssignOrbitAngle(minerEntity, nodeEntity);
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

            ClearPatrolAndPath(entity);

            var ct = _world.GetComponent<CombatTargetComponent>(entity);
            if (ct != null)
            {
                ct.CurrentTarget = Entity.Null;
                ct.ManualTarget = false;
            }
        }
    }

    /// <summary>Attack-move: move to position with aggressive auto-engage.</summary>
    private void HandleAttackMoveCommand(Vector3 worldPos, bool appendWaypoint = false)
    {
        if (_world == null) return;

        var movable = new List<Entity>();
        foreach (var (entity, sel) in _world.Query<SelectionComponent>())
        {
            if (!sel.IsSelected || !IsPlayerSelectable(entity)) continue;
            if (_world.GetComponent<MovementComponent>(entity) == null) continue;
            movable.Add(entity);
        }

        if (movable.Count > 1 && _squadSystem != null)
            _squadSystem.AssignMoveRoutes(_world, movable, worldPos, appendWaypoint);
        else
        {
            foreach (var entity in movable)
                RouteCommands.AssignDestination(_world, entity, worldPos, appendWaypoint);
        }

        foreach (var entity in movable)
        {
            var stance = _world.GetComponent<StanceComponent>(entity);
            if (stance != null)
                stance.CurrentStance = Stance.Aggressive;
            else
                _world.AddComponent(entity, new StanceComponent { CurrentStance = Stance.Aggressive });

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

            var ct = _world.GetComponent<CombatTargetComponent>(entity);
            if (ct != null)
            {
                ct.CurrentTarget = Entity.Null;
                ct.ManualTarget = false;
            }

            RouteCommands.AssignPatrol(_world, entity, worldPos, transform.Position);
        }

        _moveTargetPosition = worldPos;
        _moveTargetTimer = 2f;
    }

    private void ClearPatrolAndPath(Entity entity)
    {
        if (_world == null) return;
        RouteCommands.ClearRoute(_world, entity);
    }

    /// <summary>Cycle passive, defensive, and aggressive stances on selected units.</summary>
    private void CycleSelectedStance()
    {
        if (_world == null) return;

        Stance? current = null;
        foreach (var (entity, sel) in _world.Query<SelectionComponent>())
        {
            if (!sel.IsSelected || !IsPlayerSelectable(entity)) continue;
            var stanceComp = _world.GetComponent<StanceComponent>(entity);
            if (stanceComp != null)
            {
                current = stanceComp.CurrentStance;
                break;
            }
        }

        Stance next = current switch
        {
            Stance.Neutral => Stance.Defensive,
            Stance.Defensive => Stance.Aggressive,
            Stance.Aggressive => Stance.Neutral,
            _ => Stance.Defensive,
        };

        SetSelectedStance(next);
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

        BindShipControlBar();
    }

    /// <summary>Activate hero ability at given slot index.</summary>
    private void ActivateAbility(int slot)
    {
        if (_world == null || _abilitySystem == null) return;

        foreach (var (entity, sel) in _world.Query<SelectionComponent>())
        {
            if (!sel.IsSelected) continue;
            if (!_world.HasComponent<AbilityListComponent>(entity)) continue;
            _abilitySystem.ActivateAbility(entity, slot);
        }

        _world.Update(0f);
    }

    /// <summary>Execute a networked ability command (replay / multiplayer).</summary>
    public void ExecuteUseAbilityCommand(UseAbilityCommand command)
    {
        if (_world == null || _abilitySystem == null) return;
        _abilitySystem.HandleUseAbility(_world, command);
        _world.Update(0f);
    }

    /// <summary>Assign currently selected entities to a control group.</summary>
    private void AssignControlGroup(int group)
    {
        if (_world == null) return;

        var entities = GetSelectedPlayerEntities();
        if (entities.Count > 1)
            _squadSystem?.FormSquad(_world, entities);

        _controlGroups[group] = entities;
    }

    /// <summary>Cycle formation layout for the selected squad.</summary>
    private void CycleSelectedFormation()
    {
        if (_world == null || _squadSystem == null) return;

        var selected = GetSelectedPlayerEntities();
        if (_squadSystem.CycleFormation(_world, selected) != null)
            BindShipControlBar();
    }

    /// <summary>Return currently selected player-controllable entities.</summary>
    private List<Entity> GetSelectedPlayerEntities()
    {
        var entities = new List<Entity>();
        if (_world == null) return entities;

        foreach (var (entity, sel) in _world.Query<SelectionComponent>())
        {
            if (sel.IsSelected && IsPlayerSelectable(entity))
                entities.Add(entity);
        }

        return entities;
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
    /// Called when the player cancels a queued production item from the BuildPanel.
    /// </summary>
    private void OnBuildPanelQueueCancelRequested(int queueIndex)
    {
        if (_world == null || _resourceManager == null) return;

        foreach (var (entity, sel) in _world.Query<SelectionComponent>())
        {
            if (!sel.IsSelected) continue;
            var building = _world.GetComponent<BuildingComponent>(entity);
            if (building == null || building.Producible.Count == 0) continue;

            if (BuildingProductionQueue.TryCancelAtIndex(
                    building,
                    queueIndex,
                    defId => _definitions.GetValueOrDefault(defId),
                    _resourceManager,
                    _supplySystem,
                    building.PlayerId))
            {
                Console.WriteLine($"[Build] Cancelled queue item #{queueIndex} at {building.BuildingType}");
            }

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

    private List<string> GetDefaultProducible(string buildingId)
    {
        if (_definitions.TryGetValue(buildingId, out var def) && def.Producible is { Count: > 0 })
            return def.Producible.ToList();
        return [];
    }

    private bool HandlePlaceBuilding(Vector3 worldPos) =>
        _placementBuildingId != null && TryPlaceBuildingAt(_placementBuildingId, worldPos);

    private bool TryPlaceBuildingAt(string buildingId, Vector3 worldPos)
    {
        if (_world == null || _resourceManager == null ||
            _gridSystem == null || _buildMapCatalog == null)
            return false;

        worldPos = BuildingFootprint.SnapToCellCenter(_gridSystem, worldPos);

        if (!_definitions.TryGetValue(buildingId, out var def))
        {
            def = _assetManager?.Load<EntityDefinition>($"Bases/{buildingId}");
            if (def != null)
                _definitions[buildingId] = def;
        }

        if (def == null)
        {
            Console.WriteLine($"[Place] Unknown building: {buildingId}");
            return false;
        }

        var validation = BuildingPlacementValidator.Validate(
            _gridSystem, _world, playerId: 1, def, worldPos,
            _buildMapCatalog, _resourceManager, _supplySystem);
        if (!validation.IsValid)
        {
            string message = validation.Reason.ToPlayerMessage();
            Console.WriteLine($"[Place] Invalid location for {def.DisplayName}: {validation.Reason}");
            FlashPlacementFailure(message);
            return false;
        }

        if (_placementBuilderEntity is Entity builderEntity)
        {
            if (!IsBuilderInPlacementRange(builderEntity, worldPos, out string rangeReason))
            {
                Console.WriteLine($"[Place] Invalid location for {def.DisplayName}: {rangeReason}");
                FlashPlacementFailure(rangeReason);
                return false;
            }
        }

        int energy = def.Cost?.Energy ?? 0;
        int minerals = def.Cost?.Minerals ?? 0;
        int data = def.Cost?.Data ?? 0;
        int crew = def.Cost?.Crew ?? 0;
        if (!_resourceManager.TrySpendCost(1, energy, minerals, data, crew))
        {
            Console.WriteLine($"[Place] Cannot afford {def.DisplayName}");
            FlashPlacementFailure(PlacementFailureReason.CannotAfford.ToPlayerMessage());
            return false;
        }

        string buildingType = def.Components?.Building?.BuildingType ?? buildingId;
        var buildingDef = def.Components?.Building;
        string raceId = ResolveFactionRaceId(1, isEnemy: false);
        var (_, _, scale) = ResolveBuildingMesh(buildingType, 1);

        var building = _world.CreateEntity();
        _world.AddComponent(building, new TransformComponent { Position = worldPos, Scale = scale });
        bool instantDemoPlacement = _demoRecordingMode;
        var placedRender = new RenderComponent { Visible = true, MeshId = -1, PrimitiveType = (int)PrimitiveType.Triangles };
        if (def.BuildTime > 0f && !instantDemoPlacement)
            ApplyStructureConstructionMesh(placedRender, buildingType, raceId, playerId: 1, buildFraction: 0f);
        else
        {
            var (meshId, vertCount, _) = ResolveBuildingMesh(buildingType, 1);
            placedRender.MeshId = meshId;
            placedRender.VertexCount = vertCount;
            ApplyRaceTexturing(placedRender, raceId, 1);
        }

        _world.AddComponent(building, placedRender);
        _world.AddComponent(building, new SelectionComponent { IsSelected = false, SelectionRadius = 15f });

        var healthDef = def.Components?.Health;
        float maxHp = healthDef?.MaxHP ?? 1000f;

        if (def.BuildTime > 0f && !instantDemoPlacement)
        {
            // Reduced HP during construction: targetable but less punishing than full health.
            // True invulnerability would require ProjectileSystem damage guards (deferred to wo-sc-01-03).
            _world.AddComponent(building, new HealthComponent
            {
                MaxHP = maxHp,
                CurrentHP = maxHp * 0.25f,
                Armor = healthDef?.Armor ?? 50f,
            });

            _world.AddComponent(building, new BuildingComponent
            {
                BuildingType = buildingDef?.BuildingType ?? buildingId,
                ProductionRate = 0f,
                Footprint = buildingDef?.Footprint ?? [2, 2],
                Rotates = buildingDef?.Rotates ?? false,
                PlayerId = 1,
                RallyPoint = worldPos + new Vector3(30f, 0f, 0f),
                Producible = def.Producible?.ToList() ?? new List<string>(),
            });
            _world.AddComponent(building, new UnderConstructionComponent
            {
                DefinitionId = def.Id,
                BuildProgress = 0f,
                TotalBuildTime = def.BuildTime,
                PlayerId = 1,
            });
        }
        else
        {
            _world.AddComponent(building, new HealthComponent
            {
                MaxHP = maxHp,
                CurrentHP = maxHp,
                Armor = healthDef?.Armor ?? 50f,
            });

            _world.AddComponent(building, new BuildingComponent
            {
                BuildingType = buildingDef?.BuildingType ?? buildingId,
                ProductionRate = buildingDef?.ProductionRate ?? 1f,
                Footprint = buildingDef?.Footprint ?? [2, 2],
                Rotates = buildingDef?.Rotates ?? false,
                PlayerId = 1,
                RallyPoint = worldPos + new Vector3(30f, 0f, 0f),
                Producible = def.Producible?.ToList() ?? new List<string>(),
            });
        }

        _world.AddComponent(building, new EntityNameComponent
        {
            DisplayName = string.IsNullOrWhiteSpace(def.DisplayName) ? buildingType.Replace("_", " ") : def.DisplayName,
            DefinitionId = def.Id,
        });

        BuildingFootprint.Occupy(_gridSystem, building,
            worldPos, buildingDef?.Footprint ?? [2, 2]);

        PlayBuildingPlaced(worldPos);

        if (_placementBuilderEntity is Entity placedBuilder)
            RouteCommands.ClearRoute(_world, placedBuilder);

        if (_uiManager.Current is GameplayHUD placementHud)
            placementHud.DismissBuildFlowHint();

        string displayName = string.IsNullOrWhiteSpace(def.DisplayName) ? buildingId : def.DisplayName;
        FlashPlacementSuccess(PlacementFailureReasonExtensions.BuildPlacedMessage(displayName));

        string placeStatus = def.BuildTime > 0f ? "Started construction of" : "Built";
        Console.WriteLine($"[Place] {placeStatus} {def.DisplayName} at ({worldPos.X:F0}, {worldPos.Z:F0})");
        return true;
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

        var queuedDefIds = new HashSet<string>(selectedBuilding.BuildQueue, StringComparer.OrdinalIgnoreCase);

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
                IsQueued = queuedDefIds.Contains(def.Id),
                Role = ShipRoleResolver.Resolve(def),
            });
        }
        hud.BuildPanel.AvailableItems = items;

        // Queue
        var queueItems = new List<QueuedItem>();
        int qIdx = 0;
        foreach (string qItem in selectedBuilding.BuildQueue)
        {
            bool isCurrent = qIdx == 0;
            float progress = 0f;
            QueuedState state = QueuedState.Queued;

            float totalTime = 0f;
            float remainingSeconds = 0f;
            if (isCurrent)
            {
                state = QueuedState.Building;
                var qDef = _definitions.GetValueOrDefault(qItem);
                totalTime = qDef?.BuildTime ?? 1f;
                progress = totalTime > 0f
                    ? Math.Clamp(selectedBuilding.BuildProgress / totalTime, 0f, 1f)
                    : 0f;
                if (totalTime > 0f && selectedBuilding.ProductionRate > 0f)
                {
                    float remainingProgress = MathF.Max(0f, totalTime - selectedBuilding.BuildProgress);
                    remainingSeconds = remainingProgress / selectedBuilding.ProductionRate;
                }
            }

            string name = _definitions.TryGetValue(qItem, out var d) ? d.DisplayName : qItem;
            queueItems.Add(new QueuedItem
            {
                Name = name,
                Progress = progress,
                IsCurrent = isCurrent,
                QueueIndex = qIdx + 1,
                State = state,
                TotalBuildTime = totalTime,
                RemainingSeconds = remainingSeconds,
            });
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
            if (!sel.IsSelected || !IsPlayerSelectable(entity)) continue;
            if (unitInfos.Count >= 4) break;
            var health = _world.GetComponent<HealthComponent>(entity);
            string name = ResolveEntityDisplayName(entity);
            if (health != null)
            {
                string? raceId = _world.GetComponent<RaceComponent>(entity)?.RaceId;
                unitInfos.Add(UnitInfo.FromHealth(name, health, raceId: raceId));
            }
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
            Size.Y > 0 ? (float)Size.X / Size.Y : 4f / 3f,
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
        var uiViewport = UiViewportSize;
        GL.Viewport(0, 0, (int)uiViewport.X, (int)uiViewport.Y);
        _uiRenderer?.UpdateViewport((int)uiViewport.X, (int)uiViewport.Y);
        _uiManager?.Resize(uiViewport);
    }

    protected override void OnUnload()
    {
        base.OnUnload();
        CleanupGameplay();
        _environment.Dispose();
        _shaderManager.Dispose();
        _uiRenderer.Dispose();
        _sceneManager.Dispose();

        DisposeAudio();

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
            if (_routePreviewVbo != 0)
                MeshBuilder.DeleteMesh(_routePreviewVao, _routePreviewVbo);
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

        using var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read);
        var writer = new ImageWriter();
        writer.WritePng(flipped, width, height, ColorComponents.RedGreenBlueAlpha, stream);
        stream.Flush(true);
    }
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

