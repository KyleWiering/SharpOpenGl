using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using SharpOpenGl.Engine.ECS;
using SharpOpenGl.Rendering;

namespace SharpOpenGl;

public partial class EngineWindow
{
    private int _tractorBeamLineVao;
    private int _tractorBeamLineVbo;

    private void RenderMiningVfx()
    {
        if (_world == null) return;

        RenderTractorBeams();
    }

    private void RenderTractorBeams()
    {
        if (_tractorBeamLineVbo == 0)
            (_tractorBeamLineVao, _tractorBeamLineVbo, _) = MeshBuilder.CreateDynamicLineStrip();

        GL.Enable(EnableCap.Blend);
        GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
        GL.BindVertexArray(_tractorBeamLineVao);
        var identity = Matrix4.Identity;
        GL.UniformMatrix4(_uniformModel, false, ref identity);

        foreach (var (entity, beam) in _world!.Query<TractorBeamVisualComponent>())
        {
            if (!_world.IsAlive(beam.NodeEntity)) continue;

            var minerTf = _world.GetComponent<TransformComponent>(entity);
            var nodeTf = _world.GetComponent<TransformComponent>(beam.NodeEntity);
            if (minerTf == null || nodeTf == null) continue;

            float pulse = 0.55f + 0.25f * MathF.Sin(beam.PulsePhase);
            var segments = new List<Vector3>
            {
                nodeTf.Position with { Y = 1.5f },
                Vector3.Lerp(nodeTf.Position, minerTf.Position, 0.35f) with { Y = 2.5f },
                Vector3.Lerp(nodeTf.Position, minerTf.Position, 0.7f) with { Y = 2f },
                minerTf.Position with { Y = 1.2f },
            };

            float[] vertices = MeshBuilder.BuildLineStripVertices(
                segments, new Vector3(0.4f * pulse, 0.85f * pulse, 1f), y: 0f);
            int vertCount = MeshBuilder.UpdateDynamicLineStrip(_tractorBeamLineVbo, vertices);
            GL.Uniform4(_uniformColor, new Vector4(0.35f, 0.8f, 1f, 0.55f * pulse));
            GL.DrawArrays(PrimitiveType.LineStrip, 0, vertCount);

            // Ore particles pulled along the beam.
            if (_tractorBeamVao != 0)
            {
                for (int i = 0; i < 3; i++)
                {
                    float t = (beam.PulsePhase * 0.25f + i * 0.33f) % 1f;
                    Vector3 pos = Vector3.Lerp(nodeTf.Position, minerTf.Position, t);
                    pos.Y += 1.5f + MathF.Sin(beam.PulsePhase + i) * 0.4f;
                    float scale = 0.25f + 0.1f * MathF.Sin(beam.PulsePhase * 2f + i);
                    var model = Matrix4.CreateScale(scale) * Matrix4.CreateTranslation(pos);
                    GL.UniformMatrix4(_uniformModel, false, ref model);
                    GL.Uniform4(_uniformColor, new Vector4(0.95f, 0.7f, 0.2f, 0.85f));
                    GL.BindVertexArray(_tractorBeamVao);
                    GL.DrawArrays(PrimitiveType.Triangles, 0, _tractorBeamVertCount);
                }
            }
        }

        GL.Disable(EnableCap.Blend);
    }
}