namespace A_U_ClimateScout.Models
{
    // What the zone page shows: the zone with its strategies, the building diagram ready to inline
    // (null if the zone has none), which strategies have a layer in it, and each strategy's conflicts.
    public record ZonePageViewModel(
        ClimateZone Zone,
        string? DiagramMarkup,
        IReadOnlySet<string> DiagramLayers,
        ILookup<string, string> ConflictsBySlug);
}
