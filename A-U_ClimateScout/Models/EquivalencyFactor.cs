namespace A_U_ClimateScout.Models
{
    // An EPA conversion used in calculator results ("= 1,234 gallons of gasoline burned").
    public class EquivalencyFactor
    {
        public string Key { get; set; } = "";               // primary key: "gasoline-gallons", "tree-seedlings"
        public string Label { get; set; } = "";             // "gallons of gasoline burned"
        public decimal TonsCo2ePerUnit { get; set; }        // metric tons CO2e per unit, e.g. 0.008887
        public string? SourceUrl { get; set; }
        public int SortOrder { get; set; }
    }
}
