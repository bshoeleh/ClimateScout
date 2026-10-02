using A_U_ClimateScout.Data;
using A_U_ClimateScout.Models;
using A_U_ClimateScout.Options;
using A_U_ClimateScout.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace A_U_ClimateScout.Controllers
{
    // The carbon map at /carbon (the old site's URL): grid carbon intensity by country, US state and Canadian
    // province. A click on a region opens the carbon comparison for it.
    public class CarbonController(ApplicationDbContext db, CarbonValues carbonValues, IOptions<MapsOptions> options) : Controller
    {
        [HttpGet("carbon")]
        public async Task<IActionResult> Map(CancellationToken cancellationToken)
        {
            var current = await carbonValues.CurrentByRegionIdAsync(cancellationToken);
            var regions = await db.CarbonRegions.AsNoTracking()
                .Where(r => r.ShowOnMap)
                .Select(r => new { r.Id, r.Code, r.Name })
                .ToListAsync(cancellationToken);
            var note = await db.ContentBlocks.AsNoTracking()
                .Where(b => b.Key == "carbon.map.note")
                .Select(b => b.Html)
                .FirstOrDefaultAsync(cancellationToken);

            // The map script joins each outline's "code" in carbon-regions.geojson to these regions.
            // A region without a value (Lesotho, Yukon) is drawn grey as "no data".
            var mapData = new
            {
                map = MapSettings.Create(Url, options.Value,
                    "Carbon intensity: <a href=\"https://ember-energy.org/\">Ember</a>, <a href=\"https://www.cer-rec.gc.ca/\">Canada Energy Regulator</a>"),
                outlinesUrl = Url.Content("~/geo/carbon-regions.geojson"),
                compareUrl = Url.Content("~/carbon-comparison?region="),
                regions = regions.ToDictionary(
                    r => r.Code,
                    r => current.TryGetValue(r.Id, out var carbon)
                        ? new { r.Name, value = (decimal?)carbon.ValueGPerKWh, year = (int?)carbon.Year, source = (string?)carbon.Source }
                        : new { r.Name, value = (decimal?)null, year = (int?)null, source = (string?)null }),
            };

            return View(new CarbonMapViewModel(mapData, note));
        }
    }
}
