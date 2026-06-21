using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using SharpOpenGl.Engine.Assets;
using SharpOpenGl.Engine.Build;
using SharpOpenGl.Engine.Combat;
using SharpOpenGl.Engine.Config;
using SharpOpenGl.Engine.ECS;
using SharpOpenGl.Engine.Economy;
using SharpOpenGl.Engine.Entities;
using SharpOpenGl.Engine.Events;
using SharpOpenGl.Engine.Grid;
using SharpOpenGl.Engine.Input;
using SharpOpenGl.Engine.Missions;
using SharpOpenGl.Engine.Persistence;
using SharpOpenGl.Engine.Scenes;
using SharpOpenGl.Engine.UI;
using SharpOpenGl.Engine.UI.Screens;
using SharpOpenGl.Engine.UI.Widgets;

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
    private MultiplayerSetupResult? _pendingSkirmishSetup;
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

    private IEnumerable<MissionEntry> LoadMissionEntriesFromData()
    {
        EnsureAssets();
        string missionsPath = Path.Combine(ResolveGameDataPath(), "Missions");

        if (!Directory.Exists(missionsPath))
        {
            Console.WriteLine("[Mission] No missions directory found, using defaults");
            return DefaultMissionEntries();
        }

        var entries = new List<MissionEntry>();
        foreach (var definition in _missionLoader!.LoadAll(missionsPath))
            entries.Add(ToMissionEntry(definition));

        return entries.Count > 0 ? entries : DefaultMissionEntries();
    }

    private static IEnumerable<MissionEntry> DefaultMissionEntries() =>
    [
        new MissionEntry
        {
            Id = "tutorial_01",
            Title = "Tutorial - First Steps",
            Description = "Learn the basics of fleet command.",
            MapId = "sector_alpha",
            BriefingText = "Commander, sensors have detected an enemy scout in Sector Alpha.",
            ObjectivesPreview = ["Destroy the enemy scout", "Protect your base"],
            PlanetName = "Helios Prime",
            StarMapPosition = new Vector2(0.15f, 0.55f),
            PlanetColor = new Vector4(0.3f, 0.65f, 1f, 1f),
        },
        new MissionEntry
        {
            Id = "example_scenario",
            Title = "First Contact",
            Description = "Encounter unknown hostiles in Sector Alpha.",
            MapId = "sector_alpha",
            BriefingText = "Move your fleet to the waypoint and eliminate hostile scouts.",
            ObjectivesPreview = ["Reach the sector center", "Destroy enemy scouts"],
            PlanetName = "Sigma Drift",
            StarMapPosition = new Vector2(0.35f, 0.78f),
            PlanetColor = new Vector4(0.53f, 0.8f, 1f, 1f),
            PrerequisiteMissionId = "tutorial_01",
            IsLocked = true,
        },
    ];

    private static MissionEntry ToMissionEntry(MissionDefinition definition) =>
        MissionEntryMapper.FromDefinition(definition);

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

    private void InitializeWorldCore(string? missionId, bool skipWorldSpawn = false)
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
        _world.AddSystem(new StanceSystem());
        _world.AddSystem(new CombatSystem(_eventBus));
        _world.AddSystem(new ShieldRegenSystem());
        _world.AddSystem(new ProjectileSystem(_eventBus));
        _abilitySystem = new AbilitySystem(_eventBus);
        _world.AddSystem(_abilitySystem);
        _world.AddSystem(_aiSystem);
        InitializeMapSystems();

        _resourceManager = new ResourceManager(_eventBus);
        var playerRes = _resourceManager.AddPlayer(1);
        playerRes.SetStartingAmount(ResourceType.Energy, 500f);
        playerRes.SetStartingAmount(ResourceType.Minerals, 300f);
        playerRes.SetStartingAmount(ResourceType.Data, 100f);
        playerRes.SetStartingAmount(ResourceType.Crew, 50f);
        playerRes.AddIncome(ResourceType.Energy, 5f);
        playerRes.AddIncome(ResourceType.Minerals, 3f);

        _world.AddSystem(new ResourceSystem(_resourceManager));
        _miningVisualSystem = new MiningVisualSystem
        {
            Meshes = new MiningVisualMeshHandles
            {
                DroneMeshId = _miningDroneVao,
                DroneVertexCount = _miningDroneVertCount,
                EvaMeshId = _evaCrewVao,
                EvaVertexCount = _evaCrewVertCount,
            },
        };
        _world.AddSystem(_miningVisualSystem);

        LoadEntityDefinitions();
        EnsureAssets();
        RegisterProceduralMeshes();
        _unitFactory = new UnitFactory(_assetManager);
        _buildSystem = new BuildSystem(_unitFactory, id =>
            _definitions.TryGetValue(id, out var def) ? def : null);
        _buildSystem.OnUnitSpawned = (world, spawned, _, building, def) =>
            FinalizeSpawnedUnit(spawned, def, building.PlayerId, isEnemy: false);
        _world.AddSystem(_buildSystem);

        _supplySystem = new SupplySystem(_resourceManager);
        _world.AddSystem(_supplySystem);

        if (!skipWorldSpawn)
        {
            if (!string.IsNullOrEmpty(missionId) && _missionController?.CurrentMission != null)
                SetupMissionWorld(missionId);
            else if (_pendingSkirmishSetup != null)
                SetupSkirmishWorld(_pendingSkirmishSetup);
            else
                SetupSandboxWorld();
        }

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

        ConfigureDefaultFactionRaces();
        SpawnDefaultHeroAndFleet();
        RevealAreaAt(Vector3.Zero, 18);
        SpawnAIPlayer(new Random(123));
        SpawnMapContent("sector_alpha");
        SpawnPlayerBase();
        SpawnMiners(new Random(123));

        var heroMovement = _world.GetComponent<MovementComponent>(_heroEntity);
        if (heroMovement != null)
            heroMovement.PathTarget = new Vector3(50f, 0f, 50f);
    }

    private void SetupSkirmishWorld(MultiplayerSetupResult setup)
    {
        if (_world == null) return;

        ConfigureFactionRaces(setup.Players);
        MapDefinition? map = LoadSkirmishMapDefinition(setup.MapId);
        IReadOnlyList<MapSpawnPoint> spawns = map != null
            ? SkirmishMapLogic.ResolveActiveSpawns(map, setup.Players.Count)
            : [];

        if (spawns.Count == 0)
        {
            Console.WriteLine(
                $"[Skirmish] Map '{setup.MapId}' unavailable or missing spawns — using legacy ring layout.");
            SetupSkirmishWorldLegacyRing(setup);
            return;
        }

        int factionIndex = 0;
        foreach (var player in setup.Players.OrderBy(p => p.SlotIndex))
        {
            int playerId = factionIndex + 1;
            MapSpawnPoint spawn = spawns[factionIndex];
            Vector3 spawnCenter = GridCellToWorld(spawn.Position);

            if (player.IsHuman)
            {
                SpawnSkirmishHumanFaction(playerId, spawnCenter);
                _rtsCamera.Target = spawnCenter;
                RevealAreaAt(spawnCenter, 18);
            }
            else
            {
                SpawnSkirmishAiFaction(playerId, spawnCenter);
                RevealAreaAt(spawnCenter, 10);
            }

            SpawnSkirmishPlayerBase(playerId, spawn, player.IsHuman);
            factionIndex++;
        }

        SpawnMapContent(setup.MapId);
    }

    private MapDefinition? LoadSkirmishMapDefinition(string mapId)
    {
        EnsureAssets();
        var map = _assetManager?.Load<MapDefinition>($"Maps/{mapId}");
        if (map == null)
            return null;

        if (!map.Skirmish || SkirmishMapLogic.Validate(map).Count > 0)
        {
            Console.WriteLine($"[Skirmish] Map '{mapId}' failed validation; see SkirmishMapLogic.Validate.");
            return null;
        }

        return map;
    }

    private void SetupSkirmishWorldLegacyRing(MultiplayerSetupResult setup)
    {
        var spawnPositions = ComputeSkirmishSpawnPositions(setup.Players.Count);
        int factionIndex = 0;

        foreach (var player in setup.Players.OrderBy(p => p.SlotIndex))
        {
            int playerId = factionIndex + 1;
            Vector3 spawnCenter = spawnPositions[factionIndex];
            if (player.IsHuman)
            {
                SpawnSkirmishHumanFaction(playerId, spawnCenter);
                _rtsCamera.Target = spawnCenter;
                RevealAreaAt(spawnCenter, 18);
            }
            else
            {
                SpawnSkirmishAiFaction(playerId, spawnCenter);
                RevealAreaAt(spawnCenter, 10);
            }

            factionIndex++;
        }

        SpawnMapContent("sector_alpha");
        SpawnResourceNodes(new Random(setup.Players.Count * 17 + 42));
        SpawnPlayerBase();
    }

    private static Vector3[] ComputeSkirmishSpawnPositions(int playerCount)
    {
        float radius = MapWorldSize * 0.22f;
        var positions = new Vector3[playerCount];
        for (int i = 0; i < playerCount; i++)
        {
            float angle = MathF.PI * 2f * i / Math.Max(1, playerCount);
            positions[i] = new Vector3(
                MathF.Cos(angle) * radius,
                0f,
                MathF.Sin(angle) * radius);
        }

        return positions;
    }

    private void SpawnSkirmishPlayerBase(int playerId, MapSpawnPoint spawn, bool isHumanPlayer)
    {
        if (_world == null || _gridSystem == null) return;

        Vector3 spawnCenter = GridCellToWorld(spawn.Position);
        foreach (var (buildingId, gridX, gridY) in SkirmishMapLogic.ResolveStarterPlacements(spawn))
        {
            Vector3 worldPos = GridCellToWorld([gridX, gridY]);
            Entity? building = SpawnSkirmishBuilding(playerId, buildingId, worldPos, spawnCenter);
            if (building == null) continue;

            if (isHumanPlayer &&
                buildingId.Equals("command_center", StringComparison.OrdinalIgnoreCase))
            {
                _baseEntity = building.Value;
            }
        }
    }

    private Entity? SpawnSkirmishBuilding(
        int playerId,
        string buildingId,
        Vector3 worldPos,
        Vector3 rallyOrigin)
    {
        if (_world == null || _gridSystem == null) return null;
        if (!_definitions.TryGetValue(buildingId, out var def))
        {
            def = _assetManager?.Load<EntityDefinition>($"Bases/{buildingId}");
            if (def != null)
                _definitions[buildingId] = def;
        }

        if (def == null)
        {
            Console.WriteLine($"[Skirmish] Unknown starter building '{buildingId}'.");
            return null;
        }

        string buildingType = def.Components?.Building?.BuildingType ?? buildingId;
        var (meshId, vertCount, scale) = ResolveBuildingMesh(buildingType);

        var building = _world.CreateEntity();
        _world.AddComponent(building, new TransformComponent { Position = worldPos, Scale = scale });
        _world.AddComponent(building, new RenderComponent
        {
            MeshId = meshId,
            VertexCount = vertCount,
            Visible = true,
            PrimitiveType = (int)PrimitiveType.Triangles,
        });
        _world.AddComponent(building, new SelectionComponent { IsSelected = false, SelectionRadius = 15f });

        var healthDef = def.Components?.Health;
        _world.AddComponent(building, new HealthComponent
        {
            MaxHP = healthDef?.MaxHP ?? 1000f,
            CurrentHP = healthDef?.MaxHP ?? 1000f,
            Armor = healthDef?.Armor ?? 50f,
        });

        var buildingDef = def.Components?.Building;
        Vector3 rallyDir = rallyOrigin - worldPos;
        if (rallyDir.LengthSquared < 1f)
            rallyDir = Vector3.UnitX;
        rallyDir.Y = 0f;
        rallyDir = Vector3.Normalize(rallyDir);

        _world.AddComponent(building, new BuildingComponent
        {
            BuildingType = buildingDef?.BuildingType ?? buildingId,
            ProductionRate = buildingDef?.ProductionRate ?? 1f,
            Footprint = buildingDef?.Footprint ?? [2, 2],
            PlayerId = playerId,
            RallyPoint = worldPos + rallyDir * 30f,
            Producible = def.Producible?.ToList() ?? GetDefaultProducible(buildingType),
        });
        _world.AddComponent(building, new EntityNameComponent
        {
            DisplayName = string.IsNullOrWhiteSpace(def.DisplayName)
                ? buildingType.Replace('_', ' ')
                : def.DisplayName,
            DefinitionId = def.Id,
        });

        if (def.Components?.Weapons is { Length: > 0 })
        {
            var wl = new WeaponListComponent();
            foreach (var weapon in def.Components.Weapons)
            {
                wl.Weapons.Add(new WeaponComponent
                {
                    Slot = weapon.Slot,
                    Type = weapon.Type,
                    Damage = weapon.Damage,
                    Range = CombatBalance.ScaleRange(weapon.Range),
                    FireRate = weapon.FireRate,
                    ProjectileType = WeaponProfiles.DefaultProjectileTypeKey(weapon.Type),
                });
            }

            _world.AddComponent(building, wl);
            _world.AddComponent(building, new CombatTargetComponent
            {
                Faction = playerId,
                TargetingMode = TargetPriority.Closest,
                Priority = 50,
            });
        }

        int sight = def.Components?.SightRadius > 0 ? def.Components.SightRadius : 10;
        _world.AddComponent(building, new SightRadiusComponent { Radius = sight });

        BuildingFootprint.Occupy(
            _gridSystem,
            building,
            worldPos,
            buildingDef?.Footprint ?? [2, 2]);

        return building;
    }

    private void SpawnSkirmishHumanFaction(int playerId, Vector3 spawnCenter)
    {
        if (_world == null) return;

        if (_definitions.TryGetValue("hero_default", out var heroDef))
        {
            SpawnPlayerUnit(heroDef, spawnCenter, selectFirst: true);
        }
        else
        {
            SpawnDefaultHeroAndFleet();
            var heroTf = _world.GetComponent<TransformComponent>(_heroEntity);
            if (heroTf != null)
                heroTf.Position = spawnCenter;
        }

        if (_definitions.TryGetValue("fighter_basic", out var fighterDef))
        {
            for (int i = 0; i < 4; i++)
            {
                float angle = MathF.PI * 2f * i / 4f;
                var offset = new Vector3(MathF.Cos(angle) * 24f, 0f, MathF.Sin(angle) * 24f);
                SpawnPlayerUnit(fighterDef, spawnCenter + offset, selectFirst: false);
            }
        }
    }

    private void SpawnSkirmishAiFaction(int playerId, Vector3 spawnCenter)
    {
        if (_world == null || _unitFactory == null) return;

        if (!_definitions.TryGetValue("scout_light", out var scoutDef))
            scoutDef = _definitions.GetValueOrDefault("fighter_basic");
        if (scoutDef == null) return;

        var rng = new Random(playerId * 97);
        for (int i = 0; i < 4; i++)
        {
            float x = spawnCenter.X + (rng.NextSingle() - 0.5f) * 120f;
            float z = spawnCenter.Z + (rng.NextSingle() - 0.5f) * 120f;
            Entity entity = _unitFactory.Create(_world, scoutDef);
            var tf = _world.GetComponent<TransformComponent>(entity);
            if (tf != null)
                tf.Position = new Vector3(x, 1f, z);

            FinalizeSpawnedUnit(entity, scoutDef, playerId, isEnemy: true);
        }
    }

    private void SetupMissionWorld(string missionId)
    {
        if (_world == null || _missionController?.CurrentMission == null) return;

        ConfigureDefaultFactionRaces();
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
        RevealAreaAt(spawnCenter, 18);

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

        string mapId = string.IsNullOrWhiteSpace(mission.Map) ? "sector_alpha" : mission.Map;
        SpawnMapContent(mapId);
        SpawnPlayerBase();
    }

    private static Vector3 GridCellToWorld(int[] cell) =>
        MapCoordinates.GridToWorld(cell[0], cell.Length > 1 ? cell[1] : 0);

    private static Vector3 ShipDisplayScale => Vector3.One * VisualBalance.ShipScaleMultiplier;

    private static void ApplyShipDisplayScale(TransformComponent? transform)
    {
        if (transform != null)
            transform.Scale = ShipDisplayScale;
    }

    private void SpawnDefaultHeroAndFleet()
    {
        if (_world == null) return;

        _heroEntity = _world.CreateEntity();
        _world.AddComponent(_heroEntity, new TransformComponent { Position = Vector3.Zero, Scale = ShipDisplayScale });
        _world.AddComponent(_heroEntity, new MovementComponent { Speed = 80f, Acceleration = 120f, TurnRate = 180f });
        _world.AddComponent(_heroEntity, new SelectionComponent { IsSelected = true, SelectionRadius = 8f });
        _world.AddComponent(_heroEntity, new RenderComponent
        {
            MeshId = _heroVao, VertexCount = _heroVertCount,
            Color = new Vector4(0.2f, 0.8f, 1.0f, 1f), Visible = true,
            PrimitiveType = (int)PrimitiveType.Triangles,
        });
        _world.AddComponent(_heroEntity, new HeroComponent());
        RaceUltimatePolicy.ApplyAtSpawn(_world, _heroEntity, _playerRaceId);

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
        _world.AddComponent(fighter, new TransformComponent { Position = pos, Scale = ShipDisplayScale });
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

        var (vao, vertCount, color) = ResolveMeshForDefinition(def, playerId, isEnemy);
        var render = _world.GetComponent<RenderComponent>(entity);
        if (render != null)
        {
            render.MeshId = vao;
            render.VertexCount = vertCount;
            render.Color = color;
            render.Visible = true;
            render.PrimitiveType = (int)PrimitiveType.Triangles;
        }

        if (!_world.HasComponent<BuildingComponent>(entity))
            ApplyShipDisplayScale(_world.GetComponent<TransformComponent>(entity));

        bool isAiControlled = isEnemy || playerId != _humanPlayerId;
        if (isAiControlled)
        {
            _world.AddComponent(entity, new AIControlledComponent { PlayerId = playerId, Aggressiveness = 0.6f });
            if (!_world.HasComponent<SelectionComponent>(entity))
            {
                _world.AddComponent(entity, new SelectionComponent
                {
                    IsSelected = false,
                    SelectionRadius = ResolveSelectionRadius(def),
                });
            }
            var enemyTf = _world.GetComponent<TransformComponent>(entity);
            if (enemyTf != null) RevealAreaAt(enemyTf.Position, 10);
            _aiEntities.Add(entity);
        }
        else if (playerId == _humanPlayerId)
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

        if (!_world.HasComponent<SightRadiusComponent>(entity))
        {
            int radius = def.Components?.SightRadius > 0
                ? def.Components.SightRadius
                : _world.HasComponent<BuildingComponent>(entity) ? 10 : 5;
            _world.AddComponent(entity, new SightRadiusComponent { Radius = radius });
        }

        ApplyCombatComponents(entity, playerId, isAiControlled);

        if (!_world.HasComponent<BuildingComponent>(entity) && _world.HasComponent<HealthComponent>(entity))
        {
            string raceId = ResolveFactionRaceId(playerId, isEnemy);
            RaceShieldPolicy.ApplyAtSpawn(_world, entity, raceId);
            if (_world.HasComponent<HeroComponent>(entity))
                RaceUltimatePolicy.ApplyAtSpawn(_world, entity, raceId);
        }

        if (!_world.HasComponent<EntityNameComponent>(entity))
        {
            string display = string.IsNullOrWhiteSpace(def.DisplayName)
                ? def.Id.Replace('_', ' ')
                : def.DisplayName;
            if (isAiControlled)
                display = $"Enemy {display}";
            _world.AddComponent(entity, new EntityNameComponent
            {
                DisplayName = display,
                DefinitionId = def.Id,
            });
        }
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
        if (id.Contains("dreadnought")) return 14f;

        if (id.Contains("carrier") || id.Contains("cruiser")) return 12f;

        if (id.Contains("destroyer") || id.Contains("gunship") || id.Contains("frigate")) return 9f;

        if (id.Contains("bomber") || id.Contains("corvette")) return 8f;

        if (id.Contains("drone")) return 5f;

        if (id.Contains("miner")) return 6f;

        return 7f;
    }

    private (int vao, int vertCount, Vector4 color) ResolveMeshForDefinition(
        EntityDefinition def, int playerId, bool isEnemy)
        => ResolveRaceMeshForDefinition(def, playerId, isEnemy);

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
            ct.ManualTarget = true;

            var stance = _world.GetComponent<StanceComponent>(entity);
            if (stance == null)
                _world.AddComponent(entity, new StanceComponent { CurrentStance = Stance.Aggressive });
            else
                stance.CurrentStance = Stance.Aggressive;

            ClearPatrolAndPath(entity);

            var movement = _world.GetComponent<MovementComponent>(entity);
            if (movement != null)
                movement.Velocity = Vector3.Zero;
        }

        if (anyAttacker)
        {
            _moveTargetPosition = enemyTransform.Position;
            _moveTargetTimer = 2f;
        }
    }

    private void UpdateCameraControls(float dt)
    {
        bool unitsSelected = HasSelectedUnits();
        bool shiftHeld = KeyboardState.IsKeyDown(Keys.LeftShift) || KeyboardState.IsKeyDown(Keys.RightShift);

        var axes = CameraInputMapping.Resolve(
            KeyboardState.IsKeyDown(Keys.W) || KeyboardState.IsKeyDown(Keys.Up),
            KeyboardState.IsKeyDown(Keys.S) || KeyboardState.IsKeyDown(Keys.Down),
            KeyboardState.IsKeyDown(Keys.A) || KeyboardState.IsKeyDown(Keys.Left),
            KeyboardState.IsKeyDown(Keys.D) || KeyboardState.IsKeyDown(Keys.Right),
            KeyboardState.IsKeyDown(Keys.Q),
            KeyboardState.IsKeyDown(Keys.E),
            KeyboardState.IsKeyDown(Keys.Z),
            KeyboardState.IsKeyDown(Keys.X),
            unitsSelected,
            shiftHeld);

        _rtsCamera.Pan(axes.Strafe, axes.Forward, dt);
        _rtsCamera.AdjustHeight(axes.Height, dt);
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
                _attackMode = false;
                break;
            case "attack_move":
                _attackMoveMode = true;
                _patrolMode = false;
                _moveCommandMode = false;
                _attackMode = false;
                break;
            case "move":
                _moveCommandMode = true;
                _attackMoveMode = false;
                _patrolMode = false;
                _attackMode = false;
                break;
            case "attack":
                _attackMode = true;
                _attackMoveMode = false;
                _patrolMode = false;
                _moveCommandMode = false;
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
            Keys.T => 't',
            Keys.F => 'a',
            Keys.A => 'a',
            Keys.G => 'g',
            _ => '\0',
        };
        if (c == '\0') return false;
        return hud.ShipControlBar.HandleKeyShortcut(c);
    }


    private string ResolveEntityDisplayName(Entity entity)
    {
        if (_world == null) return "Unit";

        var named = _world.GetComponent<EntityNameComponent>(entity);
        if (named != null && !string.IsNullOrWhiteSpace(named.DisplayName))
            return named.DisplayName;

        if (_world.HasComponent<AIControlledComponent>(entity))
            return "Enemy Ship";

        var building = _world.GetComponent<BuildingComponent>(entity);
        if (building != null)
            return FormatBuildingName(building.BuildingType);

        var collector = _world.GetComponent<ResourceCollectorComponent>(entity);
        if (collector != null)
        {
            string cargo = collector.CarryAmount > 0
                ? $" [{collector.CarryAmount:0}/{collector.CarryCapacity:0}]"
                : "";
            return $"Resource Miner{cargo}";
        }

        if (_world.HasComponent<HeroComponent>(entity))
            return "Hero Command Ship";

        return "Fleet Ship";
    }

    private static string FormatBuildingName(string buildingType) => buildingType switch
    {
        "command_center" => "Command Center",
        "shipyard_small" => "Small Shipyard",
        _                => buildingType.Replace('_', ' '),
    };

    private void BindObjectivePanel()
    {
        if (_uiManager.Current is not GameplayHUD hud) return;

        if (_missionController?.CurrentMission == null)
        {
            hud.ObjectivePanel.Visible = false;
            return;
        }

        var mission = _missionController.CurrentMission;
        hud.ObjectivePanel.Visible = true;
        hud.ObjectivePanel.MissionTitle = mission.Definition.DisplayName;

        var lines = new List<ObjectiveLine>();
        foreach (var obj in mission.PrimaryObjectives)
        {
            string text = obj.Definition.Description;
            if (string.IsNullOrWhiteSpace(text))
                text = obj.Definition.Id.Replace('_', ' ');
            lines.Add(new ObjectiveLine { Text = text, IsCompleted = obj.IsCompleted });
        }

        hud.ObjectivePanel.Objectives = lines;
    }

    private void BindShipControlBar()
    {
        if (_world == null) return;
        if (_uiManager.Current is not GameplayHUD hud) return;

        var selected = GetSelectedPlayerEntities();
        bool anySelected = selected.Count > 0;
        bool hasWeapons = false;
        bool hasMovement = false;
        Stance? stance = null;

        foreach (var entity in selected)
        {
            if (_world.HasComponent<MovementComponent>(entity)) hasMovement = true;
            if (_world.HasComponent<WeaponListComponent>(entity)) hasWeapons = true;
            var stanceComp = _world.GetComponent<StanceComponent>(entity);
            if (stanceComp != null) stance = stanceComp.CurrentStance;
        }

        FormationType? formation = _squadSystem?.GetFormationForSelection(selected, _world);
        bool showFormation = selected.Count > 1 && hasMovement;

        hud.BindShipControlBar(hasWeapons, hasMovement, stance, anySelected, formation, showFormation);
    }
    private void RegisterProceduralMeshes()
    {
        if (_assetManager == null) return;

        foreach (var def in _definitions.Values)
        {
            if (!string.IsNullOrWhiteSpace(def.Mesh))
                _assetManager.RegisterProceduralMesh(def.Mesh);
        }

        _assetManager.RegisterProceduralMesh("meshes/command_center.obj");
        _assetManager.RegisterProceduralMesh("meshes/shipyard_small.obj");

        _assetManager.RegisterProceduralMesh("meshes/shipyard_medium.obj");

        _assetManager.RegisterProceduralMesh("meshes/shipyard_large.obj");
    }

}
