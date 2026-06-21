using OpenTK.Mathematics;
using SharpOpenGl.Browser.Assets;
using SharpOpenGl.Browser.Rendering;
using SharpOpenGl.Engine.Assets;
using SharpOpenGl.Engine.ECS;
using SharpOpenGl.Engine.Economy;
using SharpOpenGl.Engine.Events;
using SharpOpenGl.Engine.Missions;
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

    private World? _world;
    private ResourceManager? _resourceManager;
    private MovementSystem? _movementSystem;
    private string? _pendingMissionId;
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
        _sceneManager.TransitionTo(SceneMainMenu, GameState.MainMenu);
        _initialized = true;
    }

    public void Resize(int width, int height)
    {
        _viewportWidth = width;
        _viewportHeight = height;
        _uiManager.Resize(new Vector2(width, height));
        _uiRenderer.Resize(width, height);
        _glRenderer.Resize(width, height);
    }

    public void OnFrame(float deltaTime)
    {
        if (!_initialized) return;

        _sceneManager.Update(deltaTime);
        _world?.Update(deltaTime);

        if (_sceneManager.State == GameState.Playing && _world != null)
        {
            _explosionVfx.Update(deltaTime);
            _gameplayRenderer.Render(_world, _viewportWidth, _viewportHeight, _explosionVfx);
        }

        if (_uiManager.Current is GameplayHUD activeHud)
            BindResourceHud(activeHud);

        _uiRenderer.Begin();
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
        var cam = _gameplayRenderer.Camera;
        switch (key.ToUpperInvariant())
        {
            case "W": case "ARROWUP": cam.Pan(0, -pan, 1f / 60f); break;
            case "S": case "ARROWDOWN": cam.Pan(0, pan, 1f / 60f); break;
            case "A": case "ARROWLEFT": cam.Pan(-pan, 0, 1f / 60f); break;
            case "D": case "ARROWRIGHT": cam.Pan(pan, 0, 1f / 60f); break;
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

        InitializeWorld(_pendingMissionId);
        BindResourceHud(hud);
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
        hud.ResourceBar.Resources =
        [
            _resourceManager.GetDisplay(1, ResourceType.Energy),
            _resourceManager.GetDisplay(1, ResourceType.Minerals),
            _resourceManager.GetDisplay(1, ResourceType.Data),
            _resourceManager.GetDisplay(1, ResourceType.Crew),
        ];
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
        _world.AddSystem(new MiningVisualSystem());

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
        };

        var entries = new List<MissionEntry>();
        foreach (string id in known)
        {
            var definition = _missionLoader.Load(id);
            if (definition != null)
                entries.Add(ToMissionEntry(definition));
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

    private static MissionEntry ToMissionEntry(MissionDefinition definition) =>
        MissionEntryMapper.FromDefinition(definition);

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
