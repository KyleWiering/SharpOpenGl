using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using SharpOpenGl.Engine.Assets;
using SharpOpenGl.Engine.ECS;
using SharpOpenGl.Engine.Economy;
using SharpOpenGl.Engine.Entities;
using SharpOpenGl.Engine.Events;
using SharpOpenGl.Engine.Grid;
using SharpOpenGl.Engine.Input;
using SharpOpenGl.Engine.Missions;
using SharpOpenGl.Engine.Persistence;
using SharpOpenGl.Engine.Scenes;
using SharpOpenGl.Engine.UI.Screens;

namespace SharpOpenGl;

public partial class EngineWindow
{
    private TriggerSystem? _triggerSystem;
    private ObjectiveSystem? _objectiveSystem;
    private MissionController? _missionController;
    private MissionLoader? _missionLoader;
    private AssetManager? _assetManager;
    private SettingsManager? _settingsManager;
    private SaveManager? _saveManager;
    private string? _pendingMissionId;
    private bool _gameplayEventsHooked;

    private string ResolveGameDataPath()
    {
        string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GameData");
        if (Directory.Exists(path)) return path;
        path = Path.Combine(Directory.GetCurrentDirectory(), "GameData");
        return path;
    }

    private void EnsurePersistence()
    {
        string dataRoot = ResolveGameDataPath();
        string configDir = Path.Combine(dataRoot, "Config");
        _settingsManager ??= new SettingsManager(Path.Combine(configDir, "settings.json"));
        _settingsManager.Load();
        _saveManager ??= new SaveManager(Path.Combine(dataRoot, "Saves"));
    }

    private void EnsureAssets()
    {
        _assetManager ??= new AssetManager(ResolveGameDataPath());
        _missionLoader ??= new MissionLoader(_assetManager);
        _missionController ??= new MissionController(_missionLoader, _resourceManager, _eventBus);
    }

    private void EnsureGameplayEventHooks()
    {
        if (_gameplayEventsHooked) return;
        _gameplayEventsHooked = true;

        _eventBus.Subscribe<UnitDiedEvent>(_ => _triggerSystem?.RecordKill());
        _eventBus.Subscribe<DialogEvent>(evt =>
            Console.WriteLine($"[{evt.Speaker}] {evt.Text}"));
    }

    internal void ShowMissionBriefing(string missionId)
    {
        EnsureAssets();
        var definition = _missionLoader!.Load(missionId);
        if (definition == null)
        {
            Console.WriteLine($"[Mission] Could not load '{missionId}', starting sandbox.");
            _pendingMissionId = null;
            _sceneManager.TransitionTo(SceneGameplay, GameState.Playing);
            return;
        }

        _pendingMissionId = missionId;
        _missionController!.StartMission(missionId);

        var briefing = new BriefingScreen();
        briefing.SetMission(definition);
        briefing.StartRequested += () =>
        {
            _uiManager.Pop();
            _sceneManager.TransitionTo(SceneGameplay, GameState.Playing);
        };
        briefing.BackRequested += () => _uiManager.Pop();
        _uiManager.Push(briefing);
    }

    internal void ShowSettings()
    {
        EnsurePersistence();
        var settings = new SettingsScreen(_settingsManager!);
        settings.BackRequested += () => _uiManager.Pop();
        _uiManager.Push(settings);
    }

    internal void ShowShipDesigner()
    {
        var designer = new ShipDesignerScreen();
        designer.LoadShip("hero_default");
        designer.DesignConfirmed += (_, _, _) => _uiManager.Pop();
        designer.Cancelled += () => _uiManager.Pop();
        _uiManager.Push(designer);
    }

    internal void ContinueSavedGame()
    {
        EnsurePersistence();
        var saves = _saveManager!.ListSaveFiles();
        if (saves.Length == 0)
        {
            Console.WriteLine("[Save] No save files found.");
            return;
        }

        var data = _saveManager.Load(saves[0]);
        if (data == null)
        {
            Console.WriteLine("[Save] Failed to load latest save.");
            return;
        }

        _pendingMissionId = string.IsNullOrWhiteSpace(data.MissionId) ? null : data.MissionId;
        _sceneManager.TransitionTo(SceneGameplay, GameState.Playing);
    }

