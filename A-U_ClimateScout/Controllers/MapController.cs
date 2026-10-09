using System.Net;
using A_U_ClimateScout.Options;
using A_U_ClimateScout.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace A_U_ClimateScout.Controllers
{
    // Map tiles through our server (plan §5): the browser asks for /map/tiles/{z}/{x}/{y}; we fetch that tile
    // from the configured provider and pass it on. Any provider key stays on the server, and the provider
    // can change in configuration without touching the pages.
    // Limited per visitor (the "tiles" rate-limit policy) and per day (ProxyUsage).
    public class MapController(IHttpClientFactory httpClientFactory, IOptions<MapsOptions> options, ProxyUsage usage) : Controller
    {
        [HttpGet("map/tiles/{z:int}/{x:int}/{y:int}")]
        [EnableRateLimiting(ProxyUsage.Tiles)]
        public async Task<IActionResult> Tile(int z, int x, int y, CancellationToken cancellationToken)
        {
            var maps = options.Value;
            var tilesPerSide = 1 << z;
            if (z < maps.MinZoom || z > maps.MaxZoom || x < 0 || y < 0 || x >= tilesPerSide || y >= tilesPerSide)
            {
                return NotFound();
            }

            if (!usage.TryUse(ProxyUsage.Tiles))
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable);
            }

            var url = maps.TileUrl.Replace("{z}", z.ToString()).Replace("{x}", x.ToString()).Replace("{y}", y.ToString());
            try
            {
                using var response = await httpClientFactory.CreateClient(MapsOptions.HttpClientName)
                    .GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
                if (!response.IsSuccessStatusCode)
                {
                    return StatusCode(response.StatusCode == HttpStatusCode.NotFound ? 404 : 502);
                }

                // Let the browser cache the tile as long as the provider allows (a day if it doesn't say).
                var maxAge = response.Headers.CacheControl?.MaxAge ?? TimeSpan.FromDays(1);
                Response.Headers.CacheControl = $"public, max-age={(int)maxAge.TotalSeconds}";

                var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
                return File(bytes, response.Content.Headers.ContentType?.MediaType ?? "image/png");
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // The browser dropped the tile (the map zoomed or panned past it): nobody is waiting for an answer.
                return new EmptyResult();
            }
            catch (OperationCanceledException)
            {
                // The provider took longer than the HttpClient timeout.
                return StatusCode(StatusCodes.Status504GatewayTimeout);
            }
            catch (HttpRequestException)
            {
                return StatusCode(StatusCodes.Status502BadGateway);
            }
        }
    }
}
