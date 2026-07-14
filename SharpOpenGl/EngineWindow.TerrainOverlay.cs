using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using SharpOpenGl.Engine.Rendering;

namespace SharpOpenGl;

public partial class EngineWindow
{
    private TerrainReadabilityOverlay? _terrainReadabilityOverlay;

    private void RenderTerrainReadabilityOverlay()
    {
        if (_gridSystem == null || _sandboxChunkedMode || _groundQuadVertCount == 0) return;

        Vector4? bounds = TryGetFogCameraBoundsXZ();
        if (bounds is null) return;

        _terrainReadabilityOverlay ??= new TerrainReadabilityOverlay();
        _terrainReadabilityOverlay.Sync(_gridSystem, _fogOfWar, playerId: 0, bounds.Value);

        if (_terrainReadabilityOverlay.CellCount == 0) return;

        GL.Enable(EnableCap.Blend);
        GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
        GL.BindVertexArray(_groundQuadVao);

        foreach (TerrainReadabilityOverlay.TerrainTintCell cell in _terrainReadabilityOverlay.Cells)
        {
            var model = Matrix4.CreateScale(cell.CellSize, 1f, cell.CellSize) *
                        Matrix4.CreateTranslation(cell.Center.X, 0.14f, cell.Center.Z);
            GL.UniformMatrix4(_uniformModel, false, ref model);
            GL.Uniform4(_uniformColor, cell.Color);
            GL.Uniform1(_uniformRaceTextureIndex, -1);
            GL.Uniform1(_uniformComponentTextureIndex, -1);
            GL.DrawArrays(PrimitiveType.Triangles, 0, _groundQuadVertCount);
        }

        GL.BindVertexArray(0);
        GL.Disable(EnableCap.Blend);
    }
}