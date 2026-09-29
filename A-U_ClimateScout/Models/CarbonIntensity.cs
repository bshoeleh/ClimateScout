namespace A_U_ClimateScout.Models
{
    // Grid carbon intensity (grams of CO2 per kWh) for one region, source and year.
    // Rows are never edited: an import that changes a value adds a new row and marks the old one
    // superseded, so a batch can be rolled back by deleting its rows and un-marking the ones it replaced.
    public class CarbonIntensity
    {
        public int Id { get; set; }
        public int RegionId { get; set; }
        public int SourceId { get; set; }
        public int Year { get; set; }
        public decimal ValueGPerKWh { get; set; }
        public int ImportBatchId { get; set; }              // the batch that added this row
        public int? SupersededByBatchId { get; set; }       // set when a later batch replaced the value; null = current

        public CarbonRegion Region { get; set; } = null!;
        public CarbonDataSource Source { get; set; } = null!;
        public CarbonImportBatch ImportBatch { get; set; } = null!;
        public CarbonImportBatch? SupersededByBatch { get; set; }
    }
}
