namespace A_U_ClimateScout.Models
{
    // What kind of area a carbon region is. Stored as text in the database.
    public enum CarbonRegionType
    {
        Country,
        State,
        Province,
        Aggregate      // World, ASEAN, Africa … (in the data, but not drawn on the map)
    }

    // Where a carbon CSV upload is in its life. Stored as text in the database.
    public enum CarbonImportStatus
    {
        Preview,       // parsed and matched, waiting for an admin to commit
        Committed,     // values written
        RolledBack     // values removed again, previous values restored
    }
}
