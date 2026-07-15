using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using SharpOpenGl.Engine.Build;
using SharpOpenGl.Engine.Combat;
using SharpOpenGl.Engine.Config;
using SharpOpenGl.Engine.ECS;
using SharpOpenGl.Engine.Economy;
using SharpOpenGl.Engine.Entities;
using SharpOpenGl.Engine.Rendering;

namespace SharpOpenGl;

public partial class EngineWindow
{
    private void SpawnFleetGalleryWorld()
    {
        if (_world == null) return;

        _fleetGalleryMode = true;
        ConfigureFleetGalleryFactions();
        Console.WriteLine("[FleetGallery] Building race meshes for 8 zones...");
        PrewarmFleetGalleryMeshes();
        Console.WriteLine("[FleetGallery] Spawning showcase fleets...");

        for (int zone = 0; zone < FleetGalleryLayout.ZoneAnchors.Length; zone++)
        {
            int playerId = FleetGalleryLayout.PlayerIdForZone(zone);
            Vector3 zoneCenter = FleetGalleryLayout.ZoneWorldCenter(zone);
            RevealAreaAt(zoneCenter, 35);

            for (int i = 0; i < FleetGalleryLayout.AllShipIds.Length; i++)
            {
                string unitId = FleetGalleryLayout.AllShipIds[i];
                if (!_definitions.TryGetValue(unitId, out var def))
                    def = _assetManager?.Load<EntityDefinition>($"Ships/{unitId}");
                if (def == null) continue;

                bool selectHero = zone == 0 && i == 0;
                SpawnGalleryUnit(def, zoneCenter + FleetGalleryLayout.ShipOffset(i), playerId, selectHero);
            }

            for (int i = 0; i < FleetGalleryLayout.AllBaseIds.Length; i++)
            {
                string baseId = FleetGalleryLayout.AllBaseIds[i];
                SpawnGalleryBuilding(baseId, zoneCenter + FleetGalleryLayout.BaseOffset(i), playerId);
            }

            if (zone == 0)
                ConfigureZone0GalleryArticulationDrivers(zoneCenter);
        }

        if (_screenshotMode && ScreenshotLaunchOptions.Round2ArticulationGallery)
            FocusRound2ArticulationGalleryCamera();
        else if (_screenshotMode && IsZone0FleetGalleryScreenshot())
            FocusFleetGalleryZone0Camera();
        else if (_screenshotMode && IsTeamOverlayZones01Screenshot())
            FocusFleetGalleryZones01Camera();
    }

    private bool IsZone0FleetGalleryScreenshot() =>
        _screenshotPath.Contains("zone0", StringComparison.OrdinalIgnoreCase);

    private bool IsTeamOverlayZones01Screenshot() =>
        _screenshotPath.Contains("team-overlay-zones01", StringComparison.OrdinalIgnoreCase);

    /// <summary>Frames Terran zone 0 full roster for small-craft vs capital overlap proof captures.</summary>
    private void FocusFleetGalleryZone0Camera()
    {
        _rtsCamera.Target = FleetGalleryLayout.ZoneWorldCenter(0)
            + FleetGalleryLayout.ZoneScreenshotCameraOffset();
        _rtsCamera.Height = FleetGalleryLayout.ZoneScreenshotCameraHeight;
        Console.WriteLine("[Screenshot] Fleet gallery zone=0 terran fleet overview");
    }

    /// <summary>Frames Terran zone-0 medium combat row + station turret row for Round 2 articulation proof.</summary>
    private void FocusRound2ArticulationGalleryCamera()
    {
        _rtsCamera.Target = FleetGalleryLayout.ZoneWorldCenter(0)
            + FleetGalleryLayout.Round2ArticulationScreenshotCameraOffset();
        _rtsCamera.Height = FleetGalleryLayout.Round2ArticulationScreenshotCameraHeight;
        Console.WriteLine("[Screenshot] Round 2 articulation gallery focus");
    }

