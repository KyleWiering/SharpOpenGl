using System.Text.Json;
using Microsoft.JSInterop;
using OpenTK.Mathematics;
using SharpOpenGl.Browser.Assets;
using SharpOpenGl.Browser.Rendering;
using SharpOpenGl.Engine.Assets;
using SharpOpenGl.Engine.Config;
using SharpOpenGl.Engine.ECS;
using SharpOpenGl.Engine.Input;
using SharpOpenGl.Engine.Grid;
using SharpOpenGl.Engine.Economy;
using SharpOpenGl.Engine.Events;
using SharpOpenGl.Engine.Missions;
using SharpOpenGl.Engine.Persistence;
using SharpOpenGl.Engine.Rendering;
using SharpOpenGl.Engine.Scenes;
using SharpOpenGl.Engine.UI;
using SharpOpenGl.Engine.UI.Screens;
using SharpOpenGl.Engine.UI.Widgets;

namespace SharpOpenGl.Browser.Game;

/// <summary>
/// Browser game shell — same Engine UI, ECS, shaders, and mesh pipeline as desktop.
/// </summary>
public sealed class BrowserGameHost : IDisposable
{
    private const string SceneMainMenu = "MainMenu";
    private const string SceneGameplay = "Gameplay";
    private const string GameDataRoot = "GameData";

    private readonly EventBus _eventBus = new();
    private readonly SceneManager _sceneManager;
    private readonly UIManager _uiManager;
    private readonly CanvasUIRenderer _uiRenderer;
    private readonly WebGlRenderer _glRenderer;
    private readonly BrowserMeshLibrary _meshes = new();
    private readonly BrowserGameplayRenderer _gameplayRenderer;
    private readonly ExplosionVfxController _explosionVfx = new();
    private readonly HttpAssetTextSource _assetSource;
    private readonly AssetManager _assetManager;
    private readonly MissionLoader _missionLoader;
    private readonly MissionController _missionController;
    private readonly CampaignProgressManager _campaignProgress = new();
    private readonly SettingsManager _settingsManager = new();
    private readonly SaveManager _saveManager = SaveManager.CreateInMemory();

    private readonly CombatFogGate _combatFogGate = new();
    private readonly TouchInput _touchInput = new();
    private readonly Dictionary<int, TouchPoint> _touchState = new();
    private readonly List<TouchPoint> _touchFrame = new();

    private World? _world;
    private ResourceManager? _resourceManager;
    private MovementSystem? _movementSystem;
    private ArticulationSystem? _articulationSystem;
    private ObjectiveSystem? _objectiveSystem;
    private string? _pendingMissionId;
    private bool _missionEventsHooked;
    private bool _missionResultOverlayShown;
    private bool _victoryRewardsApplied;
    private int _viewportWidth = 1024;
    private int _viewportHeight = 768;
    private bool _initialized;

    public BrowserGameHost(
        CanvasUIRenderer uiRenderer,
        WebGlRenderer glRenderer,
        HttpAssetTextSource assetSource)
    {
        _uiRenderer = uiRenderer;
        _glRenderer = glRenderer;
        _assetSource = assetSource;
        _gameplayRenderer = new BrowserGameplayRenderer(glRenderer, _meshes);

        _assetManager = new AssetManager(GameDataRoot, assetSource);
        _missionLoader = new MissionLoader(_assetManager);
        _missionController = new MissionController(_missionLoader, null, _eventBus);

        _sceneManager = new SceneManager(_eventBus);
        _uiManager = new UIManager(_eventBus);

        _sceneManager.Register(SceneMainMenu, () => new BrowserMainMenuScene(this));
        _sceneManager.Register(SceneGameplay, () => new BrowserGameplayScene(this));
    }

    public async Task InitializeAsync(int width, int height)
    {
        _viewportWidth = width;
        _viewportHeight = height;

        await PreloadAssetsAsync();
        await _uiRenderer.InitializeAsync("ui-canvas", width, height);
        await _glRenderer.InitializeAsync("gl-canvas", width, height);
        await _meshes.InitializeAsync(_glRenderer);

        _uiManager.Resize(new Vector2(width, height));
        _explosionVfx.Bind(_eventBus);
        EnsureMissionEventHooks();
        _sceneManager.TransitionTo(SceneMainMenu, GameState.MainMenu);
        _initialized = true;
    }

