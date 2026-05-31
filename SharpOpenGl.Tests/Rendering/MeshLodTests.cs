using OpenTK.Mathematics;
using SharpOpenGl.Engine.ECS;
using SharpOpenGl.Engine.Rendering;
using Xunit;

namespace SharpOpenGl.Tests.Rendering;

public class MeshLodTests
{
    // ── Select returns detail when close ──────────────────────────────────────

    [Fact]
    public void Select_returns_detail_when_within_simple_distance()
    {
        var lod = new MeshLod
        {
            DetailMesh     = (vao: 1, vertexCount: 300),
            SimpleMesh     = (vao: 2, vertexCount: 100),
            IconMesh       = (vao: 3, vertexCount: 12),
            SimpleDistance = 100f,
            IconDistance   = 300f,
        };

        var (vao, _) = lod.Select(50f);
        Assert.Equal(1, vao);
    }

    [Fact]
    public void Select_returns_simple_at_medium_distance()
    {
        var lod = new MeshLod
        {
            DetailMesh     = (1, 300),
            SimpleMesh     = (2, 100),
            IconMesh       = (3, 12),
            SimpleDistance = 100f,
            IconDistance   = 300f,
        };

        var (vao, _) = lod.Select(150f);
        Assert.Equal(2, vao);
    }

    [Fact]
    public void Select_returns_icon_at_far_distance()
    {
        var lod = new MeshLod
        {
            DetailMesh     = (1, 300),
            SimpleMesh     = (2, 100),
            IconMesh       = (3, 12),
            SimpleDistance = 100f,
            IconDistance   = 300f,
        };

        var (vao, _) = lod.Select(500f);
        Assert.Equal(3, vao);
    }

    // ── Fallback when LOD not set ─────────────────────────────────────────────

    [Fact]
    public void Select_falls_back_to_detail_when_simple_mesh_vao_is_zero()
    {
        var lod = new MeshLod
        {
            DetailMesh     = (5, 100),
            SimpleMesh     = (0, 0),   // not set
            IconMesh       = (0, 0),   // not set
            SimpleDistance = 50f,
            IconDistance   = 200f,
        };

        var (vao, _) = lod.Select(100f); // beyond SimpleDistance but SimpleMesh not set
        Assert.Equal(5, vao);
    }

    [Fact]
    public void Select_falls_back_to_simple_when_icon_mesh_vao_is_zero()
    {
        var lod = new MeshLod
        {
            DetailMesh     = (1, 300),
            SimpleMesh     = (2, 100),
            IconMesh       = (0, 0),   // not set
            SimpleDistance = 100f,
            IconDistance   = 300f,
        };

        var (vao, _) = lod.Select(500f); // beyond IconDistance but icon not set
        Assert.Equal(2, vao);
    }

    // ── MeshLodSystem updates RenderComponent ────────────────────────────────

    [Fact]
    public void MeshLodSystem_picks_detail_mesh_for_nearby_entity()
    {
        var world     = new World();
        var lodSystem = new MeshLodSystem { CameraPosition = Vector3.Zero };

        var entity = world.CreateEntity();
        world.AddComponent(entity, new TransformComponent
        {
            Position = new Vector3(10f, 0f, 0f) // close
        });
        world.AddComponent(entity, new RenderComponent());
        world.AddComponent(entity, new MeshLodComponent
        {
            Lod = new MeshLod
            {
                DetailMesh     = (42, 300),
                SimpleMesh     = (7, 100),
                IconMesh       = (3, 12),
                SimpleDistance = 100f,
                IconDistance   = 300f,
            }
        });

        lodSystem.Update(world, 0f);

        var render = world.GetComponent<RenderComponent>(entity)!;
        Assert.Equal(42, render.MeshId);
        Assert.Equal(300, render.VertexCount);
    }

    [Fact]
    public void MeshLodSystem_picks_icon_mesh_for_distant_entity()
    {
        var world     = new World();
        var lodSystem = new MeshLodSystem { CameraPosition = Vector3.Zero };

        var entity = world.CreateEntity();
        world.AddComponent(entity, new TransformComponent
        {
            Position = new Vector3(500f, 0f, 0f) // far
        });
        world.AddComponent(entity, new RenderComponent());
        world.AddComponent(entity, new MeshLodComponent
        {
            Lod = new MeshLod
            {
                DetailMesh     = (42, 300),
                SimpleMesh     = (7,  100),
                IconMesh       = (3,   12),
                SimpleDistance = 100f,
                IconDistance   = 300f,
            }
        });

        lodSystem.Update(world, 0f);

        var render = world.GetComponent<RenderComponent>(entity)!;
        Assert.Equal(3, render.MeshId);
    }
}
