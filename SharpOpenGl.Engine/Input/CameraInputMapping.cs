namespace SharpOpenGl.Engine.Input;

/// <summary>
/// Resolves camera axis input while avoiding overlap with unit command keys.
/// </summary>
public static class CameraInputMapping
{
    public readonly struct CameraAxes
    {
        public float Forward { get; init; }
        public float Strafe { get; init; }
        public float Height { get; init; }
    }

    /// <summary>
    /// Build camera pan axes from held keys.
    /// When <paramref name="unitsSelected"/> is true, A/S/D/X are reserved for unit commands
    /// unless <paramref name="shiftHeld"/> requests camera override.
    /// </summary>
    public static CameraAxes Resolve(
        bool w, bool s, bool a, bool d, bool q, bool e, bool z, bool x,
        bool unitsSelected, bool shiftHeld)
    {
        float forward = 0f, strafe = 0f, height = 0f;

        bool cameraOverride = !unitsSelected || shiftHeld;

        if (cameraOverride || !s)
        {
            if (w) forward = -1f;
            if (s) forward = 1f;
        }

        if (cameraOverride)
        {
            if (a || q) strafe = -1f;
            else if (d || e) strafe = 1f;
        }
        else if (q) strafe = -1f;
        else if (e) strafe = 1f;

        if (z) height = 1f;
        if (x && cameraOverride) height = -1f;

        return new CameraAxes { Forward = forward, Strafe = strafe, Height = height };
    }
}