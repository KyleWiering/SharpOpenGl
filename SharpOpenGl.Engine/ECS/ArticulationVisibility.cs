using OpenTK.Mathematics;
using SharpOpenGl.Engine.Rendering;

namespace SharpOpenGl.Engine.ECS;

/// <summary>
/// Distance-based visibility gate for articulated parts, aligned with <see cref="MeshLod"/> far swap distance.
/// </summary>
public static class ArticulationVisibility
{
    /// <summary>
    /// Returns the farthest LOD swap distance for <paramref name="owner"/>, or positive infinity when
    /// the owner has no <see cref="MeshLodComponent"/> (articulation always active).
    /// </summary>
    public static float GetFarLodDistance(World world, Entity owner)
    {
        MeshLodComponent? lodComp = world.GetComponent<MeshLodComponent>(owner);
        if (lodComp == null)
            return float.PositiveInfinity;

        return lodComp.Lod.IconDistance;
    }

    /// <summary>
    /// Returns <c>true</c> when the owner is close enough that articulated angles should update this frame.
    /// Owners without <see cref="MeshLodComponent"/> are always active.
    /// </summary>
    public static bool IsActive(World world, Entity owner, Vector3 cameraPos)
    {
        TransformComponent? transform = world.GetComponent<TransformComponent>(owner);
        Vector3 ownerPos = transform?.Position ?? Vector3.Zero;
        float dist = Vector3.Distance(ownerPos, cameraPos);
        return dist <= GetFarLodDistance(world, owner);
    }
}