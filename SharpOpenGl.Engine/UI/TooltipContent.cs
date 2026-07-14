using SharpOpenGl.Engine.Build;

namespace SharpOpenGl.Engine.UI;

/// <summary>Immutable tooltip metadata shown on HUD hover.</summary>
public sealed record TooltipContent(
    string? Title = null,
    string? CostLine = null,
    string? Footprint = null,
    string? BuildTime = null,
    IReadOnlyList<string>? Prerequisites = null,
    string? LockReason = null,
    string? AffordReason = null)
{
    /// <summary>
    /// Ordered display lines with empty sections omitted.
    /// Prerequisites expand to one bullet line each.
    /// </summary>
    public IReadOnlyList<string> ToLines()
    {
        var lines = new List<string>();

        if (!string.IsNullOrWhiteSpace(Title))
            lines.Add(Title.Trim());

        if (!string.IsNullOrWhiteSpace(CostLine))
            lines.Add(CostLine.Trim());

        if (!string.IsNullOrWhiteSpace(Footprint))
            lines.Add(Footprint.Trim());

        if (!string.IsNullOrWhiteSpace(BuildTime))
            lines.Add(BuildTime.Trim());

        if (Prerequisites is { Count: > 0 })
        {
            foreach (string prereq in Prerequisites)
            {
                if (string.IsNullOrWhiteSpace(prereq))
                    continue;
                lines.Add($"• {prereq.Trim()}");
            }
        }

        if (!string.IsNullOrWhiteSpace(LockReason))
            lines.Add(LockReason.Trim());

        if (!string.IsNullOrWhiteSpace(AffordReason))
            lines.Add(AffordReason.Trim());

        return lines;
    }

    /// <summary>Build tooltip lines from a build-map entry view.</summary>
    public static TooltipContent FromBuildEntry(BuildMapEntryView entry)
    {
        string? buildTime = entry.BuildTime <= 0f
            ? "Build: instant"
            : $"Build: {entry.BuildTime:0}s";

        return new TooltipContent(
            Title: entry.Name,
            CostLine: $"E:{entry.EnergyCost} M:{entry.MineralsCost} D:{entry.DataCost} C:{entry.CrewCost}",
            Footprint: $"Footprint: {entry.FootprintCols}×{entry.FootprintRows}",
            BuildTime: buildTime,
            Prerequisites: entry.Prerequisites.Count > 0 ? entry.Prerequisites : null,
            LockReason: entry.LockReason,
            AffordReason: entry.AffordReason);
    }
}

/// <summary>Optional tooltip source for widgets probed during hover tracking.</summary>
public interface ITooltipProvider
{
    /// <summary>Tooltip payload for the current hover state, or null when none.</summary>
    TooltipContent? GetTooltipContent();
}