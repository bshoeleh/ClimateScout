namespace A_U_ClimateScout.Models
{
    // A country, US state, Canadian province, or aggregate (World, ASEAN …) with grid carbon data.
    public class CarbonRegion
    {
        public int Id { get; set; }
        public string Code { get; set; } = "";              // ISO 3166: "US", "US-AL", "CA-AB"; aggregates "AGG-ASEAN"
        public string Name { get; set; } = "";
        public CarbonRegionType RegionType { get; set; }
        public int? ParentRegionId { get; set; }            // US-AL → US
        public string? Continent { get; set; }              // "Asia"; null for aggregates
        public bool ShowOnMap { get; set; } = true;         // aggregates: false
        public int? PreferredSourceId { get; set; }         // which source's latest year is the "current" value

        public CarbonRegion? ParentRegion { get; set; }
        public CarbonDataSource? PreferredSource { get; set; }
        public List<CarbonRegionAlias> Aliases { get; set; } = [];
        public List<CarbonIntensity> Intensities { get; set; } = [];
    }
}
