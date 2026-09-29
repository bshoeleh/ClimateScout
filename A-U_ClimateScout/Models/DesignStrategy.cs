namespace A_U_ClimateScout.Models
{
    // A design strategy, e.g. Cool Roof (27 of them).
    public class DesignStrategy
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Slug { get; set; } = "";          // cool-roof (URL /design-strategy/{slug}; diagram layer ds-cool-roof)
        public string? Summary { get; set; }            // plain text, from the WordPress excerpt
        public string? BodyHtml { get; set; }
        public int? ImageAssetId { get; set; }          // featured image
        public int? DiagramImageAssetId { get; set; }   // illustration shown with the diagram
        public string? Palette2030Url { get; set; }     // link to 2030palette.org
        public int SortOrder { get; set; }
        public bool IsActive { get; set; } = true;

        public MediaAsset? ImageAsset { get; set; }
        public MediaAsset? DiagramImageAsset { get; set; }
        public List<ClimateZoneStrategy> Zones { get; set; } = [];
        public List<StrategyConflict> Conflicts { get; set; } = [];
        public List<ReferenceProject> ReferenceProjects { get; set; } = [];
    }
}
