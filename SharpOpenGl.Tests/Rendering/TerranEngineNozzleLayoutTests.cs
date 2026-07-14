using OpenTK.Mathematics;
using SharpOpenGl.Engine.Rendering;
using Xunit;

namespace SharpOpenGl.Tests.Rendering;

public class TerranEngineNozzleLayoutTests
{
    public TerranEngineNozzleLayoutTests() => RaceVisualSchema.Load();

    [Fact]
    public void ComputeLocalOffsets_fighter_basic_returns_two_nozzles()
    {
        var (len, wid, hgt) = TerranEngineNozzleLayout.ResolveHullDimensions("fighter_basic");
        IReadOnlyList<Vector3> offsets = TerranEngineNozzleLayout.ComputeLocalOffsets(
            "fighter_basic", len, wid, hgt, engineCount: 2);

        Assert.Equal(2, offsets.Count);
        Assert.True(offsets[0].X < 0f);
        Assert.True(offsets[1].X > 0f);
        Assert.Equal(offsets[0].X, -offsets[1].X, precision: 4);
        Assert.Equal(offsets[0].Y, offsets[1].Y, precision: 4);
        Assert.Equal(offsets[0].Z, offsets[1].Z, precision: 4);
        Assert.All(offsets, o => Assert.True(o.Z < 0f, $"Expected stern Z < 0, got {o.Z}"));
    }

    [Fact]
    public void ComputeLocalOffsets_dreadnought_offsets_scale_with_hull_size()
    {
        var fighterDims = TerranEngineNozzleLayout.ResolveHullDimensions("fighter_basic");
        var dreadDims = TerranEngineNozzleLayout.ResolveHullDimensions("dreadnought");

        IReadOnlyList<Vector3> fighterOffsets = TerranEngineNozzleLayout.ComputeLocalOffsets(
            "fighter_basic", fighterDims.Len, fighterDims.Wid, fighterDims.Hgt);
        IReadOnlyList<Vector3> dreadOffsets = TerranEngineNozzleLayout.ComputeLocalOffsets(
            "dreadnought", dreadDims.Len, dreadDims.Wid, dreadDims.Hgt);

        float fighterXSpread = MathF.Abs(fighterOffsets[0].X);
        float dreadXSpread = MathF.Abs(dreadOffsets[0].X);
        float fighterZ = MathF.Abs(fighterOffsets[0].Z);
        float dreadZ = MathF.Abs(dreadOffsets[0].Z);

        Assert.True(dreadXSpread > fighterXSpread,
            $"Dreadnought X spread {dreadXSpread} should exceed fighter {fighterXSpread}");
        Assert.True(dreadZ > fighterZ,
            $"Dreadnought |Z| {dreadZ} should exceed fighter {fighterZ}");
    }

    [Fact]
    public void ComputeLocalOffsets_three_engines_includes_center_nozzle()
    {
        var (len, wid, hgt) = TerranEngineNozzleLayout.ResolveHullDimensions("fighter_basic");
        IReadOnlyList<Vector3> offsets = TerranEngineNozzleLayout.ComputeLocalOffsets(
            "fighter_basic", len, wid, hgt, engineCount: 3);

        Assert.Equal(3, offsets.Count);
        Assert.Contains(offsets, o => MathF.Abs(o.X) < 0.05f);
    }
}