    private void InitializeWorldCore(string? missionId)
    {
        _world?.Dispose();
        _fighterEntities.Clear();
        _resourceNodeEntities.Clear();
        _aiEntities.Clear();
        _triggerSystem = null;
        _objectiveSystem = null;

        EnsureGameplayEventHooks();

        _world = new World();
        _movementSystem = new MovementSystem();
        _aiSystem = new AIPlayerSystem(MapWorldSize);
        _world.AddSystem(_movementSystem);
        _world.AddSystem(new StanceSystem());
        _world.AddSystem(new CombatSystem(_eventBus));
        _world.AddSystem(new ProjectileSystem(_eventBus));
        _world.AddSystem(_aiSystem);

        _resourceManager = new ResourceManager(_eventBus);
        var playerRes = _resourceManager.AddPlayer(1);
        playerRes.SetStartingAmount(ResourceType.Energy, 500f);
        playerRes.SetStartingAmount(ResourceType.Minerals, 300f);
        playerRes.SetStartingAmount(ResourceType.Data, 100f);
        playerRes.SetStartingAmount(ResourceType.Crew, 50f);
        playerRes.AddIncome(ResourceType.Energy, 5f);
        playerRes.AddIncome(ResourceType.Minerals, 3f);

        _world.AddSystem(new ResourceSystem(_resourceManager));

        LoadEntityDefinitions();
        EnsureAssets();
        _unitFactory = new UnitFactory(_assetManager);
        _buildSystem = new BuildSystem(_unitFactory, id =>
            _definitions.TryGetValue(id, out var def) ? def : null);
        _buildSystem.OnUnitSpawned = (world, spawned, _, building, def) =>
            FinalizeSpawnedUnit(spawned, def, building.PlayerId, isEnemy: false);
        _world.AddSystem(_buildSystem);

        _supplySystem = new SupplySystem(_resourceManager);
        _world.AddSystem(_supplySystem);

        if (!string.IsNullOrEmpty(missionId) && _missionController?.CurrentMission != null)
            SetupMissionWorld(missionId);
        else
            SetupSandboxWorld();

        _missionController?.BeginGameplay();

        if (_missionController?.CurrentMission != null)
        {
            var state = _missionController.CurrentMission;
            state.Phase = MissionPhase.InProgress;
            _objectiveSystem = new ObjectiveSystem(state, _eventBus, _resourceManager) { PlayerId = 1 };
            _triggerSystem = new TriggerSystem(state, _eventBus, _resourceManager, _unitFactory, _assetManager)
            {
                PlayerId = 1,
                OnUnitSpawned = (world, spawned, def, tag) =>
                {
                    bool isEnemy = !string.IsNullOrEmpty(tag) && tag.Contains("enemy", StringComparison.OrdinalIgnoreCase);
                    FinalizeSpawnedUnit(spawned, def, isEnemy ? 2 : 1, isEnemy);
                    if (!string.IsNullOrEmpty(tag))
                        state.RegisterEntityTag(tag, spawned);
                },
            };
            _world.AddSystem(_objectiveSystem);
            _world.AddSystem(_triggerSystem);
        }
    }

    private void SetupSandboxWorld()
    {
        if (_world == null) return;

        SpawnDefaultHeroAndFleet();
        SpawnAIPlayer(new Random(123));
        SpawnResourceNodes(new Random(123));
        SpawnPlayerBase();
        SpawnMiners(new Random(123));

        var heroMovement = _world.GetComponent<MovementComponent>(_heroEntity);
        if (heroMovement != null)
            heroMovement.PathTarget = new Vector3(50f, 0f, 50f);
    }

    private void SetupMissionWorld(string missionId)
    {
        if (_world == null || _missionController?.CurrentMission == null) return;

        var mission = _missionController.CurrentMission.Definition;
        var start = mission.StartConditions;
        var rng = new Random(missionId.GetHashCode());

        if (start?.StartingResources != null)
        {
            var res = _resourceManager!.GetPlayer(1);
            if (res != null)
            {
                res.SetStartingAmount(ResourceType.Energy, start.StartingResources.Energy);
                res.SetStartingAmount(ResourceType.Minerals, start.StartingResources.Minerals);
                res.SetStartingAmount(ResourceType.Data, start.StartingResources.Data);
                res.SetStartingAmount(ResourceType.Crew, start.StartingResources.Crew);
            }
        }

        Vector3 spawnCenter = GridCellToWorld(start?.PlayerSpawn ?? [3, 3]);
        _rtsCamera.Target = spawnCenter;

        if (start?.StartingUnits is { Length: > 0 })
        {
            float spacing = 18f;
            for (int i = 0; i < start.StartingUnits.Length; i++)
            {
                string unitId = start.StartingUnits[i];
                if (!_definitions.TryGetValue(unitId, out var def))
                    def = _assetManager?.Load<EntityDefinition>($"Ships/{unitId}");
                if (def == null) continue;

                float angle = MathF.PI * 2f * i / start.StartingUnits.Length;
                var offset = new Vector3(MathF.Cos(angle) * spacing, 0f, MathF.Sin(angle) * spacing);
                SpawnPlayerUnit(def, spawnCenter + offset, selectFirst: i == 0);
            }
        }
        else
        {
            SpawnDefaultHeroAndFleet();
        }

        SpawnResourceNodes(rng);
        SpawnPlayerBase();
    }

