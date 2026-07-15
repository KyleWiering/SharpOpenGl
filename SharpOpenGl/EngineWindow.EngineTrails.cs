using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using SharpOpenGl.Engine.ECS;
using SharpOpenGl.Engine.Rendering;

namespace SharpOpenGl;

public partial class EngineWindow
{
    private const int EngineTrailParticleBufferFloats = 4096 * 6;
    private readonly float[] _engineTrailParticleBuffer = new float[EngineTrailParticleBufferFloats];

    private static bool EntityHasShipEngineNozzles(World world, Entity entity)
    {
        foreach (var (_, nozzle) in world.Query<ShipEngineNozzleComponent>())
        {
            if (nozzle.Owner == entity)
                return true;
        }

        return false;
    }

    private void RenderEngineTrailParticles()
    {
        if (_world == null) return;

        int totalPoints = 0;
        int floatIndex = 0;

        foreach (var (entity, nozzle) in _world.Query<ShipEngineNozzleComponent>())
        {
            var emitterComp = _world.GetComponent<ParticleEmitterComponent>(entity);
            if (emitterComp == null) continue;

            int written = emitterComp.Emitter.WriteColoredPoints(_engineTrailParticleBuffer, floatIndex);
            if (written == 0) continue;

            totalPoints += written;
            floatIndex += written * 6;
            if (floatIndex >= _engineTrailParticleBuffer.Length)
                break;
        }

        if (totalPoints == 0) return;

        GL.BindBuffer(BufferTarget.ArrayBuffer, _particleVbo);
        GL.BufferSubData(
            BufferTarget.ArrayBuffer,
            IntPtr.Zero,
            floatIndex * sizeof(float),
            _engineTrailParticleBuffer);

        var identity = Matrix4.Identity;
        GL.UniformMatrix4(_uniformModel, false, ref identity);
        GL.Uniform4(_uniformColor, Vector4.Zero);
        GL.Uniform1(_uniformRaceTextureIndex, -1);
        GL.Uniform1(_uniformComponentTextureIndex, -1);
        GL.Uniform1(_uniformPointSize, 5f);

        GL.BindVertexArray(_particleVao);
        GL.DrawArrays(PrimitiveType.Points, 0, totalPoints);
        GL.BindVertexArray(0);
    }
}