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

    /// <summary>
    /// 0-based fog player index for minimap/HUD display when capturing.
    /// All <see cref="FogOfWar.PlayerCount"/> layers are serialized regardless of this value.
    /// </summary>
    public required int FogPlayerId { get; init; }
    public required float CameraX { get; init; }
    public required float CameraY { get; init; }
    public required float CameraZoom { get; init; }
    public required string SlotName { get; init; }

    /// <summary>True when capturing a menu sandbox session (no active mission).</summary>
    public bool IsSandboxSession { get; init; }

    /// <summary>Deterministic procedural seed for chunked world generation.</summary>
    public int ProceduralMapSeed { get; init; }

    /// <summary>Original sandbox seed text for display (optional).</summary>
    public string? SandboxSeedText { get; init; }
}