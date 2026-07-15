using OpenTK.Mathematics;
using SharpOpenGl.Engine.Grid;
using SharpOpenGl.Engine.Rendering;
using SharpOpenGl.Engine.UI;

namespace SharpOpenGl.Engine.UI.Widgets;

/// <summary>Economy and planet markers on the minimap.</summary>
public enum MinimapFeatureKind
{
    ResourceEnergy,
    ResourceMinerals,
    ResourceData,
    ResourceCrew,
    HarvestablePlanet,
    NeutralPlanet,
    AnomalyNebula,
    AnomalyAsteroid,
    AnomalyDebris,
    AnomalyIonStorm,
    AnomalyWormhole,
}

/// <summary>Harvestable resource or planet marker on the minimap.</summary>
public readonly record struct MinimapFeatureMarker(
    Vector2 NormalizedPosition,
    MinimapFeatureKind Kind,
    Vector4 Color);

/// <summary>
/// Represents a unit dot rendered on the minimap.
/// </summary>
public readonly record struct MinimapUnit(
    Vector2 NormalizedPosition,
    Vector4 Color,
    bool IsFriendly,
    int PlayerId = 1
);

/// <summary>
/// Mission waypoint marker on the minimap.
/// </summary>
public readonly record struct MinimapMarker(
    Vector2 NormalizedPosition,
    Vector4 Color
);

/// <summary>
/// Mini-map widget that renders fog-of-war, units, and mission markers.
/// </summary>
public sealed class Minimap : Widget
{
    public FogOfWar? FogOfWar { get; set; }
    public int PlayerId { get; set; }
    public Vector2i GridSize { get; set; } = new(32, 32);

    /// <summary>
    /// Display resolution for fog tiles. Defaults to <see cref="GridSize"/> when zero.
    /// </summary>
    public Vector2i FogDisplayCells { get; set; }

    public IReadOnlyList<MinimapUnit> Units { get; set; } = Array.Empty<MinimapUnit>();
    public IReadOnlyList<MinimapMarker> ObjectiveMarkers { get; set; } = Array.Empty<MinimapMarker>();
    public IReadOnlyList<MinimapFeatureMarker> FeatureMarkers { get; set; } = Array.Empty<MinimapFeatureMarker>();
    public (Vector2 Min, Vector2 Max) CameraViewport { get; set; } =
        (new Vector2(0.25f, 0.25f), new Vector2(0.75f, 0.75f));

    public Vector4 BackgroundColor { get; set; } = new Vector4(0.02f, 0.02f, 0.05f, 1f);
    public Vector4 UnexploredColor { get; set; } =
        FogVisualPalette.ToColor(FogVisualPalette.UnexploredCore, FogVisualPalette.UnexploredAlpha);
    public Vector4 ExploredColor { get; set; } =
        FogVisualPalette.ToColor(FogVisualPalette.ExploredCore, FogVisualPalette.ExploredAlpha);
    public Vector4 VisibleColor { get; set; } =
        FogVisualPalette.ToColor(FogVisualPalette.ExploredVeil, 0.15f);

    /// <summary>When true, fog tiles use layered nebula palette rects instead of flat fills.</summary>
    public bool UseNebulaFogStyle { get; set; } = true;
    public Vector4 BorderColor { get; set; } = new Vector4(0.5f, 0.5f, 0.7f, 1f);
    public Vector4 CameraBoxColor { get; set; } = new Vector4(1f, 1f, 1f, 0.6f);

    public static readonly Vector4 ResourceEnergyColor = new(0.2f, 0.9f, 1f, 1f);
    public static readonly Vector4 ResourceMineralsColor = new(1f, 0.75f, 0.2f, 1f);
    public static readonly Vector4 ResourceDataColor = new(0.7f, 0.4f, 1f, 1f);
    public static readonly Vector4 ResourceCrewColor = new(0.35f, 0.9f, 0.45f, 1f);
    public static readonly Vector4 AnomalyNebulaColor = new(0.78f, 0.35f, 0.95f, 1f);
    public static readonly Vector4 AnomalyAsteroidColor = new(0.58f, 0.52f, 0.46f, 1f);
    public static readonly Vector4 AnomalyDebrisColor = new(0.72f, 0.56f, 0.38f, 1f);
    public static readonly Vector4 AnomalyIonStormColor = new(0.62f, 0.28f, 1f, 1f);
    public static readonly Vector4 AnomalyWormholeColor = new(0.2f, 0.88f, 1f, 1f);

