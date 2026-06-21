using SharpOpenGl.Engine.ECS;
using SharpOpenGl.Engine.Economy;
using SharpOpenGl.Engine.Grid;
using SharpOpenGl.Engine.Missions;

namespace SharpOpenGl.Engine.Persistence;

/// <summary>
/// Live gameplay state required to build a <see cref="SaveData"/> snapshot.
/// </summary>
public sealed class WorldSaveContext
{
    public required World World { get; init; }
    public required ResourceManager ResourceManager { get; init; }
    public MissionState? MissionState { get; init; }
    public required GridSystem GridSystem { get; init; }
    public required FogOfWar FogOfWar { get; init; }
    public required int FogPlayerId { get; init; }
    public required float CameraX { get; init; }
    public required float CameraY { get; init; }
    public required float CameraZoom { get; init; }
    public required string SlotName { get; init; }
}