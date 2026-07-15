using System.Text.Json;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using SharpOpenGl.Engine.Rendering;

namespace SharpOpenGl;

class Program
{
    static void Main(string[] args)
    {
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            string message = e.ExceptionObject?.ToString() ?? "Unknown error";
            Console.Error.WriteLine(message);
            try { File.WriteAllText("sharpopengl-crash.log", message); } catch { }
        };

        if (args.Contains("--score-mesh"))
        {
            RunMeshScore(args);
            return;
        }

        if (args.Contains("--score-race"))
        {
            RunRaceScore(args);
            return;
        }

        if (args.Contains("--score-all-races"))
        {
            RunAllRacesScore(args);
            return;
        }

        bool sandboxMode = args.Contains("--sandbox");
        bool screenshotMode = args.Contains("--screenshot");
        bool demoRecordingMode = args.Contains("--demo-recording");
        bool meshPreviewMode = args.Contains("--mesh-preview");
        string screenshotPath = "screenshot.png";
        string demoMissionId = "example_scenario";
        string? demoOutputPath = null;
        string meshRace = "vesper";
        string meshHull = "fighter_basic";
        string meshCategory = ModelMeshSource.KindShip;

        for (int i = 0; i < args.Length - 1; i++)
        {
            if (args[i] == "--screenshot-path")
                screenshotPath = args[i + 1];
            if (args[i] == "--mission")
                demoMissionId = args[i + 1];
            if (args[i] is "--demo-output" or "--demo-output-path")
                demoOutputPath = args[i + 1];
            if (args[i] == "--race")
                meshRace = args[i + 1];
            if (args[i] is "--hull" or "--model")
                meshHull = args[i + 1];
            if (args[i] is "--category" or "--asset-kind")
                meshCategory = args[i + 1];
            if (args[i] == "--sandbox-seed")
                SandboxLaunchOptions.SeedText = args[i + 1];
        }

        SandboxLaunchOptions.Enabled = sandboxMode;
        bool headlessCapture = screenshotMode || demoRecordingMode || meshPreviewMode || sandboxMode;

        MeshPreviewLaunchOptions.Enabled = meshPreviewMode;
        MeshPreviewLaunchOptions.Race = meshRace;
        MeshPreviewLaunchOptions.Hull = meshHull;
        MeshPreviewLaunchOptions.Category = meshCategory;

        ScreenshotLaunchOptions.GalleryHull = ResolveGalleryScreenshotHull(args, screenshotMode, screenshotPath, meshHull);
        ScreenshotLaunchOptions.GalleryRace = ResolveGalleryScreenshotRace(args, screenshotMode, screenshotPath, meshRace);
        ScreenshotLaunchOptions.MediumCombatRow = ResolveMediumCombatGalleryScreenshot(screenshotMode, screenshotPath);
        ScreenshotLaunchOptions.Round2ArticulationGallery = screenshotMode
            && screenshotPath.Contains("unit-base-articulation-round2", StringComparison.OrdinalIgnoreCase);
        ScreenshotLaunchOptions.ShipyardPreviewMidBuild = screenshotMode
            && screenshotPath.Contains("shipyard-preview-mid-build", StringComparison.OrdinalIgnoreCase);

        var nativeWindowSettings = new NativeWindowSettings
        {
            ClientSize = new Vector2i(1024, 768),
            Title = meshPreviewMode
                ? $"SharpOpenGL Mesh Preview — {meshCategory} {meshRace}/{meshHull}"
                : demoRecordingMode
                    ? $"SharpOpenGL Demo — {demoMissionId}"
                    : sandboxMode
                        ? "SharpOpenGL Sandbox"
                        : "SharpOpenGL - Space RTS",
            APIVersion = new Version(3, 3),
            Profile = ContextProfile.Core,
            Flags = headlessCapture
                ? ContextFlags.Offscreen
                : ContextFlags.Default,
            WindowBorder = WindowBorder.Resizable,
            StartVisible = true,
            StartFocused = true,
        };

        var gameWindowSettings = new GameWindowSettings
        {
            UpdateFrequency = 60.0
        };

