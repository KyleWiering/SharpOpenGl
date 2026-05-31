using OpenTK.Mathematics;
using SharpOpenGl.Engine.ECS;
using SharpOpenGl.Engine.Rendering;
using Xunit;

namespace SharpOpenGl.Tests.Rendering;

public class BillboardSystemTests
{
    // ── Billboard activation ──────────────────────────────────────────────────

    [Fact]
    public void Billboard_activates_when_entity_exceeds_threshold()
    {
        var world     = new World();
        var system    = new BillboardSystem { CameraPosition = Vector3.Zero };

        var entity = world.CreateEntity();
        world.AddComponent(entity, new TransformComponent
        {
            Position = new Vector3(500f, 0f, 0f) // beyond threshold
        });
        var billboard = new BillboardComponent { FarThreshold = 400f };
        world.AddComponent(entity, billboard);

        system.Update(world, 0f);

        Assert.True(billboard.IsActive);
    }

    [Fact]
    public void Billboard_is_inactive_when_entity_is_within_threshold()
    {
        var world  = new World();
        var system = new BillboardSystem { CameraPosition = Vector3.Zero };

        var entity = world.CreateEntity();
        world.AddComponent(entity, new TransformComponent
        {
            Position = new Vector3(100f, 0f, 0f) // within threshold
        });
        var billboard = new BillboardComponent { FarThreshold = 400f };
        world.AddComponent(entity, billboard);

        system.Update(world, 0f);

        Assert.False(billboard.IsActive);
    }

    // ── Render component visibility ───────────────────────────────────────────

    [Fact]
    public void Billboard_hides_render_component_when_active()
    {
        var world  = new World();
        var system = new BillboardSystem { CameraPosition = Vector3.Zero };

        var entity = world.CreateEntity();
        world.AddComponent(entity, new TransformComponent
        {
            Position = new Vector3(600f, 0f, 0f)
        });
        var render    = new RenderComponent { Visible = true };
        var billboard = new BillboardComponent { FarThreshold = 400f };
        world.AddComponent(entity, render);
        world.AddComponent(entity, billboard);

        system.Update(world, 0f);

        Assert.False(render.Visible);
        Assert.True(billboard.IsActive);
    }

    [Fact]
    public void Billboard_restores_render_visibility_when_entity_moves_close()
    {
        var world     = new World();
        var system    = new BillboardSystem { CameraPosition = Vector3.Zero };
        var transform = new TransformComponent { Position = new Vector3(600f, 0f, 0f) };
        var render    = new RenderComponent { Visible = true };
        var billboard = new BillboardComponent { FarThreshold = 400f };

        var entity = world.CreateEntity();
        world.AddComponent(entity, transform);
        world.AddComponent(entity, render);
        world.AddComponent(entity, billboard);

        // First update — far away, billboard active
        system.Update(world, 0f);
        Assert.False(render.Visible);

        // Move close, second update — billboard should deactivate
        transform.Position = new Vector3(50f, 0f, 0f);
        system.Update(world, 0f);

        Assert.True(render.Visible);
        Assert.False(billboard.IsActive);
    }

    // ── Entity without TransformComponent uses origin ─────────────────────────

    [Fact]
    public void Billboard_entity_without_transform_defaults_to_origin()
    {
        var world  = new World();
        var system = new BillboardSystem { CameraPosition = new Vector3(500f, 0f, 0f) };

        var entity    = world.CreateEntity();
        var billboard = new BillboardComponent { FarThreshold = 400f };
        world.AddComponent(entity, billboard);

        system.Update(world, 0f);

        // Entity at origin, camera 500 away — should activate
        Assert.True(billboard.IsActive);
    }
}