    private static Vector3 GridCellToWorld(int[] cell) =>
        MapCoordinates.GridToWorld(cell[0], cell.Length > 1 ? cell[1] : 0);

    private void SpawnDefaultHeroAndFleet()
    {
        if (_world == null) return;

        _heroEntity = _world.CreateEntity();
        _world.AddComponent(_heroEntity, new TransformComponent { Position = Vector3.Zero, Scale = Vector3.One });
        _world.AddComponent(_heroEntity, new MovementComponent { Speed = 80f, Acceleration = 120f, TurnRate = 180f });
        _world.AddComponent(_heroEntity, new SelectionComponent { IsSelected = true, SelectionRadius = 8f });
        _world.AddComponent(_heroEntity, new RenderComponent
        {
            MeshId = _heroVao, VertexCount = _heroVertCount,
            Color = new Vector4(0.2f, 0.8f, 1.0f, 1f), Visible = true,
            PrimitiveType = (int)PrimitiveType.Triangles,
        });
        _world.AddComponent(_heroEntity, new HeroComponent());

        var rng = new Random(123);
        for (int i = 0; i < 5; i++)
        {
            float x = (rng.NextSingle() - 0.5f) * 400f;
            float z = (rng.NextSingle() - 0.5f) * 400f;
            SpawnSimpleShip(new Vector3(x, 0f, z), _fighterVao, _fighterVertCount,
                new Vector4(0.4f, 1.0f, 0.4f, 1f), 6f, 100f, 150f, 220f);
        }
    }

    private Entity SpawnPlayerUnit(EntityDefinition def, Vector3 position, bool selectFirst)
    {
        Entity entity = _unitFactory!.Create(_world!, def);
        var tf = _world!.GetComponent<TransformComponent>(entity);
        if (tf != null) tf.Position = position;

        FinalizeSpawnedUnit(entity, def, 1, isEnemy: false);
        var sel = _world.GetComponent<SelectionComponent>(entity);
        if (sel != null) sel.IsSelected = selectFirst;

        if (_world.HasComponent<HeroComponent>(entity))
            _heroEntity = entity;

        return entity;
    }

    private void SpawnSimpleShip(Vector3 pos, int vao, int vertCount, Vector4 color,
        float selRadius, float speed, float accel, float turn)
    {
        var fighter = _world!.CreateEntity();
        _world.AddComponent(fighter, new TransformComponent { Position = pos, Scale = Vector3.One });
        _world.AddComponent(fighter, new MovementComponent { Speed = speed, Acceleration = accel, TurnRate = turn });
        _world.AddComponent(fighter, new SelectionComponent { IsSelected = false, SelectionRadius = selRadius });
        _world.AddComponent(fighter, new RenderComponent
        {
            MeshId = vao, VertexCount = vertCount, Color = color, Visible = true,
            PrimitiveType = (int)PrimitiveType.Triangles,
        });
        _fighterEntities.Add(fighter);
    }

    private void FinalizeSpawnedUnit(Entity entity, EntityDefinition def, int playerId, bool isEnemy)
    {
        if (_world == null) return;

        var (vao, vertCount, color) = ResolveMeshForDefinition(def, isEnemy);
        var render = _world.GetComponent<RenderComponent>(entity);
        if (render != null)
        {
            render.MeshId = vao;
            render.VertexCount = vertCount;
            render.Color = color;
            render.Visible = true;
            render.PrimitiveType = (int)PrimitiveType.Triangles;
        }

        if (isEnemy)
        {
            _world.AddComponent(entity, new AIControlledComponent { PlayerId = 2, Aggressiveness = 0.6f });
            _aiEntities.Add(entity);
        }
        else
        {
            if (!_world.HasComponent<SelectionComponent>(entity))
            {
                _world.AddComponent(entity, new SelectionComponent
                {
                    IsSelected = false,
                    SelectionRadius = ResolveSelectionRadius(def),
                });
            }

            var collector = _world.GetComponent<ResourceCollectorComponent>(entity);
            if (collector != null)
            {
                collector.PlayerId = playerId;
                if (collector.DepositTarget == default)
                    collector.DepositTarget = _baseEntity;
            }
        }

        ApplyCombatComponents(entity, isEnemy ? 2 : playerId, isEnemy);
    }