        try
        {
            using var game = new EngineWindow(
                gameWindowSettings,
                nativeWindowSettings,
                screenshotMode,
                screenshotPath,
                demoRecordingMode,
                demoMissionId,
                demoVideoPath: demoOutputPath);
            game.Run();
        }
        catch (Exception ex)
        {
            string message = ex.ToString();
            Console.Error.WriteLine(message);
            try { File.WriteAllText("sharpopengl-crash.log", message); } catch { }
            System.Environment.ExitCode = 1;
        }
    }

    private static void RunMeshScore(string[] args)
    {
        string race = "vesper";
        string model = "fighter_basic";
        string category = ModelMeshSource.KindShip;
        string? screenshot = null;
        string? output = null;

        for (int i = 0; i < args.Length - 1; i++)
        {
            if (args[i] == "--race") race = args[i + 1];
            if (args[i] is "--hull" or "--model") model = args[i + 1];
            if (args[i] is "--category" or "--asset-kind") category = args[i + 1];
            if (args[i] == "--screenshot-path") screenshot = args[i + 1];
            if (args[i] == "--output") output = args[i + 1];
        }

        string gameData = ResolveGameDataRoot();
        string kind = ModelMeshSource.NormalizeKind(category);
        string? raceArg = kind == ModelMeshSource.KindObject ? null : race;
        var report = ModelQualityScorer.ScoreAsset(kind, model, gameData, raceArg, screenshot);
        string json = report.ToJson();
        Console.WriteLine(json);
        if (!string.IsNullOrWhiteSpace(output))
            ModelQualityScorer.WriteReport(output, report);
    }

    private static void RunRaceScore(string[] args)
    {
        string race = "vesper";
        string? output = null;
        bool fromScoreFiles = args.Contains("--from-score-files");
        string scoreFileName = "loop-01.json";

        for (int i = 0; i < args.Length - 1; i++)
        {
            if (args[i] == "--race") race = args[i + 1];
            if (args[i] == "--output") output = args[i + 1];
            if (args[i] == "--score-file") scoreFileName = args[i + 1];
        }

        string repoRoot = ResolveRepoRoot();
        var report = fromScoreFiles
            ? ModelQualityScorer.AggregateRaceFromScoreDirectory(race, repoRoot, scoreFileName)
            : ModelQualityScorer.ScoreRace(race, ResolveGameDataRoot());
        string json = report.ToJson();
        Console.WriteLine(json);
        if (!string.IsNullOrWhiteSpace(output))
            ModelQualityScorer.WriteRaceReport(output, report);
    }

    private static void RunAllRacesScore(string[] args)
    {
        string? output = null;
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (args[i] == "--output") output = args[i + 1];
        }

        string gameData = ResolveGameDataRoot();
        var reports = ModelQualityScorer.ScoreAllRaces(gameData);
        string json = JsonSerializer.Serialize(reports, ModelQualityScorer.JsonOptions);
        Console.WriteLine(json);
        if (!string.IsNullOrWhiteSpace(output))
        {
            string? dir = Path.GetDirectoryName(output);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);
            File.WriteAllText(output, json);
        }
    }

    private static string? ResolveGalleryScreenshotHull(string[] args, bool screenshotMode, string screenshotPath, string cliHull)
    {
        if (!screenshotMode) return null;

        if (args.Contains("--hull") || args.Contains("--model"))
            return cliHull;

        if (TryParseFleetGalleryScreenshotFileName(screenshotPath, out _, out string? hullFromFile))
            return hullFromFile;

        string fileName = Path.GetFileNameWithoutExtension(screenshotPath);
        const string prefix = "vesper-";
        const string suffix = "-gameplay-iter";
        if (!fileName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            return null;

        int suffixIndex = fileName.IndexOf(suffix, StringComparison.OrdinalIgnoreCase);
        if (suffixIndex <= prefix.Length)
            return null;

        return fileName[prefix.Length..suffixIndex];
    }

    private static string? ResolveGalleryScreenshotRace(string[] args, bool screenshotMode, string screenshotPath, string cliRace)
    {
        if (!screenshotMode) return null;

        if (args.Contains("--race"))
            return cliRace;

        if (TryParseFleetGalleryScreenshotFileName(screenshotPath, out string? raceFromFile, out _))
            return raceFromFile;

        string fileName = Path.GetFileNameWithoutExtension(screenshotPath);
        if (fileName.StartsWith("vesper-", StringComparison.OrdinalIgnoreCase)
            && fileName.Contains("-gameplay-iter", StringComparison.OrdinalIgnoreCase))
            return "vesper";

        return "vesper";
    }

    /// <summary>Parses <c>{race}-{hull}-fleet-gallery-iter*</c> screenshot filenames.</summary>
    private static bool TryParseFleetGalleryScreenshotFileName(string screenshotPath, out string? raceId, out string? hullId)
    {
        raceId = null;
        hullId = null;
        string fileName = Path.GetFileNameWithoutExtension(screenshotPath);
        const string suffix = "-fleet-gallery-iter";
        int suffixIndex = fileName.IndexOf(suffix, StringComparison.OrdinalIgnoreCase);
        if (suffixIndex <= 0)
            return false;

        string prefix = fileName[..suffixIndex];
        int dashIndex = prefix.IndexOf('-');
        if (dashIndex <= 0 || dashIndex >= prefix.Length - 1)
            return false;

        raceId = prefix[..dashIndex];
        hullId = prefix[(dashIndex + 1)..];
        return true;
    }

    private static bool ResolveMediumCombatGalleryScreenshot(bool screenshotMode, string screenshotPath)
    {
        if (!screenshotMode) return false;
        string fileName = Path.GetFileNameWithoutExtension(screenshotPath);
        return fileName.Contains("medium-combat", StringComparison.OrdinalIgnoreCase);
    }

    private static string ResolveGameDataRoot()
    {
        string? dir = AppDomain.CurrentDomain.BaseDirectory;
        while (dir != null)
        {
            string candidate = Path.Combine(dir, "GameData");
            if (Directory.Exists(candidate))
                return candidate;
            dir = Directory.GetParent(dir)?.FullName;
        }

        return Path.Combine(Directory.GetCurrentDirectory(), "GameData");
    }

    private static string ResolveRepoRoot()
    {
        string? dir = AppDomain.CurrentDomain.BaseDirectory;
        while (dir != null)
        {
            if (Directory.Exists(Path.Combine(dir, "GameData"))
                && File.Exists(Path.Combine(dir, "SharpOpenGl.sln")))
                return dir;
            dir = Directory.GetParent(dir)?.FullName;
        }

        return Directory.GetCurrentDirectory();
    }
}