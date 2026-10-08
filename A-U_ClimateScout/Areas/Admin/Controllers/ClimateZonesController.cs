using A_U_ClimateScout.Areas.Admin.Models;
using A_U_ClimateScout.Controllers;
using A_U_ClimateScout.Data;
using A_U_ClimateScout.Models;
using A_U_ClimateScout.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace A_U_ClimateScout.Areas.Admin.Controllers
{
    // Climate zones: list by group; edit name, colour, description, diagram, active, and the recommended strategies.
    // Zones aren't added or deleted here: each one belongs to the Köppen map data (MapId). Saves are audited.
    public class ClimateZonesController(ApplicationDbContext db, IMemoryCache cache) : AdminController
    {
        public async Task<IActionResult> Index(CancellationToken cancellationToken) =>
            View(await db.ClimateZones.AsNoTracking()
                .Include(z => z.Group).Include(z => z.Diagram).Include(z => z.Strategies)
                .OrderBy(z => z.Group.SortOrder).ThenBy(z => z.SortOrder).ToListAsync(cancellationToken));

        public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
        {
            var zone = await db.ClimateZones.AsNoTracking().Include(z => z.Strategies).FirstOrDefaultAsync(z => z.Id == id, cancellationToken);
            if (zone is null)
            {
                return NotFound();
            }

            await FillChoicesAsync(cancellationToken);
            return View(new ZoneForm
            {
                Id = zone.Id,
                KoppenCode = zone.KoppenCode,
                Slug = zone.Slug,
                Name = zone.Name,
                Color = zone.Color,
                DescriptionHtml = zone.DescriptionHtml,
                DiagramId = zone.DiagramId,
                IsActive = zone.IsActive,
                StrategyIds = zone.Strategies.Select(l => l.StrategyId).ToList(),
            });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ZoneForm form, CancellationToken cancellationToken)
        {
            var zone = await db.ClimateZones.Include(z => z.Strategies).ThenInclude(l => l.Strategy)
                .FirstOrDefaultAsync(z => z.Id == form.Id, cancellationToken);
            if (zone is null)
            {
                return NotFound();
            }
            if (!ModelState.IsValid)
            {
                form.KoppenCode = zone.KoppenCode;
                form.Slug = zone.Slug;
                await FillChoicesAsync(cancellationToken);
                return View(form);
            }

            zone.Name = form.Name.Trim();
            zone.Color = form.Color.ToUpperInvariant();
            zone.DescriptionHtml = HtmlCleaner.Clean(form.DescriptionHtml) is { Length: > 0 } description ? description : null;
            zone.DiagramId = form.DiagramId;
            zone.IsActive = form.IsActive;

            // Strategies: drop the unticked, add the newly ticked, then number them alphabetically (as imported).
            var wanted = form.StrategyIds.ToHashSet();
            zone.Strategies.RemoveAll(l => !wanted.Contains(l.StrategyId));
            var have = zone.Strategies.Select(l => l.StrategyId).ToHashSet();
            var added = await db.DesignStrategies.Where(s => wanted.Contains(s.Id) && !have.Contains(s.Id)).ToListAsync(cancellationToken);
            zone.Strategies.AddRange(added.Select(s => new ClimateZoneStrategy { Zone = zone, Strategy = s }));
            var order = 1;
            foreach (var link in zone.Strategies.OrderBy(l => l.Strategy.Name))
            {
                link.SortOrder = order++;
            }

            await db.SaveChangesAsync(cancellationToken);
            cache.Remove(SeoController.SitemapCacheKey);   // activating or deactivating a zone changes the sitemap

            TempData["Status"] = $"Saved {zone.KoppenCode} {zone.Name}.";
            return RedirectToAction(nameof(Index));
        }

        private async Task FillChoicesAsync(CancellationToken cancellationToken)
        {
            ViewData["Diagrams"] = new SelectList(await db.Diagrams.AsNoTracking().OrderBy(d => d.Name).ToListAsync(cancellationToken), "Id", "Name");
            ViewData["Strategies"] = await db.DesignStrategies.AsNoTracking().OrderBy(s => s.Name).ToListAsync(cancellationToken);
        }
    }
}
