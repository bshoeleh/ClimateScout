namespace A_U_ClimateScout.Models
{
    // The carbon comparison page: every region with a current value (for the location list and the chart), the region
    // chosen on the map (if it has a value), the name of a chosen region without one, the page's content blocks,
    // and the data for the page script (serialised as JSON).
    public record CarbonComparisonViewModel(
        IReadOnlyList<CarbonComparisonRegion> Regions,
        CarbonComparisonRegion? Selected,
        string? NoDataRegionName,
        IReadOnlyDictionary<string, string> Blocks,
        object PageData);

    public record CarbonComparisonRegion(string Code, string Name, CarbonRegionType Type, string? Continent,
        decimal Value, int Year, string Source, string? SourceUrl);
}