    /// <summary>Frames Terran zone 0 vs Vesper zone 1 for dual-faction team-overlay proof captures.</summary>
    private void FocusFleetGalleryZones01Camera()
    {
        const int terranZone = 0;
        const int vesperZone = 1;
        const int fighterBasicIndex = 2;
        Vector3 terranFighter = FleetGalleryLayout.ZoneWorldCenter(terranZone)
            + FleetGalleryLayout.ShipOffset(fighterBasicIndex);
        Vector3 vesperFighter = FleetGalleryLayout.ZoneWorldCenter(vesperZone)
            + FleetGalleryLayout.ShipOffset(fighterBasicIndex);
        _rtsCamera.Target = (terranFighter + vesperFighter) * 0.5f;
        _rtsCamera.Height = 420f;
        Console.WriteLine("[Screenshot] Fleet gallery team-overlay zones 0 (terran/P1) + 1 (vesper/P2)");
    }

    private Entity SpawnGalleryUnit(EntityDefinition def, Vector3 position, int playerId, bool selectHero)
    {
        Entity entity = _unitFactory!.Create(_world!, def);
        var tf = _world!.GetComponent<TransformComponent>(entity);
        if (tf != null) tf.Position = position;

        bool isHuman = playerId == _humanPlayerId;
        FinalizeSpawnedUnit(entity, def, playerId, isEnemy: !isHuman);
        ApplyGalleryShipDisplayScale(entity, def.Id);
        ConfigureGalleryShowcaseUnit(entity);

        var sel = _world.GetComponent<SelectionComponent>(entity);
        if (sel != null) sel.IsSelected = selectHero;

        if (_world.HasComponent<HeroComponent>(entity))
            _heroEntity = entity;

        return entity;
    }

    private void ApplyGalleryShipDisplayScale(Entity entity, string definitionId)
    {
        if (_world == null) return;

        var transform = _world.GetComponent<TransformComponent>(entity);
        if (transform == null) return;

        float galleryTaper = FleetGalleryLayout.GalleryScaleMultiplier(definitionId);
        transform.Scale = Vector3.One * VisualBalance.ShipScaleMultiplier * galleryTaper;
    }

    private void SpawnGalleryBuilding(string buildingId, Vector3 position, int playerId)
    {
        if (_world == null) return;

        if (!_definitions.TryGetValue(buildingId, out var def))
        {
            def = _assetManager?.Load<EntityDefinition>($"Bases/{buildingId}");
            if (def != null)
                _definitions[buildingId] = def;
        }

        if (def == null) return;

        string buildingType = def.Components?.Building?.BuildingType ?? buildingId;
        var (meshId, vertCount, scale) = ResolveBuildingMesh(buildingType, playerId);
        string raceId = ResolveFactionRaceId(playerId, isEnemy: playerId != _humanPlayerId);

        var building = _world.CreateEntity();
        _world.AddComponent(building, new TransformComponent { Position = position, Scale = scale });
        var render = new RenderComponent
        {
            MeshId = meshId,
            VertexCount = vertCount,
            Visible = true,
            PrimitiveType = (int)PrimitiveType.Triangles,
        };
        ApplyRaceTexturing(render, raceId, playerId);
        _world.AddComponent(building, render);
        _world.AddComponent(building, new SelectionComponent { IsSelected = false, SelectionRadius = 18f });
        _world.AddComponent(building, new BuildingComponent
        {
            BuildingType = buildingType,
            ProductionRate = 0f,
            Footprint = def.Components?.Building?.Footprint ?? [2, 2],
            Rotates = def.Components?.Building?.Rotates ?? false,
            PlayerId = playerId,
            RallyPoint = position + Vector3.UnitX * 20f,
            Producible = def.Producible?.ToList() ?? GetDefaultProducible(buildingType),
        });
        _world.AddComponent(building, new EntityNameComponent
        {
            DisplayName = def.DisplayName ?? buildingType.Replace('_', ' '),
            DefinitionId = def.Id,
        });
        _world.AddComponent(building, new HealthComponent
        {
            MaxHP = def.Components?.Health?.MaxHP ?? 1500f,
            CurrentHP = def.Components?.Health?.MaxHP ?? 1500f,
            Armor = def.Components?.Health?.Armor ?? 50f,
        });
        _world.AddComponent(building, new CombatTargetComponent { Faction = playerId });
        _world.AddComponent(building, new SightRadiusComponent { Radius = 8 });

        if (def.Components?.Weapons is { Length: > 0 })
        {
            var weaponList = new WeaponListComponent();
            foreach (var weapon in def.Components.Weapons)
            {
                weaponList.Weapons.Add(new WeaponComponent
                {
                    Slot = weapon.Slot,
                    Type = weapon.Type,
                    Damage = weapon.Damage,
                    Range = CombatBalance.ScaleRange(weapon.Range),
                    FireRate = weapon.FireRate,
                    ProjectileType = WeaponProfiles.DefaultProjectileTypeKey(weapon.Type),
                });
            }

            _world.AddComponent(building, weaponList);
        }

        BaseFactory.TrySpawnArticulation(_world, building, buildingType, def);

        if (_gridSystem != null)
        {
            BuildingFootprint.Occupy(
                _gridSystem,
                building,
                position,
                def.Components?.Building?.Footprint ?? [2, 2]);
        }

        ConfigureGalleryShowcaseUnit(building);
    }

