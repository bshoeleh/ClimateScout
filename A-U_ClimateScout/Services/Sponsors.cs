using A_U_ClimateScout.Data;
using A_U_ClimateScout.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace A_U_ClimateScout.Services
{
    // The sponsors visitors see (plan §3.2): active, and today within their start and end dates (open-ended when
    // empty), ordered by tier (1 first) then sort order. Kept in memory for 10 minutes because the footer shows them
    // on every page; Admin will clear the cache (CacheKey) when a sponsor is saved.
    public class Sponsors(ApplicationDbContext db, IMemoryCache cache)
    {
        public const string CacheKey = "sponsors:visible";

        public async Task<IReadOnlyList<Sponsor>> VisibleAsync(CancellationToken cancellationToken = default) =>
            await cache.GetOrCreateAsync(CacheKey, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10);
                var today = DateOnly.FromDateTime(DateTime.Today);
                return (IReadOnlyList<Sponsor>)await db.Sponsors.AsNoTracking()
                    .Include(s => s.LogoAsset)
                    .Where(s => s.IsActive
                        && (s.StartDate == null || s.StartDate <= today)
                        && (s.EndDate == null || s.EndDate >= today))
                    .OrderBy(s => s.Tier).ThenBy(s => s.SortOrder).ThenBy(s => s.Name)
                    .ToListAsync(cancellationToken);
            }) ?? [];
    }
}
