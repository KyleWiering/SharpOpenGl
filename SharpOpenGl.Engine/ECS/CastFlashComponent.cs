using OpenTK.Mathematics;

namespace SharpOpenGl.Engine.ECS;

/// <summary>
/// Brief ability-cast color pulse on a hero — driven by <see cref="CombatFeedbackSystem"/>.
/// Uses <see cref="RenderComponent"/> tint only; no geometry rebuild.
/// </summary>
public sealed class CastFlashComponent
{
    public float Duration = 0.36f;
    public float Remaining;
    public Vector4 BaseColor;
    public Vector3 BaseTeamTint;
    public Vector3 CastTint = new(0.55f, 0.85f, 1f);
    public bool UsesRaceTexture;
}