    private void EnsureMissionEventHooks()
    {
        if (_missionEventsHooked) return;
        _missionEventsHooked = true;

        _eventBus.Subscribe<MissionVictoryEvent>(OnMissionVictory);
        _eventBus.Subscribe<MissionDefeatEvent>(OnMissionDefeat);
    }

    private bool IsMissionResultOverlayActive() =>
        _uiManager.Current is MissionVictoryScreen;

    public void Resize(int width, int height)
    {
        _viewportWidth = width;
        _viewportHeight = height;
        _uiManager.Resize(new Vector2(width, height));
        _uiRenderer.Resize(width, height);
        _glRenderer.Resize(width, height);

        if (_uiManager.Current is GameplayHUD hud)
            hud.ConfigureTouchLayout(AdaptiveLayout.Detect(new Vector2(width, height)));
    }

    public void OnFrame(float deltaTime)
    {
        if (!_initialized) return;

        UpdateTouchInput(deltaTime);

        _sceneManager.Update(deltaTime);

        if (_sceneManager.State == GameState.Playing && _world != null && _articulationSystem != null)
            _articulationSystem.CameraPosition = GetGameplayCameraPosition();

        if (_sceneManager.State == GameState.Playing && _world != null && !IsMissionResultOverlayActive())
            _world.Update(deltaTime);

        if (_sceneManager.State == GameState.Playing && _world != null)
        {
            _explosionVfx.Update(deltaTime);
            _gameplayRenderer.Render(_world, _viewportWidth, _viewportHeight, _explosionVfx);
        }

        if (_uiManager.Current is GameplayHUD activeHud)
        {
            BindResourceHud(activeHud);
            MissionHudBinder.BindObjectivePanel(activeHud, _missionController.CurrentMission);
        }

        _uiRenderer.Begin();
        _uiManager.Draw(_uiRenderer);
        _uiRenderer.End();
    }

    public bool HandlePointer(int x, int y, bool isDown)
    {
        if (!isDown) return false;
        return _uiManager.HandlePointerTapped(new Vector2(x, y), 0, new Vector2(_viewportWidth, _viewportHeight));
    }

    /// <summary>Forward pointer movement for hover/tooltip tracking on the UI canvas.</summary>
    public void HandlePointerMove(int x, int y, bool isDown = false)
    {
        _uiManager.HandlePointerMove(
            new Vector2(x, y),
            isDown,
            new Vector2(_viewportWidth, _viewportHeight));
    }

    public void HandleKey(string key)
    {
        if (_sceneManager.State != GameState.Playing || IsMissionResultOverlayActive()) return;

        float pan = 1f;
        float dt = 1f / 60f;
        var cam = _gameplayRenderer.Camera;
        switch (key.ToUpperInvariant())
        {
            case "W": case "ARROWUP": cam.Pan(0, -pan, dt); break;
            case "S": case "ARROWDOWN": cam.Pan(0, pan, dt); break;
            case "A": case "ARROWLEFT": cam.Pan(-pan, 0, dt); break;
            case "D": case "ARROWRIGHT": cam.Pan(pan, 0, dt); break;
            case "Q": cam.Pan(-pan, 0, dt); break;
            case "E": cam.Pan(pan, 0, dt); break;
            case "Z": cam.AdjustHeight(1f, dt); break;
            case "X": cam.AdjustHeight(-1f, dt); break;
        }
    }

    [JSInvokable]
    public void OnResize(int width, int height) => Resize(width, height);

    [JSInvokable]
    public void OnTouchPoints(string json)
    {
        _touchFrame.Clear();
        if (string.IsNullOrWhiteSpace(json)) return;

        try
        {
            using var doc = JsonDocument.Parse(json);
            foreach (var element in doc.RootElement.EnumerateArray())
            {
                int id = element.GetProperty("id").GetInt32();
                float x = element.GetProperty("x").GetSingle();
                float y = element.GetProperty("y").GetSingle();
                bool isActive = element.GetProperty("isActive").GetBoolean();

                if (!_touchState.TryGetValue(id, out TouchPoint? point))
                {
                    point = new TouchPoint
                    {
                        Id = id,
                        StartPosition = new Vector2(x, y),
                    };
                    _touchState[id] = point;
                }

                point.WasActive = point.IsActive;
                point.Position = new Vector2(x, y);
                point.IsActive = isActive;
                point.ContactDuration += isActive ? 0f : 0f;
                _touchFrame.Add(point);

                if (!isActive)
                    _touchState.Remove(id);
            }
        }
        catch (JsonException ex)
        {
            Console.WriteLine($"[Touch] Parse error: {ex.Message}");
        }
    }

