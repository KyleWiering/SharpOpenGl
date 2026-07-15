using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using SharpOpenGl.Engine.ECS;
using SharpOpenGl.Engine.Input;
using SharpOpenGl.Engine.UI;
using SharpOpenGl.Engine.UI.Widgets;

namespace SharpOpenGl;

public partial class EngineWindow
{
    private bool _selectionDragActive;
    private bool _selectionBoxVisible;
    private Vector2 _selectionDragStart;
    private Vector2 _selectionDragCurrent;
    private const float SelectionDragThresholdPx = 6f;

    private bool _cameraPanDragActive;
    private bool _cameraPanDragMoved;
    private Vector2 _cameraPanDragLast;
    private Vector2 _cameraPanDragStart;
    private const float CameraPanDragThresholdPx = 8f;

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

    private void BeginCameraPanDrag(Vector2 screenPoint)
    {
        _cameraPanDragActive = true;
        _cameraPanDragMoved = false;
        _cameraPanDragStart = screenPoint;
        _cameraPanDragLast = screenPoint;
    }

    private void CancelCameraPanDrag()
    {
        _cameraPanDragActive = false;
        _cameraPanDragMoved = false;
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
            ClearAllSelections();

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

    private void ClearAllSelections()
    {
        if (_world == null) return;
        foreach (var (_, sel) in _world.Query<SelectionComponent>())
            sel.IsSelected = false;
    }

    private IUIButton? _lastHoveredButton;

    private void UpdateUiPointerState()
    {
        if (_uiManager.Current == null) return;
        var screenPoint = UiMousePosition;
        bool pointerDown = MouseState.IsButtonDown(MouseButton.Left) ||
                           MouseState.IsButtonDown(MouseButton.Right);
        _uiManager.HandlePointerMove(screenPoint, pointerDown, UiViewportSize);

        IUIButton? hovered = _uiManager.FindHoveredButton();
        if (hovered != null && hovered != _lastHoveredButton && hovered.IsEnabled)
            PlayUiHover();
        _lastHoveredButton = hovered;
    }
}