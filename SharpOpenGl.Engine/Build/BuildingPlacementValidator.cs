using OpenTK.Mathematics;
using SharpOpenGl.Engine.Economy;
using SharpOpenGl.Engine.ECS;
using SharpOpenGl.Engine.Entities;
using SharpOpenGl.Engine.Grid;

namespace SharpOpenGl.Engine.Build;

/// <summary>Reasons a building placement may be rejected.</summary>
public enum PlacementFailureReason
{
    None,
    UnknownDefinition,
    Locked,
    CannotAfford,
    SupplyCap,
    OutOfBounds,
    ImpassableTerrain,
    CellOccupied,
    ResourceBlocked,
}

/// <summary>Player-facing copy for placement validation failures.</summary>
public static class PlacementFailureReasonExtensions
{
    /// <summary>Short HUD message for a validation failure reason.</summary>
    public static string ToPlayerMessage(this PlacementFailureReason reason) => reason switch
    {
        PlacementFailureReason.None => string.Empty,
        PlacementFailureReason.UnknownDefinition => "Unknown structure",
        PlacementFailureReason.Locked => "Requires prerequisite structure",
        PlacementFailureReason.CannotAfford => "Insufficient resources",
        PlacementFailureReason.SupplyCap => "Supply cap reached",
        PlacementFailureReason.OutOfBounds => "Outside build area",
        PlacementFailureReason.ImpassableTerrain => "Impassable terrain",
        PlacementFailureReason.CellOccupied => "Cell occupied",
        PlacementFailureReason.ResourceBlocked => "Resource node blocked",
        _ => "Cannot place here",
    };

    /// <summary>HUD message when the structure builder is too far from the cursor.</summary>
    public static string ToBuilderRangeMessage(float rangeMeters) =>
        $"Builder out of range ({rangeMeters:0}m)";

    /// <summary>HUD status while hovering a valid placement location.</summary>
    public static string ValidPlacementStatus => "Click to place";

    /// <summary>HUD toast after a structure is placed successfully.</summary>
    public static string BuildPlacedMessage(string displayName) =>
        $"{displayName} — Placed";

    /// <summary>
    /// Player-facing placement hint for ghost preview and HUD band.
    /// Range failures take priority over cell/terrain validation.
    /// </summary>
    public static string BuildStatusMessage(
        PlacementValidationResult validation,
        bool inRange,
        string rangeReason)
    {
        if (!inRange)
            return rangeReason;

        if (validation.IsValid)
            return ValidPlacementStatus;

        return validation.Reason.ToPlayerMessage();
    }
}

/// <summary>Outcome of a placement validation check.</summary>
public readonly struct PlacementValidationResult
{
    public bool IsValid { get; init; }
    public PlacementFailureReason Reason { get; init; }

    public static PlacementValidationResult Ok() =>
        new() { IsValid = true, Reason = PlacementFailureReason.None };

    public static PlacementValidationResult Fail(PlacementFailureReason reason) =>
        new() { IsValid = false, Reason = reason };
}

/// <summary>
/// Validates structure placement against prerequisites, economy, supply, terrain, and occupancy.
/// </summary>
public static class BuildingPlacementValidator
{
    public static PlacementValidationResult Validate(
        GridSystem grid,
        World world,
        int playerId,
        EntityDefinition def,
        Vector3 worldPos,
        BuildMapCatalog catalog,
        ResourceManager resources,
        SupplySystem? supply)
    {
        if (def == null)
            return PlacementValidationResult.Fail(PlacementFailureReason.UnknownDefinition);

        var builtTypes = BuildingFootprint.GetBuiltTypes(world, playerId);
        var prerequisites = catalog.GetPrerequisites(def.Id);
        if (!BuildMapCatalog.IsUnlocked(prerequisites, builtTypes))
            return PlacementValidationResult.Fail(PlacementFailureReason.Locked);

        var player = resources.GetPlayer(playerId);
        if (!BuildMapCatalog.CanAfford(def, player))
            return PlacementValidationResult.Fail(PlacementFailureReason.CannotAfford);

        int crew = def.Cost?.Crew ?? 0;
        if (!BuildMapCatalog.HasSupplyHeadroom(supply, playerId, crew))
            return PlacementValidationResult.Fail(PlacementFailureReason.SupplyCap);

        var (cols, rows) = BuildingFootprint.GetSize(def.Components?.Building?.Footprint);
        foreach (var (x, y) in BuildingFootprint.EnumerateCells(grid, worldPos, cols, rows))
        {
            if (!grid.InBounds(x, y))
                return PlacementValidationResult.Fail(PlacementFailureReason.OutOfBounds);

            GridCell? cell = grid.GetCell(x, y);
            if (cell == null)
                return PlacementValidationResult.Fail(PlacementFailureReason.OutOfBounds);

            if (!cell.IsPassable)
                return PlacementValidationResult.Fail(PlacementFailureReason.ImpassableTerrain);

            if (cell.Occupant != Entity.Null)
                return PlacementValidationResult.Fail(PlacementFailureReason.CellOccupied);

            if (cell.HasResource)
                return PlacementValidationResult.Fail(PlacementFailureReason.ResourceBlocked);
        }

        return PlacementValidationResult.Ok();
    }
}