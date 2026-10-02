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

        // The carbon comparison at /carbon-comparison?region=CA-AB: the region's grid intensity, the carbon calculator
        // (which calls POST /api/v1/carbon/calculate) and a chart comparing regions.
        [HttpGet("carbon-comparison")]
        public async Task<IActionResult> Comparison(string? region, string? l, CancellationToken cancellationToken)
        {
            // The old carbon map linked here with the region's name (?l=Alberta&swlat=…); send those links to the new form.
            if (string.IsNullOrEmpty(region) && !string.IsNullOrWhiteSpace(l))
            {
                var code = await db.CarbonRegions.Where(r => r.Name == l).Select(r => r.Code).FirstOrDefaultAsync(cancellationToken);
                if (code is not null)
                {
                    return RedirectToActionPermanent(nameof(Comparison), new { region = code });
                }
            }

            var current = await carbonValues.CurrentByRegionIdAsync(cancellationToken);
            var allRegions = await db.CarbonRegions.AsNoTracking()
                .OrderBy(r => r.Name)
                .Select(r => new { r.Id, r.Code, r.Name, r.RegionType, r.Continent })
                .ToListAsync(cancellationToken);
            var regions = allRegions
                .Where(r => current.ContainsKey(r.Id))
                .Select(r =>
                {
                    var value = current[r.Id];
                    return new CarbonComparisonRegion(r.Code, r.Name, r.RegionType, r.Continent,
                        value.ValueGPerKWh, value.Year, value.Source, value.SourceUrl);
                })
                .ToList();

            var requested = allRegions.FirstOrDefault(r => string.Equals(r.Code, region, StringComparison.OrdinalIgnoreCase));
            var selected = regions.FirstOrDefault(r => r.Code == requested?.Code);

            var blocks = await db.ContentBlocks.AsNoTracking()
                .Where(b => b.Key.StartsWith("carbon.calculator.") || b.Key == "carbon.comparison.learn-more")
                .ToDictionaryAsync(b => b.Key, b => b.Html, cancellationToken);

            var pageData = new
            {
                calculateUrl = Url.Content("~/api/v1/carbon/calculate"),
                selected = selected?.Code,
                regions,
            };

            return View(new CarbonComparisonViewModel(regions, selected,
                NoDataRegionName: selected is null ? requested?.Name : null, blocks, pageData));
        }
    }
}