    /// <summary>Returns the default tint for a minimap feature kind.</summary>
    public static Vector4 ColorForFeature(MinimapFeatureKind kind) => kind switch
    {
        MinimapFeatureKind.ResourceEnergy => ResourceEnergyColor,
        MinimapFeatureKind.ResourceMinerals => ResourceMineralsColor,
        MinimapFeatureKind.ResourceData => ResourceDataColor,
        MinimapFeatureKind.ResourceCrew => ResourceCrewColor,
        MinimapFeatureKind.HarvestablePlanet => GameplayEntityDisplay.HarvestableColor,
        MinimapFeatureKind.NeutralPlanet => GameplayEntityDisplay.NeutralColor,
        MinimapFeatureKind.AnomalyNebula => AnomalyNebulaColor,
        MinimapFeatureKind.AnomalyAsteroid => AnomalyAsteroidColor,
        MinimapFeatureKind.AnomalyDebris => AnomalyDebrisColor,
        MinimapFeatureKind.AnomalyIonStorm => AnomalyIonStormColor,
        MinimapFeatureKind.AnomalyWormhole => AnomalyWormholeColor,
        _ => ResourceMineralsColor,
    };

    /// <summary>Maps scenery <c>featureType</c> strings to minimap anomaly markers.</summary>
    public static MinimapFeatureKind SceneryFeatureTypeToKind(string? featureType) =>
        featureType?.ToLowerInvariant() switch
        {
            "nebula" => MinimapFeatureKind.AnomalyNebula,
            "asteroid_field" => MinimapFeatureKind.AnomalyAsteroid,
            "debris" => MinimapFeatureKind.AnomalyDebris,
            "ion_storm" => MinimapFeatureKind.AnomalyIonStorm,
            "wormhole_remnant" => MinimapFeatureKind.AnomalyWormhole,
            _ => MinimapFeatureKind.AnomalyDebris,
        };

    /// <summary>Raised when the player clicks the minimap. Argument is normalised 0..1.</summary>
    public event Action<Vector2>? Clicked;

    private bool _isHovered;

    /// <inheritdoc/>
    public override TooltipContent? GetTooltipContent()
    {
        if (!Visible || !_isHovered)
            return null;

        return new TooltipContent(
            Title: "Tactical Minimap",
            RoleLine: "Click to pan camera",
            Footprint: "White box = current viewport",
            BuildTime: "Diamonds = resources · crosses = anomalies");
    }

    /// <inheritdoc/>
    public override void UpdatePointerState(
        Vector2 pointerPosition, bool isPointerDown,
        Vector2 containerPosition, Vector2 containerSize)
    {
        _isHovered = Visible && Contains(pointerPosition, containerPosition, containerSize);
    }

    /// <summary>
    /// Returns normalised click position when <paramref name="screenPoint"/> hits the minimap.
    /// </summary>
    public bool TryGetClick(Vector2 screenPoint, Vector2 containerPosition, Vector2 containerSize,
        out Vector2 normalizedPosition)
    {
        normalizedPosition = Vector2.Zero;
        if (!Visible) return false;

        var (pos, size) = Resolve(containerPosition, containerSize);
        if (size.X <= 0f || size.Y <= 0f) return false;

        // Inclusive edge hits so border taps still pan the camera.
        if (screenPoint.X < pos.X || screenPoint.X > pos.X + size.X
            || screenPoint.Y < pos.Y || screenPoint.Y > pos.Y + size.Y)
            return false;

        Vector2 local = screenPoint - pos;
        normalizedPosition = new Vector2(
            Math.Clamp(local.X / size.X, 0f, 1f),
            Math.Clamp(local.Y / size.Y, 0f, 1f));
        return true;
    }

    /// <summary>Re-sync fog tint properties from the shared world palette.</summary>
    public void SyncFogPaletteFromWorld()
    {
        UnexploredColor = FogVisualPalette.ToColor(
            FogVisualPalette.UnexploredCore, FogVisualPalette.UnexploredAlpha);
        ExploredColor = FogVisualPalette.ToColor(
            FogVisualPalette.ExploredCore, FogVisualPalette.ExploredAlpha);
        VisibleColor = FogVisualPalette.ToColor(FogVisualPalette.ExploredVeil, 0.15f);
    }

    /// <inheritdoc/>
    public override bool HandlePointerTapped(
        Vector2 screenPoint, int button,
        Vector2 containerPosition, Vector2 containerSize)
    {
        if (!TryGetClick(screenPoint, containerPosition, containerSize, out Vector2 norm))
            return false;

        Clicked?.Invoke(norm);
        return true;
    }

