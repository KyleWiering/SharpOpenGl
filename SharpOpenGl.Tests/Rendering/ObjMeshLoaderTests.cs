using OpenTK.Mathematics;
using SharpOpenGl.Engine.Rendering;
using Xunit;

namespace SharpOpenGl.Tests.Rendering;

public class ObjMeshLoaderTests
{
    private static string TempFile(string content)
    {
        string path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".obj");
        File.WriteAllText(path, content);
        return path;
    }

    // ── Parse returns null for missing file ───────────────────────────────────

    [Fact]
    public void Parse_returns_null_for_missing_file()
    {
        var result = ObjMeshLoader.Parse("/nonexistent/path/mesh.obj");
        Assert.Null(result);
    }

    // ── Basic triangle ────────────────────────────────────────────────────────

    [Fact]
    public void Parse_single_triangle_produces_3_vertices()
    {
        string path = TempFile("""
            v 0 0 0
            v 1 0 0
            v 0 1 0
            vn 0 0 1
            f 1//1 2//1 3//1
            """);
        try
        {
            var data = ObjMeshLoader.Parse(path);
            Assert.NotNull(data);
            Assert.Equal(3, data!.VertexCount);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void Parse_vertex_positions_are_correct()
    {
        string path = TempFile("""
            v 1.0 2.0 3.0
            v 4.0 5.0 6.0
            v 7.0 8.0 9.0
            f 1 2 3
            """);
        try
        {
            var data = ObjMeshLoader.Parse(path);
            Assert.NotNull(data);
            // First vertex position
            Assert.Equal(1.0f, data!.Vertices[0], precision: 4);
            Assert.Equal(2.0f, data.Vertices[1], precision: 4);
            Assert.Equal(3.0f, data.Vertices[2], precision: 4);
        }
        finally { File.Delete(path); }
    }

    // ── Quad fan-triangulation ────────────────────────────────────────────────

    [Fact]
    public void Parse_quad_produces_6_vertices()
    {
        string path = TempFile("""
            v -1  0 -1
            v  1  0 -1
            v  1  0  1
            v -1  0  1
            vn  0 1  0
            f 1//1 2//1 3//1 4//1
            """);
        try
        {
            var data = ObjMeshLoader.Parse(path);
            Assert.NotNull(data);
            Assert.Equal(6, data!.VertexCount); // quad → 2 triangles → 6 vertices
        }
        finally { File.Delete(path); }
    }

    // ── Normal fallback ───────────────────────────────────────────────────────

    [Fact]
    public void Parse_face_without_normals_uses_fallback_UnitY()
    {
        string path = TempFile("""
            v 0 0 0
            v 1 0 0
            v 0 1 0
            f 1 2 3
            """);
        try
        {
            var data = ObjMeshLoader.Parse(path);
            Assert.NotNull(data);
            // Normal floats are at indices 3,4,5 (first vertex)
            Assert.Equal(0f, data!.Vertices[3], precision: 4);
            Assert.Equal(1f, data.Vertices[4], precision: 4);
            Assert.Equal(0f, data.Vertices[5], precision: 4);
        }
        finally { File.Delete(path); }
    }

    // ── Comments and blank lines are ignored ──────────────────────────────────

    [Fact]
    public void Parse_ignores_comments_and_blank_lines()
    {
        string path = TempFile("""
            # this is a comment
            
            v 0 0 0
            v 1 0 0
            v 0 1 0
            # another comment
            f 1 2 3
            """);
        try
        {
            var data = ObjMeshLoader.Parse(path);
            Assert.NotNull(data);
            Assert.Equal(3, data!.VertexCount);
        }
        finally { File.Delete(path); }
    }

    // ── Multiple faces ────────────────────────────────────────────────────────

    [Fact]
    public void Parse_two_triangles_produces_6_vertices()
    {
        string path = TempFile("""
            v 0 0 0
            v 1 0 0
            v 0 1 0
            v 1 1 0
            f 1 2 3
            f 2 4 3
            """);
        try
        {
            var data = ObjMeshLoader.Parse(path);
            Assert.NotNull(data);
            Assert.Equal(6, data!.VertexCount);
        }
        finally { File.Delete(path); }
    }

    // ── Stride is 6 ──────────────────────────────────────────────────────────

    [Fact]
    public void ObjMeshData_Stride_is_6()
    {
        Assert.Equal(6, ObjMeshData.Stride);
    }

    // ── Name comes from file stem ─────────────────────────────────────────────

    [Fact]
    public void Parse_name_matches_file_stem()
    {
        string path = Path.Combine(Path.GetTempPath(), "my_ship.obj");
        File.WriteAllText(path, "v 0 0 0\nv 1 0 0\nv 0 1 0\nf 1 2 3");
        try
        {
            var data = ObjMeshLoader.Parse(path);
            Assert.NotNull(data);
            Assert.Equal("my_ship", data!.Name);
        }
        finally { File.Delete(path); }
    }

    // ── Empty file returns null ───────────────────────────────────────────────

    [Fact]
    public void Parse_file_with_no_faces_returns_null()
    {
        string path = TempFile("""
            # just vertices, no faces
            v 0 0 0
            v 1 0 0
            v 0 1 0
            """);
        try
        {
            var data = ObjMeshLoader.Parse(path);
            Assert.Null(data);
        }
        finally { File.Delete(path); }
    }

    // ── Default OBJ files in GameData can be parsed ───────────────────────────

    private static string GetGameDataPath()
    {
        string? dir = AppDomain.CurrentDomain.BaseDirectory;
        while (dir != null && !File.Exists(Path.Combine(dir, "SharpOpenGl.sln")))
            dir = Directory.GetParent(dir)?.FullName;
        return dir == null
            ? throw new InvalidOperationException("Cannot find repo root.")
            : Path.Combine(dir, "GameData", "Meshes");
    }

    [Fact]
    public void DefaultShip_obj_parses_successfully()
    {
        string path = Path.Combine(GetGameDataPath(), "default_ship.obj");
        var data = ObjMeshLoader.Parse(path);
        Assert.NotNull(data);
        Assert.True(data!.VertexCount > 0);
        Assert.Equal(0, data.VertexCount % 3); // must be whole triangles
    }

    [Fact]
    public void DefaultBase_obj_parses_successfully()
    {
        string path = Path.Combine(GetGameDataPath(), "default_base.obj");
        var data = ObjMeshLoader.Parse(path);
        Assert.NotNull(data);
        Assert.True(data!.VertexCount > 0);
        Assert.Equal(0, data.VertexCount % 3);
    }

    [Fact]
    public void DefaultProjectile_obj_parses_successfully()
    {
        string path = Path.Combine(GetGameDataPath(), "default_projectile.obj");
        var data = ObjMeshLoader.Parse(path);
        Assert.NotNull(data);
        Assert.True(data!.VertexCount > 0);
        Assert.Equal(0, data.VertexCount % 3);
    }
}
