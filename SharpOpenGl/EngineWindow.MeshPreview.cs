using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using SharpOpenGl.Engine.Rendering;
using SharpOpenGl.Rendering;

namespace SharpOpenGl;

/// <summary>CLI options for <c>--mesh-preview</c> capture (set from Program before EngineWindow starts).</summary>
public static class MeshPreviewLaunchOptions
{
    public static bool Enabled { get; set; }
    public static string Race { get; set; } = "vesper";
    public static string Hull { get; set; } = "fighter_basic";
    public static string Category { get; set; } = ModelMeshSource.KindShip;
    public static string ModelId => Hull;
}

public partial class EngineWindow
{
    // Mesh-preview framing infra — hull-class envelopes only; do NOT add per-race camDist/scaleBoost branches.
    //
    // MeshPreviewCameraPullback: global camera-distance multiplier (raise to zoom out).
    // MeshPreviewScaleFactor: global model-scale multiplier (lower to shrink foreground).
    //
    // Screenshot target: ~4% foreground per panel (ships), ~5% (stations) — ModelQualityScorer.
    //
    // Ship envelope (bucket from FleetGalleryLayout.AllShipIds, size from RaceVisualSchema.ResolveHullProfile):
    //   size    = max(hull.Size, 0.85)
    //   camDist = (bucket.CamIntercept + bucket.CamSlope * size) * MeshPreviewCameraPullback
    //   scale   = clamp(bucket.ScaleK / size, bucket.ScaleMin, bucket.ScaleMax) * MeshPreviewScaleFactor
    //
    // Station envelope (tall vs short from FleetGalleryLayout.AllBaseIds):
    //   size    = tall ? 9.0 : 6.5
    //   camDist = (2.6 + size * 0.26) * MeshPreviewCameraPullback
    //   scale   = clamp((tall ? 25f : 20.5f) / size, 1.12f, 1.62f) * MeshPreviewScaleFactor
    private const float MeshPreviewCameraPullback = 2.8f;
    private const float MeshPreviewScaleFactor = 0.38f;
    // Oblique top-down quadrants — panel 1 is the RTS gameplay angle (see mesh-improvement-loop rubric).
    private static readonly float[] MeshPreviewYawAngles = { 35f, 125f, 215f };
    private static readonly float[] MeshPreviewStationYawAngles = { 18f, 105f, 198f };

    private readonly bool _meshPreviewMode = MeshPreviewLaunchOptions.Enabled;
    private readonly string _meshPreviewRace = MeshPreviewLaunchOptions.Race;
    private readonly string _meshPreviewCategory = ModelMeshSource.NormalizeKind(MeshPreviewLaunchOptions.Category);
    private readonly string _meshPreviewModel = MeshPreviewLaunchOptions.ModelId;
    private int _meshPreviewVao;
    private int _meshPreviewVertCount;
    private readonly List<ParticleEmitter> _meshPreviewEngineEmitters = [];
    private IReadOnlyList<Vector3> _meshPreviewNozzleOffsets = [];

    private void InitializeMeshPreview()
    {
        EnsureMeshAssets();
        string meshKey = ModelMeshSource.ResolveMeshKey(_meshPreviewCategory, _meshPreviewModel, _meshPreviewRace);
        string fallback = _meshPreviewCategory switch
        {
            ModelMeshSource.KindStation => "meshes/shared/default_base.obj",
            ModelMeshSource.KindObject => "meshes/shared/default_projectile.obj",
            _ => "meshes/shared/default_ship.obj",
        };

        float[] vertices = ModelMeshSource.Build(_meshPreviewCategory, _meshPreviewModel, _meshPreviewRace);
        var uploaded = MeshBuilder.UploadProcedural(vertices);
        _meshRegistry!.Register(meshKey, uploaded);
        _objMeshCache[meshKey] = (uploaded.vao, uploaded.vbo, uploaded.vertexCount);

        var entry = _meshRegistry!.GetOrFallback(meshKey, fallback);
        _meshPreviewVao = entry?.Vao ?? 0;
        _meshPreviewVertCount = entry?.VertexCount ?? 0;

        InitializeMeshPreviewEngineEmitters();

        Console.WriteLine(
            $"[MeshPreview] category={_meshPreviewCategory} race={_meshPreviewRace} model={_meshPreviewModel} key={meshKey} verts={_meshPreviewVertCount} pullback={MeshPreviewCameraPullback}x");
    }

