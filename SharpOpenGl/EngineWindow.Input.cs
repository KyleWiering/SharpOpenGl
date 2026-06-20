using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using SharpOpenGl.Engine.Scenes;
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

        var screenPoint = new Vector2(MousePosition.X, MousePosition.Y);
        var viewport = new Vector2(Size.X, Size.Y);

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
            if (!_cameraPanDragMoved && _sceneManager.State == GameState.Playing && _world != null)
            {
                Vector3? worldPos = ScreenToWorldGround(_cameraPanDragStart);
                if (worldPos != null)
                {
                    _attackMoveMode = false;
                    _patrolMode = false;
                    _moveCommandMode = false;
                    _placementBuildingId = null;
                    if (_uiManager.Current is GameplayHUD cancelHud)
                        cancelHud.ShipControlBar.ClearActiveCommand();
                    HandleMoveCommand(worldPos.Value);
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

    private void ProcessMouseDownRight()
    {
        CancelSelectionDrag();
        _attackMoveMode = false;
        _patrolMode = false;
        _moveCommandMode = false;
        _placementBuildingId = null;
        if (_uiManager.Current is GameplayHUD cancelHud)
            cancelHud.ShipControlBar.ClearActiveCommand();

        var screenPoint = new Vector2(MousePosition.X, MousePosition.Y);
        BeginCameraPanDrag(screenPoint);
    }
}