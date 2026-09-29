namespace A_U_ClimateScout.Models
{
    // One of the 5 Köppen main groups (A Tropical … E Polar).
    public class ClimateZoneGroup
    {
        public int Id { get; set; }
        public string Code { get; set; } = "";          // A–E
        public string Name { get; set; } = "";          // Tropical
        public string Slug { get; set; } = "";          // a-tropical
        public string Color { get; set; } = "";         // #RRGGBB
        public string? DescriptionHtml { get; set; }
        public int SortOrder { get; set; }

        public List<ClimateZone> Zones { get; set; } = [];
    }
}