    private void InitializeMeshPreviewEngineEmitters()
    {
        _meshPreviewEngineEmitters.Clear();
        _meshPreviewNozzleOffsets = [];

        if (_meshPreviewCategory != ModelMeshSource.KindShip) return;
        if (!_meshPreviewRace.Equals("terran", StringComparison.OrdinalIgnoreCase)) return;

        string hullKey = RaceVisualSchema.ResolveHullKey(_meshPreviewModel);
        var (len, wid, hgt) = TerranEngineNozzleLayout.ResolveHullDimensions(_meshPreviewModel);
        int engineCount = RaceVisualSchema.TryGetRace("terran", out var race)
            ? Math.Max(2, race.Modifiers.EngineCount)
            : 2;

        _meshPreviewNozzleOffsets = TerranEngineNozzleLayout.ComputeLocalOffsets(
            hullKey, len, wid, hgt, engineCount);

        float emitScale = TerranEngineNozzleLayout.ResolveEmitRateScale(hullKey);
        foreach (Vector3 _ in _meshPreviewNozzleOffsets)
        {
            var emitter = ParticleEffects.CreateEngineTrail(Vector3.Zero, -Vector3.UnitZ);
            emitter.EmitRate = TerranEngineNozzleLayout.CapEmitRate(emitter, emitter.EmitRate * emitScale);
            emitter.IsEmitting = true;
            _meshPreviewEngineEmitters.Add(emitter);
        }
    }

    private void RenderMeshPreview(Matrix4 projection, float deltaTime)
    {
        if (_meshPreviewVao == 0) return;

        EnsureOpenGlRenderer();

        int fullW = Size.X;
        int fullH = Size.Y;
        float[] angles = _meshPreviewCategory == ModelMeshSource.KindStation
            ? MeshPreviewStationYawAngles
            : MeshPreviewYawAngles;
        int panelW = Math.Max(fullW / angles.Length, 1);
        var framing = ResolveMeshPreviewFraming();

        for (int i = 0; i < angles.Length; i++)
        {
            int x = i * panelW;
            int w = i == angles.Length - 1 ? fullW - x : panelW;
            RenderMeshPreviewPanel(projection, angles[i], framing, x, 0, w, fullH, deltaTime);
        }

        OpenTK.Graphics.OpenGL4.GL.Viewport(0, 0, fullW, fullH);
    }

    private void RenderMeshPreviewPanel(
        Matrix4 projection,
        float yawDegrees,
        MeshPreviewFraming framing,
        int viewportX,
        int viewportY,
        int viewportW,
        int viewportH,
        float deltaTime)
    {
        OpenTK.Graphics.OpenGL4.GL.Viewport(viewportX, viewportY, viewportW, viewportH);

        var view = Matrix4.LookAt(
            new Vector3(framing.CamDist, framing.CamHeight, framing.CamDist),
            new Vector3(0f, framing.LookAtY, 0f),
            Vector3.UnitY);

        float yaw = MathHelper.DegreesToRadians(yawDegrees);
        Matrix4 model =
            Matrix4.CreateScale(framing.Scale) *
            Matrix4.CreateRotationY(yaw) *
            Matrix4.CreateTranslation(0f, framing.TranslateY, 0f);

        int raceTexture = _meshPreviewCategory == ModelMeshSource.KindObject
            ? 0
            : RaceTextureIndex.Resolve(_meshPreviewRace);
        _openGlRenderer!.BeginFrame(projection, view);
        _openGlRenderer.DrawMesh(
            _meshPreviewVao,
            _meshPreviewVertCount,
            model,
            Vector4.Zero,
            4,
            raceTexture,
            framing.ColorBoost);
        RenderMeshPreviewEngineTrails(model, deltaTime);
        _openGlRenderer.EndFrame();
    }

