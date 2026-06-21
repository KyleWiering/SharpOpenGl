using OpenTK.Mathematics;
using SharpOpenGl.Engine.Rendering;
using SharpOpenGl.Rendering;

namespace SharpOpenGl;

public partial class EngineWindow
{
    private static readonly Vector3 GridColor = new(0.15f, 0.15f, 0.25f);

    private void UpdateGridMeshLod()
    {
        int step = GridRenderLod.ResolveLineStep(
            _rtsCamera.Height, _rtsCamera.MinHeight, _rtsCamera.MaxHeight);
        if (step == _gridLineStep && _gridVao != 0) return;

        if (_gridVao != 0)
            MeshBuilder.DeleteMesh(_gridVao, _gridVbo);

        (_gridVao, _gridVbo, _gridVertCount) =
            MeshBuilder.BuildGrid(GridColumns, GridRows, GridCellSize, GridColor, step);
        _gridLineStep = step;
    }
}