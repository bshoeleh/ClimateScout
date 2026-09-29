namespace A_U_ClimateScout.Models
{
    // A Köppen sub-zone (31 of them), e.g. Cfa Humid Subtropical.
    public class ClimateZone
    {
        public int Id { get; set; }
        public int GroupId { get; set; }
        public string KoppenCode { get; set; } = "";    // Cfa
        public string Name { get; set; } = "";          // Humid Subtropical
        public string Slug { get; set; } = "";          // cfa-humid-subtropical (URL /zone/{slug})
        public string? DescriptionHtml { get; set; }
        public string Color { get; set; } = "";         // #RRGGBB, map fill
        public int? MapId { get; set; }                 // joins to property "n" in koppen.json
        public int? DiagramId { get; set; }
        public int SortOrder { get; set; }
        public bool IsActive { get; set; } = true;

        public ClimateZoneGroup Group { get; set; } = null!;
        public Diagram? Diagram { get; set; }
        public List<ClimateZoneStrategy> Strategies { get; set; } = [];
    }
}
