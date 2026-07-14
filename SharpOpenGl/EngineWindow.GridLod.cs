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
        int columns = _sandboxChunkedMode && _gridSystem != null
            ? _gridSystem.Width
            : GridColumns;
        int rows = _sandboxChunkedMode && _gridSystem != null
            ? _gridSystem.Height
            : GridRows;

        if (step == _gridLineStep && _gridVao != 0 &&
            columns == _gridLodColumns && rows == _gridLodRows)
            return;

        if (_gridVao != 0)
            MeshBuilder.DeleteMesh(_gridVao, _gridVbo);

        (_gridVao, _gridVbo, _gridVertCount) =
            MeshBuilder.BuildGrid(columns, rows, GridCellSize, GridColor, step);
        _gridLineStep = step;
        _gridLodColumns = columns;
        _gridLodRows = rows;
    }

    private int _gridLodColumns;
    private int _gridLodRows;
}