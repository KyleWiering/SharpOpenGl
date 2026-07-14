using OpenTK.Mathematics;

namespace SharpOpenGl.Engine.Rendering;

/// <summary>
/// Progressive structure scaffold meshes during timed construction.
/// Strategy (Option A): 50% scaled desaturated wireframe at low fraction, ground-up triangle
/// reveal with patchwork blend mid-build, full race station mesh near completion.
/// Quantized like <see cref="ShipyardBuildPreviewMeshes"/> to limit GPU uploads.
/// </summary>
public static class StructureConstructionMeshes
{
    public const float ScaffoldScale = 0.5f;
    public const float ScaffoldMaxFraction = 0.25f;
    public const float FullBuildingFraction = 0.95f;
    public const float QuantizeStep = 0.02f;

    private static readonly Vector3 ScaffoldLineColor = new(0.38f, 0.40f, 0.44f);
    private static readonly Vector3 PatchworkSubstrate = new(0.34f, 0.36f, 0.40f);

    private static readonly Dictionary<string, float[]> CpuCache = new(StringComparer.Ordinal);
    private static readonly Dictionary<string, (int MeshId, int VertexCount, int PrimitiveType)> GpuCache =
        new(StringComparer.Ordinal);

    public static float QuantizeFraction(float fraction01) =>
        Math.Clamp(MathF.Round(fraction01 / QuantizeStep) * QuantizeStep, 0f, 1f);

    public static string BuildCacheKey(string buildingType, string raceId, float fraction01) =>
        $"scaffold:{raceId}:{buildingType}:{QuantizeFraction(fraction01):F2}";

    public static int ResolvePrimitiveType(float fraction01) =>
        QuantizeFraction(fraction01) < ScaffoldMaxFraction ? 1 : 4;

    public static float[] BuildPartial(string buildingType, string raceId, float fraction01)
    {
        float quantized = QuantizeFraction(fraction01);
        string cacheKey = BuildCacheKey(buildingType, raceId, quantized);
        if (CpuCache.TryGetValue(cacheKey, out float[]? cached))
            return cached;

        float[] full = RaceBuildingMeshes.Build(buildingType, raceId);
        if (full.Length == 0)
        {
            CpuCache[cacheKey] = full;
            return full;
        }

        if (quantized >= FullBuildingFraction)
        {
            CpuCache[cacheKey] = full;
            return full;
        }

        if (quantized < ScaffoldMaxFraction)
        {
            float[] scaffold = BuildScaffoldInternal(full);
            CpuCache[cacheKey] = scaffold;
            return scaffold;
        }

        float reveal01 = (quantized - ScaffoldMaxFraction) / (FullBuildingFraction - ScaffoldMaxFraction);
        float[] revealed = TakeTrianglesFromGround(full, reveal01);
        ApplyPatchworkBlend(revealed, reveal01);
        CpuCache[cacheKey] = revealed;
        return revealed;
    }

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

    private static float[] BuildScaffoldInternal(float[] fullMesh)
    {
        float[] scaled = ScaleMesh(fullMesh, ScaffoldScale);
        DesaturateMesh(scaled, 0.55f);
        return ExtractWireframeEdges(scaled, ScaffoldLineColor);
    }

    private static float[] ScaleMesh(float[] mesh, float scale)
    {
        int stride = ProceduralMeshes.Stride;
        var result = new float[mesh.Length];
        Array.Copy(mesh, result, mesh.Length);
        for (int i = 0; i < result.Length; i += stride)
        {
            result[i] *= scale;
            result[i + 1] *= scale;
            result[i + 2] *= scale;
        }

        return result;
    }

    private static void DesaturateMesh(float[] mesh, float strength)
    {
        int stride = ProceduralMeshes.Stride;
        strength = Math.Clamp(strength, 0f, 1f);
        for (int i = 0; i < mesh.Length; i += stride)
        {
            var color = new Vector3(mesh[i + 3], mesh[i + 4], mesh[i + 5]);
            float avg = (color.X + color.Y + color.Z) / 3f;
            var muted = Vector3.Lerp(color, new Vector3(avg, avg, avg), strength) * 0.72f;
            mesh[i + 3] = muted.X;
            mesh[i + 4] = muted.Y;
            mesh[i + 5] = muted.Z;
        }
    }

    private static float[] TakeTrianglesFromGround(float[] fullMesh, float reveal01)
    {
        int stride = ProceduralMeshes.Stride;
        int totalVerts = fullMesh.Length / stride;
        int triCount = totalVerts / 3;
        if (triCount <= 0)
            return Array.Empty<float>();

        int triToTake = Math.Clamp((int)MathF.Floor(reveal01 * triCount), 1, triCount);
        var centroids = new (float Y, int TriIndex)[triCount];
        for (int t = 0; t < triCount; t++)
        {
            int baseVert = t * 3;
            float y = (fullMesh[baseVert * stride + 1]
                     + fullMesh[(baseVert + 1) * stride + 1]
                     + fullMesh[(baseVert + 2) * stride + 1]) / 3f;
            centroids[t] = (y, t);
        }

        Array.Sort(centroids, static (a, b) => a.Y.CompareTo(b.Y));

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