namespace A_U_ClimateScout.Models
{
    // One of the 4 building diagrams (Hot-Humid, Hot-Dry, Temperate, Cold) shown on zone pages.
    public class Diagram
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Slug { get; set; } = "";          // hot-humid
        public int? SvgAssetId { get; set; }            // SVG with a ds-{strategy-slug} layer per strategy; null until imported

        public MediaAsset? SvgAsset { get; set; }
    }
}
