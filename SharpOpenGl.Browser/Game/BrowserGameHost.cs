using OpenTK.Mathematics;
using SharpOpenGl.Browser.Assets;
using SharpOpenGl.Browser.Rendering;
using SharpOpenGl.Engine.Assets;
using SharpOpenGl.Engine.ECS;
using SharpOpenGl.Engine.Economy;
using SharpOpenGl.Engine.Entities;
using SharpOpenGl.Engine.Events;
using SharpOpenGl.Engine.Missions;
using SharpOpenGl.Engine.Rendering;
using SharpOpenGl.Engine.Scenes;
using SharpOpenGl.Engine.UI;
using SharpOpenGl.Engine.UI.Screens;
using SharpOpenGl.Engine.UI.Widgets;

namespace SharpOpenGl.Browser.Game;

/// <summary>
/// Browser game shell — same Engine UI, scenes, ECS, and missions as the desktop app.
/// </summary>
public sealed class BrowserGameHost : IDisposable
{
    private const string SceneMainMenu = "MainMenu";
    private const string SceneGameplay = "Gameplay";
    private const string GameDataRoot = "GameData";
    private const float MapWorldSize = 2000f;

    private readonly EventBus _eventBus = new();
    private readonly SceneManager _sceneManager;
    private readonly UIManager _uiManager;
    private readonly CanvasUIRenderer _uiRenderer;
    private readonly WebGlSceneRenderer _sceneRenderer;
    private readonly HttpAssetTextSource _assetSource;
    private readonly AssetManager _assetManager;
    private readonly MissionLoader _missionLoader;
    private readonly MissionController _missionController;

    private World? _world;
    private ResourceManager? _resourceManager;
    private MovementSystem? _movementSystem;
    private string? _pendingMissionId;
    private int _viewportWidth = 1024;
    private int _viewportHeight = 768;
    private bool _initialized;

    public BrowserGameHost(CanvasUIRenderer uiRenderer, WebGlSceneRenderer sceneRenderer, HttpAssetTextSource assetSource)
    {
        _uiRenderer = uiRenderer;
        _sceneRenderer = sceneRenderer;
        _assetSource = assetSource;
        _assetManager = new AssetManager(GameDataRoot, assetSource);
        _missionLoader = new MissionLoader(_assetManager);
        _missionController = new MissionController(_missionLoader, null, _eventBus);

        _sceneManager = new SceneManager(_eventBus);
        _uiManager = new UIManager(_eventBus);

        _sceneManager.Register(SceneMainMenu, () => new BrowserMainMenuScene(this));
        _sceneManager.Register(SceneGameplay, () => new BrowserGameplayScene(this));
    }

    public SceneManager SceneManager => _sceneManager;
    public UIManager UIManager => _uiManager;
    public ResourceManager? ResourceManager => _resourceManager;

    public async Task InitializeAsync(int width, int height)
    {
        _viewportWidth = width;
        _viewportHeight = height;

        await PreloadAssetsAsync();
        await _uiRenderer.InitializeAsync("ui-canvas", width, height);
        await _sceneRenderer.InitializeAsync("gl-canvas", width, height);
        _sceneRenderer.Camera.Height = 200f;
        _sceneRenderer.Camera.FocusPoint = Vector2.Zero;

        _uiManager.Resize(new Vector2(width, height));
        _sceneManager.TransitionTo(SceneMainMenu, GameState.MainMenu);
        _initialized = true;
    }

    public void Resize(int width, int height)
    {
        _viewportWidth = width;
        _viewportHeight = height;
        _uiManager.Resize(new Vector2(width, height));
        _uiRenderer.Resize(width, height);
        _sceneRenderer.Resize(width, height);
    }

