using OpenTK.Mathematics;

namespace SharpOpenGl.Engine.Rendering;

/// <summary>
/// Progressive shipyard hull preview meshes — scaffold wireframe at low build fraction,
/// patchwork partial plating mid-build, full race hull near completion.
/// </summary>
public static class ShipyardBuildPreviewMeshes
{
    // Strategy: dual-pass reveal.
    // fraction < ScaffoldMaxFraction → GL_LINES stern-anchored hull wireframe (sparse scaffold).
    // ScaffoldMaxFraction–FullHullFraction → GL_TRIANGLES with stern-anchored triangle reveal
    //   and vertex-color blend from muted patchwork substrate to full RaceSurfaceDetail panels.
    // fraction ≥ FullHullFraction → complete hull triangle mesh (spawn-equivalent geometry).

    public const float ScaffoldMaxFraction = 0.25f;
    public const float FullHullFraction = 0.95f;
    public const float QuantizeStep = 0.02f;

    private static readonly Vector3 ScaffoldLineColor = new(0.40f, 0.43f, 0.48f);
    private static readonly Vector3 PatchworkSubstrate = new(0.36f, 0.38f, 0.42f);

    private static readonly Dictionary<string, float[]> CpuCache = new(StringComparer.Ordinal);
    private static readonly Dictionary<string, (int MeshId, int VertexCount, int PrimitiveType)> GpuCache = new(StringComparer.Ordinal);

    /// <summary>Quantize a 0–1 fraction to cache steps (~2%).</summary>
    public static float QuantizeFraction(float fraction01) =>
        Math.Clamp(MathF.Round(fraction01 / QuantizeStep) * QuantizeStep, 0f, 1f);

    /// <summary>Cache key for CPU/GPU partial meshes.</summary>
    public static string BuildCacheKey(string raceId, string hullKey, float fraction01) =>
        $"{raceId}:{hullKey}:{QuantizeFraction(fraction01):F2}";

    /// <summary>OpenGL primitive type for the given build fraction (1 = lines, 4 = triangles).</summary>
    public static int ResolvePrimitiveType(float fraction01) =>
        QuantizeFraction(fraction01) < ScaffoldMaxFraction ? 1 : 4;

    /// <summary>
    /// Build stern-anchored partial hull triangles with patchwork color blend.
    /// At low fractions within the triangle pass, colors stay desaturated; near completion they match the full hull.
    /// </summary>
    public static float[] BuildPartial(string raceId, string hullKey, float fraction01)
    {
        float quantized = QuantizeFraction(fraction01);
        string cacheKey = BuildCacheKey(raceId, hullKey, quantized);
        if (CpuCache.TryGetValue(cacheKey, out float[]? cached))
            return cached;

        float[] full = RaceShipMeshes.Build(raceId, hullKey);
        if (full.Length == 0)
        {
            CpuCache[cacheKey] = full;
            return full;
        }

        if (quantized >= FullHullFraction)
        {
            CpuCache[cacheKey] = full;
            return full;
        }

        if (quantized < ScaffoldMaxFraction)
        {
            float[] scaffold = BuildScaffoldInternal(raceId, hullKey, full);
            CpuCache[cacheKey] = scaffold;
            return scaffold;
        }

        float reveal01 = (quantized - ScaffoldMaxFraction) / (FullHullFraction - ScaffoldMaxFraction);
        float[] revealed = TakeTrianglesFromStern(full, reveal01);
        ApplyPatchworkBlend(revealed, reveal01);
        CpuCache[cacheKey] = revealed;
        return revealed;
    }

    /// <summary>Wireframe/scaffold variant for low build fractions.</summary>
    public static float[] BuildScaffold(string raceId, string hullKey)
    {
        string cacheKey = $"{raceId}:{hullKey}:scaffold";
        if (CpuCache.TryGetValue(cacheKey, out float[]? cached))
            return cached;

        float[] full = RaceShipMeshes.Build(raceId, hullKey);
        float[] scaffold = BuildScaffoldInternal(raceId, hullKey, full);
        CpuCache[cacheKey] = scaffold;
        return scaffold;
    }

    /// <summary>
    /// Resolve or upload a cached GPU mesh for the preview hull.
    /// Returns false when <paramref name="uploader"/> is null or vertices are empty.
    /// </summary>
    public static bool TryResolveGpuMesh(
        string cacheKey,
        float[] vertices,
        int primitiveType,
        Func<float[], int, (int MeshId, int VertexCount)>? uploader,
        out int meshId,
        out int vertexCount)
    {
        meshId = -1;
        vertexCount = 0;
        if (uploader == null || vertices.Length == 0)
            return false;

        if (GpuCache.TryGetValue(cacheKey, out var gpu) && gpu.PrimitiveType == primitiveType)
        {
            meshId = gpu.MeshId;
            vertexCount = gpu.VertexCount;
            return true;
        }

        var uploaded = uploader(vertices, primitiveType);
        if (uploaded.MeshId < 0 || uploaded.VertexCount <= 0)
            return false;

        GpuCache[cacheKey] = (uploaded.MeshId, uploaded.VertexCount, primitiveType);
        meshId = uploaded.MeshId;
        vertexCount = uploaded.VertexCount;
        return true;
    }

