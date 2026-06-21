using SharpOpenGl.Engine.Config;
using SharpOpenGl.Engine.ECS;

namespace SharpOpenGl.Engine.Entities;

/// <summary>Assigns per-race ultimate weapon abilities to hero ships at spawn.</summary>
public static class RaceUltimatePolicy
{
    public const int UltimateSlot = 2;

    private static readonly (int Slot, string Id, float Cooldown)[] DefaultHeroAbilities =
    [
        (0, "shield_boost", 30f),
        (1, "emp_burst", 60f),
    ];

    /// <summary>
    /// Ensures the hero has default kit abilities (slots 0–1) and the race ultimate on slot 2.
    /// Safe to call on factory-spawned heroes (merges with existing <see cref="AbilityListComponent"/>).
    /// </summary>
    public static void ApplyAtSpawn(World world, Entity entity, string raceId)
    {
        if (!world.HasComponent<HeroComponent>(entity)) return;

        var hero = world.GetComponent<HeroComponent>(entity)!;
        var list = world.GetComponent<AbilityListComponent>(entity) ?? new AbilityListComponent();

        foreach (var (slot, id, cooldown) in DefaultHeroAbilities)
        {
            if (list.GetBySlot(slot) == null)
                AddOrReplaceAbility(list, slot, id, cooldown);
            hero.AbilitySlots[slot] = id;
        }

        if (RaceUltimateSchema.TryGetForRace(raceId, out RaceUltimateDefinition? ultimate))
        {
            AddOrReplaceAbility(list, ultimate.Slot, ultimate.AbilityId, ultimate.Cooldown);
            hero.AbilitySlots[ultimate.Slot] = ultimate.AbilityId;
        }

        if (world.HasComponent<AbilityListComponent>(entity))
            return;

        world.AddComponent(entity, list);
    }

    private static void AddOrReplaceAbility(AbilityListComponent list, int slot, string id, float cooldown)
    {
        var existing = list.GetBySlot(slot);
        if (existing != null)
        {
            existing.Id = id;
            existing.MaxCooldown = cooldown;
            existing.CurrentCooldown = 0f;
            return;
        }

        list.Abilities.Add(new AbilityComponent
        {
            Slot = slot,
            Id = id,
            MaxCooldown = cooldown,
            CurrentCooldown = 0f,
        });
    }
}