    public void OnFrame(float deltaTime)
    {
        if (!_initialized) return;

        _sceneManager.Update(deltaTime);
                _world?.Update(deltaTime);

        if (_sceneManager.State == GameState.Playing && _world != null)
            _sceneRenderer.Render(_world, _viewportWidth, _viewportHeight);

        if (_uiManager.Current is GameplayHUD activeHud) BindResourceHud(activeHud);`r`n`r`n        _uiRenderer.Begin();
        _uiManager.Draw(_uiRenderer);
        _uiRenderer.End();
    }

    public bool HandlePointer(int x, int y, bool isDown)
    {
        if (!isDown) return false;
        return _uiManager.HandlePointerTapped(new Vector2(x, y), 0, new Vector2(_viewportWidth, _viewportHeight));
    }

    public void HandleKey(string key)
    {
        if (_sceneManager.State != GameState.Playing) return;

        float pan = 1f;
        switch (key.ToUpperInvariant())
        {
            case "W": case "ARROWUP": _sceneRenderer.Camera.Pan(0, -pan, 1f / 60f); break;
            case "S": case "ARROWDOWN": _sceneRenderer.Camera.Pan(0, pan, 1f / 60f); break;
            case "A": case "ARROWLEFT": _sceneRenderer.Camera.Pan(-pan, 0, 1f / 60f); break;
            case "D": case "ARROWRIGHT": _sceneRenderer.Camera.Pan(pan, 0, 1f / 60f); break;
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
        menu.SettingsRequested += () => { };
        menu.QuitRequested += () => { };
        _uiManager.Push(menu);
    }

    internal void ShowMissionSelect()
    {
        var missionSelect = new MissionSelectScreen();
        missionSelect.SetMissions(LoadMissionEntries());
        missionSelect.MissionStartRequested += ShowMissionBriefing;
        missionSelect.BackRequested += () => _uiManager.Pop();
        _uiManager.Push(missionSelect);
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
        _uiManager.Push(hud);
        BindResourceHud(hud);

        InitializeWorld(_pendingMissionId);
    }

    private void ShowPauseMenu()
    {
        var pause = new PauseScreen();
        pause.ResumeRequested += () => _uiManager.Pop();
        pause.QuitToMenuRequested += () =>
        {
            _world?.Dispose();
            _world = null;
            _uiManager.Clear();
            _sceneManager.TransitionTo(SceneMainMenu, GameState.MainMenu);
        };
        _uiManager.Push(pause);
    }

    private void BindResourceHud(GameplayHUD hud)
    {
        if (_resourceManager == null) return;
        hud.ResourceBar.Resources = new List<ResourceDisplay> { _resourceManager!.GetDisplay(1, ResourceType.Energy), _resourceManager.GetDisplay(1, ResourceType.Minerals), _resourceManager.GetDisplay(1, ResourceType.Data), _resourceManager.GetDisplay(1, ResourceType.Crew) };
    }

    private void InitializeWorld(string? missionId)
    {
        _world?.Dispose();

        _world = new World();
        _movementSystem = new MovementSystem();
        _world.AddSystem(new StanceSystem());
        _world.AddSystem(new CombatSystem(_eventBus));
        _world.AddSystem(new ProjectileSystem(_eventBus));
        _world.AddSystem(_movementSystem);

        _resourceManager = new ResourceManager(_eventBus);
        var playerRes = _resourceManager.AddPlayer(1);
        playerRes.SetStartingAmount(ResourceType.Energy, 500f);
        playerRes.SetStartingAmount(ResourceType.Minerals, 300f);
        playerRes.SetStartingAmount(ResourceType.Data, 100f);
        playerRes.SetStartingAmount(ResourceType.Crew, 50f);
        playerRes.AddIncome(ResourceType.Energy, 5f);
        playerRes.AddIncome(ResourceType.Minerals, 3f);
        _world.AddSystem(new ResourceSystem(_resourceManager));
        if (!string.IsNullOrEmpty(missionId) && _missionController.CurrentMission != null)
            SpawnMissionFleet(missionId);
        else
            SpawnSandboxFleet();

        _missionController.BeginGameplay();
    }

    private void SpawnSandboxFleet()
    {
        if (_world == null) return;

        var hero = _world.CreateEntity();
        _world.AddComponent(hero, new TransformComponent { Position = Vector3.Zero, Scale = Vector3.One });
        _world.AddComponent(hero, new MovementComponent { Speed = 80f, Acceleration = 120f, TurnRate = 180f });
        _world.AddComponent(hero, new SelectionComponent { IsSelected = true, SelectionRadius = 8f });
        _world.AddComponent(hero, new RenderComponent
        {
            Color = new Vector4(0.2f, 0.8f, 1f, 1f), Visible = true, VertexCount = 3,
        });
        _world.AddComponent(hero, new HeroComponent());

        var rng = new Random(42);
        for (int i = 0; i < 5; i++)
        {
            var fighter = _world.CreateEntity();
            float x = (rng.NextSingle() - 0.5f) * 400f;
            float z = (rng.NextSingle() - 0.5f) * 400f;
            _world.AddComponent(fighter, new TransformComponent { Position = new Vector3(x, 0, z), Scale = Vector3.One });
            _world.AddComponent(fighter, new MovementComponent { Speed = 150f, Acceleration = 220f, TurnRate = 220f });
            _world.AddComponent(fighter, new SelectionComponent { SelectionRadius = 6f });
            _world.AddComponent(fighter, new RenderComponent
            {
                Color = new Vector4(0.4f, 1f, 0.4f, 1f), Visible = true, VertexCount = 3,
            });
        }
    }

    private void SpawnMissionFleet(string missionId)
    {
        if (_world == null || _missionController.CurrentMission == null) return;

        var start = _missionController.CurrentMission.Definition.StartConditions;
        Vector3 spawn = new((start?.PlayerSpawn?[0] ?? 3) * 10f, 0f, (start?.PlayerSpawn?[1] ?? 3) * 10f);
        _sceneRenderer.Camera.LookAt(new Vector2(spawn.X, spawn.Z));

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
            var entity = _world.CreateEntity();
            _world.AddComponent(entity, new TransformComponent { Position = pos, Scale = Vector3.One });
            _world.AddComponent(entity, new MovementComponent { Speed = 80f, Acceleration = 120f, TurnRate = 180f });
            _world.AddComponent(entity, new SelectionComponent { IsSelected = i == 0, SelectionRadius = 8f });
            _world.AddComponent(entity, new RenderComponent
            {
                Color = new Vector4(0.2f, 0.8f, 1f, 1f), Visible = true, VertexCount = 3,
            });
            if (units[i].Contains("hero", StringComparison.OrdinalIgnoreCase))
                _world.AddComponent(entity, new HeroComponent());
        }
    }

    private IEnumerable<MissionEntry> LoadMissionEntries()
    {
        var known = new[]
        {
            ("tutorial_01", "Tutorial - First Steps"),
            ("example_scenario", "First Contact"),
            ("mission_02", "Resource Rush"),
            ("mission_03", "Defensive Stand"),
            ("mission_04", "Deep Strike"),
            ("mission_05", "Final Assault"),
        };

        var entries = new List<MissionEntry>();
        foreach (var (id, title) in known)
        {
            if (_assetManager.Exists($"Missions/{id}"))
            {
                entries.Add(new MissionEntry
                {
                    Id = id,
                    Title = title,
                    Description = $"Mission: {title}",
                });
            }
        }

        if (entries.Count == 0)
        {
            foreach (var (id, title) in known)
            {
                entries.Add(new MissionEntry { Id = id, Title = title, Description = $"Mission: {title}" });
            }
        }

        return entries;
    }

    private async Task PreloadAssetsAsync()
    {
        string[] keys =
        [
            "Missions/tutorial_01", "Missions/example_scenario", "Missions/mission_02",
            "Missions/mission_03", "Missions/mission_04", "Missions/mission_05",
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