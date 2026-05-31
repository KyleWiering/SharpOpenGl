namespace SharpOpenGl.Engine.Rendering;

/// <summary>
/// Pure vertex data parsed from a Wavefront .obj file.
/// Vertex layout: float[6] per vertex — position XYZ then normal XYZ.
/// The normals are flat-computed when not present in the source file.
/// Call <see cref="ObjMeshLoader.Upload"/> to transfer to the GPU.
/// </summary>
public sealed class ObjMeshData
{
    /// <summary>
    /// Interleaved float buffer: [x, y, z, nx, ny, nz, x, y, z, nx, ny, nz, …].
    /// </summary>
    public float[] Vertices { get; }

    /// <summary>Number of vertices (Vertices.Length / Stride).</summary>
    public int VertexCount { get; }

    /// <summary>Number of floats per vertex (always 6: pos3 + normal3).</summary>
    public const int Stride = 6;

    /// <summary>Source file path for diagnostics.</summary>
    public string SourcePath { get; }

    /// <summary>Friendly display name (file stem, or custom label).</summary>
    public string Name { get; }

    internal ObjMeshData(float[] vertices, string sourcePath, string name)
    {
        Vertices    = vertices;
        VertexCount = vertices.Length / Stride;
        SourcePath  = sourcePath;
        Name        = name;
    }
}
