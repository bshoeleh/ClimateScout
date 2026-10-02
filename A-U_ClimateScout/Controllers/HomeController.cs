using A_U_ClimateScout.Data;
using A_U_ClimateScout.Models;
using A_U_ClimateScout.Options;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace A_U_ClimateScout.Controllers
{
    // Home page: the world climate map and the list of climate zones by group.
    public class HomeController(ApplicationDbContext db, IOptions<MapsOptions> options) : Controller
    {
        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            var groups = await db.ClimateZoneGroups
                .AsNoTracking()
                .Include(g => g.Zones.Where(z => z.IsActive).OrderBy(z => z.SortOrder))
                .OrderBy(g => g.SortOrder)
                .ToListAsync(cancellationToken);

            // The map script joins each polygon's "n" in koppen.json to a zone by MapId (As has none; plan Phase 4).
            var maps = options.Value;
            var mapData = new
            {
                tileUrl = Url.Content("~/map/tiles/{z}/{x}/{y}"),
                attribution = $"{maps.Attribution}, Köppen-Geiger: <a href=\"https://doi.org/10.1038/sdata.2018.214\">Beck et al. (2018)</a>",
                maps.MinZoom,
                maps.MaxZoom,
                polygonsUrl = Url.Content("~/geo/koppen.json"),
                zoneUrl = Url.Content("~/zone/"),
                geocodeUrl = Url.Content("~/api/v1/geocode"),
                zones = groups
                    .SelectMany(g => g.Zones.Where(z => z.MapId is not null), (g, z) => (Group: g, Zone: z))
                    .ToDictionary(
                        x => x.Zone.MapId!.Value.ToString(),
                        x => new { x.Zone.KoppenCode, x.Zone.Name, x.Zone.Slug, x.Zone.Color, group = x.Group.Slug }),
            };

            return View(new HomePageViewModel(groups, mapData));
        }
    }
}
