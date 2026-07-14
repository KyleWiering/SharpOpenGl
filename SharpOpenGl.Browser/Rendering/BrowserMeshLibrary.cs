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
    public int TeamAuraDisc { get; private set; }
    public int TeamAuraDiscCount { get; private set; }
    public int EngineTrail { get; private set; }
    public int EngineTrailCount { get; private set; }
    public int MoveTarget { get; private set; }
    public int MoveTargetCount { get; private set; }
    public int ParticleBuffer { get; private set; }

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
    private readonly Dictionary<int, (int meshId, int vertexCount)> _gridByStep = new();
    private readonly Dictionary<string, (int meshId, int vertexCount)> _articulatedParts = new(StringComparer.OrdinalIgnoreCase);

    public async Task InitializeAsync(WebGlRenderer renderer)
    {
        float[] hero = ProceduralMeshes.BuildRaceShip(RaceShipMeshes.DefaultRace, "hero_default", new Vector3(0.2f, 0.8f, 1.0f));
        Hero = await renderer.UploadMeshAsync(hero);
        HeroCount = ProceduralMeshes.VertexCount(hero);

        float[] fighter = ProceduralMeshes.BuildRaceShip(RaceShipMeshes.DefaultRace, "fighter_basic", new Vector3(0.4f, 1.0f, 0.4f));
        Fighter = await renderer.UploadMeshAsync(fighter);
        FighterCount = ProceduralMeshes.VertexCount(fighter);

        var gridColor = new Vector3(0.15f, 0.15f, 0.25f);
        foreach (int step in new[] { 1, 2, 5, 10, 20 })
        {
            float[] grid = ProceduralMeshes.BuildGrid(GridColumns, GridRows, GridCellSize, gridColor, step);
            int meshId = await renderer.UploadMeshAsync(grid);
            int count = ProceduralMeshes.VertexCount(grid);
            _gridByStep[step] = (meshId, count);
        }

        (Grid, GridCount) = _gridByStep[1];

        float[] ring = ProceduralMeshes.BuildSelectionRing(new Vector3(0f, 1f, 0f), 3f);
        SelectionRing = await renderer.UploadMeshAsync(ring);
        SelectionRingCount = ProceduralMeshes.VertexCount(ring);

        float[] auraDisc = ProceduralMeshes.BuildTeamAuraDisc();
        TeamAuraDisc = await renderer.UploadMeshAsync(auraDisc);
        TeamAuraDiscCount = ProceduralMeshes.VertexCount(auraDisc);

        float[] trail = ProceduralMeshes.BuildEngineTrail(new Vector3(1.0f, 0.6f, 0.1f), 2.5f);
        EngineTrail = await renderer.UploadMeshAsync(trail);
        EngineTrailCount = ProceduralMeshes.VertexCount(trail);

        float[] target = ProceduralMeshes.BuildMoveTarget(new Vector3(0f, 1f, 0.5f), 2f);
        MoveTarget = await renderer.UploadMeshAsync(target);
        MoveTargetCount = ProceduralMeshes.VertexCount(target);

        ParticleBuffer = await renderer.UploadMeshAsync(new float[4096 * 6]);

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

        var neutralTint = new Vector3(0.55f, 0.55f, 0.58f);
        await PreloadShipArticulationKeysAsync(renderer, neutralTint);
        await PreloadUtilityArticulationKeysAsync(renderer);
        await PreloadStationArticulationKeysAsync(renderer);
    }

    private async Task PreloadShipArticulationKeysAsync(WebGlRenderer renderer, Vector3 neutralTint)
    {
        foreach (string partKey in ArticulatedShipPartMeshes.AllPartKeys())
        {
            if (!ArticulatedShipPartMeshes.TryBuild(partKey, neutralTint, out float[] partVerts) || partVerts.Length == 0)
                continue;

            int partMeshId = await renderer.UploadMeshAsync(partVerts);
            _articulatedParts[partKey] = (partMeshId, ProceduralMeshes.VertexCount(partVerts));
        }
    }

    private async Task PreloadUtilityArticulationKeysAsync(WebGlRenderer renderer)
    {
        foreach (string hullKey in UtilityPartMeshes.MinerHullKeys)
        {
            string meshKey = UtilityPartMeshes.MiningArmMeshKey(hullKey);
            float[] armVerts = UtilityPartMeshes.BuildMiningArmMesh(hullKey);
            int armMeshId = await renderer.UploadMeshAsync(armVerts);
            _articulatedParts[meshKey] = (armMeshId, ProceduralMeshes.VertexCount(armVerts));
        }

        string repairKey = UtilityPartMeshes.RepairArmMeshKey("support_repair");
        float[] repairVerts = UtilityPartMeshes.BuildRepairArmMesh("support_repair");
        int repairMeshId = await renderer.UploadMeshAsync(repairVerts);
        _articulatedParts[repairKey] = (repairMeshId, ProceduralMeshes.VertexCount(repairVerts));
    }

    private async Task PreloadStationArticulationKeysAsync(WebGlRenderer renderer)
    {
        string defaultRace = RaceShipMeshes.DefaultRace;
        RaceVisualSchema.TryGetRace(defaultRace, out RaceVisualDefinition? race);
        race ??= RaceVisualSchema.AllRaces.FirstOrDefault()
            ?? new RaceVisualDefinition { Id = defaultRace };
        float stationScale = 7f * (0.85f + race.Modifiers.Superstructure * 0.3f);

        foreach (string partPrefix in ArticulatedStationPartMeshes.AllPartKeyPrefixes)
        {
            string meshKey = ArticulatedStationPartMeshes.ResolveMeshKey(partPrefix, defaultRace);
            if (!ArticulatedStationPartMeshes.TryBuild(partPrefix, defaultRace, stationScale, out float[] partVerts))
                continue;
            if (partVerts.Length == 0) continue;

            int partMeshId = await renderer.UploadMeshAsync(partVerts);
            _articulatedParts[meshKey] = (partMeshId, ProceduralMeshes.VertexCount(partVerts));
            _articulatedParts[partPrefix] = (partMeshId, ProceduralMeshes.VertexCount(partVerts));
        }
    }

    public bool TryGetArticulatedPart(string meshKey, out int meshId, out int vertexCount)
    {
        if (_articulatedParts.TryGetValue(meshKey, out var entry))
        {
            meshId = entry.meshId;
            vertexCount = entry.vertexCount;
            return true;
        }

        meshId = 0;
        vertexCount = 0;
#if DEBUG
        System.Diagnostics.Debug.WriteLine($"[BrowserMeshLibrary] Missing articulated mesh key: {meshKey}");
#endif
        return false;
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

    public (int meshId, int vertexCount) GetGridForHeight(float height, float minHeight, float maxHeight)
    {
        int step = GridRenderLod.ResolveLineStep(height, minHeight, maxHeight);
        return _gridByStep.TryGetValue(step, out var mesh) ? mesh : (Grid, GridCount);
    }
}