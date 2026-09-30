using A_U_ClimateScout.Models;

namespace A_U_ClimateScout.Tools
{
    // Lookup data created by "tool init reference". Values come from the old WordPress site.
    // Only missing rows are created; existing rows (possibly edited in Admin) are never overwritten.
    public static class ReferenceData
    {
        public static readonly string[] Roles = [Identity.Roles.Admin, Identity.Roles.Editor];

        // Köppen main groups; colours are the old site's heading colours.
        public static IReadOnlyList<ClimateZoneGroup> ZoneGroups =>
        [
            new() { Code = "A", Name = "Tropical", Slug = "a-tropical", Color = "#0000FF", SortOrder = 1 },
            new() { Code = "B", Name = "Arid", Slug = "b-arid", Color = "#FF0000", SortOrder = 2 },
            new() { Code = "C", Name = "Temperate", Slug = "c-temperate", Color = "#00FF00", SortOrder = 3 },
            new() { Code = "D", Name = "Continental", Slug = "d-continental", Color = "#FF00FF", SortOrder = 4 },
            new() { Code = "E", Name = "Polar", Slug = "e-polar", Color = "#B2B2B2", SortOrder = 5 },
        ];

        // The 4 building diagrams; their SVG files are added by the import (Phase 3).
        public static IReadOnlyList<Diagram> Diagrams =>
        [
            new() { Name = "Hot-Humid", Slug = "hot-humid" },
            new() { Name = "Hot-Dry", Slug = "hot-dry" },
            new() { Name = "Temperate", Slug = "temperate" },
            new() { Name = "Cold", Slug = "cold" },
        ];

        // The two equivalencies the old carbon comparison page showed (EPA greenhouse gas equivalencies).
        private const string EpaUrl = "https://www.epa.gov/energy/greenhouse-gas-equivalencies-calculator-calculations-and-references";

        public static IReadOnlyList<EquivalencyFactor> EquivalencyFactors =>
        [
            new() { Key = "gasoline-gallons", Label = "gallons of gasoline consumed", TonsCo2ePerUnit = 0.008887m, SourceUrl = EpaUrl, SortOrder = 1 },
            new() { Key = "tree-seedlings", Label = "tree seedlings grown for 10 years", TonsCo2ePerUnit = 0.06047746m, SourceUrl = EpaUrl, SortOrder = 2 },
        ];

        public static IReadOnlyList<CarbonDataSource> CarbonDataSources =>
        [
            new() { Name = "Ember", Url = "https://ember-energy.org/" },
            new() { Name = "Canada Energy Regulator", Url = "https://www.cer-rec.gc.ca/" },
        ];
    }
}
