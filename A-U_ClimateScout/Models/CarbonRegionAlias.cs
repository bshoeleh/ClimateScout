namespace A_U_ClimateScout.Models
{
    // Another name for a carbon region, as it appears in a CSV ("United States of America").
    public class CarbonRegionAlias
    {
        public int Id { get; set; }
        public int RegionId { get; set; }
        public string Alias { get; set; } = "";
        public string? Source { get; set; }                 // import profile, or "manual" (mapped on the preview screen)

        public CarbonRegion Region { get; set; } = null!;
    }
}
