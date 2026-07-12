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

        bool screenshotMode = args.Contains("--screenshot");
        bool demoRecordingMode = args.Contains("--demo-recording");
        bool meshPreviewMode = args.Contains("--mesh-preview");
        string screenshotPath = "screenshot.png";
        string demoMissionId = "example_scenario";
        string? demoOutputPath = null;
        string meshRace = "vesper";
        string meshHull = "fighter_basic";

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
            if (args[i] == "--hull")
                meshHull = args[i + 1];
        }

        bool headlessCapture = screenshotMode || demoRecordingMode || meshPreviewMode;

        MeshPreviewLaunchOptions.Enabled = meshPreviewMode;
        MeshPreviewLaunchOptions.Race = meshRace;
        MeshPreviewLaunchOptions.Hull = meshHull;

        var nativeWindowSettings = new NativeWindowSettings
        {
            ClientSize = new Vector2i(1024, 768),
            Title = meshPreviewMode
                ? $"SharpOpenGL Mesh Preview — {meshRace}/{meshHull}"
                : demoRecordingMode
                    ? $"SharpOpenGL Demo — {demoMissionId}"
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
                demoOutputPath);
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
        string hull = "fighter_basic";
        string? screenshot = null;
        string? output = null;

        for (int i = 0; i < args.Length - 1; i++)
        {
            if (args[i] == "--race") race = args[i + 1];
            if (args[i] == "--hull") hull = args[i + 1];
            if (args[i] == "--screenshot-path") screenshot = args[i + 1];
            if (args[i] == "--output") output = args[i + 1];
        }

        string gameData = ResolveGameDataRoot();
        var report = ModelQualityScorer.Score(race, hull, gameData, screenshot);
        string json = report.ToJson();
        Console.WriteLine(json);
        if (!string.IsNullOrWhiteSpace(output))
            ModelQualityScorer.WriteReport(output, report);
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
}