using OpenTK.Mathematics;

namespace SharpOpenGl.Engine.ECS;

/// <summary>
/// Brief damage-feedback flash driven by <see cref="CombatFeedbackSystem"/>.
/// Restores stored render tints when <see cref="Remaining"/> reaches zero.
/// </summary>
public sealed class HitFlashComponent
{
    public float Duration { get; set; } = 0.18f;
    public float Remaining { get; set; }
    public Vector4 BaseColor { get; set; }
    public Vector3 BaseTeamTint { get; set; } = Vector3.One;
    public bool UsesRaceTexture { get; set; }
}