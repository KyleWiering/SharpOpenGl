using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using SharpOpenGl.Engine.ECS;
using SharpOpenGl.Engine.Entities;
using SharpOpenGl.Engine.Persistence;
using SharpOpenGl.Engine.Scenes;
using SharpOpenGl.Engine.UI;
using SharpOpenGl.Engine.UI.Screens;

namespace SharpOpenGl;

public partial class EngineWindow
{
    private SaveData? _pendingSaveData;

    internal void ContinueSavedGame()
    {
        EnsurePersistence();
        _pendingSaveData = _saveManager!.LoadLatest();
        if (_pendingSaveData == null)
        {
            Console.WriteLine("[Save] No save files found.");
            return;
        }

        _sceneManager.TransitionTo(SceneGameplay, GameState.Playing);
    }

    internal void LoadSavedGame(string slotName)
    {
        EnsurePersistence();
        _pendingSaveData = _saveManager!.Load(slotName);
        if (_pendingSaveData == null)
        {
            Console.WriteLine($"[Save] Failed to load '{slotName}'.");
            return;
        }

        _sceneManager.TransitionTo(SceneGameplay, GameState.Playing);
    }

    internal void ShowLoadGameScreen()
    {
        EnsurePersistence();
        var loadScreen = new LoadGameScreen(_saveManager!);
        loadScreen.LoadRequested += slot =>
        {
            _uiManager.Clear();
            LoadSavedGame(slot);
        };
        loadScreen.BackRequested += () => _uiManager.Pop();
        _uiManager.Push(loadScreen);
    }

    private void ShowSaveGameScreen()
    {
        EnsurePersistence();
        var saveScreen = new SaveGameScreen(_saveManager!);
        saveScreen.SlotSelected += slot =>
            saveScreen.RequestSave(slot, () => BuildSaveSnapshot(slot));
        saveScreen.SaveCompleted += _ => Console.WriteLine("[Save] Game saved.");
        saveScreen.Cancelled += () => _uiManager.Pop();
        _uiManager.Push(saveScreen);
    }

    private SaveData BuildSaveSnapshot(string slotName)
    {
        return WorldSaveService.Capture(new WorldSaveContext
        {
            World = _world!,
            ResourceManager = _resourceManager!,
            MissionState = _missionController?.CurrentMission,
            GridSystem = _gridSystem!,
            FogOfWar = _fogOfWar!,
            FogPlayerId = 0,
            CameraX = _rtsCamera.Target.X,
            CameraY = _rtsCamera.Target.Z,
            CameraZoom = _rtsCamera.Height / 80f,
            SlotName = slotName,
        });
    }

    private void ApplySaveData(SaveData data)
    {
        if (_world == null || _gridSystem == null || _fogOfWar == null || _unitFactory == null)
            return;

        var result = WorldLoadService.Restore(new WorldLoadContext
        {
            World = _world,
            ResourceManager = _resourceManager!,
            MissionState = _missionController?.CurrentMission,
            GridSystem = _gridSystem,
            FogOfWar = _fogOfWar,
            FogPlayerId = 0,
            UnitFactory = _unitFactory,
            ResolveDefinition = ResolveDefinitionForLoad,
            FinalizeUnit = FinalizeLoadedEntity,
        }, data);

        if (result.HeroEntity != Entity.Null)
            _heroEntity = result.HeroEntity;
        if (result.CommandCenterEntity != Entity.Null)
            _baseEntity = result.CommandCenterEntity;

        _rtsCamera.Target = new Vector3(data.CameraX, 0f, data.CameraY);
        if (data.CameraZoom > 0f)
        {
            _rtsCamera.Height = MathHelper.Clamp(
                data.CameraZoom * 80f,
                _rtsCamera.MinHeight,
                _rtsCamera.MaxHeight);
        }

        BindResourceHUD();
        BindObjectivePanel();
        BindShipControlBar();
    }

    private void FinalizeLoadedEntity(Entity entity, EntityDefinition? def, int playerId, bool isEnemy)
    {
        if (_world == null) return;

        if (def != null)
        {
            FinalizeSpawnedUnit(entity, def, playerId, isEnemy);

            if (_world.HasComponent<BuildingComponent>(entity))
            {
                var transform = _world.GetComponent<TransformComponent>(entity);
                var building = _world.GetComponent<BuildingComponent>(entity);
                if (transform != null && building != null)
                    RegisterExistingBuildingOccupancy(entity, transform.Position, building);
            }

            if (_world.HasComponent<AIControlledComponent>(entity))
                _aiEntities.Add(entity);
            else if (_world.HasComponent<MovementComponent>(entity) && !isEnemy)
                _fighterEntities.Add(entity);

            return;
        }

        if (_world.HasComponent<ResourceNodeComponent>(entity))
        {
            var transform = _world.GetComponent<TransformComponent>(entity);
            var node = _world.GetComponent<ResourceNodeComponent>(entity);
            if (transform == null || node == null) return;

            _world.AddComponent(entity, new RenderComponent
            {
                MeshId = _resourceNodeVao,
                VertexCount = _resourceNodeVertCount,
                Color = GameplayEntityDisplay.HarvestableColor,
                Visible = true,
                PrimitiveType = (int)PrimitiveType.Triangles,
            });
            _resourceNodeEntities.Add(entity);
        }
    }

    private EntityDefinition? ResolveDefinitionForLoad(string templateId)
    {
        if (_definitions.TryGetValue(templateId, out EntityDefinition? def))
            return def;

        def = _assetManager?.Load<EntityDefinition>($"Ships/{templateId}");
        if (def != null)
        {
            _definitions[templateId] = def;
            return def;
        }

        def = _assetManager?.Load<EntityDefinition>($"Bases/{templateId}");
        if (def != null)
        {
            _definitions[templateId] = def;
            return def;
        }

        def = _assetManager?.Load<EntityDefinition>($"Units/{templateId}");
        if (def != null)
        {
            _definitions[templateId] = def;
            return def;
        }

        return null;
    }
}