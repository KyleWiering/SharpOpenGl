using OpenTK.Mathematics;
using SharpOpenGl.Engine.ECS;
using SharpOpenGl.Engine.Rendering;
using Xunit;

namespace SharpOpenGl.Tests.Rendering;

public class MaterialTests
{
    // ── Default values ────────────────────────────────────────────────────────

    [Fact]
    public void Material_defaults_to_white_diffuse()
    {
        var m = new Material();
        Assert.Equal(Vector3.One, m.DiffuseColor);
    }

    [Fact]
    public void Material_defaults_to_no_emissive()
    {
        var m = new Material();
        Assert.Equal(Vector3.Zero, m.EmissiveColor);
    }

    [Fact]
    public void Material_defaults_to_full_opacity()
    {
        var m = new Material();
        Assert.Equal(1f, m.Opacity);
    }

    // ── Solid factory ─────────────────────────────────────────────────────────

    [Fact]
    public void Solid_sets_diffuse_and_leaves_emissive_zero()
    {
        var color = new Vector3(0.5f, 0.2f, 0.8f);
        var m = Material.Solid(color);

        Assert.Equal(color, m.DiffuseColor);
        Assert.Equal(Vector3.Zero, m.EmissiveColor);
        Assert.Equal(1f, m.Opacity);
    }

    // ── Emissive factory ──────────────────────────────────────────────────────

    [Fact]
    public void Emissive_sets_both_diffuse_and_emissive()
    {
        var diff = new Vector3(0.1f, 0.1f, 0.1f);
        var emit = new Vector3(0.0f, 0.8f, 1.0f);
        var m = Material.Emissive(diff, emit);

        Assert.Equal(diff, m.DiffuseColor);
        Assert.Equal(emit, m.EmissiveColor);
    }

    // ── Translucent factory ───────────────────────────────────────────────────

    [Fact]
    public void Translucent_sets_opacity()
    {
        var m = Material.Translucent(Vector3.One, 0.3f);
        Assert.Equal(0.3f, m.Opacity, precision: 5);
    }

    // ── MaterialComponent ─────────────────────────────────────────────────────

    [Fact]
    public void MaterialComponent_default_material_is_not_null()
    {
        var comp = new MaterialComponent();
        Assert.NotNull(comp.Material);
    }

    [Fact]
    public void MaterialComponent_stores_assigned_material()
    {
        var mat = Material.Solid(new Vector3(1f, 0f, 0f));
        var comp = new MaterialComponent { Material = mat };

        Assert.Same(mat, comp.Material);
        Assert.Equal(new Vector3(1f, 0f, 0f), comp.Material.DiffuseColor);
    }
}