    private void RenderMeshPreviewEngineTrails(Matrix4 model, float deltaTime)
    {
        if (_meshPreviewEngineEmitters.Count == 0 || _meshPreviewNozzleOffsets.Count == 0) return;

        int count = Math.Min(_meshPreviewEngineEmitters.Count, _meshPreviewNozzleOffsets.Count);
        for (int i = 0; i < count; i++)
        {
            Vector3 worldOffset = Vector3.TransformPosition(_meshPreviewNozzleOffsets[i], model);
            var emitter = _meshPreviewEngineEmitters[i];
            emitter.Origin = worldOffset;
            Vector3 exhaust = Vector3.TransformNormal(-Vector3.UnitZ, model);
            if (exhaust.LengthSquared > 1e-6f)
                emitter.BaseVelocity = Vector3.Normalize(exhaust) * 3f;
            emitter.Update(deltaTime);
        }

        int totalPoints = 0;
        int floatIndex = 0;
        for (int i = 0; i < count; i++)
        {
            int written = _meshPreviewEngineEmitters[i].WriteColoredPoints(_engineTrailParticleBuffer, floatIndex);
            if (written == 0) continue;
            totalPoints += written;
            floatIndex += written * 6;
        }

        if (totalPoints == 0) return;

        GL.UseProgram(_shaderProgram);
        GL.BindBuffer(BufferTarget.ArrayBuffer, _particleVbo);
        GL.BufferSubData(
            BufferTarget.ArrayBuffer,
            IntPtr.Zero,
            floatIndex * sizeof(float),
            _engineTrailParticleBuffer);

        var identity = Matrix4.Identity;
        GL.UniformMatrix4(_uniformModel, false, ref identity);
        GL.Uniform4(_uniformColor, Vector4.Zero);
        GL.Uniform1(_uniformPointSize, 5f);
        GL.BindVertexArray(_particleVao);
        GL.DrawArrays(PrimitiveType.Points, 0, totalPoints);
        GL.BindVertexArray(0);
    }

    private readonly record struct MeshPreviewFraming(
        float CamDist, float CamHeight, float Scale, float LookAtY, float TranslateY, Vector3 ColorBoost);

    private enum MeshPreviewShipBucket
    {
        SmallCraft,
        Strike,
        Medium,
        Capital,
        Hero,
        Utility,
        UtilityWide,
    }

    private static MeshPreviewShipBucket ClassifyShipBucket(string hullId) =>
        hullId switch
        {
            "scout_light" or "interceptor_mk2" or "drone_swarm" => MeshPreviewShipBucket.SmallCraft,
            "destroyer_assault" or "cruiser_heavy" or "carrier_command" or "dreadnought" =>
                MeshPreviewShipBucket.Capital,
            "freighter_bulk" or "transport_cargo" => MeshPreviewShipBucket.UtilityWide,
            "miner_basic" or "miner_eva" or "miner_tractor" or "support_repair" => MeshPreviewShipBucket.Utility,
            "hero_default" => MeshPreviewShipBucket.Hero,
            "frigate_strike" or "gunship_heavy" or "bomber_heavy" => MeshPreviewShipBucket.Medium,
            _ => MeshPreviewShipBucket.Strike,
        };

    private static bool IsTallStation(string stationId) =>
        stationId is "command_center" or "shipyard_large" or "repair_bay";

    private static float ResolveStationEffectiveSize(string stationId) =>
        IsTallStation(stationId) ? 9f : 6.5f;

    private static (float CamIntercept, float CamSlope, float ScaleK, float ScaleMin, float ScaleMax)
        ShipBucketEnvelope(MeshPreviewShipBucket bucket) =>
        bucket switch
        {
            MeshPreviewShipBucket.SmallCraft => (1.4f, 0.22f, 20f, 4.0f, 7.5f),
            MeshPreviewShipBucket.Strike => (1.5f, 0.24f, 21f, 4.2f, 7.8f),
            MeshPreviewShipBucket.Medium => (1.8f, 0.30f, 24f, 3.5f, 6.5f),
            MeshPreviewShipBucket.Capital => (2.2f, 0.42f, 16.5f, 2.0f, 3.6f),
            MeshPreviewShipBucket.Hero => (1.9f, 0.28f, 22f, 3.8f, 6.8f),
            MeshPreviewShipBucket.Utility => (2.4f, 0.42f, 26f, 2.2f, 4.2f),
            MeshPreviewShipBucket.UtilityWide => (2.5f, 0.55f, 28f, 2.2f, 4.2f),
            _ => (1.6f, 0.26f, 21f, 4.0f, 7.0f),
        };

