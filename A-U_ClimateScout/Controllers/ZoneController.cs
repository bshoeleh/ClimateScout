using System.Text;
using A_U_ClimateScout.Data;
using A_U_ClimateScout.Models;
using A_U_ClimateScout.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace A_U_ClimateScout.Controllers
{
    // Public climate zone pages at /zone/{slug}, the same URLs as the old site (plan §4).
    public class ZoneController(ApplicationDbContext db, IWebHostEnvironment environment, IMemoryCache cache) : Controller
    {
        [HttpGet("zone/{slug}")]
        public async Task<IActionResult> Index(string slug, CancellationToken cancellationToken)
        {
            // Old links use underscores (cfa_humid-subtropical); our slugs use hyphens (plan §10).
            if (slug.Contains('_'))
            {
                return RedirectToActionPermanent(nameof(Index), new { slug = slug.Replace('_', '-') });
            }

            var zone = await db.ClimateZones
                .AsNoTracking()
                .Include(z => z.Group)
                .Include(z => z.Diagram).ThenInclude(d => d!.SvgAsset)
                .Include(z => z.Strategies.Where(l => l.Strategy.IsActive).OrderBy(l => l.SortOrder))
                    .ThenInclude(l => l.Strategy)
                    .ThenInclude(s => s.ImageAsset)
                .FirstOrDefaultAsync(z => z.Slug == slug && z.IsActive, cancellationToken);
            if (zone is null)
            {
                return NotFound();
            }

            // Conflicts between strategies on this page (stored both ways, so one direction is enough).
            var slugs = zone.Strategies.Select(l => l.Strategy.Slug).ToList();
            var conflicts = (await db.StrategyConflicts
                    .Where(c => slugs.Contains(c.Strategy.Slug) && slugs.Contains(c.ConflictsWith.Slug))
                    .Select(c => new { c.Strategy.Slug, Other = c.ConflictsWith.Slug })
                    .ToListAsync(cancellationToken))
                .ToLookup(c => c.Slug, c => c.Other);

            var (markup, layers) = await LoadDiagramAsync(zone.Diagram, cancellationToken);
            return View(new ZonePageViewModel(zone, markup, layers, conflicts));
        }

        // The diagram SVG prepared for inlining, kept in memory for an hour (the files are 0.5–0.9 MB).
        private async Task<(string? Markup, IReadOnlySet<string> Layers)> LoadDiagramAsync(Diagram? diagram, CancellationToken cancellationToken)
        {
            if (diagram?.SvgAsset is null)
            {
                return (null, new HashSet<string>());
            }

            return await cache.GetOrCreateAsync($"diagram:{diagram.SvgAsset.StoragePath}", async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
                var strategySlugs = (await db.DesignStrategies.Select(s => s.Slug).ToListAsync(cancellationToken))
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);
                var svg = await System.IO.File.ReadAllTextAsync(
                    Path.Combine(environment.WebRootPath, "img", "media", diagram.SvgAsset.StoragePath), cancellationToken);
                return DiagramMarkup.Prepare(svg, strategySlugs, $"{diagram.Name} building diagram");
            });
        }

        // The zone colours as a stylesheet (.cs-zone-cfa { … }), so pages use classes instead of inline styles.
        // Generated from the database because the colours are data (editable in Admin); cached for 10 minutes.
        [HttpGet("css/zones.css")]
        [ResponseCache(Duration = 600)]
        public async Task<IActionResult> Stylesheet(CancellationToken cancellationToken)
        {
            var zones = await db.ClimateZones.AsNoTracking()
                .OrderBy(z => z.KoppenCode)
                .Select(z => new { z.KoppenCode, z.Color })
                .ToListAsync(cancellationToken);

            var css = new StringBuilder("/* Zone colours, generated from the database by ZoneController.Stylesheet. */\n");
            foreach (var zone in zones)
            {
                css.Append($".{CssClass(zone.KoppenCode)} {{ --cs-zone-bg: {zone.Color}; --cs-zone-fg: {ColorContrast.TextColorFor(zone.Color)}; }}\n");
            }

            return Content(css.ToString(), "text/css");
        }

        // CSS class for a zone's colour: Cfa → cs-zone-cfa. Used by the stylesheet above and the views.
        public static string CssClass(string koppenCode) => $"cs-zone-{koppenCode.ToLowerInvariant()}";
    }
}
