using OpenTK.Mathematics;

namespace SharpOpenGl.Engine.Rendering;

/// <summary>
/// Loads Wavefront .obj files into <see cref="ObjMeshData"/> (pure parse, no GL).
/// Supported .obj features: v, vn, f (triangles and quads, v/vt/vn syntax).
/// </summary>
public static class ObjMeshLoader
{
    /// <summary>
    /// Parse a .obj file into vertex data.
    /// Returns <c>null</c> if the file is missing or contains no triangles.
    /// No GPU resources are allocated here.
    /// </summary>
    public static ObjMeshData? Parse(string path)
    {
        if (!File.Exists(path))
            return null;

        var positions = new List<Vector3>();
        var normals   = new List<Vector3>();
        var outVerts  = new List<float>();

        foreach (string rawLine in File.ReadLines(path))
        {
            string line = rawLine.Trim();
            if (line.Length == 0 || line[0] == '#')
                continue;

            if (line.StartsWith("vn ", StringComparison.Ordinal))
            {
                normals.Add(ParseVec3(line));
            }
            else if (line.StartsWith("v ", StringComparison.Ordinal))
            {
                positions.Add(ParseVec3(line));
            }
            else if (line.StartsWith("f ", StringComparison.Ordinal))
            {
                ProcessFace(line, positions, normals, outVerts);
            }
        }

        if (outVerts.Count == 0)
            return null;

        string name = Path.GetFileNameWithoutExtension(path);
        return new ObjMeshData(outVerts.ToArray(), path, name);
    }

    private static Vector3 ParseVec3(string line)
    {
        string[] t = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        float x = float.Parse(t[1], System.Globalization.CultureInfo.InvariantCulture);
        float y = float.Parse(t[2], System.Globalization.CultureInfo.InvariantCulture);
        float z = float.Parse(t[3], System.Globalization.CultureInfo.InvariantCulture);
        return new Vector3(x, y, z);
    }

    private static void ProcessFace(
        string line,
        List<Vector3> positions,
        List<Vector3> normals,
        List<float> outVerts)
    {
        string[] tokens = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        int faceCount = tokens.Length - 1;

        var corners = new (int posIdx, int normIdx)[faceCount];
        for (int i = 0; i < faceCount; i++)
            corners[i] = ParseFaceCorner(tokens[i + 1]);

        for (int i = 1; i < faceCount - 1; i++)
        {
            Emit(corners[0],   positions, normals, outVerts);
            Emit(corners[i],   positions, normals, outVerts);
            Emit(corners[i+1], positions, normals, outVerts);
        }
    }

    private static (int posIdx, int normIdx) ParseFaceCorner(string token)
    {
        string[] parts = token.Split('/');
        int posIdx  = int.Parse(parts[0]) - 1;
        int normIdx = parts.Length >= 3 && parts[2].Length > 0
            ? int.Parse(parts[2]) - 1
            : -1;
        return (posIdx, normIdx);
    }

    private static void Emit(
        (int posIdx, int normIdx) corner,
        List<Vector3> positions,
        List<Vector3> normals,
        List<float> outVerts)
    {
        Vector3 pos  = corner.posIdx >= 0 && corner.posIdx < positions.Count
            ? positions[corner.posIdx]
            : Vector3.Zero;

        Vector3 norm = corner.normIdx >= 0 && corner.normIdx < normals.Count
            ? normals[corner.normIdx]
            : Vector3.UnitY;

        outVerts.Add(pos.X);  outVerts.Add(pos.Y);  outVerts.Add(pos.Z);
        outVerts.Add(norm.X); outVerts.Add(norm.Y); outVerts.Add(norm.Z);
    }
}