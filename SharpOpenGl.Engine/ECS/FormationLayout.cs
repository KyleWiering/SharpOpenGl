using OpenTK.Mathematics;

namespace SharpOpenGl.Engine.ECS;

/// <summary>
/// Computes stable per-slot offsets for squad formations.
/// Local space: +Z forward, +X right, leader at slot 0 on the origin.
/// </summary>
public static class FormationLayout
{
    public const float DefaultSpacing = 12f;
    public const int BoxColumns = 3;

    /// <summary>Compute slot offsets for <paramref name="memberCount"/> units.</summary>
    public static Vector3[] ComputeOffsets(FormationType formation, int memberCount, float spacing = DefaultSpacing)
    {
        if (memberCount <= 0)
            return Array.Empty<Vector3>();

        var offsets = new Vector3[memberCount];
        offsets[0] = Vector3.Zero;

        for (int slot = 1; slot < memberCount; slot++)
        {
            offsets[slot] = formation switch
            {
                FormationType.Line => ComputeLineSlot(slot, memberCount, spacing),
                FormationType.Column => new Vector3(0f, 0f, -slot * spacing),
                FormationType.Wedge => ComputeWedgeSlot(slot, spacing),
                FormationType.Box => ComputeBoxSlot(slot, spacing),
                _ => Vector3.Zero,
            };
        }

        return offsets;
    }

    /// <summary>Rotate a local formation offset by leader yaw (degrees, Y-axis).</summary>
    public static Vector3 RotateOffset(Vector3 localOffset, float yawDegrees)
    {
        float radians = MathHelper.DegreesToRadians(yawDegrees);
        float cos = MathF.Cos(radians);
        float sin = MathF.Sin(radians);

        return new Vector3(
            localOffset.X * cos + localOffset.Z * sin,
            localOffset.Y,
            -localOffset.X * sin + localOffset.Z * cos);
    }

    /// <summary>Yaw in degrees from <paramref name="from"/> toward <paramref name="to"/> on the XZ plane.</summary>
    public static float FacingYaw(Vector3 from, Vector3 to)
    {
        Vector3 delta = to - from;
        delta.Y = 0f;
        if (delta.LengthSquared < 0.0001f)
            return 0f;

        return MathHelper.RadiansToDegrees(MathF.Atan2(delta.X, delta.Z));
    }

    /// <summary>Advance to the next formation type in cycle order.</summary>
    public static FormationType NextFormation(FormationType current) => current switch
    {
        FormationType.Line => FormationType.Wedge,
        FormationType.Wedge => FormationType.Box,
        FormationType.Box => FormationType.Column,
        FormationType.Column => FormationType.Line,
        _ => FormationType.Line,
    };

    /// <summary>Short UI label for a formation type.</summary>
    public static string GetLabel(FormationType formation) => formation switch
    {
        FormationType.Line => "Line",
        FormationType.Wedge => "Wedge",
        FormationType.Box => "Box",
        FormationType.Column => "Column",
        _ => "?",
    };

    private static Vector3 ComputeLineSlot(int slot, int memberCount, float spacing)
    {
        int followers = memberCount - 1;
        int index = slot - 1;
        float totalWidth = MathF.Max(0f, followers - 1) * spacing;
        float startX = -totalWidth * 0.5f;
        return new Vector3(startX + index * spacing, 0f, -spacing);
    }

    private static Vector3 ComputeWedgeSlot(int slot, float spacing)
    {
        int depth = (slot - 1) / 2 + 1;
        bool left = (slot - 1) % 2 == 0;
        float side = left ? -depth : depth;
        return new Vector3(side * spacing, 0f, -depth * spacing);
    }

    private static Vector3 ComputeBoxSlot(int slot, float spacing)
    {
        int index = slot - 1;
        int row = index / BoxColumns + 1;
        int col = index % BoxColumns;
        float startX = -(BoxColumns - 1) * spacing * 0.5f;
        return new Vector3(startX + col * spacing, 0f, -row * spacing);
    }
}