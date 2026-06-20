using OpenTK.Mathematics;
using SharpOpenGl.Engine.ECS;
using SharpOpenGl.Engine.UI;
using Xunit;

namespace SharpOpenGl.Tests.UI;

public class GameplayEntityDisplayTests
{
    [Fact]
    public void Classify_marks_resource_nodes_harvestable()
    {
        using var world = new World();
        var entity = world.CreateEntity();
        world.AddComponent(entity, new ResourceNodeComponent());

        Assert.Equal(EntityDisplayKind.Harvestable, GameplayEntityDisplay.Classify(world, entity));
        Assert.Equal(GameplayEntityDisplay.HarvestableColor, GameplayEntityDisplay.LabelColor(EntityDisplayKind.Harvestable));
    }

    [Fact]
    public void Classify_marks_ai_as_hostile()
    {
        using var world = new World();
        var entity = world.CreateEntity();
        world.AddComponent(entity, new AIControlledComponent());

        Assert.Equal(EntityDisplayKind.Hostile, GameplayEntityDisplay.Classify(world, entity));
        Assert.Equal(GameplayEntityDisplay.HostileColor, GameplayEntityDisplay.SelectionRingColor(EntityDisplayKind.Hostile));
    }

    [Fact]
    public void Scenery_uses_white_selection_ring()
    {
        var white = GameplayEntityDisplay.SelectionRingColor(EntityDisplayKind.Scenery);
        Assert.Equal(1f, white.X);
        Assert.Equal(1f, white.Y);
        Assert.Equal(1f, white.Z);
    }
}