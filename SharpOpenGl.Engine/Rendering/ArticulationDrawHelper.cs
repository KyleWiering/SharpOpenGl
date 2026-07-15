using OpenTK.Mathematics;
using SharpOpenGl.Engine.ECS;

namespace SharpOpenGl.Engine.Rendering;

/// <summary>
/// Shared draw-time helpers for articulated sub-part entities. Composes pivot-based model
/// matrices from owner transforms and <see cref="ArticulatedPartComponent"/> angle state.
/// </summary>
public static class ArticulationDrawHelper
{
    /// <summary>
    /// When the entity has <see cref="ArticulatedPartComponent"/>, composes
    /// <c>parentModel * pivot * rotation * meshOffset</c> and returns <c>true</c>.
    /// Plain render entities return <c>false</c> so callers use <see cref="TransformComponent.GetModelMatrix"/>.
    /// </summary>
    public static bool TryGetArticulatedModelMatrix(
        World world,
        Entity entity,
        TransformComponent transform,
        out Matrix4 model)
    {
        ArticulatedPartComponent? part = world.GetComponent<ArticulatedPartComponent>(entity);
        if (part == null)
        {
            model = default;
            return false;
        }

        Matrix4 parentModel = ResolveOwnerModelMatrix(world, part.Owner);
        Vector3 ownerScale = ResolveOwnerScale(world, part.Owner);
        model = ArticulationMath.ComposePartModelMatrix(
            parentModel,
            part.LocalPivotOffset,
            part.CurrentYaw,
            part.CurrentPitch,
            part.MeshLocalOffset,
            ownerScale);
        return true;
    }

    /// <summary>
    /// Walks the articulated owner chain to the root hull/building entity.
    /// </summary>
    public static Entity ResolveRootOwner(World world, ArticulatedPartComponent part)
    {
        Entity current = part.Owner;
        while (world.IsAlive(current))
        {
            ArticulatedPartComponent? parentPart = world.GetComponent<ArticulatedPartComponent>(current);
            if (parentPart == null)
                break;
            current = parentPart.Owner;
        }

        return current;
    }

    /// <summary>
    /// Resolves the parent model matrix for an articulated part, composing intermediate owners.
    /// </summary>
    public static Matrix4 ResolveOwnerModelMatrix(World world, Entity owner)
    {
        if (!world.IsAlive(owner))
            return Matrix4.Identity;

        ArticulatedPartComponent? ownerPart = world.GetComponent<ArticulatedPartComponent>(owner);
        if (ownerPart != null)
        {
            Matrix4 grandparent = ResolveOwnerModelMatrix(world, ownerPart.Owner);
            Vector3 grandparentScale = ResolveOwnerScale(world, ownerPart.Owner);
            return ArticulationMath.ComposePartModelMatrix(
                grandparent,
                ownerPart.LocalPivotOffset,
                ownerPart.CurrentYaw,
                ownerPart.CurrentPitch,
                ownerPart.MeshLocalOffset,
                grandparentScale);
        }

        TransformComponent? transform = world.GetComponent<TransformComponent>(owner);
        return transform?.GetModelMatrix() ?? Matrix4.Identity;
    }

    private static Vector3 ResolveOwnerScale(World world, Entity owner)
    {
        if (!world.IsAlive(owner))
            return Vector3.One;

        ArticulatedPartComponent? ownerPart = world.GetComponent<ArticulatedPartComponent>(owner);
        if (ownerPart != null)
            return ResolveOwnerScale(world, ownerPart.Owner);

        TransformComponent? transform = world.GetComponent<TransformComponent>(owner);
        return transform?.Scale ?? Vector3.One;
    }

    /// <summary>
    /// Returns the owner hull position for fog/visibility checks on articulated child entities.
    /// Falls back to <paramref name="defaultPosition"/> for non-parts or missing owner transforms.
    /// </summary>
    public static Vector3 GetVisibilityPosition(World world, Entity entity, Vector3 defaultPosition)
    {
        ArticulatedPartComponent? part = world.GetComponent<ArticulatedPartComponent>(entity);
        if (part == null || !world.IsAlive(part.Owner))
            return defaultPosition;

        TransformComponent? ownerTransform = world.GetComponent<TransformComponent>(part.Owner);
        return ownerTransform?.Position ?? defaultPosition;
    }

    /// <summary>Returns <c>true</c> when <paramref name="entity"/> is an articulated sub-part child.</summary>
    public static bool IsArticulatedPartChild(World world, Entity entity) =>
        world.HasComponent<ArticulatedPartComponent>(entity);
}