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

        // EPA greenhouse gas equivalencies shown in the carbon calculator's infographic (factors from the EPA page,
        // last updated 2026-08-04; mostly 2022 data). Admin can edit them later.
        private const string EpaUrl = "https://www.epa.gov/energy/greenhouse-gas-equivalencies-calculator-calculations-and-references";

        public static IReadOnlyList<EquivalencyFactor> EquivalencyFactors =>
        [
            new() { Key = "gasoline-gallons", Label = "gallons of gasoline consumed", TonsCo2ePerUnit = 0.008887m,
                    Kind = EquivalencyKind.Emissions, Icon = "fuel-pump-fill", UnitSingular = "gallon", UnitPlural = "gallons", SourceUrl = EpaUrl, SortOrder = 1 },
            new() { Key = "vehicle-miles", Label = "miles driven by an average gasoline-powered car", TonsCo2ePerUnit = 0.000393m,
                    Kind = EquivalencyKind.Emissions, Icon = "car-front-fill", UnitSingular = "mile", UnitPlural = "miles", SourceUrl = EpaUrl, SortOrder = 2 },
            new() { Key = "home-electricity", Label = "homes' electricity use for one year", TonsCo2ePerUnit = 4.798m,
                    Kind = EquivalencyKind.Emissions, Icon = "house-fill", UnitSingular = "home", UnitPlural = "homes", SourceUrl = EpaUrl, SortOrder = 3 },
            new() { Key = "smartphones-charged", Label = "smartphones charged", TonsCo2ePerUnit = 0.0000124m,
                    Kind = EquivalencyKind.Emissions, Icon = "lightning-charge-fill", UnitSingular = "charge", UnitPlural = "charges", SourceUrl = EpaUrl, SortOrder = 4 },
            new() { Key = "tree-seedlings", Label = "tree seedlings grown for 10 years", TonsCo2ePerUnit = 0.06047746m,
                    Kind = EquivalencyKind.Absorption, Icon = "tree-fill", UnitSingular = "seedling", UnitPlural = "seedlings", SourceUrl = EpaUrl, SortOrder = 5 },
            new() { Key = "forest-acres", Label = "acres of U.S. forest absorbing CO₂ for one year", TonsCo2ePerUnit = 1.0m,
                    Kind = EquivalencyKind.Absorption, Icon = "tree", UnitSingular = "acre", UnitPlural = "acres", SourceUrl = EpaUrl, SortOrder = 6 },
        ];

        public static IReadOnlyList<CarbonDataSource> CarbonDataSources =>
        [
            new() { Name = "Ember", Url = "https://ember-energy.org/" },
            new() { Name = "Canada Energy Regulator", Url = "https://www.cer-rec.gc.ca/" },
        ];
    }
}