    /// <inheritdoc/>
    protected override void OnDraw(IUIRenderer renderer, Vector2 position, Vector2 size)
    {
        renderer.DrawRect(position, size, BackgroundColor);

        if (FogOfWar != null)
        {
            DrawFog(renderer, position, size);
        }
        else
        {
            renderer.DrawRect(position, size, VisibleColor);
        }

        float minDim = Math.Min(size.X, size.Y);
        DrawFeatureMarkers(renderer, position, size, minDim, FeatureMarkers);

        float markerSize = Math.Max(6f, minDim * 0.045f);
        foreach (MinimapMarker marker in ObjectiveMarkers)
        {
            Vector2 markerPos = new(
                position.X + marker.NormalizedPosition.X * size.X - markerSize / 2f,
                position.Y + marker.NormalizedPosition.Y * size.Y - markerSize / 2f);
            renderer.DrawRect(markerPos, new Vector2(markerSize, markerSize), marker.Color);
            renderer.DrawRectOutline(markerPos, new Vector2(markerSize, markerSize),
                new Vector4(1f, 0.9f, 0.3f, 1f));
        }

        float dotSize = Math.Max(4f, minDim * 0.025f);
        foreach (MinimapUnit unit in Units)
        {
            Vector2 center = new(
                position.X + unit.NormalizedPosition.X * size.X,
                position.Y + unit.NormalizedPosition.Y * size.Y);
            if (unit.IsFriendly)
            {
                Vector2 dotPos = center - new Vector2(dotSize * 0.5f);
                renderer.DrawRect(dotPos, new Vector2(dotSize, dotSize), unit.Color);
                renderer.DrawRectOutline(dotPos, new Vector2(dotSize, dotSize),
                    new Vector4(1f, 1f, 1f, 0.55f));
            }
            else
            {
                DrawEnemyDiamondMarker(renderer, center, dotSize, unit.Color);
            }
        }

        var (camMin, camMax) = CameraViewport;
        Vector2 boxPos = position + camMin * size;
        Vector2 boxSize = (camMax - camMin) * size;
        renderer.DrawRectOutline(boxPos, boxSize, CameraBoxColor);
        renderer.DrawRectOutline(position, size, BorderColor);
    }

    private static void DrawFeatureMarkers(
        IUIRenderer renderer, Vector2 position, Vector2 size, float minDim,
        IReadOnlyList<MinimapFeatureMarker> markers)
    {
        foreach (MinimapFeatureMarker marker in markers)
        {
            Vector2 center = new(
                position.X + marker.NormalizedPosition.X * size.X,
                position.Y + marker.NormalizedPosition.Y * size.Y);

            switch (marker.Kind)
            {
                case MinimapFeatureKind.HarvestablePlanet:
                    DrawHarvestablePlanetMarker(renderer, center, minDim, marker.Color);
                    break;
                case MinimapFeatureKind.NeutralPlanet:
                    DrawNeutralPlanetMarker(renderer, center, minDim, marker.Color);
                    break;
                case MinimapFeatureKind.AnomalyNebula:
                case MinimapFeatureKind.AnomalyAsteroid:
                case MinimapFeatureKind.AnomalyDebris:
                case MinimapFeatureKind.AnomalyIonStorm:
                case MinimapFeatureKind.AnomalyWormhole:
                    DrawAnomalyMarker(renderer, center, minDim, marker.Color);
                    break;
                default:
                    DrawResourceDiamondMarker(renderer, center, minDim, marker.Color);
                    break;
            }
        }
    }

    private static void DrawEnemyDiamondMarker(IUIRenderer renderer, Vector2 center, float dotSize, Vector4 color)
    {
        float half = dotSize * 0.5f;
        float thickness = Math.Max(2f, dotSize * 0.38f);
        renderer.DrawRect(new Vector2(center.X - thickness * 0.5f, center.Y - half),
            new Vector2(thickness, half), color);
        renderer.DrawRect(new Vector2(center.X - thickness * 0.5f, center.Y),
            new Vector2(thickness, half), color);
        renderer.DrawRect(new Vector2(center.X - half, center.Y - thickness * 0.5f),
            new Vector2(half, thickness), color);
        renderer.DrawRect(new Vector2(center.X, center.Y - thickness * 0.5f),
            new Vector2(half, thickness), color);
        renderer.DrawRectOutline(
            new Vector2(center.X - half, center.Y - half),
            new Vector2(dotSize, dotSize),
            new Vector4(0f, 0f, 0f, 0.65f));
    }

    private static void DrawResourceDiamondMarker(
        IUIRenderer renderer, Vector2 center, float minDim, Vector4 color)
    {
        float markerSize = Math.Max(6f, minDim * 0.055f);
        float half = markerSize * 0.5f;
        float thickness = Math.Max(2f, markerSize * 0.42f);

        renderer.DrawRect(
            new Vector2(center.X - thickness * 0.5f, center.Y - half),
            new Vector2(thickness, half),
            color);
        renderer.DrawRect(
            new Vector2(center.X - thickness * 0.5f, center.Y),
            new Vector2(thickness, half),
            color);
        renderer.DrawRect(
            new Vector2(center.X - half, center.Y - thickness * 0.5f),
            new Vector2(half, thickness),
            color);
        renderer.DrawRect(
            new Vector2(center.X, center.Y - thickness * 0.5f),
            new Vector2(half, thickness),
            color);
    }

