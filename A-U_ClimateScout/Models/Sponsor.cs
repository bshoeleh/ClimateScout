namespace A_U_ClimateScout.Models
{
    // A sponsor, shown on /sponsors and (if ShowInFooter) in the footer logo strip.
    // Visible = IsActive and today within StartDate–EndDate (plan §3.2).
    public class Sponsor
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public int Tier { get; set; } = 1;                  // 1 = highest; the footer sorts by Tier, then SortOrder
        public int? LogoAssetId { get; set; }               // the footer strip skips sponsors without a logo
        public string? Url { get; set; }
        public string? DescriptionHtml { get; set; }
        public DateOnly? StartDate { get; set; }            // null = no start limit
        public DateOnly? EndDate { get; set; }              // null = open-ended
        public int SortOrder { get; set; }
        public bool IsActive { get; set; } = true;
        public bool ShowInFooter { get; set; } = true;

        public MediaAsset? LogoAsset { get; set; }
    }
}
