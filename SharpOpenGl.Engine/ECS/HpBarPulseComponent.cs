namespace SharpOpenGl.Engine.ECS;

/// <summary>
/// Brief HUD HP-bar pulse after a heavy damage hit — color-only feedback, no geometry rebuild.
/// </summary>
public sealed class HpBarPulseComponent
{
    public float Duration { get; set; } = 0.35f;
    public float Remaining { get; set; } = 0.35f;

    /// <summary>Current pulse intensity 0–1 (strongest immediately after trigger, eases out).</summary>
    public float Intensity
    {
        get
        {
            if (Remaining <= 0f || Duration <= 0f) return 0f;
            float life = Math.Clamp(Remaining / Duration, 0f, 1f);
            return life * life;
        }
    }
}