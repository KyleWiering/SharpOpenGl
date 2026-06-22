namespace SharpOpenGl.Engine.Rendering;

/// <summary>
/// Enumerates every race × ship hull × station substrate for validation and gallery layout.
/// </summary>
public static class RaceSubstrateCatalog
{
    public sealed record SubstrateEntry(string RaceId, string ModelId, SubstrateKind Kind, RaceSubstrateProfile Profile);

    public enum SubstrateKind { Ship, Station }

    public static IEnumerable<SubstrateEntry> AllEntries()
    {
        RaceVisualSchema.Load();
        foreach (var race in RaceVisualSchema.AllRaces)
        {
            RaceSubstrateProfile profile = RaceSubstrateProfile.ForRace(race);
            foreach (string hull in FleetGalleryLayout.AllShipIds)
                yield return new SubstrateEntry(race.Id, hull, SubstrateKind.Ship, profile);

            foreach (string station in FleetGalleryLayout.AllBaseIds)
                yield return new SubstrateEntry(race.Id, station, SubstrateKind.Station, profile);
        }
    }

    public static int TotalEntryCount =>
        RaceVisualSchema.AllRaces.Count * (FleetGalleryLayout.AllShipIds.Length + FleetGalleryLayout.AllBaseIds.Length);

    public static RaceSubstrateProfile ProfileForRace(string raceId)
    {
        RaceVisualSchema.TryGetRace(raceId, out RaceVisualDefinition? race);
        race ??= RaceVisualSchema.AllRaces.FirstOrDefault() ?? new RaceVisualDefinition { Id = RaceShipMeshes.DefaultRace };
        return RaceSubstrateProfile.ForRace(race);
    }
}