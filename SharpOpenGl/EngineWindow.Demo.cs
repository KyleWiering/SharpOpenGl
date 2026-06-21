using SharpOpenGl.Engine.ECS;
using SharpOpenGl.Engine.Entities;
using SharpOpenGl.Engine.Missions;
using SharpOpenGl.Engine.Multiplayer;
using SharpOpenGl.Engine.Scenes;

namespace SharpOpenGl;

public partial class EngineWindow
{
    private MissionPlaythroughAgent? _playthroughAgent;
    private DemoVideoRecorder? _demoRecorder;
    private float _demoElapsed;
    private bool _demoFinalizePending;
    private const float DemoMaxDurationSeconds = 180f;
    private const float DemoVictoryHoldSeconds = 2f;
    private float _demoVictoryHold;

    public const string GameplayDemoFileName = "gameplay-demo.mp4";
    public const string GameplayDemoPosterFileName = "gameplay-demo-poster.png";

    private static string ResolveRepoRoot()
    {
        string? dir = AppDomain.CurrentDomain.BaseDirectory;
        while (dir != null && !File.Exists(Path.Combine(dir, "SharpOpenGl.sln")))
            dir = Directory.GetParent(dir)?.FullName;

        return dir ?? Directory.GetCurrentDirectory();
    }

    private static string ResolveDemoVideoPath(string? overridePath = null)
    {
        if (!string.IsNullOrWhiteSpace(overridePath))
            return Path.GetFullPath(overridePath);

        return Path.Combine(ResolveRepoRoot(), "docs", GameplayDemoFileName);
    }

    private void InitializeDemoRecording()
    {
        EnsureAssets();
        var definition = _missionLoader!.Load(_demoMissionId);
        if (definition == null)
        {
            Console.WriteLine($"[Demo] Mission '{_demoMissionId}' not found.");
            Close();
            return;
        }

        if (definition.DemoScript.Length < 8)
            Console.WriteLine($"[Demo] Warning: mission '{_demoMissionId}' has fewer than 8 demoScript steps.");

        _missionController!.StartMission(_demoMissionId);
        _pendingMissionId = _demoMissionId;
        _demoRecorder = new DemoVideoRecorder(_demoVideoPath);
        Console.WriteLine($"[Demo] Recording mission '{_demoMissionId}' → {_demoVideoPath}");
    }

    private void EnsurePlaythroughAgent()
    {
        if (_playthroughAgent != null || _world == null) return;

        var state = _missionController?.CurrentMission;
        if (state == null) return;

        var context = new GameCommandContext
        {
            World = _world,
            PlayerId = _humanPlayerId,
            MissionState = state,
            DefinitionLoader = id =>
            {
                if (_definitions.TryGetValue(id, out var def)) return def;
                return _assetManager?.Load<EntityDefinition>($"Ships/{id}")
                       ?? _assetManager?.Load<EntityDefinition>($"Units/{id}")
                       ?? _assetManager?.Load<EntityDefinition>($"Bases/{id}");
            },
            Resources = _resourceManager,
            Supply = _supplySystem,
            AssignSquadMove = _squadSystem == null
                ? null
                : (world, entities, target, append) =>
                    _squadSystem.AssignMoveRoutes(world, entities.ToList(), target, append),
            PlaceBuilding = (buildingId, worldPos) => TryPlaceBuildingAt(buildingId, worldPos),
        };

        _playthroughAgent = new MissionPlaythroughAgent(
            state.Definition,
            context,
            (target, height) =>
            {
                _rtsCamera.Target = target;
                if (height.HasValue)
                    _rtsCamera.Height = Math.Clamp(height.Value, _rtsCamera.MinHeight, _rtsCamera.MaxHeight);
            });
    }

    private void UpdateDemoRecording(float dt)
    {
        if (!_demoRecordingMode || _sceneManager.State != GameState.Playing) return;

        EnsurePlaythroughAgent();
        _playthroughAgent?.Tick(dt);
        _demoElapsed += dt;

        if (_playthroughAgent == null) return;

        if (_playthroughAgent.MissionObjectivesComplete)
            _demoVictoryHold += dt;

        bool timedOut = _demoElapsed >= DemoMaxDurationSeconds;
        bool scriptDone = _playthroughAgent.ScriptFinished;
        bool victoryReady = _playthroughAgent.MissionObjectivesComplete &&
                            _demoVictoryHold >= DemoVictoryHoldSeconds;

        if (!_demoFinalizePending && (victoryReady || (scriptDone && _demoElapsed > 5f) || timedOut))
            _demoFinalizePending = true;
    }

    private void FinalizeDemoRecording()
    {
        if (_demoRecorder == null) return;

        var result = _demoRecorder.Finalize();
        Console.WriteLine($"[Demo] {result.Message}");
        Console.WriteLine($"[Demo] Poster: {result.PosterPath}");
        if (result.Encoded)
            Console.WriteLine($"[Demo] Video: {result.VideoPath}");

        _demoRecorder.Dispose();
        _demoRecorder = null;
    }
}