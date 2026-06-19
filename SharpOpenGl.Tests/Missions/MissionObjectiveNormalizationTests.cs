using SharpOpenGl.Engine.Missions;
using Xunit;

namespace SharpOpenGl.Tests.Missions;

public class MissionObjectiveNormalizationTests
{
    [Fact]
    public void Reach_area_normalizes_position_and_radius()
    {
        var def = new ObjectiveDefinition
        {
            Type = "reach_area",
            Position = [32f, 32f],
            Radius = 5f,
        };

        MissionState.NormalizeObjective(def);

        Assert.Equal("32,32,5", def.Condition);
    }

    [Fact]
    public void Reach_area_normalizes_area_object()
    {
        var def = new ObjectiveDefinition
        {
            Type = "reach_area",
            Area = new AreaDefinition { X = 50f, Y = 50f, Radius = 8f },
        };

        MissionState.NormalizeObjective(def);

        Assert.Equal("50,50,8", def.Condition);
    }
}