    private void ConfigureZone0GalleryArticulationDrivers(Vector3 zoneCenter)
    {
        if (_world == null) return;

        var mockTargets = SpawnFleetGalleryMockTargets(zoneCenter);
        if (mockTargets.Count == 0) return;

        Entity primaryTarget = mockTargets[0];

        foreach (var (entity, _) in _world.Query<WeaponListComponent>().ToList())
        {
            var combat = _world.GetComponent<CombatTargetComponent>(entity);
            if (combat == null) continue;

            combat.CurrentTarget = primaryTarget;
        }

        ApplyZone0SpecialHullMotionSignals(zoneCenter);
        ApplyZone0UtilityMotionSignals(zoneCenter, mockTargets);
    }

    private List<Entity> SpawnFleetGalleryMockTargets(Vector3 zoneCenter)
    {
        if (_world == null) return [];

        Vector3 mediumRowCenter = zoneCenter + (
            FleetGalleryLayout.ShipOffset(FleetGalleryLayout.MediumCombatRowFirstIndex)
            + FleetGalleryLayout.ShipOffset(FleetGalleryLayout.MediumCombatRowLastIndex)) * 0.5f;

        Vector3[] offsets =
        [
            new Vector3(42f, 0f, 18f),
            new Vector3(55f, 0f, -8f),
            new Vector3(28f, 0f, 32f),
        ];

        var targets = new List<Entity>(offsets.Length);
        const int mockFaction = 99;

        foreach (Vector3 offset in offsets)
        {
            Entity target = _world.CreateEntity();
            _world.AddComponent(target, new TransformComponent
            {
                Position = mediumRowCenter + offset,
                Scale = Vector3.One * 0.35f,
            });
            _world.AddComponent(target, new HealthComponent { MaxHP = 50f, CurrentHP = 50f });
            _world.AddComponent(target, new CombatTargetComponent { Faction = mockFaction, Priority = 1 });
            _world.AddComponent(target, new EntityNameComponent
            {
                DisplayName = "Gallery mock target",
                DefinitionId = "gallery_mock_target",
            });
            _world.AddComponent(target, new RenderComponent
            {
                Visible = false,
                MeshId = -1,
                VertexCount = 0,
            });
            targets.Add(target);
        }

        int defenseTurretIndex = Array.IndexOf(FleetGalleryLayout.AllBaseIds, "defense_turret");
        int missileBatteryIndex = Array.IndexOf(FleetGalleryLayout.AllBaseIds, "missile_battery");
        int fortressCoreIndex = Array.IndexOf(FleetGalleryLayout.AllBaseIds, "fortress_core");

        if (defenseTurretIndex >= 0 && targets.Count > 0)
            RepositionMockTarget(targets[0], zoneCenter + FleetGalleryLayout.BaseOffset(defenseTurretIndex) + new Vector3(36f, 0f, 12f));
        if (missileBatteryIndex >= 0 && targets.Count > 1)
            RepositionMockTarget(targets[1], zoneCenter + FleetGalleryLayout.BaseOffset(missileBatteryIndex) + new Vector3(34f, 0f, 10f));
        if (fortressCoreIndex >= 0 && targets.Count > 2)
            RepositionMockTarget(targets[2], zoneCenter + FleetGalleryLayout.BaseOffset(fortressCoreIndex) + new Vector3(40f, 0f, 14f));

        return targets;

        void RepositionMockTarget(Entity target, Vector3 position)
        {
            var tf = _world!.GetComponent<TransformComponent>(target);
            if (tf != null)
                tf.Position = position;
        }
    }

