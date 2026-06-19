using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using SharpOpenGl.Engine.ECS;
using SharpOpenGl.Engine.Input;
using SharpOpenGl.Engine.UI;

namespace SharpOpenGl;

public partial class EngineWindow
{
    private bool _selectionDragActive;
    private bool _selectionBoxVisible;
    private Vector2 _selectionDragStart;
    private Vector2 _selectionDragCurrent;
    private const float SelectionDragThresholdPx = 6f;

    private void BeginSelectionDrag(Vector2 screenPoint)
    {
        _selectionDragActive = true;
        _selectionBoxVisible = false;
        _selectionDragStart = screenPoint;
        _selectionDragCurrent = screenPoint;
    }

    private void CancelSelectionDrag()
    {
        _selectionDragActive = false;
        _selectionBoxVisible = false;
    }

    private void DrawSelectionBox(IUIRenderer renderer)
    {
        Vector2 min = Vector2.ComponentMin(_selectionDragStart, _selectionDragCurrent);
        Vector2 max = Vector2.ComponentMax(_selectionDragStart, _selectionDragCurrent);
        Vector2 size = max - min;

        renderer.DrawRect(min, size, new Vector4(0.2f, 1f, 0.45f, 0.18f));
        renderer.DrawRectOutline(min, size, new Vector4(0.35f, 1f, 0.55f, 0.95f));
    }

    private void HandleBoxSelection(Vector2 start, Vector2 end)
    {
        if (_world == null) return;

        bool shiftHeld = KeyboardState.IsKeyDown(Keys.LeftShift) ||
                         KeyboardState.IsKeyDown(Keys.RightShift);

        if (!shiftHeld)
        {
            foreach (var (_, sel) in _world.Query<SelectionComponent>())
                sel.IsSelected = false;
        }

        Vector2 rectMin = Vector2.ComponentMin(start, end);
        Vector2 rectMax = Vector2.ComponentMax(start, end);
        var viewport = new Vector2(Size.X, Size.Y);
        var projection = Matrix4.CreatePerspectiveFieldOfView(
            MathHelper.DegreesToRadians(45.0f),
            viewport.X / viewport.Y,
            0.1f,
            10000.0f);
        var view = _rtsCamera.GetViewMatrix();

        foreach (var (entity, sel) in _world.Query<SelectionComponent>())
        {
            if (!IsPlayerSelectable(entity)) continue;
            if (!IsBoxSelectableUnit(entity)) continue;

            var transform = _world.GetComponent<TransformComponent>(entity);
            if (transform == null) continue;

            if (!GroundPlaneRaycaster.TryWorldToScreen(
                    transform.Position, viewport, projection, view, out Vector2 screen))
                continue;

            if (!GroundPlaneRaycaster.IsInsideScreenRect(screen, rectMin, rectMax))
                continue;

            sel.IsSelected = true;
        }
    }

    /// <summary>Mobile units included in drag-box selection (ships, miners; not buildings).</summary>
    private bool IsBoxSelectableUnit(Entity entity)
    {
        if (_world == null) return false;
        return _world.HasComponent<MovementComponent>(entity);
    }
}