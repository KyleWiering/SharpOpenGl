using OpenTK.Mathematics;
using SharpOpenGl.Engine.ECS;

namespace SharpOpenGl.Engine.Rendering;

/// <summary>
/// Procedural utility-part meshes (mining arms, etc.) keyed for <see cref="RenderComponent.MeshKey"/>.
/// </summary>
public static class UtilityPartMeshes
{
    /// <summary>Miner hull ids that receive a <see cref="ArticulatedPartType.MiningArm"/> child.</summary>
    public static readonly string[] MinerHullKeys = ["miner_basic", "miner_tractor", "miner_eva"];

    private static readonly Dictionary<string, (int MeshId, int VertexCount)> GpuCache =
        new(StringComparer.OrdinalIgnoreCase);

    private static Func<float[], (int MeshId, int VertexCount)>? _gpuUploader;

    /// <summary>Stable mesh key consumed by desktop and browser render paths.</summary>
    public static string MiningArmMeshKey(string hullKey) =>
        $"utility/mining_arm/{NormalizeMinerHullId(hullKey)}";

    /// <summary>Stable mesh key for support repair tool arms.</summary>
    public static string RepairArmMeshKey(string hullKey = "support_repair") =>
        $"utility/repair_arm/{NormalizeRepairHullId(hullKey)}";

    /// <summary>Registers the desktop GPU upload delegate (no-op in headless tests).</summary>
    public static void ConfigureGpuUploader(Func<float[], (int MeshId, int VertexCount)> uploader) =>
        _gpuUploader = uploader;

    /// <summary>
    /// Small procedural mining-arm segment (pedestal + truss + bucket) keyed per miner hull.
    /// </summary>
    public static float[] BuildMiningArmMesh(string hullKey)
    {
        hullKey = NormalizeMinerHullId(hullKey);
        float scale = hullKey switch
        {
            "miner_tractor" => 1.12f,
            "miner_eva" => 0.82f,
            _ => 1.0f,
        };

        Vector3 accent = new(1f, 0.72f, 0.18f);
        Vector3 frame = accent * 0.72f;
        Vector3 bucket = accent * 1.05f;

        float s = scale;
        float bx = 0.14f * s;
        float by = 0.10f * s;
        float bz = 0.12f * s;
        float armLen = 0.55f * s;
        float armW = 0.06f * s;
        float armH = 0.05f * s;

        var verts = new List<float>();

        void AddBox(float x0, float y0, float z0, float x1, float y1, float z1, Vector3 color)
        {
            float r = color.X, g = color.Y, b = color.Z;
            verts.AddRange(
            [
                x0, y0, z1, r, g, b,
                x1, y0, z1, r, g, b,
                x1, y1, z1, r * 0.95f, g * 0.95f, b * 0.95f,
                x0, y0, z1, r, g, b,
                x1, y1, z1, r * 0.95f, g * 0.95f, b * 0.95f,
                x0, y1, z1, r * 0.9f, g * 0.9f, b * 0.9f,
                x1, y0, z0, r * 0.85f, g * 0.85f, b * 0.85f,
                x0, y0, z0, r * 0.8f, g * 0.8f, b * 0.8f,
                x0, y1, z0, r * 0.75f, g * 0.75f, b * 0.75f,
                x1, y0, z0, r * 0.85f, g * 0.85f, b * 0.85f,
                x0, y1, z0, r * 0.75f, g * 0.75f, b * 0.75f,
                x1, y1, z0, r * 0.7f, g * 0.7f, b * 0.7f,
            ]);
        }

        AddBox(-bx, 0f, -bz, bx, by, bz, frame);
        AddBox(-armW, by, 0f, armW, by + armH, armLen * 0.65f, frame);
        AddBox(-armW * 0.7f, by + armH * 0.5f, armLen * 0.55f, armW * 0.7f, by + armH * 1.4f, armLen, bucket);

        if (hullKey == "miner_tractor")
            AddBox(-armW * 1.4f, by, armLen * 0.2f, armW * 1.4f, by + armH, armLen * 0.45f, frame * 0.9f);

        return ProceduralMeshes.ClampColors(verts.ToArray());
    }