    private static void DrawHarvestablePlanetMarker(
        IUIRenderer renderer, Vector2 center, float minDim, Vector4 color)
    {
        float markerSize = Math.Max(7f, minDim * 0.07f);
        Vector2 markerPos = new(center.X - markerSize * 0.5f, center.Y - markerSize * 0.5f);
        Vector2 markerDims = new(markerSize, markerSize);
        renderer.DrawRect(markerPos, markerDims, color);
        renderer.DrawRectOutline(markerPos, markerDims, color with { W = 1f });
    }

    private static void DrawNeutralPlanetMarker(
        IUIRenderer renderer, Vector2 center, float minDim, Vector4 color)
    {
        float markerSize = Math.Max(6f, minDim * 0.065f);
        Vector2 markerPos = new(center.X - markerSize * 0.5f, center.Y - markerSize * 0.5f);
        renderer.DrawRectOutline(markerPos, new Vector2(markerSize, markerSize), color);
    }

    private static void DrawAnomalyMarker(
        IUIRenderer renderer, Vector2 center, float minDim, Vector4 color)
    {
        float markerSize = Math.Max(5f, minDim * 0.05f);
        float half = markerSize * 0.5f;
        float arm = Math.Max(2f, markerSize * 0.22f);

        renderer.DrawRect(
            new Vector2(center.X - arm * 0.5f, center.Y - half),
            new Vector2(arm, markerSize),
            color);
        renderer.DrawRect(
            new Vector2(center.X - half, center.Y - arm * 0.5f),
            new Vector2(markerSize, arm),
            color);
        renderer.DrawRectOutline(
            new Vector2(center.X - half, center.Y - half),
            new Vector2(markerSize, markerSize),
            color with { W = 1f });
    }

    private void DrawFog(IUIRenderer renderer, Vector2 position, Vector2 size)
    {
        Vector2i displayCells = FogDisplayCells;
        if (displayCells.X <= 0 || displayCells.Y <= 0)
            displayCells = GridSize;

        float cellW = size.X / displayCells.X;
        float cellH = size.Y / displayCells.Y;
        int sampleW = Math.Max(1, GridSize.X / displayCells.X);
        int sampleH = Math.Max(1, GridSize.Y / displayCells.Y);

        for (int row = 0; row < displayCells.Y; row++)
        {
            for (int col = 0; col < displayCells.X; col++)
            {
                int startX = col * sampleW;
                int startY = row * sampleH;
                FogState state = FogNebulaStyle.SampleTileState(
                    FogOfWar!, PlayerId, startX, startY, sampleW, sampleH, GridSize.X, GridSize.Y);

                if (state == FogState.Visible)
                    continue;

                Vector2 cellPos = new(position.X + col * cellW, position.Y + row * cellH);
                Vector2 cellSize = new(cellW + 0.5f, cellH + 0.5f);

                if (!UseNebulaFogStyle)
                {
                    Vector4 cellColor = state == FogState.Explored ? ExploredColor : UnexploredColor;
                    renderer.DrawRect(cellPos, cellSize, cellColor);
                    continue;
                }

                DrawNebulaFogCell(renderer, cellPos, cellSize, state, col, row);
            }
        }
    }

    private static void DrawNebulaFogCell(
        IUIRenderer renderer, Vector2 cellPos, Vector2 cellSize, FogState state, int col, int row)
    {
        IReadOnlyList<FogNebulaStyle.VeilLayer> layers = state == FogState.Unexplored
            ? FogNebulaStyle.UnexploredLayers(col, row)
            : FogNebulaStyle.ExploredLayers(col, row);

        for (int layerIndex = 0; layerIndex < layers.Count; layerIndex++)
        {
            FogNebulaStyle.VeilLayer layer = layers[layerIndex];
            Vector2 layerSize = new(cellSize.X * layer.SizeFactor.X, cellSize.Y * layer.SizeFactor.Y);
            Vector2 layerPos = cellPos + new Vector2(
                layer.OffsetFactor.X * cellSize.X,
                layer.OffsetFactor.Z * cellSize.Y);
            renderer.DrawRect(
                layerPos,
                layerSize,
                FogNebulaStyle.LayerColor(layer.Rgb, layer.Alpha, col, row, layerIndex));
        }
    }
}