    private void UpdateTouchInput(float dt)
    {
        if (_touchFrame.Count == 0) return;

        _touchInput.SetTouchPoints(_touchFrame);
        _touchInput.Update(dt);

        var viewport = new Vector2(_viewportWidth, _viewportHeight);

        if (_touchInput.IsActionPressed(InputAction.Select))
            _uiManager.HandlePointerTapped(_touchInput.PointerPosition, 0, viewport);

        if (_sceneManager.State != GameState.Playing || IsMissionResultOverlayActive())
            return;

        var cam = _gameplayRenderer.Camera;
        var panH = _touchInput.GetAxis("MoveHorizontal");
        var panV = _touchInput.GetAxis("MoveVertical");
        cam.Pan(panH.X, panV.Y, dt);

        float pinch = _touchInput.GetAxis("PinchZoom").X;
        if (MathF.Abs(pinch) > 0.001f)
            cam.Zoom(pinch * 12f);

        if (_uiManager.Current is GameplayHUD hud)
        {
            hud.UpdateTouchJoystick(_touchFrame);
            Vector2 joy = hud.ReadCameraJoystickAxis();
            if (joy.LengthSquared > 0.01f)
            {
                float panBoost = 2.1f * _settingsManager.Current.Clamped().CameraPanSpeed;
                cam.PanSpeedMultiplier = panBoost;
                cam.Pan(joy.X, joy.Y, dt);
            }
        }
    }

    internal void ShowMainMenu()
    {
        _uiManager.Clear();
        var menu = new MainMenuScreen();
        menu.NewGameRequested += ShowMissionSelect;
        menu.MultiplayerRequested += () => StartGameplay(null);
        menu.ContinueRequested += () => StartGameplay(null);
        menu.ShipDesignerRequested += () => { };
        menu.SettingsRequested += ShowSettings;
        menu.QuitRequested += () => { };
        _uiManager.Push(menu);
    }

    internal void ShowMissionSelect()
    {
        var missionSelect = new MissionSelectScreen();
        missionSelect.SetMissions(LoadMissionEntries(), _campaignProgress.CompletedMissionIds);
        missionSelect.MissionStartRequested += ShowMissionBriefing;
        missionSelect.BackRequested += () => _uiManager.Pop();
        _uiManager.Push(missionSelect);
    }

    internal void ShowSettings()
    {
        var settings = new SettingsScreen(_settingsManager);
        settings.BackRequested += () => _uiManager.Pop();
        _uiManager.Push(settings);
    }

    internal void ShowMissionBriefing(string missionId)
    {
        var definition = _missionLoader.Load(missionId);
        if (definition == null)
        {
            StartGameplay(null);
            return;
        }

        _pendingMissionId = missionId;
        _missionController.StartMission(missionId);

        var briefing = new BriefingScreen();
        briefing.SetMission(definition);
        briefing.StartRequested += () =>
        {
            _uiManager.Pop();
            StartGameplay(missionId);
        };
        briefing.BackRequested += () => _uiManager.Pop();
        _uiManager.Push(briefing);
    }

    internal void StartGameplay(string? missionId)
    {
        _pendingMissionId = missionId;
        _sceneManager.TransitionTo(SceneGameplay, GameState.Playing);
    }

    internal void BeginGameplaySession()
    {
        _uiManager.Clear();
        var hud = new GameplayHUD();
        hud.PauseRequested += ShowPauseMenu;
        hud.ConfigureTouchLayout(AdaptiveLayout.Detect(new Vector2(_viewportWidth, _viewportHeight)));
        _uiManager.Push(hud);

        InitializeWorld(_pendingMissionId);
        BindResourceHud(hud);
        MissionHudBinder.BindObjectivePanel(hud, _missionController.CurrentMission);
    }