    /// <summary>
    /// Small procedural repair-arm segment (pedestal + truss + emitter head) for support tenders.
    /// </summary>
    public static float[] BuildRepairArmMesh(string hullKey)
    {
        hullKey = NormalizeRepairHullId(hullKey);
        Vector3 accent = new(0.42f, 0.95f, 0.72f);
        Vector3 frame = accent * 0.78f;
        Vector3 head = accent * 1.08f;

        float bx = 0.12f;
        float by = 0.09f;
        float bz = 0.10f;
        float armLen = 0.48f;
        float armW = 0.05f;
        float armH = 0.045f;

        var verts = new List<float>();

        void AddBox(float x0, float y0, float z0, float x1, float y1, float z1, Vector3 color)
        {
            float r = color.X, g = color.Y, b = color.Z;
            verts.AddRange(
            [
                x0, y0, z1, r, g, b,
                x1, y0, z1, r, g, b,
                x1, y1, z1, r * 0.95f, g * 0.95f, b * 0.95f,
                x0, y0, z1, r, g, b,
                x1, y1, z1, r * 0.95f, g * 0.95f, b * 0.95f,
                x0, y1, z1, r * 0.9f, g * 0.9f, b * 0.9f,
                x1, y0, z0, r * 0.85f, g * 0.85f, b * 0.85f,
                x0, y0, z0, r * 0.8f, g * 0.8f, b * 0.8f,
                x0, y1, z0, r * 0.75f, g * 0.75f, b * 0.75f,
                x1, y0, z0, r * 0.85f, g * 0.85f, b * 0.85f,
                x0, y1, z0, r * 0.75f, g * 0.75f, b * 0.75f,
                x1, y1, z0, r * 0.7f, g * 0.7f, b * 0.7f,
            ]);
        }

        AddBox(-bx, 0f, -bz, bx, by, bz, frame);
        AddBox(-armW, by, 0f, armW, by + armH, armLen * 0.62f, frame);
        AddBox(-armW * 0.8f, by + armH * 0.4f, armLen * 0.52f, armW * 0.8f, by + armH * 1.5f, armLen, head);

        if (hullKey == "support_repair")
            AddBox(-armW * 1.2f, by + armH * 0.2f, armLen * 0.15f, armW * 0.2f, by + armH * 1.1f, armLen * 0.42f, frame * 0.92f);

        return ProceduralMeshes.ClampColors(verts.ToArray());
    }

    /// <summary>
    /// Hull-specific pivot on the miner hull (owner local space, pre-display-scale).
    /// Derived from <see cref="TerranEngineNozzleLayout.ResolveHullDimensions"/> envelopes.
    /// </summary>
    public static Vector3 ResolveMiningArmPivot(string hullKey)
    {
        hullKey = NormalizeMinerHullId(hullKey);
        var (len, wid, hgt) = TerranEngineNozzleLayout.ResolveHullDimensions(hullKey);
        return hullKey switch
        {
            // Top-center emitter housing — tractor beam origin.
            "miner_tractor" => new Vector3(0f, hgt * 0.32f, len * 0.08f),
            // Starboard EVA airlock rail.
            "miner_eva" => new Vector3(wid * 0.36f, hgt * 0.18f, -len * 0.06f),
            // Port-side ore scoop on basic barge.
            _ => new Vector3(-wid * 0.40f, hgt * 0.24f, len * 0.04f),
        };
    }

    /// <summary>Mesh origin offset from pivot so the truss extends forward.</summary>
    public static Vector3 ResolveMiningArmMeshOffset(string hullKey)
    {
        hullKey = NormalizeMinerHullId(hullKey);
        var (len, _, _) = TerranEngineNozzleLayout.ResolveHullDimensions(hullKey);
        float forward = hullKey == "miner_eva" ? len * 0.04f : len * 0.06f;
        return new Vector3(0f, 0f, forward);
    }

    /// <summary>Stowed rest pose when the collector is not actively harvesting.</summary>
    public static (float Yaw, float Pitch) ResolveMiningArmStowedPose(string hullKey) =>
        NormalizeMinerHullId(hullKey) switch
        {
            "miner_tractor" => (0f, -8f),
            "miner_eva" => (0f, -12f),
            _ => (0f, 0f),
        };

