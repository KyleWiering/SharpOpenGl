using OpenTK.Mathematics;
using SharpOpenGl.Engine.ECS;
using SharpOpenGl.Engine.Economy;
using SharpOpenGl.Engine.UI;
using SharpOpenGl.Engine.UI.Screens;
using SharpOpenGl.Engine.UI.Widgets;

namespace SharpOpenGl;

public partial class EngineWindow
{
    private Entity? FindInspectableAt(Vector3 worldPos)
    {
        if (_world == null) return null;

        Entity? closest = null;
        float closestDist = float.MaxValue;

        foreach (var (entity, sel) in _world.Query<SelectionComponent>())
        {
            var transform = _world.GetComponent<TransformComponent>(entity);
            if (transform == null) continue;

            float dist = HorizontalDistance(transform.Position, worldPos);
            float effectiveRadius = sel.SelectionRadius * (_rtsCamera.Height / 100f + 1f);
            if (dist < effectiveRadius && dist < closestDist)
            {
                closestDist = dist;
                closest = entity;
            }
        }

        foreach (var (entity, _) in _world.Query<ResourceNodeComponent>())
        {
            var transform = _world.GetComponent<TransformComponent>(entity);
            if (transform == null) continue;

            float dist = HorizontalDistance(transform.Position, worldPos);
            if (dist < 14f && dist < closestDist)
            {
                closestDist = dist;
                closest = entity;
            }
        }

        foreach (var (entity, _) in _world.Query<AIControlledComponent>())
        {
            var transform = _world.GetComponent<TransformComponent>(entity);
            if (transform == null) continue;

            float dist = HorizontalDistance(transform.Position, worldPos);
            if (dist < 20f && dist < closestDist)
            {
                closestDist = dist;
                closest = entity;
            }
        }

        return closest;
    }

    private UnitInfo? BuildUnitInfoSnapshot(Entity entity)
    {
        if (_world == null) return null;

        var kind = GameplayEntityDisplay.Classify(_world, entity);
        string name = ResolveEntityDisplayName(entity);

        if (_world.HasComponent<ResourceNodeComponent>(entity))
        {
            var node = _world.GetComponent<ResourceNodeComponent>(entity)!;
            return new UnitInfo
            {
                Name = $"{node.ResourceType} Deposit",
                Subtitle = node.IsDepleted
                    ? "Depleted — right-click miner to reassign"
                    : $"Harvestable — {node.Amount:0}/{node.MaxAmount:0} (right-click miner)",
                DisplayKind = EntityDisplayKind.Harvestable,
            };
        }

        var health = _world.GetComponent<HealthComponent>(entity);
        if (health != null)
        {
            string subtitle = kind == EntityDisplayKind.Hostile
                ? "Hostile — right-click selected ships to attack"
                : string.Empty;
            var info = UnitInfo.FromHealth(name, health, kind);
            info = new UnitInfo
            {
                Name = info.Name,
                HPFraction = info.HPFraction,
                ShieldFraction = info.ShieldFraction,
                CurrentHP = info.CurrentHP,
                MaxHP = info.MaxHP,
                CurrentShields = info.CurrentShields,
                MaxShields = info.MaxShields,
                Armor = info.Armor,
                DisplayKind = info.DisplayKind,
                Subtitle = subtitle,
            };
            return info;
        }

        if (_world.HasComponent<BuildingComponent>(entity))
        {
            return new UnitInfo
            {
                Name = name,
                Subtitle = "Structure",
                DisplayKind = EntityDisplayKind.Scenery,
            };
        }

        return new UnitInfo { Name = name, DisplayKind = kind };
    }

    private void BindUnitInfoPanelExtended()
    {
        if (_world == null) return;
        if (_uiManager.Current is not GameplayHUD hud) return;

        var unitInfos = new List<UnitInfo>();
        foreach (var (entity, sel) in _world.Query<SelectionComponent>())
        {
            if (!sel.IsSelected) continue;
            var info = BuildUnitInfoSnapshot(entity);
            if (info != null) unitInfos.Add(info);
            if (unitInfos.Count >= 4) break;
        }

        hud.UnitInfoPanel.SelectedUnits = unitInfos;
    }
}