    /// <summary>Drop cached CPU/GPU meshes when the queued hull or faction race changes.</summary>
    public static void InvalidateRaceHull(string raceId, string hullKey)
    {
        string prefix = $"{raceId}:{hullKey}:";
        foreach (string key in CpuCache.Keys.Where(k => k.StartsWith(prefix, StringComparison.Ordinal)).ToArray())
            CpuCache.Remove(key);

        CpuCache.Remove($"{raceId}:{hullKey}:scaffold");

        foreach (string key in GpuCache.Keys.Where(k => k.StartsWith(prefix, StringComparison.Ordinal)).ToArray())
            GpuCache.Remove(key);
    }

    private static float[] BuildScaffoldInternal(string raceId, string hullKey, float[]? fullMesh = null)
    {
        fullMesh ??= RaceShipMeshes.Build(raceId, hullKey);
        if (fullMesh.Length == 0)
            return fullMesh;

        return ExtractWireframeEdges(fullMesh, ScaffoldLineColor);
    }

    private static float[] TakeTrianglesFromStern(float[] fullMesh, float reveal01)
    {
        int stride = ProceduralMeshes.Stride;
        int totalVerts = fullMesh.Length / stride;
        int triCount = totalVerts / 3;
        if (triCount <= 0)
            return Array.Empty<float>();

        int triToTake = Math.Clamp((int)MathF.Floor(reveal01 * triCount), 1, triCount);
        var centroids = new (float Z, int TriIndex)[triCount];
        for (int t = 0; t < triCount; t++)
        {
            int baseVert = t * 3;
            float z = (fullMesh[baseVert * stride + 2]
                     + fullMesh[(baseVert + 1) * stride + 2]
                     + fullMesh[(baseVert + 2) * stride + 2]) / 3f;
            centroids[t] = (z, t);
        }

        Array.Sort(centroids, static (a, b) => a.Z.CompareTo(b.Z));

        var result = new float[triToTake * 3 * stride];
        for (int i = 0; i < triToTake; i++)
        {
            int tri = centroids[i].TriIndex;
            int src = tri * 3 * stride;
            int dst = i * 3 * stride;
            Array.Copy(fullMesh, src, result, dst, 3 * stride);
        }

        return result;
    }

    private static void ApplyPatchworkBlend(float[] mesh, float reveal01)
    {
        int stride = ProceduralMeshes.Stride;
        float blend = Math.Clamp(reveal01, 0f, 1f);
        for (int i = 0; i < mesh.Length; i += stride)
        {
            var original = new Vector3(mesh[i + 3], mesh[i + 4], mesh[i + 5]);
            Vector3 muted = Vector3.Lerp(PatchworkSubstrate, original * 0.55f, 0.35f);
            Vector3 color = Vector3.Lerp(muted, original, blend);
            mesh[i + 3] = color.X;
            mesh[i + 4] = color.Y;
            mesh[i + 5] = color.Z;
        }
    }

    private static float[] ExtractWireframeEdges(float[] triangles, Vector3 lineColor)
    {
        int stride = ProceduralMeshes.Stride;
        int triCount = triangles.Length / (stride * 3);
        var edgeSet = new HashSet<(long, long)>();
        var lines = new List<float>(triCount * 6 * stride);

        for (int t = 0; t < triCount; t++)
        {
            int v0 = t * 3;
            AddEdge(triangles, v0, v0 + 1, edgeSet, lines, lineColor, stride);
            AddEdge(triangles, v0 + 1, v0 + 2, edgeSet, lines, lineColor, stride);
            AddEdge(triangles, v0 + 2, v0, edgeSet, lines, lineColor, stride);
        }

        return lines.ToArray();
    }

    private static void AddEdge(
        float[] mesh,
        int vertA,
        int vertB,
        HashSet<(long, long)> edgeSet,
        List<float> lines,
        Vector3 lineColor,
        int stride)
    {
        long keyA = PositionKey(mesh, vertA, stride);
        long keyB = PositionKey(mesh, vertB, stride);
        var edge = keyA <= keyB ? (keyA, keyB) : (keyB, keyA);
        if (!edgeSet.Add(edge))
            return;

        AppendVertex(lines, mesh, vertA, lineColor, stride);
        AppendVertex(lines, mesh, vertB, lineColor, stride);
    }

    private static void AppendVertex(List<float> target, float[] source, int vertIndex, Vector3 color, int stride)
    {
        int i = vertIndex * stride;
        target.Add(source[i]);
        target.Add(source[i + 1]);
        target.Add(source[i + 2]);
        target.Add(color.X);
        target.Add(color.Y);
        target.Add(color.Z);
    }

    private static long PositionKey(float[] mesh, int vertIndex, int stride)
    {
        int i = vertIndex * stride;
        int x = (int)MathF.Round(mesh[i] * 500f);
        int y = (int)MathF.Round(mesh[i + 1] * 500f);
        int z = (int)MathF.Round(mesh[i + 2] * 500f);
        return HashCode.Combine(x, y, z);
    }
}