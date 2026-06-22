using OpenTK.Mathematics;
using SharpOpenGl.Engine.ECS;
using SharpOpenGl.Engine.Rendering;
using Xunit;

namespace SharpOpenGl.Tests.Rendering;

public class TeamVisualTests
{
    [Fact]
    public void BuildTeamAuraDisc_produces_triangle_fan()
    {
        float[] mesh = ProceduralMeshes.BuildTeamAuraDisc();
        Assert.True(mesh.Length >= 36);
        Assert.Equal(0, mesh.Length % ProceduralMeshes.Stride);
    }

    [Fact]
    public void GetAuraColor_uses_transparent_alpha_for_ground_disc()
    {
        Vector4 aura = PlayerColorPalette.GetAuraColor(2, 1f);
        Assert.InRange(aura.W, 0.25f, 0.6f);
        Assert.True(aura.X > 0.5f);
    }

    [Fact]
    public void GameShaders_include_insignia_and_aura_helpers()
    {
        Assert.Contains("applyTeamInsignia", GameShaders.DesktopFragment);
        Assert.Contains("applyTeamHullAura", GameShaders.WebFragment);
        Assert.Contains("vLocalPos", GameShaders.DesktopVertex);
        Assert.Contains("vLocalPos.xz * uvScale", GameShaders.DesktopFragment);
        Assert.DoesNotContain("vWorldPos.xz * uvScale", GameShaders.DesktopFragment);
        Assert.DoesNotContain("color * tex * teamTint", GameShaders.DesktopFragment);
    }

    [Fact]
    public void ResolvePlayerId_reads_building_and_combat_faction()
    {
        var world = new World();
        var building = world.CreateEntity();
        world.AddComponent(building, new BuildingComponent { PlayerId = 4 });
        Assert.Equal(4, TeamVisualResolver.ResolvePlayerId(world, building));

        var ship = world.CreateEntity();
        world.AddComponent(ship, new CombatTargetComponent { Faction = 6 });
        Assert.Equal(6, TeamVisualResolver.ResolvePlayerId(world, ship));
    }
}