    private void ApplyZone0SpecialHullMotionSignals(Vector3 zoneCenter)
    {
        if (_world == null) return;

        string[] specialHullIds = ["bomber_heavy", "carrier_command", "scout_light", "drone_swarm"];
        var namedEntities = _world.Query<EntityNameComponent>().ToList();
        foreach (string hullId in specialHullIds)
        {
            int shipIndex = Array.IndexOf(FleetGalleryLayout.AllShipIds, hullId);
            if (shipIndex < 0) continue;

            Vector3 expectedPos = zoneCenter + FleetGalleryLayout.ShipOffset(shipIndex);
            foreach (var (entity, name) in namedEntities)
            {
                if (!hullId.Equals(name.DefinitionId, StringComparison.OrdinalIgnoreCase))
                    continue;

                var tf = _world.GetComponent<TransformComponent>(entity);
                if (tf == null || (tf.Position - expectedPos).LengthSquared > 4f)
                    continue;

                if (hullId is "bomber_heavy" or "drone_swarm")
                {
                    var stance = _world.GetComponent<StanceComponent>(entity);
                    if (stance != null)
                        stance.CurrentStance = Stance.Aggressive;
                }

                if (hullId is "fighter_basic" or "interceptor_mk2" or "scout_light" or "drone_swarm")
                {
                    var movement = _world.GetComponent<MovementComponent>(entity);
                    if (movement != null)
                        movement.Velocity = new Vector3(0f, 0f, 12f);
                }
            }
        }
    }

    private void ApplyZone0UtilityMotionSignals(Vector3 zoneCenter, IReadOnlyList<Entity> mockTargets)
    {
        if (_world == null) return;

        int minerIndex = Array.IndexOf(FleetGalleryLayout.AllShipIds, "miner_basic");
        int repairIndex = Array.IndexOf(FleetGalleryLayout.AllShipIds, "support_repair");
        Vector3 minerPos = minerIndex >= 0 ? zoneCenter + FleetGalleryLayout.ShipOffset(minerIndex) : zoneCenter;
        Vector3 repairPos = repairIndex >= 0 ? zoneCenter + FleetGalleryLayout.ShipOffset(repairIndex) : zoneCenter;

        Entity? fakeNode = null;
        Entity? damagedAlly = null;

        foreach (var (entity, name) in _world.Query<EntityNameComponent>().ToList())
        {
            var tf = _world.GetComponent<TransformComponent>(entity);
            if (tf == null) continue;

            if (name.DefinitionId.Equals("miner_basic", StringComparison.OrdinalIgnoreCase)
                && (tf.Position - minerPos).LengthSquared < 4f)
            {
                fakeNode ??= SpawnGalleryResourceNode(minerPos + new Vector3(22f, 0f, 8f));
                var collector = _world.GetComponent<ResourceCollectorComponent>(entity);
                if (collector != null)
                {
                    collector.AssignedNode = fakeNode.Value;
                    collector.State = CollectorState.Collecting;
                }
            }

            if (name.DefinitionId.Equals("support_repair", StringComparison.OrdinalIgnoreCase)
                && (tf.Position - repairPos).LengthSquared < 4f)
            {
                damagedAlly ??= SpawnGalleryDamagedAlly(
                    repairPos + new Vector3(-18f, 0f, 6f),
                    mockTargets.Count > 0 ? mockTargets[0] : Entity.Null);
                var repair = _world.GetComponent<ShipRepairComponent>(entity);
                if (repair != null)
                    repair.ActiveTarget = damagedAlly.Value;
            }
        }
    }

    private Entity SpawnGalleryResourceNode(Vector3 position)
    {
        Entity node = _world!.CreateEntity();
        _world.AddComponent(node, new TransformComponent { Position = position });
        _world.AddComponent(node, new ResourceNodeComponent
        {
            ResourceType = ResourceType.Minerals,
            Amount = 500f,
            MaxAmount = 500f,
        });
        _world.AddComponent(node, new EntityNameComponent { DefinitionId = "gallery_resource_node" });
        return node;
    }

    private Entity SpawnGalleryDamagedAlly(Vector3 position, Entity _)
    {
        Entity ally = _world!.CreateEntity();
        _world.AddComponent(ally, new TransformComponent { Position = position });
        _world.AddComponent(ally, new HealthComponent { MaxHP = 100f, CurrentHP = 18f });
        _world.AddComponent(ally, new CombatTargetComponent { Faction = _humanPlayerId });
        _world.AddComponent(ally, new EntityNameComponent { DefinitionId = "fighter_basic" });
        return ally;
    }
}