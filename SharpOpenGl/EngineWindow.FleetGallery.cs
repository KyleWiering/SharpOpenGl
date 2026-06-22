using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using SharpOpenGl.Engine.ECS;
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
        }
    }

    private Entity SpawnGalleryUnit(EntityDefinition def, Vector3 position, int playerId, bool selectHero)
    {
        Entity entity = _unitFactory!.Create(_world!, def);
        var tf = _world!.GetComponent<TransformComponent>(entity);
        if (tf != null) tf.Position = position;

        bool isHuman = playerId == _humanPlayerId;
        FinalizeSpawnedUnit(entity, def, playerId, isEnemy: !isHuman);
        ConfigureGalleryShowcaseUnit(entity);

        var sel = _world.GetComponent<SelectionComponent>(entity);
        if (sel != null) sel.IsSelected = selectHero;

        if (_world.HasComponent<HeroComponent>(entity))
            _heroEntity = entity;

        return entity;
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
        ConfigureGalleryShowcaseUnit(building);
    }
}