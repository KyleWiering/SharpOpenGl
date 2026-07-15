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

    [Theory]
    [InlineData("Desktop")]
    [InlineData("Web")]
    public void Team_insignia_mix_uses_per_race_lookup_no_default_fallback(string shaderTarget)
    {
        const string expectedLookup =
            "float insigniaMixByRace[8] = float[8](0.20, 0.18, 0.12, 0.16, 0.18, 0.20, 0.15, 0.14);";
        string fragment = shaderTarget == "Desktop"
            ? GameShaders.DesktopFragment
            : GameShaders.WebFragment;

        Assert.Contains(expectedLookup, fragment);
        Assert.Contains("insigniaMix = insigniaMixByRace[clamp(raceTextureIndex, 0, 7)]", fragment);
        Assert.DoesNotContain("insigniaMix = raceTextureIndex", fragment);
        Assert.DoesNotContain("? 0.18 : 0.75", fragment);
    }

    [Theory]
    [InlineData("Desktop", 0, "terran", 0.20f)]
    [InlineData("Desktop", 1, "vesper", 0.18f)]
    [InlineData("Desktop", 2, "korath", 0.12f)]
    [InlineData("Desktop", 3, "aetherian", 0.16f)]
    [InlineData("Desktop", 4, "nexar", 0.18f)]
    [InlineData("Desktop", 5, "solari", 0.20f)]
    [InlineData("Desktop", 6, "voidborn", 0.15f)]
    [InlineData("Desktop", 7, "cryo", 0.14f)]
    [InlineData("Web", 0, "terran", 0.20f)]
    [InlineData("Web", 1, "vesper", 0.18f)]
    [InlineData("Web", 2, "korath", 0.12f)]
    [InlineData("Web", 3, "aetherian", 0.16f)]
    [InlineData("Web", 4, "nexar", 0.18f)]
    [InlineData("Web", 5, "solari", 0.20f)]
    [InlineData("Web", 6, "voidborn", 0.15f)]
    [InlineData("Web", 7, "cryo", 0.14f)]
    public void Team_insignia_mix_weight_for_each_playable_race(
        string shaderTarget,
        int raceIndex,
        string raceId,
        float expectedWeight)
    {
        Assert.Equal(raceId, RaceTextureIndex.AllRaceIds[raceIndex]);
        Assert.InRange(expectedWeight, 0.12f, 0.25f);

        string fragment = shaderTarget == "Desktop"
            ? GameShaders.DesktopFragment
            : GameShaders.WebFragment;

        string[] weights = fragment
            .Split("float insigniaMixByRace[8] = float[8](", StringSplitOptions.None)[1]
            .Split(')')[0]
            .Split(',')
            .Select(part => part.Trim())
            .ToArray();

        Assert.Equal(8, weights.Length);
        Assert.Equal(expectedWeight, float.Parse(weights[raceIndex], System.Globalization.CultureInfo.InvariantCulture), 3);
    }

    [Theory]
    [InlineData("Desktop")]
    [InlineData("Web")]
    public void Team_hull_aura_rim_and_crest_weights(string shaderTarget)
    {
        string fragment = shaderTarget == "Desktop"
            ? GameShaders.DesktopFragment
            : GameShaders.WebFragment;

        Assert.Contains("return color + teamTint * (rim * 0.3 + crest);", fragment);
        Assert.Contains("float crest = pow(max(localPos.y * 0.06 + 0.35, 0.0), 1.4) * 0.1;", fragment);
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

    [Theory]
    [InlineData(0, "terran", 1)]
    [InlineData(1, "vesper", 2)]
    [InlineData(2, "korath", 3)]
    [InlineData(3, "aetherian", 4)]
    [InlineData(4, "nexar", 5)]
    [InlineData(5, "solari", 6)]
    [InlineData(6, "voidborn", 7)]
    [InlineData(7, "cryo", 8)]
    public void Gallery_zone_maps_race_and_player_to_texture_index(int zone, string raceId, int playerId)
    {
        Assert.Equal(raceId, FleetGalleryLayout.RaceForZone(zone));
        Assert.Equal(playerId, FleetGalleryLayout.PlayerIdForZone(zone));
        Assert.Equal(zone, RaceTextureIndex.Resolve(FleetGalleryLayout.RaceForZone(zone)));
    }

    [Fact]
    public void All_race_texture_indices_are_distinct()
    {
        int[] indices = RaceTextureIndex.AllRaceIds.Select(RaceTextureIndex.Resolve).ToArray();
        Assert.Equal(8, indices.Length);
        Assert.Equal(8, indices.Distinct().Count());
        Assert.All(indices, index => Assert.InRange(index, 0, 7));
    }

    [Fact]
    public void Gallery_player_ids_one_and_two_produce_distinct_team_tints()
    {
        Vector3 terranTint = PlayerColorPalette.GetTint(FleetGalleryLayout.PlayerIdForZone(0));
        Vector3 vesperTint = PlayerColorPalette.GetTint(FleetGalleryLayout.PlayerIdForZone(1));
        Assert.NotEqual(terranTint, vesperTint);
        Assert.True(terranTint.LengthSquared > 0f);
        Assert.True(vesperTint.LengthSquared > 0f);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    public void Gallery_zone_texturing_sets_race_substrate_and_player_tint(int zone)
    {
        string raceId = FleetGalleryLayout.RaceForZone(zone);
        int playerId = FleetGalleryLayout.PlayerIdForZone(zone);
        var render = new RenderComponent { Color = new Vector4(0.4f, 0.4f, 0.4f, 1f) };

        TeamVisualResolver.ApplyRaceTexturing(render, raceId, playerId);

        Assert.Equal(zone, render.RaceTextureIndex);
        Assert.Equal(PlayerColorPalette.GetTint(playerId), render.TeamTint);
        Assert.Equal(-1, render.ComponentTextureIndex);
        Assert.Equal(Vector4.Zero, render.Color);
    }

    [Fact]
    public void Gallery_all_zones_produce_distinct_team_tints()
    {
        Vector3[] tints = Enumerable.Range(0, 8)
            .Select(zone => PlayerColorPalette.GetTint(FleetGalleryLayout.PlayerIdForZone(zone)))
            .ToArray();

        Assert.Equal(8, tints.Distinct().Count());
        Assert.All(tints, tint => Assert.True(tint.LengthSquared > 0f));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(3)]
    [InlineData(7)]
    public void Gallery_building_spawn_pattern_applies_race_texturing(int zone)
    {
        string raceId = FleetGalleryLayout.RaceForZone(zone);
        int playerId = FleetGalleryLayout.PlayerIdForZone(zone);
        var render = new RenderComponent
        {
            MeshId = 7,
            VertexCount = 240,
            Visible = true,
            Color = new Vector4(0.2f, 0.2f, 0.2f, 1f),
        };

        TeamVisualResolver.ApplyRaceTexturing(render, raceId, playerId);

        var (world, entity) = BuildGalleryTexturedWorld(render);
        Assert.True(TeamVisualResolver.HasTeamVisuals(world, entity));
        Assert.Equal(RaceTextureIndex.Resolve(raceId), render.RaceTextureIndex);
        Assert.Equal(PlayerColorPalette.GetTint(playerId), render.TeamTint);
    }

    [Fact]
    public void ApplyRaceTexturing_sets_substrate_index_team_tint_and_clears_flat_color()
    {
        var render = new RenderComponent
        {
            Color = new Vector4(0.5f, 0.5f, 0.5f, 1f),
            ComponentTextureIndex = ComponentTextureIndex.ShieldGenerator,
        };

        TeamVisualResolver.ApplyRaceTexturing(render, FleetGalleryLayout.RaceForZone(1), 2);

        Assert.Equal(RaceTextureIndex.Resolve("vesper"), render.RaceTextureIndex);
        Assert.Equal(PlayerColorPalette.GetTint(2), render.TeamTint);
        Assert.Equal(-1, render.ComponentTextureIndex);
        Assert.Equal(Vector4.Zero, render.Color);
    }

    [Fact]
    public void HasTeamVisuals_true_for_gallery_textured_render_component()
    {
        var world = new World();
        var entity = world.CreateEntity();
        var render = new RenderComponent();
        TeamVisualResolver.ApplyRaceTexturing(
            render,
            FleetGalleryLayout.RaceForZone(0),
            FleetGalleryLayout.PlayerIdForZone(0));
        world.AddComponent(entity, render);

        Assert.True(TeamVisualResolver.HasTeamVisuals(world, entity));
    }

    [Fact]
    public void HasTeamVisuals_false_when_race_texture_not_applied()
    {
        var world = new World();
        var entity = world.CreateEntity();
        world.AddComponent(entity, new RenderComponent { RaceTextureIndex = -1 });

        Assert.False(TeamVisualResolver.HasTeamVisuals(world, entity));
    }

    [Fact]
    public void RenderSystem_forwards_race_texture_and_team_tint_to_renderer()
    {
        var recorder = new RecordingRenderer();
        var renderSystem = new RenderSystem(recorder);
        var world = new World();

        var entity = world.CreateEntity();
        world.AddComponent(entity, new TransformComponent());
        var render = new RenderComponent
        {
            MeshId = 42,
            VertexCount = 120,
            Visible = true,
        };
        TeamVisualResolver.ApplyRaceTexturing(
            render,
            FleetGalleryLayout.RaceForZone(0),
            FleetGalleryLayout.PlayerIdForZone(0));
        world.AddComponent(entity, render);

        renderSystem.Update(world, 0f);

        Assert.Single(recorder.DrawCalls);
        var call = recorder.DrawCalls[0];
        Assert.Equal(render.RaceTextureIndex, call.RaceTextureIndex);
        Assert.Equal(render.TeamTint, call.TeamTint);
        Assert.Equal(render.ComponentTextureIndex, call.ComponentTextureIndex);
        Assert.Equal(Vector4.Zero, call.Color);
    }

    private static (World World, Entity Entity) BuildGalleryTexturedWorld(RenderComponent render)
    {
        var world = new World();
        var entity = world.CreateEntity();
        world.AddComponent(entity, render);
        return (world, entity);
    }

    private sealed class RecordingRenderer : IRenderer
    {
        public List<(int RaceTextureIndex, Vector3 TeamTint, int ComponentTextureIndex, Vector4 Color)> DrawCalls { get; } = [];

        public void BeginFrame(Matrix4 projection, Matrix4 view) { }

        public void DrawMesh(
            int vao, int vertexCount, Matrix4 model, Vector4 color, int primitiveType,
            int raceTextureIndex = -1, Vector3 teamTint = default, int componentTextureIndex = -1)
        {
            DrawCalls.Add((raceTextureIndex, teamTint, componentTextureIndex, color));
        }

        public void EndFrame() { }

        public void Resize(int width, int height) { }
    }
}