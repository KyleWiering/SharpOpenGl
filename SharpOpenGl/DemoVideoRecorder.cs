using System.Diagnostics;
using OpenTK.Graphics.OpenGL4;
using StbImageWriteSharp;
using ColorComponents = StbImageWriteSharp.ColorComponents;
using PixelFormat = OpenTK.Graphics.OpenGL4.PixelFormat;

namespace SharpOpenGl;

/// <summary>Captures gameplay frames and encodes an MP4 via ffmpeg when available.</summary>
public sealed class DemoVideoRecorder : IDisposable
{
    private readonly string _outputMp4Path;
    private readonly string _posterPngPath;
    private readonly string _framesDir;
    private readonly int _captureInterval;
    private readonly int _maxFrames;
    private readonly List<string> _framePaths = new();
    private int _frameCounter;
    private bool _posterSaved;

    public DemoVideoRecorder(
        string outputMp4Path,
        int targetFps = 30,
        int updateFps = 60,
        int maxFrames = 0)
    {
        _outputMp4Path = outputMp4Path;
        _maxFrames = Math.Max(0, maxFrames);
        string baseName = Path.GetFileNameWithoutExtension(outputMp4Path);
        string outputDir = Path.GetDirectoryName(outputMp4Path) ?? ".";
        Directory.CreateDirectory(outputDir);
        _posterPngPath = Path.Combine(outputDir, $"{baseName}-poster.png");
        _framesDir = Path.Combine(Path.GetTempPath(), $"sharpopengl-demo-{baseName}-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_framesDir);
        _captureInterval = Math.Max(1, updateFps / Math.Max(1, targetFps));
    }

    public int CapturedFrameCount => _framePaths.Count;

    /// <summary>Read the current framebuffer and store a frame when the interval elapses.</summary>
    public void CaptureFrame(int width, int height, int renderFrameNumber)
    {
        if (_maxFrames > 0 && _framePaths.Count >= _maxFrames) return;
        if (renderFrameNumber % _captureInterval != 0) return;

        byte[] pixels = new byte[width * height * 4];
        GL.ReadPixels(0, 0, width, height, PixelFormat.Rgba, PixelType.UnsignedByte, pixels);

        byte[] flipped = new byte[width * height * 4];
        int stride = width * 4;
        for (int y = 0; y < height; y++)
            Array.Copy(pixels, (height - 1 - y) * stride, flipped, y * stride, stride);

        string framePath = Path.Combine(_framesDir, $"frame_{_frameCounter:D6}.png");
        SavePng(flipped, width, height, framePath);
        _framePaths.Add(framePath);
        _frameCounter++;

        if (!_posterSaved)
        {
            File.Copy(framePath, _posterPngPath, overwrite: true);
            _posterSaved = true;
        }
    }

    /// <summary>Encode captured frames to MP4 and write the poster PNG path.</summary>
    public DemoVideoResult Finalize(int targetFps = 30)
    {
        if (_framePaths.Count == 0)
        {
            return new DemoVideoResult(_outputMp4Path, _posterPngPath, Encoded: false,
                Message: "No frames captured.");
        }

        if (TryEncodeWithFfmpeg(targetFps))
        {
            return new DemoVideoResult(_outputMp4Path, _posterPngPath, Encoded: true,
                Message: $"Demo video saved ({_framePaths.Count} frames).");
        }

        string fallbackDir = Path.ChangeExtension(_outputMp4Path, null) + "_frames";
        Directory.CreateDirectory(fallbackDir);
        int index = 0;
        foreach (string frame in _framePaths)
        {
            string dest = Path.Combine(fallbackDir, $"frame_{index:D6}.png");
            File.Copy(frame, dest, overwrite: true);
            index++;
        }

        return new DemoVideoResult(_outputMp4Path, _posterPngPath, Encoded: false,
            Message: $"ffmpeg unavailable; wrote {_framePaths.Count} frames to {fallbackDir}");
    }

    private bool TryEncodeWithFfmpeg(int targetFps)
    {
        string inputPattern = Path.Combine(_framesDir, "frame_%06d.png");
        string? ffmpeg = ResolveFfmpegPath();
        if (ffmpeg == null) return false;

        var args = $"-y -framerate {targetFps} -i \"{inputPattern}\" " +
                   $"-c:v libx264 -pix_fmt yuv420p -crf 28 -preset ultrafast -movflags +faststart \"{_outputMp4Path}\"";

        try
        {
            using var process = Process.Start(new ProcessStartInfo
            {
                FileName = ffmpeg,
                Arguments = args,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            });

            if (process == null) return false;
            process.WaitForExit();
            return process.ExitCode == 0 && File.Exists(_outputMp4Path);
        }
        catch
        {
            return false;
        }
    }

    private static string? ResolveFfmpegPath()
    {
        string[] candidates = ["ffmpeg", "ffmpeg.exe"];
        foreach (string candidate in candidates)
        {
            try
            {
                using var process = Process.Start(new ProcessStartInfo
                {
                    FileName = candidate,
                    Arguments = "-version",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                });

                if (process == null) continue;
                process.WaitForExit(3000);
                if (process.ExitCode == 0)
                    return candidate;
            }
            catch
            {
                // Try next candidate.
            }
        }

        return null;
    }

    private static void SavePng(byte[] rgba, int width, int height, string path)
    {
        using var stream = File.OpenWrite(path);
        var writer = new ImageWriter();
        writer.WritePng(rgba, width, height, ColorComponents.RedGreenBlueAlpha, stream);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_framesDir))
                Directory.Delete(_framesDir, recursive: true);
        }
        catch
        {
            // Best-effort temp cleanup.
        }
    }
}

/// <summary>Result of <see cref="DemoVideoRecorder.Finalize"/>.</summary>
public readonly record struct DemoVideoResult(
    string VideoPath,
    string PosterPath,
    bool Encoded,
    string Message);