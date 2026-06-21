using OpenTK.Mathematics;
using SharpOpenGl.Engine.Missions;
using Xunit;

namespace SharpOpenGl.Tests.Missions;

public class StarMapLogicTests
{
    [Fact]
    public void IsPlanetHit_returns_true_inside_radius()
    {
        var center = new Vector2(100f, 200f);
        var inside = new Vector2(110f, 205f);

        Assert.True(StarMapLogic.IsPlanetHit(inside, center));
    }

    [Fact]
    public void IsPlanetHit_returns_false_outside_radius()
    {
        var center = new Vector2(100f, 200f);
        var outside = new Vector2(140f, 240f);

        Assert.False(StarMapLogic.IsPlanetHit(outside, center));
    }

    [Fact]
    public void IsMissionUnlocked_true_without_prerequisite()
    {
        var completed = new HashSet<string>();

        Assert.True(StarMapLogic.IsMissionUnlocked(null, completed));
        Assert.True(StarMapLogic.IsMissionUnlocked("", completed));
    }

    [Fact]
    public void IsMissionUnlocked_false_when_prerequisite_not_completed()
    {
        var completed = new HashSet<string> { "mission_a" };

        Assert.False(StarMapLogic.IsMissionUnlocked("mission_b", completed));
    }

    [Fact]
    public void IsMissionUnlocked_true_when_prerequisite_completed()
    {
        var completed = new HashSet<string> { "tutorial_01" };

        Assert.True(StarMapLogic.IsMissionUnlocked("tutorial_01", completed));
    }

    [Fact]
    public void HitTestPlanets_returns_unlocked_planet_under_point()
    {
        var nodes = new List<StarMapNode>
        {
            new("a", "Alpha", new Vector2(0.2f, 0.5f), Vector4.One, null, true, false),
            new("b", "Beta", new Vector2(0.8f, 0.5f), Vector4.One, "a", false, false),
        };

        var canvasPos = new Vector2(0f, 0f);
        var canvasSize = new Vector2(1000f, 1000f);
        var tap = new Vector2(200f, 500f);

        StarMapNode? hit = StarMapLogic.HitTestPlanets(tap, nodes, canvasPos, canvasSize);

        Assert.NotNull(hit);
        Assert.Equal("a", hit.Id);
    }

    [Fact]
    public void HitTestPlanets_ignores_locked_planets()
    {
        var nodes = new List<StarMapNode>
        {
            new("a", "Alpha", new Vector2(0.2f, 0.5f), Vector4.One, null, true, false),
            new("b", "Beta", new Vector2(0.8f, 0.5f), Vector4.One, "a", false, false),
        };

        var canvasPos = new Vector2(0f, 0f);
        var canvasSize = new Vector2(1000f, 1000f);
        var tap = new Vector2(800f, 500f);

        StarMapNode? hit = StarMapLogic.HitTestPlanets(tap, nodes, canvasPos, canvasSize);

        Assert.Null(hit);
    }

    [Fact]
    public void BuildHyperlanes_connects_unlocked_prerequisite_chain()
    {
        var nodes = new List<StarMapNode>
        {
            new("a", "Alpha", new Vector2(0.2f, 0.5f), Vector4.One, null, true, false),
            new("b", "Beta", new Vector2(0.5f, 0.5f), Vector4.One, "a", true, false),
            new("c", "Gamma", new Vector2(0.8f, 0.5f), Vector4.One, "b", false, false),
        };

        IReadOnlyList<StarMapHyperlane> lanes = StarMapLogic.BuildHyperlanes(nodes);

        Assert.Single(lanes);
        Assert.Equal(new Vector2(0.2f, 0.5f), lanes[0].From);
        Assert.Equal(new Vector2(0.5f, 0.5f), lanes[0].To);
    }

    [Fact]
    public void ParsePlanetColor_parses_hex_string()
    {
        Vector4 color = StarMapLogic.ParsePlanetColor("#4DA6FF", Vector4.One);

        Assert.Equal(0.302f, color.X, 2);
        Assert.Equal(0.651f, color.Y, 2);
        Assert.Equal(1f, color.Z, 2);
        Assert.Equal(1f, color.W, 2);
    }
}