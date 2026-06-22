using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using SharpOpenGl.Engine.ECS;
using SharpOpenGl.Engine.Rendering;
using SharpOpenGl.Rendering;

namespace SharpOpenGl;

public partial class EngineWindow
{
    private int _teamAuraVao, _teamAuraVbo, _teamAuraVertCount;
    private const float TeamAuraBaseRingRadius = 3f;

    private void LoadTeamAuraMesh()
    {
        if (_teamAuraVertCount > 0) return;

        var uploaded = MeshBuilder.UploadProcedural(ProceduralMeshes.BuildTeamAuraDisc());
        _teamAuraVao = uploaded.vao;
        _teamAuraVbo = uploaded.vbo;
        _teamAuraVertCount = uploaded.vertexCount;
    }

    private void RenderTeamAuras()
    {
        if (_world == null || _teamAuraVertCount == 0) return;

        GL.Enable(EnableCap.Blend);
        GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
        GL.DepthMask(false);
        GL.Uniform1(_uniformRaceTextureIndex, -1);

        float pulse = 0.78f + 0.22f * MathF.Sin(_shieldRingPulse * 2.4f);

        foreach (var (entity, render) in _world.Query<RenderComponent>())
        {
            if (!render.Visible || render.RaceTextureIndex < 0) continue;

            var transform = _world.GetComponent<TransformComponent>(entity);
            if (transform == null || !ShouldRenderEntity(entity, transform.Position)) continue;

            int playerId = TeamVisualResolver.ResolvePlayerId(_world, entity);
            float selRadius = _world.GetComponent<SelectionComponent>(entity)?.SelectionRadius ?? 7f;
            float auraRadius = selRadius * 0.68f;

            var discModel = Matrix4.CreateScale(auraRadius) *
                            Matrix4.CreateTranslation(transform.Position with { Y = 0.12f });
            GL.UniformMatrix4(_uniformModel, false, ref discModel);
            GL.Uniform4(_uniformColor, PlayerColorPalette.GetAuraColor(playerId, pulse));
            GL.BindVertexArray(_teamAuraVao);
            GL.DrawArrays(PrimitiveType.Triangles, 0, _teamAuraVertCount);

            float outerScale = auraRadius / TeamAuraBaseRingRadius * 1.35f;
            var outerModel = Matrix4.CreateScale(outerScale) *
                             Matrix4.CreateTranslation(transform.Position with { Y = 0.16f });
            GL.UniformMatrix4(_uniformModel, false, ref outerModel);
            GL.Uniform4(_uniformColor, PlayerColorPalette.GetAuraOuterColor(playerId, pulse));
            GL.BindVertexArray(_selectionVao);
            GL.DrawArrays(PrimitiveType.Lines, 0, _selectionVertCount);

            float ringScale = auraRadius / TeamAuraBaseRingRadius * 1.08f;
            var ringModel = Matrix4.CreateScale(ringScale) *
                            Matrix4.CreateTranslation(transform.Position with { Y = 0.2f });
            GL.UniformMatrix4(_uniformModel, false, ref ringModel);
            GL.Uniform4(_uniformColor, PlayerColorPalette.GetAuraRingColor(playerId, pulse));
            GL.BindVertexArray(_selectionVao);
            GL.DrawArrays(PrimitiveType.Lines, 0, _selectionVertCount);
        }

        GL.DepthMask(true);
    }
}