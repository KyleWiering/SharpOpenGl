using OpenTK.Mathematics;
using SharpOpenGl.Engine.Rendering;
using Xunit;

namespace SharpOpenGl.Tests.Rendering;

public class ProceduralShipGeneratorTests
{
    // ── Vertex count ──────────────────────────────────────────────────────────

    [Fact]
    public void Generate_default_params_returns_non_empty_mesh()
    {
        var data = ProceduralShipGenerator.Generate(new ShipParameters());
        Assert.NotNull(data);
        Assert.True(data.VertexCount > 0);
    }

    [Fact]
    public void Generate_vertex_count_is_multiple_of_3()
    {
        var data = ProceduralShipGenerator.Generate(new ShipParameters());
        Assert.Equal(0, data.VertexCount % 3);
    }

    // ── Stride ────────────────────────────────────────────────────────────────

    [Fact]
    public void Generate_vertices_array_length_matches_stride()
    {
        var data = ProceduralShipGenerator.Generate(new ShipParameters());
        Assert.Equal(data.VertexCount * ObjMeshData.Stride, data.Vertices.Length);
    }

    // ── Wings toggle ──────────────────────────────────────────────────────────

    [Fact]
    public void Generate_with_wings_produces_more_vertices_than_no_wings()
    {
        var withWings = ProceduralShipGenerator.Generate(
            new ShipParameters { WingLength = 1f });
        var noWings = ProceduralShipGenerator.Generate(
            new ShipParameters { WingLength = 0f });

        Assert.True(withWings.VertexCount > noWings.VertexCount);
    }

    // ── Engine count ──────────────────────────────────────────────────────────

    [Fact]
    public void Generate_more_engines_produces_more_vertices()
    {
        var twoEngines  = ProceduralShipGenerator.Generate(
            new ShipParameters { EngineCount = 2 });
        var fourEngines = ProceduralShipGenerator.Generate(
            new ShipParameters { EngineCount = 4 });

        Assert.True(fourEngines.VertexCount > twoEngines.VertexCount);
    }

    [Fact]
    public void Generate_zero_engines_still_returns_valid_mesh()
    {
        var data = ProceduralShipGenerator.Generate(
            new ShipParameters { EngineCount = 0 });
        Assert.True(data.VertexCount > 0);
    }

    // ── Named correctly ───────────────────────────────────────────────────────

    [Fact]
    public void Generate_result_name_is_procedural_ship()
    {
        var data = ProceduralShipGenerator.Generate(new ShipParameters());
        Assert.Equal("procedural_ship", data.Name);
    }

    // ── Parameters affect geometry ────────────────────────────────────────────

    [Fact]
    public void Generate_different_lengths_produce_different_vertex_positions()
    {
        var short_ = ProceduralShipGenerator.Generate(
            new ShipParameters { Length = 1f, EngineCount = 0, WingLength = 0f });
        var long_ = ProceduralShipGenerator.Generate(
            new ShipParameters { Length = 4f, EngineCount = 0, WingLength = 0f });

        // The bow vertex (first position) should differ
        Assert.NotEqual(short_.Vertices[0], long_.Vertices[0]);
    }

    // ── EngineCount is clamped ────────────────────────────────────────────────

    [Fact]
    public void Generate_engine_count_above_4_is_clamped()
    {
        var capped  = ProceduralShipGenerator.Generate(
            new ShipParameters { EngineCount = 4 });
        var overMax = ProceduralShipGenerator.Generate(
            new ShipParameters { EngineCount = 99 });

        // Both should produce the same number of engine nozzle vertices
        Assert.Equal(capped.VertexCount, overMax.VertexCount);
    }
}
