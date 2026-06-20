using OpenTK.Mathematics;
using SharpOpenGl.Engine.Rendering;

namespace SharpOpenGl.Browser.Rendering;

/// <summary>GPU mesh handles for browser gameplay — same procedural geometry as desktop.</summary>
public sealed class BrowserMeshLibrary
{
    public int Hero { get; private set; }
    public int HeroCount { get; private set; }
    public int Fighter { get; private set; }
    public int FighterCount { get; private set; }
    public int Grid { get; private set; }
    public int GridCount { get; private set; }
    public int SelectionRing { get; private set; }
    public int SelectionRingCount { get; private set; }
    public int EngineTrail { get; private set; }
    public int EngineTrailCount { get; private set; }
    public int MoveTarget { get; private set; }
    public int MoveTargetCount { get; private set; }

    public int LaserBolt { get; private set; }
    public int LaserBoltCount { get; private set; }
    public int Beam { get; private set; }
    public int BeamCount { get; private set; }
    public int Torpedo { get; private set; }
    public int TorpedoCount { get; private set; }
    public int Rocket { get; private set; }
    public int RocketCount { get; private set; }
    public int Bomb { get; private set; }
    public int BombCount { get; private set; }
    public int EnergyPulse { get; private set; }
    public int EnergyPulseCount { get; private set; }
    public int Wave { get; private set; }
    public int WaveCount { get; private set; }

    private const int GridColumns = 200;
    private const int GridRows = 200;
    private const float GridCellSize = 10f;

    public async Task InitializeAsync(WebGlRenderer renderer)
    {
        float[] hero = ProceduralMeshes.BuildShipMesh(new Vector3(0.2f, 0.8f, 1.0f), 3f);
        Hero = await renderer.UploadMeshAsync(hero);
        HeroCount = ProceduralMeshes.VertexCount(hero);

        float[] fighter = ProceduralMeshes.BuildShipMesh(new Vector3(0.4f, 1.0f, 0.4f), 1.5f);
        Fighter = await renderer.UploadMeshAsync(fighter);
        FighterCount = ProceduralMeshes.VertexCount(fighter);

        float[] grid = ProceduralMeshes.BuildGrid(GridColumns, GridRows, GridCellSize,
            new Vector3(0.15f, 0.15f, 0.25f));
        Grid = await renderer.UploadMeshAsync(grid);
        GridCount = ProceduralMeshes.VertexCount(grid);

        float[] ring = ProceduralMeshes.BuildSelectionRing(new Vector3(0f, 1f, 0f), 3f);
        SelectionRing = await renderer.UploadMeshAsync(ring);
        SelectionRingCount = ProceduralMeshes.VertexCount(ring);

        float[] trail = ProceduralMeshes.BuildEngineTrail(new Vector3(1.0f, 0.6f, 0.1f), 2.5f);
        EngineTrail = await renderer.UploadMeshAsync(trail);
        EngineTrailCount = ProceduralMeshes.VertexCount(trail);

        float[] target = ProceduralMeshes.BuildMoveTarget(new Vector3(0f, 1f, 0.5f), 2f);
        MoveTarget = await renderer.UploadMeshAsync(target);
        MoveTargetCount = ProceduralMeshes.VertexCount(target);

        float[] laser = ProceduralMeshes.BuildLaserBolt(new Vector3(1f, 0.4f, 0.3f));
        LaserBolt = await renderer.UploadMeshAsync(laser);
        LaserBoltCount = ProceduralMeshes.VertexCount(laser);

        float[] beam = ProceduralMeshes.BuildBeamStreak(new Vector3(0.5f, 0.9f, 1f));
        Beam = await renderer.UploadMeshAsync(beam);
        BeamCount = ProceduralMeshes.VertexCount(beam);

        float[] torpedo = ProceduralMeshes.BuildTorpedo(new Vector3(0.8f, 0.85f, 0.9f));
        Torpedo = await renderer.UploadMeshAsync(torpedo);
        TorpedoCount = ProceduralMeshes.VertexCount(torpedo);

        float[] rocket = ProceduralMeshes.BuildRocket(new Vector3(1f, 0.7f, 0.2f));
        Rocket = await renderer.UploadMeshAsync(rocket);
        RocketCount = ProceduralMeshes.VertexCount(rocket);

        float[] bomb = ProceduralMeshes.BuildBomb(new Vector3(0.95f, 0.5f, 0.15f));
        Bomb = await renderer.UploadMeshAsync(bomb);
        BombCount = ProceduralMeshes.VertexCount(bomb);

        float[] pulse = ProceduralMeshes.BuildEnergyPulse(new Vector3(0.6f, 0.4f, 1f));
        EnergyPulse = await renderer.UploadMeshAsync(pulse);
        EnergyPulseCount = ProceduralMeshes.VertexCount(pulse);

        float[] wave = ProceduralMeshes.BuildWaveRing(new Vector3(0.4f, 1f, 0.85f));
        Wave = await renderer.UploadMeshAsync(wave);
        WaveCount = ProceduralMeshes.VertexCount(wave);
    }

    public bool TryResolveProjectileMesh(string meshKey, out int meshId, out int vertexCount)
    {
        meshId = 0;
        vertexCount = 0;
        switch (meshKey)
        {
            case "projectile/laser_bolt": meshId = LaserBolt; vertexCount = LaserBoltCount; return true;
            case "projectile/beam": meshId = Beam; vertexCount = BeamCount; return true;
            case "projectile/torpedo": meshId = Torpedo; vertexCount = TorpedoCount; return true;
            case "projectile/rocket": meshId = Rocket; vertexCount = RocketCount; return true;
            case "projectile/bomb": meshId = Bomb; vertexCount = BombCount; return true;
            case "projectile/energy_pulse": meshId = EnergyPulse; vertexCount = EnergyPulseCount; return true;
            case "projectile/wave": meshId = Wave; vertexCount = WaveCount; return true;
            default: return false;
        }
    }

    public static float MapWorldSize => GridColumns * GridCellSize;
}