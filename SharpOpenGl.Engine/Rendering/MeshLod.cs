namespace SharpOpenGl.Engine.Rendering;

/// <summary>
/// Holds up to three levels-of-detail for a mesh.
/// Each level is a (vao, vertexCount) pair referring to GPU resources.
/// </summary>
public sealed class MeshLod
{
    /// <summary>Detailed mesh — used when camera is within <see cref="SimpleDistance"/>.</summary>
    public (int vao, int vertexCount) DetailMesh { get; set; }

    /// <summary>Simplified mesh — used between <see cref="SimpleDistance"/> and <see cref="IconDistance"/>.</summary>
    public (int vao, int vertexCount) SimpleMesh { get; set; }

    /// <summary>Icon / silhouette mesh — used beyond <see cref="IconDistance"/>.</summary>
    public (int vao, int vertexCount) IconMesh { get; set; }

    /// <summary>Camera distance (world units) at which to switch to the simple mesh.</summary>
    public float SimpleDistance { get; set; } = 80f;

    /// <summary>Camera distance at which to switch to the icon mesh.</summary>
    public float IconDistance { get; set; } = 250f;

    /// <summary>
    /// Select the appropriate (vao, vertexCount) pair for the given camera distance.
    /// Falls back to the next-lower detail level if a level's vao is 0 (unset).
    /// </summary>
    public (int vao, int vertexCount) Select(float distanceFromCamera)
    {
        if (distanceFromCamera >= IconDistance && IconMesh.vao != 0)
            return IconMesh;

        if (distanceFromCamera >= SimpleDistance && SimpleMesh.vao != 0)
            return SimpleMesh;

        return DetailMesh;
    }
}
