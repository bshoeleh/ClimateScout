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
        public EquivalencyKind Kind { get; set; }            // what the amount means: emissions "the same as", or nature needed to absorb them
        public string? Icon { get; set; }                   // Bootstrap Icons name for the infographic, e.g. "fuel-pump-fill"
        public string UnitSingular { get; set; } = "";     // for the scale note: "1 gallon" …
        public string UnitPlural { get; set; } = "";       // … "200 gallons"
    }

    // Which part of the calculator's infographic an equivalency belongs to. Stored as text in the database.
    public enum EquivalencyKind
    {
        Emissions,     // "The same as": gasoline burned, miles driven, homes powered, phones charged
        Absorption     // "To absorb it you would need": tree seedlings, acres of forest
    }
}
