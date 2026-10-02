using A_U_ClimateScout.Models;

namespace A_U_ClimateScout.Services.CarbonImport
{
    // Finds the carbon region each CSV row is about (plan §6). Rules, in order:
    //  1. an alias tagged with this file's profile, which may point at any region
    //     ("US Total" in the US-states file is the United States);
    //  2. the region's own name, among the kinds of region the profile allows (and under its parent country),
    //     so "Georgia" in the US-states file can only be the state.
    // Comparisons ignore case. No match, or more than one, becomes a problem; nothing is guessed.
    public static class CarbonRegionMatcher
    {
        public static CarbonMatchResult Match(
            CarbonImportProfile profile,
            IEnumerable<CarbonCsvRow> rows,
            IReadOnlyList<CarbonRegionInfo> regions,
            IReadOnlyList<CarbonAliasInfo> aliases)
        {
            var byAlias = aliases
                .Where(a => string.Equals(a.Source, profile.Name, StringComparison.OrdinalIgnoreCase))
                .ToLookup(a => a.Alias, a => a.RegionId, StringComparer.OrdinalIgnoreCase);
            var byName = regions
                .Where(r => profile.RegionTypes.Contains(r.Type) && (profile.ParentCode is null || r.ParentCode == profile.ParentCode))
                .ToLookup(r => r.Name, r => r.Id, StringComparer.OrdinalIgnoreCase);

            var matched = new List<CarbonMatchedRow>();
            var problems = new List<CarbonCsvProblem>();
            var firstLineFor = new Dictionary<(int RegionId, int Year), int>();
            foreach (var row in rows)
            {
                var ids = byAlias[row.RegionName].Distinct().ToList();
                if (ids.Count == 0)
                {
                    ids = byName[row.RegionName].Distinct().ToList();
                }

                if (ids.Count == 0)
                {
                    problems.Add(new(row.Line, row.RegionName, "No matching region (if it's another name for one, add an alias)."));
                }
                else if (ids.Count > 1)
                {
                    problems.Add(new(row.Line, row.RegionName, "Matches more than one region."));
                }
                else if (firstLineFor.TryGetValue((ids[0], row.Year), out var firstLine))
                {
                    // Two different names for the same region and year, e.g. a name on one line and its alias on another.
                    problems.Add(new(row.Line, row.RegionName, $"Same region and year as line {firstLine}."));
                }
                else
                {
                    firstLineFor[(ids[0], row.Year)] = row.Line;
                    matched.Add(new(row, ids[0]));
                }
            }

            return new CarbonMatchResult(matched, problems);
        }
    }

    // What the matcher needs to know about regions and aliases, so it can run (and be tested) without the database.
    public record CarbonRegionInfo(int Id, string Code, string Name, CarbonRegionType Type, string? ParentCode);
    public record CarbonAliasInfo(int RegionId, string Alias, string? Source);

    public record CarbonMatchedRow(CarbonCsvRow Row, int RegionId);
    public record CarbonMatchResult(IReadOnlyList<CarbonMatchedRow> Matched, IReadOnlyList<CarbonCsvProblem> Problems);
}
