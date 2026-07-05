using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using SharpOpenGl.Engine.ECS;
using SharpOpenGl.Engine.Scenes;
using SharpOpenGl.Engine.UI;
using SharpOpenGl.Engine.UI.Screens;

namespace SharpOpenGl;

public partial class EngineWindow
{
    protected override void OnMouseMove(MouseMoveEventArgs e)
    {
        base.OnMouseMove(e);
        ProcessMouseMove();
    }

    protected override void OnMouseUp(MouseButtonEventArgs e)
    {
        base.OnMouseUp(e);
        ProcessMouseUp(e);
    }

    private void ProcessMouseMove()
    {
        UpdateUiPointerState();

        if (_sceneManager.State != GameState.Playing)
            return;

        var screenPoint = UiMousePosition;
        var viewport = UiViewportSize;

        if (_cameraPanDragActive && MouseState.IsButtonDown(MouseButton.Right))
        {
            Vector2 delta = screenPoint - _cameraPanDragLast;
            _cameraPanDragLast = screenPoint;
            if (delta.LengthSquared > 0.25f)
            {
                _cameraPanDragMoved = true;
                _rtsCamera.PanByScreenDelta(delta, viewport);
            }
            return;
        }

        if (_world != null)
        {
            UpdateAttackHoverTarget(screenPoint);
            if (_placementBuildingId != null)
                UpdatePlacementPreview();
        }

        if (!_selectionDragActive)
            return;

        _selectionDragCurrent = screenPoint;
        if ((_selectionDragCurrent - _selectionDragStart).Length >= SelectionDragThresholdPx)
            _selectionBoxVisible = true;
    }

    private void ProcessMouseUp(MouseButtonEventArgs e)
    {
        if (e.Button == MouseButton.Right && _cameraPanDragActive)
        {
            if (_sceneManager.State == GameState.Playing && _world != null)
            {
                _attackMoveMode = false;
                _patrolMode = false;
                _moveCommandMode = false;
                _attackMode = false;
                _placementBuildingId = null;
                _placementPreviewValid = false;
                if (_uiManager.Current is GameplayHUD cancelHud)
                {
                    cancelHud.ShipControlBar.ClearActiveCommand();
                    cancelHud.BuildMapPanel.Visible = false;
                }

                var releasePoint = new Vector2(MousePosition.X, MousePosition.Y);
                Entity? attackTarget = HasSelectedUnits()
                    ? ResolveAttackTargetAt(releasePoint, preferHover: !_cameraPanDragMoved)
                    : null;

                if (attackTarget.HasValue)
                {
                    HandleAttackCommand(attackTarget.Value);
                }
                else if (!_cameraPanDragMoved)
                {
                    Vector3? worldPos = ScreenToWorldGround(releasePoint);
                    if (worldPos != null)
                    {
                        bool shiftHeld = KeyboardState.IsKeyDown(Keys.LeftShift) ||
                                         KeyboardState.IsKeyDown(Keys.RightShift);
                        HandleMoveCommand(worldPos.Value, appendWaypoint: shiftHeld);
                    }
                }
            }

            CancelCameraPanDrag();
            return;
        }

        if (e.Button != MouseButton.Left || !_selectionDragActive)
            return;

        if (_sceneManager.State == GameState.Playing && _world != null)
        {
            if (_selectionBoxVisible)
                HandleBoxSelection(_selectionDragStart, _selectionDragCurrent);
            else
            {
                Vector3? worldPos = ScreenToWorldGround(_selectionDragStart);
                if (worldPos != null)
                    HandleSelection(worldPos.Value);
            }
        }

        CancelSelectionDrag();
    }

    private bool TryHandleMenuKey(Keys key)
    {
        if (_uiManager.Current == null)
            return false;

        UIKey? uiKey = key switch
        {
            Keys.Up => UIKey.Up,
            Keys.Down => UIKey.Down,
            Keys.Enter => UIKey.Enter,
            _ => null,
        };

        if (uiKey == null)
            return false;

        if (_uiManager.HandleKey(uiKey.Value))
        {
            if (uiKey.Value == UIKey.Enter)
                PlayUiClick();
            return true;
        }

        return false;
    }

    private void ProcessMouseDownRight()
    {
        CancelSelectionDrag();
        _attackMoveMode = false;
        _patrolMode = false;
        _moveCommandMode = false;
        _attackMode = false;
        _placementBuildingId = null;
        _placementPreviewValid = false;
        if (_uiManager.Current is GameplayHUD cancelHud)
        {
            cancelHud.ShipControlBar.ClearActiveCommand();
            cancelHud.BuildMapPanel.Visible = false;
        }

        var screenPoint = new Vector2(MousePosition.X, MousePosition.Y);
        BeginCameraPanDrag(screenPoint);
    }
}