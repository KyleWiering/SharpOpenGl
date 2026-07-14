using SharpOpenGl.Engine.Rendering;
using Xunit;

namespace SharpOpenGl.Tests.Rendering;

/// <summary>Regression guards for mesh-preview bucket envelope math (mirrors EngineWindow.MeshPreview.cs).</summary>
public class MeshPreviewFramingTests
{
    private const float Pullback = 2.8f;
    private const float ScaleFactor = 0.38f;

    private static (float CamDist, float Scale) ResolveShip(
        string hullId, float camIntercept, float camSlope, float scaleK, float scaleMin, float scaleMax)
    {
        RaceVisualSchema.Load();
        float size = Math.Max(RaceVisualSchema.ResolveHullProfile(hullId).Size, 0.85f);
        float camDist = (camIntercept + camSlope * size) * Pullback;
        float scale = Math.Clamp(scaleK / size, scaleMin, scaleMax) * ScaleFactor;
        return (camDist, scale);
    }

    [Fact]
    public void Proof_assets_use_expected_bucket_envelopes()
    {
        var fighter = ResolveShip("fighter_basic", 1.5f, 0.24f, 21f, 4.2f, 7.8f);
        var cruiser = ResolveShip("cruiser_heavy", 2.2f, 0.42f, 16.5f, 2.0f, 3.6f);
        var dread = ResolveShip("dreadnought", 2.2f, 0.42f, 16.5f, 2.0f, 3.6f);

        Assert.True(fighter.CamDist < cruiser.CamDist);
        Assert.True(cruiser.CamDist < dread.CamDist);
        Assert.True(fighter.Scale > cruiser.Scale);
        Assert.True(cruiser.Scale >= dread.Scale);
    }

    [Fact]
    public void Station_tall_command_center_uses_tall_envelope()
    {
        float size = 9f;
        float camDist = (2.6f + size * 0.26f) * Pullback;
        float scale = Math.Clamp(25f / size, 1.12f, 1.62f) * ScaleFactor;

        Assert.InRange(camDist, 10f, 20f);
        Assert.InRange(scale, 0.4f, 0.8f);
    }

    [Fact]
    public void Capital_bucket_pulls_back_further_than_strike_for_same_pullback()
    {
        var fighter = ResolveShip("fighter_basic", 1.5f, 0.24f, 21f, 4.2f, 7.8f);
        var dread = ResolveShip("dreadnought", 2.2f, 0.42f, 16.5f, 2.0f, 3.6f);

        Assert.True(dread.CamDist > fighter.CamDist);
        Assert.True(fighter.Scale > dread.Scale);
    }
}