using System.Collections.Concurrent;
using A_U_ClimateScout.Identity;
using A_U_ClimateScout.Options;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace A_U_ClimateScout.Services
{
    // Daily caps for the map tile and address search proxies (plan Phase 9; OpenStreetMap's usage policies forbid
    // heavy use). Counts requests per UTC day in memory; past the cap a request is refused until midnight UTC.
    // When a day's count reaches 80 % of a cap, Admins get one email about it. A cap of 0 means no cap.
    // Per-visitor limits (requests per minute) are the "tiles" and "geocode" rate-limit policies in Program.cs.
    public class ProxyUsage(IOptionsMonitor<MapsOptions> options, IServiceScopeFactory scopes, ILogger<ProxyUsage> logger)
    {
        public const string Tiles = "tiles", Geocode = "geocode";

        private readonly ConcurrentDictionary<(string Kind, DateOnly Day), int> counts = new();
        private readonly ConcurrentDictionary<(string Kind, DateOnly Day), bool> warned = new();

        // Counts one request; false if today's cap is already used up.
        public bool TryUse(string kind)
        {
            var cap = kind == Tiles ? options.CurrentValue.DailyTileCap : options.CurrentValue.DailyGeocodeCap;
            var day = DateOnly.FromDateTime(DateTime.UtcNow);
            var count = counts.AddOrUpdate((kind, day), 1, (_, n) => n + 1);
            foreach (var old in counts.Keys.Where(k => k.Day < day))
            {
                counts.TryRemove(old, out _);
            }

            if (cap <= 0)
            {
                return true;
            }
            if (count == (int)(cap * 0.8) && warned.TryAdd((kind, day), true))
            {
                _ = WarnAsync(kind, count, cap);   // in the background: the visitor's request doesn't wait for email
            }
            if (count > cap)
            {
                if (count == cap + 1)
                {
                    logger.LogWarning("Daily {Kind} cap of {Cap} reached; refusing until midnight UTC.", kind, cap);
                }
                return false;
            }
            return true;
        }

        public int Today(string kind) => counts.GetValueOrDefault((kind, DateOnly.FromDateTime(DateTime.UtcNow)));

        private async Task WarnAsync(string kind, int count, int cap)
        {
            try
            {
                using var scope = scopes.CreateScope();
                var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
                var email = scope.ServiceProvider.GetRequiredService<IEmailService>();
                var what = kind == Tiles ? "map tiles" : "address searches";
                var text = $"ClimateScout has served {count:N0} {what} today (UTC), 80 % of the daily cap of {cap:N0}.\n\n"
                    + $"At the cap, {(kind == Tiles ? "maps show grey squares" : "address search stops working")} until midnight UTC. "
                    + "The cap is the Maps:Daily" + (kind == Tiles ? "TileCap" : "GeocodeCap") + " setting.";
                foreach (var admin in (await users.GetUsersInRoleAsync(Roles.Admin)).Where(u => u.LockoutEnd is null && u.Email is not null))
                {
                    await email.SendAsync(admin.Email!, $"ClimateScout: {what} at 80 % of today's cap", text);
                }
                logger.LogWarning("Daily {Kind} use at {Count} of {Cap} (80 %); Admins emailed.", kind, count, cap);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Couldn't send the {Kind} cap warning.", kind);
            }
        }
    }
}
