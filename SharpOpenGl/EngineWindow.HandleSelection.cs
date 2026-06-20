using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using SharpOpenGl.Engine.ECS;

namespace SharpOpenGl;

public partial class EngineWindow
{
    private void HandleSelection(Vector3 worldPos)
    {
        if (_world == null) return;

        bool shiftHeld = KeyboardState.IsKeyDown(Keys.LeftShift) ||
                         KeyboardState.IsKeyDown(Keys.RightShift);

        Entity? hitEntity = FindInspectableAt(worldPos);

        if (!shiftHeld)
            ClearAllSelections();

        if (!hitEntity.HasValue) return;

        if (_world.HasComponent<SelectionComponent>(hitEntity.Value))
        {
            var sel = _world.GetComponent<SelectionComponent>(hitEntity.Value)!;
            sel.IsSelected = shiftHeld ? !sel.IsSelected : true;
            return;
        }

        if (_world.HasComponent<ResourceNodeComponent>(hitEntity.Value) ||
            _world.HasComponent<AIControlledComponent>(hitEntity.Value))
        {
            _world.AddComponent(hitEntity.Value, new SelectionComponent
            {
                IsSelected = true,
                SelectionRadius = _world.HasComponent<ResourceNodeComponent>(hitEntity.Value) ? 14f : 12f,
            });
        }
    }
}