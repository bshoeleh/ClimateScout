namespace A_U_ClimateScout.Models
{
    // Who published the carbon numbers (Ember, Canada Energy Regulator …).
    public class CarbonDataSource
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string? Url { get; set; }
        public string? Notes { get; set; }
    }
}
