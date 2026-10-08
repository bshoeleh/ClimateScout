using System.ComponentModel.DataAnnotations;
using A_U_ClimateScout.Data;
using A_U_ClimateScout.Data.Configurations;
using A_U_ClimateScout.Models;
using A_U_ClimateScout.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace A_U_ClimateScout.Areas.Admin.Controllers
{
    // Carbon regions (countries, US states, Canadian provinces, aggregates): edit name, continent, map visibility and
    // which source's figure is "current"; manage the aliases the importer matches names against; see every value
    // imported for the region. Codes and types are fixed: the map outlines (carbon-regions.geojson) join on the code.
    public class CarbonRegionsController(ApplicationDbContext db, CarbonValues values) : AdminController
    {
        public class RegionForm
        {
            public int Id { get; set; }

            [Required, StringLength(FieldLengths.Name)]
            public string Name { get; set; } = "";

            [StringLength(FieldLengths.Name)]
            public string? Continent { get; set; }

            [Display(Name = "Show on the carbon map")]
            public bool ShowOnMap { get; set; }

            [Display(Name = "Preferred source")]
            public int? PreferredSourceId { get; set; }
        }

        public async Task<IActionResult> Index(string? search, CarbonRegionType? type, CancellationToken cancellationToken)
        {
            var query = db.CarbonRegions.AsNoTracking().Include(r => r.Aliases).AsQueryable();
            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(r => r.Name.Contains(search) || r.Code.Contains(search) || r.Aliases.Any(a => a.Alias.Contains(search)));
            }
            if (type is not null)
            {
                query = query.Where(r => r.RegionType == type);
            }

            ViewData["Search"] = search;
            ViewData["Type"] = type;
            ViewData["Current"] = await values.CurrentByRegionIdAsync(cancellationToken);
            return View(await query.OrderBy(r => r.Name).ToListAsync(cancellationToken));
        }

        public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
        {
            var region = await LoadAsync(id, cancellationToken);
            if (region is null)
            {
                return NotFound();
            }
            await FillAsync(region, cancellationToken);
            return View(new RegionForm { Id = region.Id, Name = region.Name, Continent = region.Continent, ShowOnMap = region.ShowOnMap, PreferredSourceId = region.PreferredSourceId });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(RegionForm form, CancellationToken cancellationToken)
        {
            var region = await db.CarbonRegions.FirstOrDefaultAsync(r => r.Id == form.Id, cancellationToken);
            if (region is null)
            {
                return NotFound();
            }
            if (!ModelState.IsValid)
            {
                await FillAsync((await LoadAsync(form.Id, cancellationToken))!, cancellationToken);
                return View(form);
            }

            region.Name = form.Name.Trim();
            region.Continent = string.IsNullOrWhiteSpace(form.Continent) ? null : form.Continent.Trim();
            region.ShowOnMap = form.ShowOnMap;
            region.PreferredSourceId = form.PreferredSourceId;
            await db.SaveChangesAsync(cancellationToken);

            TempData["Status"] = $"Saved {region.Name}.";
            return RedirectToAction(nameof(Edit), new { id = region.Id });
        }

        // Another name the importer should recognise for this region ("Viet Nam" → Vietnam).
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> AddAlias(int id, string alias, CancellationToken cancellationToken)
        {
            alias = alias?.Trim() ?? "";
            var existing = await db.CarbonRegionAliases.Include(a => a.Region).FirstOrDefaultAsync(a => a.Alias == alias, cancellationToken);
            if (alias.Length == 0)
            {
                TempData["Status"] = "Type the other name first.";
            }
            else if (existing is not null)
            {
                TempData["Status"] = $"\"{alias}\" is already an alias of {existing.Region.Name}.";
            }
            else
            {
                db.CarbonRegionAliases.Add(new CarbonRegionAlias { RegionId = id, Alias = alias, Source = "manual" });
                await db.SaveChangesAsync(cancellationToken);
                TempData["Status"] = $"Added the alias \"{alias}\".";
            }
            return RedirectToAction(nameof(Edit), new { id });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAlias(int id, int aliasId, CancellationToken cancellationToken)
        {
            var alias = await db.CarbonRegionAliases.FirstOrDefaultAsync(a => a.Id == aliasId && a.RegionId == id, cancellationToken);
            if (alias is not null)
            {
                db.CarbonRegionAliases.Remove(alias);
                await db.SaveChangesAsync(cancellationToken);
                TempData["Status"] = $"Removed the alias \"{alias.Alias}\".";
            }
            return RedirectToAction(nameof(Edit), new { id });
        }

        private Task<CarbonRegion?> LoadAsync(int id, CancellationToken cancellationToken) =>
            db.CarbonRegions.AsNoTracking().Include(r => r.Aliases).Include(r => r.ParentRegion)
                .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

        private async Task FillAsync(CarbonRegion region, CancellationToken cancellationToken)
        {
            ViewData["Region"] = region;
            ViewData["Sources"] = await db.CarbonDataSources.AsNoTracking().OrderBy(s => s.Name).ToListAsync(cancellationToken);
            ViewData["Values"] = await db.CarbonIntensities.AsNoTracking().Include(i => i.Source)
                .Where(i => i.RegionId == region.Id).OrderByDescending(i => i.Year).ThenBy(i => i.ImportBatchId).ToListAsync(cancellationToken);
            ViewData["Current"] = (await values.CurrentByRegionIdAsync(cancellationToken)).GetValueOrDefault(region.Id);
        }
    }
}