    private void ShowPauseMenu()
    {
        bool hasSave = _saveManager.ListSaveFiles().Length > 0;
        var pause = new PauseScreen(hasSave);
        pause.ResumeRequested += () => _uiManager.Pop();
        pause.SaveGameRequested += ShowSaveGameScreen;
        pause.LoadGameRequested += ShowLoadGameScreen;
        pause.SettingsRequested += ShowSettings;
        pause.QuitToMenuRequested += () =>
        {
            _world?.Dispose();
            _world = null;
            _uiManager.Clear();
            _sceneManager.TransitionTo(SceneMainMenu, GameState.MainMenu);
        };
        _uiManager.Push(pause);
    }

    private void ShowSaveGameScreen()
    {
        var saveScreen = new SaveGameScreen(_saveManager);
        saveScreen.SlotSelected += slot =>
            saveScreen.RequestSave(slot, () => BuildBrowserSaveSnapshot(slot));
        saveScreen.SaveCompleted += _ => Console.WriteLine("[Save] Browser session saved.");
        saveScreen.Cancelled += () => _uiManager.Pop();
        _uiManager.Push(saveScreen);
    }

    private void ShowLoadGameScreen()
    {
        var loadScreen = new LoadGameScreen(_saveManager);
        loadScreen.LoadRequested += slot =>
        {
            var data = _saveManager.Load(slot);
            if (data == null)
            {
                Console.WriteLine($"[Save] Failed to load '{slot}'.");
                return;
            }

            ApplyBrowserSaveData(data);
            _uiManager.Pop();
        };
        loadScreen.BackRequested += () => _uiManager.Pop();
        _uiManager.Push(loadScreen);
    }

    private SaveData BuildBrowserSaveSnapshot(string slotName)
    {
        var cam = _gameplayRenderer.Camera;
        var data = new SaveData
        {
            SlotName = slotName,
            MissionId = _missionController.CurrentMission?.Definition.Id ?? string.Empty,
            IsSandboxSession = string.IsNullOrEmpty(_pendingMissionId),
            ElapsedMissionTime = _missionController.CurrentMission?.ElapsedTime ?? 0f,
            CameraX = cam.Target.X,
            CameraY = cam.Target.Z,
            CameraZoom = cam.Height / 80f,
        };

        if (_resourceManager != null)
        {
            var res = _resourceManager.GetPlayer(1);
            if (res != null)
            {
                data.PlayerResources.Add(new PlayerResourceRecord
                {
                    PlayerId = 1,
                    Energy = res.GetAmount(ResourceType.Energy),
                    Minerals = res.GetAmount(ResourceType.Minerals),
                    Data = res.GetAmount(ResourceType.Data),
                    Crew = res.GetAmount(ResourceType.Crew),
                });
            }
        }

        if (_world != null)
        {
            foreach (var (entity, transform) in _world.Query<TransformComponent>())
            {
                var health = _world.GetComponent<HealthComponent>(entity);
                data.Entities.Add(new EntitySaveRecord
                {
                    EntityId = (int)entity.Index,
                    X = transform.Position.X,
                    Y = transform.Position.Z,
                    Health = health?.CurrentHP ?? 0f,
                    Shields = health?.CurrentShields ?? 0f,
                    PlayerId = 1,
                });
            }
        }

        return data;
    }

    private void ApplyBrowserSaveData(SaveData data)
    {
        if (_world == null) return;

        var cam = _gameplayRenderer.Camera;
        cam.Target = new Vector3(data.CameraX, 0f, data.CameraY);
        if (data.CameraZoom > 0f)
            cam.Height = MathHelper.Clamp(data.CameraZoom * 80f, cam.MinHeight, cam.MaxHeight);

        if (_resourceManager != null && data.PlayerResources.Count > 0)
        {
            var record = data.PlayerResources[0];
            var res = _resourceManager.GetPlayer(record.PlayerId);
            res?.SetStartingAmount(ResourceType.Energy, record.Energy);
            res?.SetStartingAmount(ResourceType.Minerals, record.Minerals);
            res?.SetStartingAmount(ResourceType.Data, record.Data);
            res?.SetStartingAmount(ResourceType.Crew, record.Crew);
        }

        if (_uiManager.Current is GameplayHUD hud)
            BindResourceHud(hud);
    }

    private void BindResourceHud(GameplayHUD hud)
    {
        if (_resourceManager == null) return;
        hud.ResourceBar.Resources =
        [
            _resourceManager.GetDisplay(1, ResourceType.Energy),
            _resourceManager.GetDisplay(1, ResourceType.Minerals),
            _resourceManager.GetDisplay(1, ResourceType.Data),
            _resourceManager.GetDisplay(1, ResourceType.Crew),
        ];
    }

