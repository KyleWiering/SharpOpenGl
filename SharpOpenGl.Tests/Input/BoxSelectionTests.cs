using OpenTK.Mathematics;
using SharpOpenGl.Engine.ECS;
using SharpOpenGl.Engine.Input;
using Xunit;

namespace SharpOpenGl.Tests.Input;

public class BoxSelectionTests
{
    [Fact]
    public void Box_selection_selects_units_whose_screen_position_is_inside_rect()
    {
        var viewport = new Vector2(1024f, 768f);
        var projection = Matrix4.CreatePerspectiveFieldOfView(
            MathHelper.DegreesToRadians(45f), viewport.X / viewport.Y, 0.1f, 10000f);
        var view = Matrix4.LookAt(new Vector3(0f, 80f, 80f), Vector3.Zero, Vector3.UnitY);

        Assert.True(GroundPlaneRaycaster.TryWorldToScreen(
            Vector3.Zero, viewport, projection, view, out Vector2 center));

        var rectMin = center - new Vector2(40f, 40f);
        var rectMax = center + new Vector2(40f, 40f);

        var world = new World();
        Entity inside = world.CreateEntity();
        world.AddComponent(inside, new TransformComponent { Position = Vector3.Zero });
        world.AddComponent(inside, new SelectionComponent { IsSelected = false });
        world.AddComponent(inside, new MovementComponent());

        Entity outside = world.CreateEntity();
        world.AddComponent(outside, new TransformComponent { Position = new Vector3(500f, 0f, 500f) });
        world.AddComponent(outside, new SelectionComponent { IsSelected = false });
        world.AddComponent(outside, new MovementComponent());

        SelectUnitsInRect(world, rectMin, rectMax, viewport, projection, view);

        Assert.True(world.GetComponent<SelectionComponent>(inside)!.IsSelected);
        Assert.False(world.GetComponent<SelectionComponent>(outside)!.IsSelected);

        world.Dispose();
    }

    private static void SelectUnitsInRect(
        World world, Vector2 rectMin, Vector2 rectMax,
        Vector2 viewport, Matrix4 projection, Matrix4 view)
    {
        foreach (var (entity, sel) in world.Query<SelectionComponent>())
        {
            if (!world.HasComponent<MovementComponent>(entity)) continue;
            var transform = world.GetComponent<TransformComponent>(entity);
            if (transform == null) continue;

            if (!GroundPlaneRaycaster.TryWorldToScreen(
                    transform.Position, viewport, projection, view, out Vector2 screen))
                continue;

            if (GroundPlaneRaycaster.IsInsideScreenRect(screen, rectMin, rectMax))
                sel.IsSelected = true;
        }
    }
}