    private void ApplyCombatComponents(Entity entity, int faction, bool isEnemy)
    {
        if (_world == null) return;

        bool canFight = _world.HasComponent<WeaponListComponent>(entity);
        bool canBeTargeted = _world.HasComponent<HealthComponent>(entity);
        if (!canFight && !canBeTargeted) return;

        if (!_world.HasComponent<CombatTargetComponent>(entity))
        {
            _world.AddComponent(entity, new CombatTargetComponent
            {
                Faction = faction,
                Priority = isEnemy ? 10 : _world.HasComponent<HeroComponent>(entity) ? 100 : 20,
            });
        }

        if (canFight && !_world.HasComponent<StanceComponent>(entity))
        {
            _world.AddComponent(entity, new StanceComponent
            {
                CurrentStance = isEnemy ? Stance.Aggressive : Stance.Defensive,
            });
        }
    }

    private float ResolveSelectionRadius(EntityDefinition def)
    {
        string id = def.Id.ToLowerInvariant();
        if (id.Contains("carrier") || id.Contains("cruiser")) return 12f;
        if (id.Contains("destroyer") || id.Contains("bomber")) return 8f;
        if (id.Contains("miner")) return 6f;
        return 7f;
    }

    private (int vao, int vertCount, Vector4 color) ResolveMeshForDefinition(
        EntityDefinition def, bool isEnemy)
    {
        if (isEnemy)
            return (_fighterVao, _fighterVertCount, new Vector4(1f, 0.2f, 0.2f, 1f));

        string id = def.Id.ToLowerInvariant();
        if (id.Contains("hero"))
            return (_heroVao, _heroVertCount, new Vector4(0.2f, 0.8f, 1f, 1f));
        if (id.Contains("bomber"))
            return (_bomberVao, _bomberVertCount, new Vector4(0.9f, 0.5f, 0.2f, 1f));
        if (id.Contains("destroyer"))
            return (_destroyerVao, _destroyerVertCount, new Vector4(0.7f, 0.2f, 0.9f, 1f));
        if (id.Contains("carrier") || id.Contains("cruiser"))
            return (_carrierVao, _carrierVertCount, new Vector4(0.6f, 0.6f, 0.8f, 1f));
        if (id.Contains("miner"))
            return (_minerVao, _minerVertCount, new Vector4(0.9f, 0.8f, 0.2f, 1f));
        if (id.Contains("scout") || id.Contains("fighter"))
            return (_fighterVao, _fighterVertCount, new Vector4(0.4f, 1f, 0.4f, 1f));
        return (_fighterVao, _fighterVertCount, new Vector4(0.5f, 0.8f, 1f, 1f));
    }

    private Vector3? ScreenToWorldGround(Vector2 screenPos)
    {
        var projection = Matrix4.CreatePerspectiveFieldOfView(
            MathHelper.DegreesToRadians(45.0f),
            (float)Size.X / Size.Y,
            0.1f,
            10000.0f);
        return GroundPlaneRaycaster.ScreenToGround(
            screenPos, new Vector2(Size.X, Size.Y), projection, _rtsCamera.GetViewMatrix());
    }

    private bool IsPlayerSelectable(Entity entity)
    {
        if (_world == null) return false;
        if (_world.HasComponent<AIControlledComponent>(entity)) return false;
        if (_world.HasComponent<ResourceNodeComponent>(entity)) return false;
        return _world.HasComponent<SelectionComponent>(entity);
    }

    private bool HasSelectedUnits()
    {
        if (_world == null) return false;
        foreach (var (entity, sel) in _world.Query<SelectionComponent>())
        {
            if (!sel.IsSelected) continue;
            if (IsPlayerSelectable(entity)) return true;
        }
        return false;
    }

    private static float HorizontalDistance(Vector3 a, Vector3 b)
    {
        float dx = a.X - b.X;
        float dz = a.Z - b.Z;
        return MathF.Sqrt(dx * dx + dz * dz);
    }

    private Entity? FindEnemyAt(Vector3 worldPos)
    {
        if (_world == null) return null;

        const float clickRadius = 20f;
        Entity? closest = null;
        float closestDist = float.MaxValue;

        foreach (var (entity, _) in _world.Query<AIControlledComponent>())
        {
            var transform = _world.GetComponent<TransformComponent>(entity);
            var health = _world.GetComponent<HealthComponent>(entity);
            if (transform == null || health == null || health.IsDead) continue;

            float dist = HorizontalDistance(transform.Position, worldPos);
            if (dist < clickRadius && dist < closestDist)
            {
                closestDist = dist;
                closest = entity;
            }
        }

        return closest;
    }