    private void OnMissionVictory(MissionVictoryEvent evt)
    {
        if (_missionResultOverlayShown) return;
        if (_sceneManager.State != GameState.Playing) return;
        if (_missionController.CurrentMission == null) return;
        if (!evt.MissionId.Equals(_missionController.CurrentMission.Definition.Id, StringComparison.Ordinal))
            return;

        if (!_victoryRewardsApplied)
        {
            _missionController.DistributeRewards(1);
            _victoryRewardsApplied = true;
        }

        _campaignProgress.MarkCompleted(evt.MissionId);
        ShowMissionResultOverlay(isVictory: true);
    }

    private void OnMissionDefeat(MissionDefeatEvent evt)
    {
        if (_missionResultOverlayShown) return;
        if (_sceneManager.State != GameState.Playing) return;
        if (_missionController.CurrentMission == null) return;
        if (!evt.MissionId.Equals(_missionController.CurrentMission.Definition.Id, StringComparison.Ordinal))
            return;

        ShowMissionResultOverlay(isVictory: false, defeatReason: evt.Reason);
    }

    private void ShowMissionResultOverlay(bool isVictory, string? defeatReason = null)
    {
        if (_missionResultOverlayShown || _missionController.CurrentMission == null)
            return;

        _missionResultOverlayShown = true;

        var screen = new MissionVictoryScreen();
        screen.SetMissionResult(_missionController.CurrentMission, isVictory, defeatReason);
        screen.ReturnToMenuRequested += OnMissionResultReturnToMenu;
        screen.ReplayMissionRequested += OnMissionResultReplay;
        _uiManager.Push(screen);
    }

    private void OnMissionResultReturnToMenu()
    {
        if (_uiManager.Current is MissionVictoryScreen)
            _uiManager.Pop();

        _missionResultOverlayShown = false;
        _victoryRewardsApplied = false;
        _missionController.Unload();
        _world?.Dispose();
        _world = null;
        _objectiveSystem = null;
        _uiManager.Clear();
        _sceneManager.TransitionTo(SceneMainMenu, GameState.MainMenu);
    }

    private void OnMissionResultReplay()
    {
        if (_missionController.CurrentMission == null) return;

        string missionId = _missionController.CurrentMission.Definition.Id;
        if (_uiManager.Current is MissionVictoryScreen)
            _uiManager.Pop();

        _missionResultOverlayShown = false;
        _victoryRewardsApplied = false;
        _missionController.ReplayMission();

        var definition = _missionLoader.Load(missionId);
        if (definition == null)
        {
            StartGameplay(missionId);
            return;
        }

        var briefing = new BriefingScreen();
        briefing.SetMission(definition);
        briefing.StartRequested += () =>
        {
            _uiManager.Pop();
            StartGameplay(missionId);
        };
        briefing.BackRequested += OnMissionResultReturnToMenu;
        _uiManager.Push(briefing);
    }

    private void InitializeWorld(string? missionId)
    {
        _world?.Dispose();
        _objectiveSystem = null;
        _missionResultOverlayShown = false;
        _victoryRewardsApplied = false;

        _world = new World();
        _movementSystem = new MovementSystem();
        _world.AddSystem(new StanceSystem(_combatFogGate));
        _world.AddSystem(new CombatSystem(_eventBus, _combatFogGate));
        _world.AddSystem(new ProjectileSystem(_eventBus));
        _world.AddSystem(new CombatFeedbackSystem(_eventBus));
        _world.AddSystem(_movementSystem);
        _world.AddSystem(new UtilityPartMeshResolveSystem());
        _world.AddSystem(new UtilityArticulationAimSystem());
        _world.AddSystem(new SpecialHullArticulationAimSystem { FogGate = _combatFogGate });
        _articulationSystem = new ArticulationSystem(_combatFogGate);
        _world.AddSystem(_articulationSystem);

        _resourceManager = new ResourceManager(_eventBus);
        var playerRes = _resourceManager.AddPlayer(1);
        playerRes.SetStartingAmount(ResourceType.Energy, 500f);
        playerRes.SetStartingAmount(ResourceType.Minerals, 300f);
        playerRes.SetStartingAmount(ResourceType.Data, 100f);
        playerRes.SetStartingAmount(ResourceType.Crew, 50f);
        playerRes.AddIncome(ResourceType.Energy, 5f);
        playerRes.AddIncome(ResourceType.Minerals, 3f);
        _world.AddSystem(new ResourceSystem(_resourceManager));
        _world.AddSystem(new MiningVisualSystem());

        if (!string.IsNullOrEmpty(missionId) && _missionController.CurrentMission != null)
            SpawnMissionFleet(missionId);
        else
        {
            // Browser sandbox save/load: desktop-first iteration 4 — seed persisted via SaveData v2 when wired.
            // TODO: sandbox menu — wire SandboxSetupScreen when browser TextField stack is ready.
            // Desktop WorldSaveService.Capture / WorldLoadService.Restore are not reachable here yet; fixed-seed spawn below.
            SpawnSandboxFleet();
        }

        _missionController.BeginGameplay();

        if (_missionController.CurrentMission != null)
        {
            var state = _missionController.CurrentMission;
            state.Phase = MissionPhase.InProgress;
            _objectiveSystem = new ObjectiveSystem(state, _eventBus, _resourceManager) { PlayerId = 1 };
            _world.AddSystem(_objectiveSystem);
        }
    }

