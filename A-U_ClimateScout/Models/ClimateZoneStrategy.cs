namespace A_U_ClimateScout.Models
{
    // Which design strategies apply to which climate zone (many-to-many).
    public class ClimateZoneStrategy
    {
        public int ZoneId { get; set; }
        public int StrategyId { get; set; }
        public int SortOrder { get; set; }

        public ClimateZone Zone { get; set; } = null!;
        public DesignStrategy Strategy { get; set; } = null!;
    }
}