    private void HandleAttackCommand(Entity enemy)
    {
        if (_world == null) return;

        var enemyTransform = _world.GetComponent<TransformComponent>(enemy);
        if (enemyTransform == null) return;

        bool anyAttacker = false;
        foreach (var (entity, sel) in _world.Query<SelectionComponent>())
        {
            if (!sel.IsSelected || !IsPlayerSelectable(entity)) continue;
            if (!_world.HasComponent<WeaponListComponent>(entity)) continue;

            anyAttacker = true;

            var ct = _world.GetComponent<CombatTargetComponent>(entity);
            if (ct == null)
            {
                ct = new CombatTargetComponent { Faction = 1 };
                _world.AddComponent(entity, ct);
            }
            ct.CurrentTarget = enemy;

            var stance = _world.GetComponent<StanceComponent>(entity);
            if (stance == null)
                _world.AddComponent(entity, new StanceComponent { CurrentStance = Stance.Aggressive });
            else
                stance.CurrentStance = Stance.Aggressive;

            var movement = _world.GetComponent<MovementComponent>(entity);
            if (movement != null)
                movement.PathTarget = enemyTransform.Position;
        }

        if (anyAttacker)
        {
            _moveTargetPosition = enemyTransform.Position;
            _moveTargetTimer = 2f;
        }
    }

    private void UpdateCameraControls(float dt)
    {
        float forward = 0f, strafe = 0f, rotate = 0f, height = 0f;

        if (KeyboardState.IsKeyDown(Keys.W) || KeyboardState.IsKeyDown(Keys.Up))
            forward = -1f;
        if (KeyboardState.IsKeyDown(Keys.S) || KeyboardState.IsKeyDown(Keys.Down))
            forward = 1f;
        if (KeyboardState.IsKeyDown(Keys.Q))
            strafe = -1f;
        if (KeyboardState.IsKeyDown(Keys.E))
            strafe = 1f;

        bool unitKeys = HasSelectedUnits();
        if (!unitKeys || KeyboardState.IsKeyDown(Keys.LeftShift) || KeyboardState.IsKeyDown(Keys.RightShift))
        {
            if (KeyboardState.IsKeyDown(Keys.A) || KeyboardState.IsKeyDown(Keys.Left))
                rotate = -1f;
            if (KeyboardState.IsKeyDown(Keys.D) || KeyboardState.IsKeyDown(Keys.Right))
                rotate = 1f;
        }

        if (KeyboardState.IsKeyDown(Keys.Z))
            height = 1f;
        if (KeyboardState.IsKeyDown(Keys.X) && !unitKeys)
            height = -1f;

        _rtsCamera.Pan(strafe, forward, dt);
        _rtsCamera.Rotate(rotate, dt);
        _rtsCamera.AdjustHeight(height, dt);
    }

    private void HandleShipControlCommand(string command)
    {
        switch (command)
        {
            case "stop":
                HandleStopCommand();
                break;
            case "patrol":
                _patrolMode = true;
                _attackMoveMode = false;
                _moveCommandMode = false;
                break;
            case "attack_move":
                _attackMoveMode = true;
                _patrolMode = false;
                _moveCommandMode = false;
                break;
            case "move":
                _moveCommandMode = true;
                _attackMoveMode = false;
                _patrolMode = false;
                break;
        }
    }

    private bool _moveCommandMode;

    private bool TryHandleUnitShortcut(Keys key)
    {
        if (!HasSelectedUnits()) return false;
        if (_uiManager.Current is not GameplayHUD hud) return false;

        char c = key switch
        {
            Keys.M => 'm',
            Keys.S => 's',
            Keys.P => 'p',
            Keys.A => 'a',
            _ => '\0',
        };
        if (c == '\0') return false;
        return hud.ShipControlBar.HandleKeyShortcut(c);
    }

    private void BindShipControlBar()
    {
        if (_world == null) return;
        if (_uiManager.Current is not GameplayHUD hud) return;

        bool anySelected = false;
        bool hasWeapons = false;
        bool hasMovement = false;
        Stance? stance = null;

        foreach (var (entity, sel) in _world.Query<SelectionComponent>())
        {
            if (!sel.IsSelected || !IsPlayerSelectable(entity)) continue;
            anySelected = true;
            if (_world.HasComponent<MovementComponent>(entity)) hasMovement = true;
            if (_world.HasComponent<WeaponListComponent>(entity)) hasWeapons = true;
            var stanceComp = _world.GetComponent<StanceComponent>(entity);
            if (stanceComp != null) stance = stanceComp.CurrentStance;
        }

        hud.BindShipControlBar(hasWeapons, hasMovement, stance, anySelected);
    }
}