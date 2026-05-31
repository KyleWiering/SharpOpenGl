using OpenTK.Mathematics;
using SharpOpenGl.Engine.ECS;
using SharpOpenGl.Engine.Rendering;
using Xunit;

namespace SharpOpenGl.Tests.Rendering;

public class MeshRegistryTests
{
    // ── Register and retrieve ─────────────────────────────────────────────────

    [Fact]
    public void Register_and_TryGet_returns_registered_entry()
    {
        var registry = new MeshRegistry();
        registry.Register("ships/hero", 10, 20, 300);

        bool found = registry.TryGet("ships/hero", out var entry);

        Assert.True(found);
        Assert.NotNull(entry);
        Assert.Equal(10, entry!.Vao);
        Assert.Equal(20, entry.Vbo);
        Assert.Equal(300, entry.VertexCount);
    }

    [Fact]
    public void TryGet_returns_false_for_unregistered_key()
    {
        var registry = new MeshRegistry();
        bool found = registry.TryGet("missing/mesh", out _);
        Assert.False(found);
    }

    // ── Contains ─────────────────────────────────────────────────────────────

    [Fact]
    public void Contains_returns_true_after_registration()
    {
        var registry = new MeshRegistry();
        registry.Register("foo", 1, 2, 3);
        Assert.True(registry.Contains("foo"));
    }

    [Fact]
    public void Contains_returns_false_for_unregistered_key()
    {
        var registry = new MeshRegistry();
        Assert.False(registry.Contains("bar"));
    }

    // ── Overwrite ─────────────────────────────────────────────────────────────

    [Fact]
    public void Register_overwrites_existing_entry()
    {
        var registry = new MeshRegistry();
        registry.Register("ship", 1, 2, 100);
        registry.Register("ship", 9, 8, 999);

        registry.TryGet("ship", out var entry);
        Assert.Equal(9, entry!.Vao);
        Assert.Equal(999, entry.VertexCount);
    }

    // ── GetOrFallback ─────────────────────────────────────────────────────────

    [Fact]
    public void GetOrFallback_returns_entry_when_key_exists()
    {
        var registry = new MeshRegistry();
        registry.Register("hero_ship", 5, 6, 200);

        var entry = registry.GetOrFallback("hero_ship", "default_ship");

        Assert.NotNull(entry);
        Assert.Equal(5, entry!.Vao);
    }

    [Fact]
    public void GetOrFallback_returns_fallback_when_key_missing()
    {
        var registry = new MeshRegistry();
        registry.Register("default_ship", 99, 0, 30);

        var entry = registry.GetOrFallback("nonexistent_ship", "default_ship");

        Assert.NotNull(entry);
        Assert.Equal(99, entry!.Vao);
    }

    [Fact]
    public void GetOrFallback_returns_null_when_both_missing()
    {
        var registry = new MeshRegistry();
        var entry = registry.GetOrFallback("missing", "also_missing");
        Assert.Null(entry);
    }

    // ── Unregister ────────────────────────────────────────────────────────────

    [Fact]
    public void Unregister_removes_entry()
    {
        var registry = new MeshRegistry();
        registry.Register("temp_mesh", 1, 2, 3);
        registry.Unregister("temp_mesh");

        Assert.False(registry.Contains("temp_mesh"));
    }

    // ── Count ─────────────────────────────────────────────────────────────────

    [Fact]
    public void Count_reflects_registered_entries()
    {
        var registry = new MeshRegistry();
        Assert.Equal(0, registry.Count);

        registry.Register("a", 1, 2, 3);
        registry.Register("b", 4, 5, 6);
        Assert.Equal(2, registry.Count);

        registry.Unregister("a");
        Assert.Equal(1, registry.Count);
    }

    // ── Key lookup is case-insensitive ────────────────────────────────────────

    [Fact]
    public void Lookup_is_case_insensitive()
    {
        var registry = new MeshRegistry();
        registry.Register("Ships/Hero", 7, 8, 9);

        bool found = registry.TryGet("ships/hero", out var entry);
        Assert.True(found);
        Assert.Equal(7, entry!.Vao);
    }

    // ── Tuple registration overload ───────────────────────────────────────────

    [Fact]
    public void Register_tuple_overload_works()
    {
        var registry = new MeshRegistry();
        registry.Register("my_mesh", (vao: 11, vbo: 22, vertexCount: 333));

        registry.TryGet("my_mesh", out var entry);
        Assert.Equal(11, entry!.Vao);
        Assert.Equal(333, entry.VertexCount);
    }
}
