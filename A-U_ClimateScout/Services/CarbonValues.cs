using A_U_ClimateScout.Data;
using Microsoft.EntityFrameworkCore;

namespace A_U_ClimateScout.Services
{
    // The current grid carbon intensity of each region (plan §6): from the region's preferred source if it has a
    // value, otherwise from any source; the latest year wins. Superseded values (replaced by a later import) are
    // ignored. Used by the carbon map now, and the comparison page and API later.
    public class CarbonValues(ApplicationDbContext db)
    {
        public async Task<IReadOnlyDictionary<int, CurrentCarbonValue>> CurrentByRegionIdAsync(CancellationToken cancellationToken)
        {
            // A few hundred rows, so the choice is made in memory.
            var values = await db.CarbonIntensities.AsNoTracking()
                .Where(i => i.SupersededByBatchId == null)
                .Select(i => new
                {
                    i.RegionId,
                    i.Year,
                    i.ValueGPerKWh,
                    IsPreferred = i.SourceId == i.Region.PreferredSourceId,
                    Source = i.Source.Name,
                    SourceUrl = i.Source.Url,
                })
                .ToListAsync(cancellationToken);

            return values
                .GroupBy(v => v.RegionId)
                .ToDictionary(
                    g => g.Key,
                    g => g.OrderByDescending(v => v.IsPreferred).ThenByDescending(v => v.Year)
                        .Select(v => new CurrentCarbonValue(v.ValueGPerKWh, v.Year, v.Source, v.SourceUrl))
                        .First());
        }
    }

    public record CurrentCarbonValue(decimal ValueGPerKWh, int Year, string Source, string? SourceUrl);
}
