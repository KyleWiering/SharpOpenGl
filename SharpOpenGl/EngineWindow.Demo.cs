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
    private const float DemoMaxDurationSeconds = 45f;
    private const float DemoScriptDoneHoldSeconds = 1.5f;
    private const float DemoVictoryHoldSeconds = 2f;
    private const int DemoCaptureTargetFps = 20;
    /// <summary>Speeds simulation during recording so CI finishes near the advertised ~45s runtime.</summary>
    internal const float DemoSimulationTimeScale = 4f;
    private float _demoVictoryHold;
    private float _demoScriptDoneHold;

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

    private void InitializeScreenshotCapture()
    {
        EnsureAssets();
        var definition = _missionLoader!.Load(_demoMissionId);
        if (definition == null)
        {
            Console.WriteLine($"[Screenshot] Mission '{_demoMissionId}' not found — using sandbox.");
            return;
        }

        _missionController!.StartMission(_demoMissionId);
        _pendingMissionId = _demoMissionId;
        Console.WriteLine($"[Screenshot] Capturing mission '{_demoMissionId}'.");
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
        _demoRecorder = new DemoVideoRecorder(_demoVideoPath, targetFps: DemoCaptureTargetFps, maxFrames: 1000);
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
            BuildMapCatalog = _buildMapCatalog,
            Grid = _gridSystem,
            Fog = _fogOfWar,
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

    private void UpdateDemoRecording(float simDt, float wallDt)
    {
        if (!_demoRecordingMode || _sceneManager.State != GameState.Playing) return;

        EnsurePlaythroughAgent();
        _playthroughAgent?.Tick(simDt);
        _demoElapsed += wallDt;

        if (_playthroughAgent == null) return;

        if (_playthroughAgent.MissionObjectivesComplete)
            _demoVictoryHold += simDt;

        if (_playthroughAgent.ScriptFinished)
            _demoScriptDoneHold += wallDt;

        bool timedOut = _demoElapsed >= DemoMaxDurationSeconds;
        bool scriptDone = _playthroughAgent.ScriptFinished &&
                          _demoScriptDoneHold >= DemoScriptDoneHoldSeconds;
        bool victoryReady = _playthroughAgent.MissionObjectivesComplete &&
                            _demoVictoryHold >= DemoVictoryHoldSeconds;

        if (!_demoFinalizePending && (victoryReady || scriptDone || timedOut))
        {
            _demoFinalizePending = true;
            Console.WriteLine(
                $"[Demo] Finalizing after {_demoElapsed:F1}s " +
                $"(scriptDone={_playthroughAgent.ScriptFinished}, step={_playthroughAgent.StepIndex}/{_playthroughAgent.StepCount}, timedOut={timedOut})");
        }
    }

    private void FinalizeDemoRecording()
    {
        if (_demoRecorder == null) return;

        var result = _demoRecorder.Finalize(DemoCaptureTargetFps);
        Console.WriteLine($"[Demo] {result.Message}");
        Console.WriteLine($"[Demo] Poster: {result.PosterPath}");
        if (result.Encoded)
            Console.WriteLine($"[Demo] Video: {result.VideoPath}");

        _demoRecorder.Dispose();
        _demoRecorder = null;
    }
}