    private static float ShipBucketCamHeight(MeshPreviewShipBucket bucket, float size) =>
        bucket switch
        {
            MeshPreviewShipBucket.SmallCraft => 2.4f + size * 0.15f,
            MeshPreviewShipBucket.Capital => 4.5f + size * 0.12f,
            MeshPreviewShipBucket.UtilityWide => 5.0f + size * 0.08f,
            MeshPreviewShipBucket.Utility => 4.2f + size * 0.10f,
            MeshPreviewShipBucket.Hero => 4.8f,
            _ => 3.2f + size * 0.20f,
        };

    private static float ShipBucketColorBoost(MeshPreviewShipBucket bucket) =>
        bucket switch
        {
            MeshPreviewShipBucket.Capital => 1.12f,
            MeshPreviewShipBucket.Utility or MeshPreviewShipBucket.UtilityWide => 1.14f,
            MeshPreviewShipBucket.SmallCraft => 1.06f,
            MeshPreviewShipBucket.Hero => 1.10f,
            _ => 1.08f,
        };

    private MeshPreviewFraming ResolveMeshPreviewFraming()
    {
        if (_meshPreviewCategory == ModelMeshSource.KindObject)
        {
            bool isPlanet = _meshPreviewModel is "neutral_planet" or "harvestable_planet";
            float objectSize = isPlanet ? 12f : 5f;
            float camDist = (4.2f + objectSize * 0.35f) * MeshPreviewCameraPullback;
            float scale = Math.Clamp((isPlanet ? 14f : 9f) / objectSize, 0.7f, 1.4f) * MeshPreviewScaleFactor;
            return new MeshPreviewFraming(camDist, 4.5f, scale, 0f, 0f, new Vector3(1.12f, 1.12f, 1.14f));
        }

        if (_meshPreviewCategory == ModelMeshSource.KindStation)
        {
            bool isTall = IsTallStation(_meshPreviewModel);
            float size = ResolveStationEffectiveSize(_meshPreviewModel);
            float camDist = (2.6f + size * 0.26f) * MeshPreviewCameraPullback;
            float scale = Math.Clamp((isTall ? 25f : 20.5f) / size, 1.12f, 1.62f) * MeshPreviewScaleFactor;
            float camHeight = isTall ? 4.8f + size * 0.06f : 3.8f + size * 0.05f;
            return new MeshPreviewFraming(camDist, camHeight, scale, 0.28f, 0.02f, new Vector3(1.22f, 1.22f, 1.26f));
        }

        return ResolveShipMeshPreviewFraming();
    }

    private MeshPreviewFraming ResolveShipMeshPreviewFraming()
    {
        var hull = RaceVisualSchema.ResolveHullProfile(_meshPreviewModel);
        float size = Math.Max(hull.Size, 0.85f);
        var bucket = ClassifyShipBucket(_meshPreviewModel);
        var envelope = ShipBucketEnvelope(bucket);

        float camDist = (envelope.CamIntercept + envelope.CamSlope * size) * MeshPreviewCameraPullback;
        float scale = Math.Clamp(envelope.ScaleK / size, envelope.ScaleMin, envelope.ScaleMax) * MeshPreviewScaleFactor;
        float camHeight = ShipBucketCamHeight(bucket, size);
        float colorBoost = ShipBucketColorBoost(bucket);

        return new MeshPreviewFraming(camDist, camHeight, scale, 0.18f, 0.20f,
            new Vector3(colorBoost, colorBoost, colorBoost * 1.02f));
    }
}