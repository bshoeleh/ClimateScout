using A_U_ClimateScout.Models;

namespace A_U_ClimateScout.Services.CarbonImport
{
    // One kind of carbon CSV (plan §6). A file's profile is found from its header row, so whoever imports it never
    // has to pick one. Each profile says which column names the region, which kinds of region those names may match,
    // and who published the numbers.
    public record CarbonImportProfile(
        string Name,                                    // stored on the import batch and on aliases ("Ember-Countries")
        string SourceName,                              // matches CarbonDataSource.Name
        string RegionColumn,                            // the column holding the region name
        IReadOnlyList<CarbonRegionType> RegionTypes,    // the kinds of region a name may match
        string? ParentCode,                             // states and provinces must belong to this country ("US")
        IReadOnlyList<string> Headers)                  // the header row that identifies the file
    {
        public static readonly IReadOnlyList<CarbonImportProfile> All =
        [
            new("Ember-Countries", "Ember", "Area", [CarbonRegionType.Country, CarbonRegionType.Aggregate], null,
                ["Area", "Year", "Continent", "Variable", "Unit", "Value"]),
            new("Ember-US-States", "Ember", "State", [CarbonRegionType.State], "US",
                ["Country", "State", "Year", "Variable", "Unit", "Value"]),
            new("CER-Canada", "Canada Energy Regulator", "Province", [CarbonRegionType.Province], "CA",
                ["Country", "Province", "Year", "Variable", "Unit", "Value"]),
        ];

        // The profile whose header names match the file's (any order, ignoring case and surrounding spaces), or null.
        public static CarbonImportProfile? Detect(IReadOnlyList<string> headers) =>
            All.FirstOrDefault(profile =>
                profile.Headers.Count == headers.Count &&
                profile.Headers.All(name => headers.Any(header => string.Equals(header.Trim(), name, StringComparison.OrdinalIgnoreCase))));
    }
}
