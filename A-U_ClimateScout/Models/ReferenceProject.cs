namespace A_U_ClimateScout.Models
{
    // An example building shown on a design strategy page.
    public class ReferenceProject
    {
        public int Id { get; set; }
        public int StrategyId { get; set; }
        public string Name { get; set; } = "";
        public string? Location { get; set; }           // "San Antonio, Texas"
        public string? Sector { get; set; }
        public string? Url { get; set; }
        public int? ImageAssetId { get; set; }
        public int SortOrder { get; set; }

        public DesignStrategy Strategy { get; set; } = null!;
        public MediaAsset? ImageAsset { get; set; }
    }
}
