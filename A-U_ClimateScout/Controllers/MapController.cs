using System.Net;
using A_U_ClimateScout.Options;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace A_U_ClimateScout.Controllers
{
    // Map tiles through our server (plan §5): the browser asks for /map/tiles/{z}/{x}/{y}; we fetch that tile
    // from the configured provider and pass it on. Any provider key stays on the server, and the provider
    // can change in configuration without touching the pages.
    public class MapController(IHttpClientFactory httpClientFactory, IOptions<MapsOptions> options) : Controller
    {
        [HttpGet("map/tiles/{z:int}/{x:int}/{y:int}")]
        public async Task<IActionResult> Tile(int z, int x, int y, CancellationToken cancellationToken)
        {
            var maps = options.Value;
            var tilesPerSide = 1 << z;
            if (z < maps.MinZoom || z > maps.MaxZoom || x < 0 || y < 0 || x >= tilesPerSide || y >= tilesPerSide)
            {
                return NotFound();
            }

            var url = maps.TileUrl.Replace("{z}", z.ToString()).Replace("{x}", x.ToString()).Replace("{y}", y.ToString());
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
    }
}
