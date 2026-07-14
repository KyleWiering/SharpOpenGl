using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using SharpOpenGl.Engine.Rendering;

namespace SharpOpenGl;

public partial class EngineWindow
{
    private CombatRingOverlayController? _combatRingOverlays;

    private void InitializeCombatRingOverlays()
    {
        _combatRingOverlays = new CombatRingOverlayController();
        _combatRingOverlays.Bind(_eventBus);
    }

    private void UpdateCombatRingOverlays(float deltaTime) =>
        _combatRingOverlays?.Update(deltaTime);

    private void RenderCombatRingOverlays()
    {
        if (_combatRingOverlays == null) return;

        var rings = _combatRingOverlays.BuildDrawStates();
        if (rings.Count == 0) return;

        foreach (var ring in rings)
        {
            float ringScale = (ring.Scale / SelectionRingMeshRadius) * 1.12f;
            var ringModel = Matrix4.CreateScale(ringScale) *
                            Matrix4.CreateTranslation(ring.Position with { Y = 0.38f });
            GL.UniformMatrix4(_uniformModel, false, ref ringModel);
            GL.Uniform4(_uniformColor, ring.Color with { W = ring.Alpha });
            GL.BindVertexArray(_selectionVao);
            GL.DrawArrays(PrimitiveType.Lines, 0, _selectionVertCount);
        }
    }
}