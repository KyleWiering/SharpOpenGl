using SharpOpenGl.Engine.Diagnostics;
using Xunit;

namespace SharpOpenGl.Tests.Diagnostics;

public class PerformanceProfilerTests
{
    [Fact]
    public void Initial_fps_is_zero()
    {
        var p = new PerformanceProfiler();
        Assert.Equal(0f, p.AverageFps);
    }

    [Fact]
    public void SampleCount_starts_at_zero()
    {
        var p = new PerformanceProfiler(30);
        Assert.Equal(0, p.SampleCount);
    }

    [Fact]
    public void SampleCount_increments_after_frames()
    {
        var p = new PerformanceProfiler(10);
        for (int i = 0; i < 5; i++) { p.BeginFrame(); p.EndFrame(); }
        Assert.Equal(5, p.SampleCount);
    }

    [Fact]
    public void SampleCount_caps_at_window_size()
    {
        var p = new PerformanceProfiler(4);
        for (int i = 0; i < 20; i++) { p.BeginFrame(); p.EndFrame(); }
        Assert.Equal(4, p.SampleCount);
    }

    [Fact]
    public void Reset_clears_samples()
    {
        var p = new PerformanceProfiler(10);
        for (int i = 0; i < 5; i++) { p.BeginFrame(); p.EndFrame(); }
        p.Reset();
        Assert.Equal(0, p.SampleCount);
        Assert.Equal(0f, p.AverageFps);
    }

    [Fact]
    public void AverageFps_is_nonzero_after_frames()
    {
        var p = new PerformanceProfiler(10);
        for (int i = 0; i < 10; i++)
        {
            p.BeginFrame();
            System.Threading.Thread.Sleep(1); // ensure non-zero elapsed
            p.EndFrame();
        }
        Assert.True(p.AverageFps > 0f);
    }

    [Fact]
    public void PeakFrameMs_is_zero_before_any_frames()
    {
        var p = new PerformanceProfiler();
        Assert.Equal(0f, p.PeakFrameMs);
    }

    [Fact]
    public void PeakFrameMs_is_at_least_AverageFrameMs()
    {
        var p = new PerformanceProfiler(10);
        for (int i = 0; i < 10; i++) { p.BeginFrame(); p.EndFrame(); }
        Assert.True(p.PeakFrameMs >= p.AverageFrameMs);
    }

    [Fact]
    public void Constructor_throws_on_zero_sample_count()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new PerformanceProfiler(0));
    }
}
