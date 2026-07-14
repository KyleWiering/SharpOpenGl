namespace SharpOpenGl;

/// <summary>CLI options for <c>--screenshot</c> fleet-gallery per-hull focus (set from Program before EngineWindow starts).</summary>
public static class ScreenshotLaunchOptions
{
    /// <summary>Gallery ship id (e.g. fighter_basic) for per-race zone screenshot framing; null keeps legacy dual-zone framing.</summary>
    public static string? GalleryHull { get; set; }

    /// <summary>Gallery race id (e.g. vesper, korath) for zone focus; defaults to vesper when unset.</summary>
    public static string? GalleryRace { get; set; }

    /// <summary>Frame Vesper zone-1 medium combat row (corvette through bomber, indices 5–8).</summary>
    public static bool MediumCombatRow { get; set; }

    /// <summary>Frame a shipyard exit pad with a mid-build hull preview (~40–60%).</summary>
    public static bool ShipyardPreviewMidBuild { get; set; }

    /// <summary>Frame Terran zone-0 medium combat row + station turret row for Round 2 articulation captures.</summary>
    public static bool Round2ArticulationGallery { get; set; }
}