    // Browser sandbox save/load: desktop-first iteration 4 — seed persisted via SaveData v2 when wired.
    private void SpawnSandboxFleet()
    {
        if (_world == null) return;

        int sandboxSeed = ProceduralSeedHelper.ParseSeed("browser");
        var sandboxConfig = SandboxConfig.Load(GameDataRoot);
        sandboxConfig.ApplyStartingResources(_resourceManager!, playerId: 1);
        Console.WriteLine($"[Sandbox] Browser stub seed={sandboxSeed} text='browser'");

        var hero = _world.CreateEntity();
        _world.AddComponent(hero, new TransformComponent { Position = Vector3.Zero, Scale = Vector3.One });
        var heroMove = new MovementComponent { Speed = 80f, Acceleration = 120f, TurnRate = 180f };
        heroMove.PathTarget = new Vector3(50f, 0f, 50f);
        _world.AddComponent(hero, heroMove);
        _world.AddComponent(hero, new SelectionComponent { IsSelected = true, SelectionRadius = 8f });
        _world.AddComponent(hero, new RenderComponent
        {
            MeshId = _meshes.Hero,
            VertexCount = _meshes.HeroCount,
            Visible = true,
            PrimitiveType = GlPrimitive.Triangles,
        });
        _world.AddComponent(hero, new HeroComponent());

        var rng = new Random(42);
        for (int i = 0; i < 5; i++)
        {
            float x = (rng.NextSingle() - 0.5f) * 400f;
            float z = (rng.NextSingle() - 0.5f) * 400f;
            SpawnFighter(new Vector3(x, 0f, z), selected: false);
        }
    }

    private void SpawnFighter(Vector3 position, bool selected)
    {
        var fighter = _world!.CreateEntity();
        _world.AddComponent(fighter, new TransformComponent { Position = position, Scale = Vector3.One });
        _world.AddComponent(fighter, new MovementComponent { Speed = 150f, Acceleration = 220f, TurnRate = 220f });
        _world.AddComponent(fighter, new SelectionComponent { IsSelected = selected, SelectionRadius = 6f });
        _world.AddComponent(fighter, new RenderComponent
        {
            MeshId = _meshes.Fighter,
            VertexCount = _meshes.FighterCount,
            Color = new Vector4(0.4f, 1f, 0.4f, 1f),
            Visible = true,
            PrimitiveType = GlPrimitive.Triangles,
        });
    }