    /// <summary>
    /// Starboard tool-arm pivot on support repair hulls (owner local space, pre-display-scale).
    /// </summary>
    public static Vector3 ResolveRepairArmPivot(string hullKey)
    {
        hullKey = NormalizeRepairHullId(hullKey);
        var (len, wid, hgt) = TerranEngineNozzleLayout.ResolveHullDimensions(hullKey);
        return new Vector3(wid * 0.38f, hgt * 0.44f, len * 0.10f);
    }

    /// <summary>Mesh origin offset from pivot so the truss extends forward.</summary>
    public static Vector3 ResolveRepairArmMeshOffset(string hullKey)
    {
        hullKey = NormalizeRepairHullId(hullKey);
        var (len, _, _) = TerranEngineNozzleLayout.ResolveHullDimensions(hullKey);
        return new Vector3(0f, 0f, len * 0.05f);
    }

    /// <summary>Stowed rest pose when no repair target is active.</summary>
    public static (float Yaw, float Pitch) ResolveRepairArmStowedPose(string hullKey)
    {
        _ = NormalizeRepairHullId(hullKey);
        return (0f, -20f);
    }

    /// <summary>Resolves pending utility-part <see cref="RenderComponent.MeshKey"/> values to GPU ids.</summary>
    public static void ResolvePendingRenders(World world)
    {
        foreach (var (_, render) in world.Query<RenderComponent>())
        {
            if (render.MeshId >= 0 || string.IsNullOrEmpty(render.MeshKey))
                continue;

            if (!TryResolveGpuMesh(render.MeshKey, out int meshId, out int vertexCount))
                continue;

            render.MeshId = meshId;
            render.VertexCount = vertexCount;
        }
    }

    /// <summary>Uploads (or reuses) a GPU mesh for <paramref name="meshKey"/>.</summary>
    public static bool TryResolveGpuMesh(string meshKey, out int meshId, out int vertexCount)
    {
        meshId = -1;
        vertexCount = 0;
        if (string.IsNullOrEmpty(meshKey))
            return false;

        if (GpuCache.TryGetValue(meshKey, out var cached))
        {
            meshId = cached.MeshId;
            vertexCount = cached.VertexCount;
            return true;
        }

        float[]? vertices = meshKey switch
        {
            var key when key.Equals(MiningArmMeshKey("miner_basic"), StringComparison.OrdinalIgnoreCase)
                => BuildMiningArmMesh("miner_basic"),
            var key when key.Equals(MiningArmMeshKey("miner_tractor"), StringComparison.OrdinalIgnoreCase)
                => BuildMiningArmMesh("miner_tractor"),
            var key when key.Equals(MiningArmMeshKey("miner_eva"), StringComparison.OrdinalIgnoreCase)
                => BuildMiningArmMesh("miner_eva"),
            var key when key.Equals(RepairArmMeshKey("support_repair"), StringComparison.OrdinalIgnoreCase)
                => BuildRepairArmMesh("support_repair"),
            _ => null,
        };

        if (vertices == null || vertices.Length == 0 || _gpuUploader == null)
            return false;

        var uploaded = _gpuUploader(vertices);
        if (uploaded.MeshId < 0 || uploaded.VertexCount <= 0)
            return false;

        GpuCache[meshKey] = uploaded;
        meshId = uploaded.MeshId;
        vertexCount = uploaded.VertexCount;
        return true;
    }

    private static string NormalizeMinerHullId(string hullKey) =>
        hullKey.ToLowerInvariant() switch
        {
            "miner_basic" or "miner_tractor" or "miner_eva" => hullKey.ToLowerInvariant(),
            _ => "miner_basic",
        };

    private static string NormalizeRepairHullId(string hullKey) =>
        hullKey.ToLowerInvariant() switch
        {
            "support_repair" => hullKey.ToLowerInvariant(),
            _ => "support_repair",
        };
}

/// <summary>Resolves utility-part mesh keys before gameplay render each tick.</summary>
public sealed class UtilityPartMeshResolveSystem : GameSystem
{
    /// <inheritdoc/>
    public override void Update(World world, float deltaTime) =>
        UtilityPartMeshes.ResolvePendingRenders(world);
}