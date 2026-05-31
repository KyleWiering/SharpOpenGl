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
        string screenshotPath = "screenshot.png";

        // Check for custom screenshot path
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (args[i] == "--screenshot-path")
            {
                screenshotPath = args[i + 1];
            }
        }

        var nativeWindowSettings = new NativeWindowSettings
        {
            ClientSize = new Vector2i(1024, 768),
            Title = "SharpOpenGL - Space RTS",
            Flags = screenshotMode
                ? ContextFlags.Offscreen
                : ContextFlags.Default,
            WindowBorder = WindowBorder.Resizable,
        };

        var gameWindowSettings = new GameWindowSettings
        {
            UpdateFrequency = 60.0
        };

        using var game = new EngineWindow(gameWindowSettings, nativeWindowSettings, screenshotMode, screenshotPath);
        game.Run();
    }
}