    private void SpawnMissionFleet(string missionId)
    {
        if (_world == null || _missionController.CurrentMission == null) return;

        var start = _missionController.CurrentMission.Definition.StartConditions;
        Vector3 spawn = new((start?.PlayerSpawn?[0] ?? 3) * 10f, 0f, (start?.PlayerSpawn?[1] ?? 3) * 10f);
        _gameplayRenderer.Camera.Target = spawn;

        if (_resourceManager != null)
            _resourceManager.UnlimitedResources = start?.UnlimitedResources == true;

        if (start?.StartingResources != null)
        {
            var res = _resourceManager!.GetPlayer(1);
            res?.SetStartingAmount(ResourceType.Energy, start.StartingResources.Energy);
            res?.SetStartingAmount(ResourceType.Minerals, start.StartingResources.Minerals);
            res?.SetStartingAmount(ResourceType.Data, start.StartingResources.Data);
            res?.SetStartingAmount(ResourceType.Crew, start.StartingResources.Crew);
        }

        string[] units = start?.StartingUnits is { Length: > 0 }
            ? start.StartingUnits
            : ["hero_default"];

        float spacing = 18f;
        for (int i = 0; i < units.Length; i++)
        {
            float angle = MathF.PI * 2f * i / units.Length;
            var pos = spawn + new Vector3(MathF.Cos(angle) * spacing, 0f, MathF.Sin(angle) * spacing);
            bool isHero = units[i].Contains("hero", StringComparison.OrdinalIgnoreCase);

            var entity = _world.CreateEntity();
            _world.AddComponent(entity, new TransformComponent { Position = pos, Scale = Vector3.One });
            _world.AddComponent(entity, new MovementComponent { Speed = 80f, Acceleration = 120f, TurnRate = 180f });
            _world.AddComponent(entity, new SelectionComponent { IsSelected = i == 0, SelectionRadius = 8f });
            _world.AddComponent(entity, new RenderComponent
            {
                MeshId = isHero ? _meshes.Hero : _meshes.Fighter,
                VertexCount = isHero ? _meshes.HeroCount : _meshes.FighterCount,
                Visible = true,
                PrimitiveType = GlPrimitive.Triangles,
            });
            if (isHero)
                _world.AddComponent(entity, new HeroComponent());
        }
    }

    private IEnumerable<MissionEntry> LoadMissionEntries()
    {
        var known = new[]
        {
            "tutorial_01", "example_scenario", "mission_02",
            "mission_03", "mission_04", "mission_05",
            "mission_build_tree",
        };

        var completed = _campaignProgress.CompletedMissionIds;
        var entries = new List<MissionEntry>();
        foreach (string id in known)
        {
            var definition = _missionLoader.Load(id);
            if (definition != null)
                entries.Add(ToMissionEntry(definition, completed));
        }

        if (entries.Count == 0)
        {
            entries.Add(new MissionEntry
            {
                Id = "tutorial_01",
                Title = "Tutorial - First Steps",
                Description = "Learn the basics of fleet command.",
                MapId = "sector_alpha",
                BriefingText = "Commander, sensors have detected an enemy scout in Sector Alpha.",
                ObjectivesPreview = ["Destroy the enemy scout", "Protect your base"],
            });
        }

        return entries;
    }

    private static MissionEntry ToMissionEntry(
        MissionDefinition definition,
        IReadOnlySet<string>? completedMissionIds = null) =>
        MissionEntryMapper.FromDefinition(definition, completedMissionIds);

    private Vector3 GetGameplayCameraPosition()
    {
        var cam = _gameplayRenderer.Camera;
        float tiltRad = MathHelper.DegreesToRadians(cam.TiltAngle);
        float offsetZ = cam.Height * MathF.Tan(tiltRad);
        return cam.Target + new Vector3(0f, cam.Height, offsetZ);
    }

    private async Task PreloadAssetsAsync()
    {
        string[] keys =
        [
            "Missions/tutorial_01", "Missions/example_scenario", "Missions/mission_02",
            "Missions/mission_03", "Missions/mission_04", "Missions/mission_05",
            "Missions/mission_build_tree",
            "Ships/hero_default", "Ships/fighter_basic", "Config/balance",
        ];
        await _assetSource.PreloadAsync(keys, GameDataRoot);
    }

    public void Dispose()
    {
        _world?.Dispose();
        _sceneManager.Dispose();
    }
}

internal sealed class BrowserMainMenuScene : IScene
{
    private readonly BrowserGameHost _host;
    public BrowserMainMenuScene(BrowserGameHost host) => _host = host;
    public void Load() => _host.ShowMainMenu();
    public void Update(float deltaTime) { }
    public void Unload() { }
}

internal sealed class BrowserGameplayScene : IScene
{
    private readonly BrowserGameHost _host;
    public BrowserGameplayScene(BrowserGameHost host) => _host = host;
    public void Load() => _host.BeginGameplaySession();
    public void Update(float deltaTime) { }
    public void Unload() { }
}
