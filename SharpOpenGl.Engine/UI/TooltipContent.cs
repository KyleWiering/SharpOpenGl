using SharpOpenGl.Engine.Build;

namespace SharpOpenGl.Engine.UI;

/// <summary>Immutable tooltip metadata shown on HUD hover.</summary>
public sealed record TooltipContent(
    string? Title = null,
    string? CostLine = null,
    string? Footprint = null,
    string? BuildTime = null,
    string? RoleLine = null,
    string? CategoryLockHint = null,
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

        if (!string.IsNullOrWhiteSpace(RoleLine))
            lines.Add(RoleLine.Trim());

        if (!string.IsNullOrWhiteSpace(CategoryLockHint))
            lines.Add(CategoryLockHint.Trim());

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

        string? roleLine = string.IsNullOrWhiteSpace(entry.CategoryName)
            ? null
            : $"Category: {entry.CategoryName.Trim()}";

        string? prerequisiteChain = FormatPrerequisiteChain(entry);

        return new TooltipContent(
            Title: entry.Name,
            CostLine: $"E:{entry.EnergyCost} M:{entry.MineralsCost} D:{entry.DataCost} C:{entry.CrewCost}",
            Footprint: $"Footprint: {entry.FootprintCols}×{entry.FootprintRows}",
            BuildTime: buildTime,
            RoleLine: roleLine,
            Prerequisites: prerequisiteChain == null && entry.Prerequisites.Count > 0
                ? entry.Prerequisites
                : null,
            LockReason: prerequisiteChain ?? entry.LockReason,
            AffordReason: entry.AffordReason);
    }

    internal static string? FormatPrerequisiteChain(BuildMapEntryView entry)
    {
        if (entry.IsUnlocked || entry.Prerequisites.Count == 0)
            return null;

        bool isCapstone = string.Equals(entry.CategoryId, "capstone", StringComparison.OrdinalIgnoreCase);
        bool showProgress = isCapstone
            || entry.PrerequisiteTotalCount > 1
            || entry.PrerequisiteMetCount > 0;

        string chain = string.Join(" → ", entry.Prerequisites);
        if (showProgress && entry.PrerequisiteTotalCount > 0)
        {
            return $"Prerequisites: {entry.PrerequisiteMetCount}/{entry.PrerequisiteTotalCount} met — {chain}";
        }

        return $"Prerequisite chain: {chain}";
    }

    /// <summary>Tooltip for abbreviated HUD command buttons.</summary>
    public static TooltipContent FromCommandButton(string label, string hint)
    {
        string title = string.IsNullOrWhiteSpace(hint) ? label.Trim() : hint.Trim();
        string? roleLine = !string.IsNullOrWhiteSpace(label)
            && !string.IsNullOrWhiteSpace(hint)
            && !string.Equals(label.Trim(), hint.Trim(), StringComparison.OrdinalIgnoreCase)
            ? $"Button: {label.Trim()}"
            : null;

        return new TooltipContent(Title: title, RoleLine: roleLine);
    }
}

/// <summary>Optional tooltip source for widgets probed during hover tracking.</summary>
public interface ITooltipProvider
{
    /// <summary>Tooltip payload for the current hover state, or null when none.</summary>
    TooltipContent? GetTooltipContent();
}