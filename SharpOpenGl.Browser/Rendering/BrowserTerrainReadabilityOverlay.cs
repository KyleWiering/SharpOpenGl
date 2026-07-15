using OpenTK.Mathematics;
using SharpOpenGl.Engine.Grid;
using SharpOpenGl.Engine.Rendering;

namespace SharpOpenGl.Browser.Rendering;

/// <summary>
/// Browser parity stub for desktop <see cref="TerrainReadabilityOverlay"/>.
/// Syncs tint cells from grid terrain; draw path deferred until WebGL ground-quad batch lands.
/// </summary>
public sealed class BrowserTerrainReadabilityOverlay
{
    private readonly TerrainReadabilityOverlay _desktop = new();

    public int CellCount => _desktop.CellCount;

    public IReadOnlyList<TerrainReadabilityOverlay.TerrainTintCell> Cells => _desktop.Cells;

    /// <summary>Rebuild visible terrain tint cells for the current camera bounds (parity with desktop).</summary>
    public void Sync(GridSystem grid, FogOfWar? fog, int playerId, Vector4 cameraBoundsXZ) =>
        _desktop.Sync(grid, fog, playerId, cameraBoundsXZ);

    /// <summary>Whether browser should attempt terrain tint draw this frame.</summary>
    public bool IsDrawEnabled => CellCount > 0;
}