using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System.Drawing;
using System.Drawing.Imaging;

namespace SharpOpenGl;

class Program
{
    static void Main(string[] args)
    {
        bool screenshotMode = args.Contains("--screenshot");
        bool demoRecordingMode = args.Contains("--demo-recording");
        string screenshotPath = "screenshot.png";
        string demoMissionId = "example_scenario";
        string? demoOutputPath = null;

        for (int i = 0; i < args.Length - 1; i++)
        {
            if (args[i] == "--screenshot-path")
                screenshotPath = args[i + 1];
            if (args[i] == "--mission")
                demoMissionId = args[i + 1];
            if (args[i] is "--demo-output" or "--demo-output-path")
                demoOutputPath = args[i + 1];
        }

        bool headlessCapture = screenshotMode || demoRecordingMode;

        var nativeWindowSettings = new NativeWindowSettings
        {
            ClientSize = new Vector2i(1024, 768),
            Title = demoRecordingMode
                ? $"SharpOpenGL Demo — {demoMissionId}"
                : "SharpOpenGL - Space RTS",
            Flags = headlessCapture
                ? ContextFlags.Offscreen
                : ContextFlags.Default,
            WindowBorder = WindowBorder.Resizable,
        };

        var gameWindowSettings = new GameWindowSettings
        {
            UpdateFrequency = 60.0
        };

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
}
