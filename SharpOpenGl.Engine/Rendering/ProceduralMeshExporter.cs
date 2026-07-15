using System.Globalization;
using System.Text;
using OpenTK.Mathematics;

namespace SharpOpenGl.Engine.Rendering;

/// <summary>
/// Converts procedural vertex buffers (stride 6: pos XYZ + color RGB) to Wavefront OBJ files.
/// </summary>
public static class ProceduralMeshExporter
{
    /// <summary>Export procedural mesh to OBJ with per-face normals.</summary>
    public static void WriteObj(float[] vertices, string outputPath, string objectName = "mesh")
    {
        if (vertices.Length == 0 || vertices.Length % ProceduralMeshes.Stride != 0)
            throw new ArgumentException("Vertex buffer must be non-empty and stride-aligned.", nameof(vertices));

        int triCount = vertices.Length / ProceduralMeshes.Stride / 3;
        var positions = new List<Vector3>(triCount * 3);
        var normals = new List<Vector3>(triCount * 3);

        for (int t = 0; t < triCount; t++)
        {
            int baseIdx = t * 3 * ProceduralMeshes.Stride;
            var a = ReadPos(vertices, baseIdx);
            var b = ReadPos(vertices, baseIdx + ProceduralMeshes.Stride);
            var c = ReadPos(vertices, baseIdx + 2 * ProceduralMeshes.Stride);
            Vector3 normal = Vector3.Normalize(Vector3.Cross(b - a, c - a));
            if (!float.IsFinite(normal.X))
                normal = Vector3.UnitY;

            positions.Add(a);
            positions.Add(b);
            positions.Add(c);
            normals.Add(normal);
            normals.Add(normal);
            normals.Add(normal);
        }

        string? dir = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        var sb = new StringBuilder();
        sb.AppendLine("# Exported from SharpOpenGl procedural mesh");
        sb.AppendLine($"o {objectName}");
        sb.AppendLine($"# vertices {positions.Count}");

        foreach (var p in positions)
            sb.AppendLine(Form(CultureInfo.InvariantCulture, "v {0} {1} {2}", p.X, p.Y, p.Z));

        foreach (var n in normals)
            sb.AppendLine(Form(CultureInfo.InvariantCulture, "vn {0} {1} {2}", n.X, n.Y, n.Z));

        for (int i = 0; i < triCount; i++)
        {
            int v = i * 3 + 1;
            sb.AppendLine(Form(CultureInfo.InvariantCulture, "f {0}//{0} {1}//{1} {2}//{2}", v, v + 1, v + 2));
        }

        File.WriteAllText(outputPath, sb.ToString());
    }

    /// <summary>Export <see cref="ObjMeshData"/> (pos+normal stride) to OBJ.</summary>
    public static void WriteObj(ObjMeshData data, string outputPath)
        => WriteObj(data.Vertices, outputPath, data.Name);

    private static Vector3 ReadPos(float[] vertices, int offset) =>
        new(vertices[offset], vertices[offset + 1], vertices[offset + 2]);

    private static string Form(IFormatProvider provider, string format, params object[] args) =>
        string.Format(provider, format, args);
}