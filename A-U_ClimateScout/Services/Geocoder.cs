using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using A_U_ClimateScout.Options;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace A_U_ClimateScout.Services
{
    // Address search for the maps (plan §5): asks the configured provider (Nominatim for now) from the server.
    // Results are only shown, never stored ("temporary" geocoding); they're kept in memory for a day so repeat
    // searches don't reach the provider. Requests are spaced at least a second apart, as Nominatim's policy requires.
    // Registered as a singleton, so the spacing applies to the whole site.
    public class Geocoder(IHttpClientFactory httpClientFactory, IOptions<MapsOptions> options, IMemoryCache cache)
    {
        private static readonly TimeSpan MinimumInterval = TimeSpan.FromSeconds(1);
        private readonly SemaphoreSlim gate = new(1, 1);
        private DateTimeOffset lastRequest = DateTimeOffset.MinValue;

        public async Task<IReadOnlyList<GeocodeResult>> SearchAsync(string query, CancellationToken cancellationToken)
        {
            query = query.Trim();
            return await cache.GetOrCreateAsync($"geocode:{query.ToLowerInvariant()}", async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(1);
                return await FetchAsync(query, cancellationToken);
            }) ?? [];
        }

        private async Task<IReadOnlyList<GeocodeResult>> FetchAsync(string query, CancellationToken cancellationToken)
        {
            await gate.WaitAsync(cancellationToken);
            try
            {
                var wait = lastRequest + MinimumInterval - DateTimeOffset.UtcNow;
                if (wait > TimeSpan.Zero)
                {
                    await Task.Delay(wait, cancellationToken);
                }
                lastRequest = DateTimeOffset.UtcNow;

                var url = QueryHelpers.AddQueryString(options.Value.GeocodeUrl, new Dictionary<string, string?>
                {
                    ["q"] = query,
                    ["format"] = "jsonv2",
                    ["limit"] = "5",
                });
                var places = await httpClientFactory.CreateClient(MapsOptions.HttpClientName)
                    .GetFromJsonAsync<List<NominatimPlace>>(url, cancellationToken) ?? [];

                // Nominatim sends numbers as strings, and the box as [south, north, west, east].
                return places.Select(p => new GeocodeResult(
                    p.DisplayName,
                    Number(p.Lat),
                    Number(p.Lon),
                    [Number(p.BoundingBox[0]), Number(p.BoundingBox[2]), Number(p.BoundingBox[1]), Number(p.BoundingBox[3])]))
                    .ToList();
            }
            finally
            {
                gate.Release();
            }
        }

        private static double Number(string value) => double.Parse(value, CultureInfo.InvariantCulture);

        private record NominatimPlace(
            [property: JsonPropertyName("display_name")] string DisplayName,
            [property: JsonPropertyName("lat")] string Lat,
            [property: JsonPropertyName("lon")] string Lon,
            [property: JsonPropertyName("boundingbox")] string[] BoundingBox);
    }

    // One search result: a readable name, the point, and the area to zoom to as [south, west, north, east].
    public record GeocodeResult(string Name, double Lat, double Lng, double[] Bounds);
}
