using System.Text.RegularExpressions;
using A_U_ClimateScout.Areas.Admin.Models;
using A_U_ClimateScout.Controllers;
using A_U_ClimateScout.Data;
using A_U_ClimateScout.Models;
using A_U_ClimateScout.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace A_U_ClimateScout.Areas.Admin.Controllers
{
    // Design strategies: list; add; edit text, link, illustration, zones and conflicts. Conflicts are saved in both
    // directions (StrategyConflictRules). Strategies are hidden (IsActive) rather than deleted, so their links survive.
    // Reference projects are edited by ReferenceProjectsController. Saves are audited.
    public partial class DesignStrategiesController(ApplicationDbContext db, ImageUploads uploads, IMemoryCache cache) : AdminController
    {
        private const string ImageFolder = "strategies";

        public async Task<IActionResult> Index(CancellationToken cancellationToken) =>
            View(await db.DesignStrategies.AsNoTracking()
                .Include(s => s.ImageAsset).Include(s => s.Zones).Include(s => s.Conflicts).Include(s => s.ReferenceProjects)
                .OrderBy(s => s.Name).ToListAsync(cancellationToken));

        public async Task<IActionResult> Create(CancellationToken cancellationToken)
        {
            await FillChoicesAsync(null, cancellationToken);
            return View("Edit", new StrategyForm());
        }

        public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
        {
            var strategy = await db.DesignStrategies.AsNoTracking()
                .Include(s => s.ImageAsset).Include(s => s.Zones).Include(s => s.Conflicts)
                .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
            if (strategy is null)
            {
                return NotFound();
            }

            await FillChoicesAsync(id, cancellationToken);
            return View(new StrategyForm
            {
                Id = strategy.Id,
                Slug = strategy.Slug,
                Name = strategy.Name,
                Summary = strategy.Summary,
                BodyHtml = strategy.BodyHtml,
                Palette2030Url = strategy.Palette2030Url,
                IsActive = strategy.IsActive,
                CurrentImagePath = strategy.ImageAsset?.StoragePath,
                ZoneIds = strategy.Zones.Select(l => l.ZoneId).ToList(),
                ConflictIds = strategy.Conflicts.Select(c => c.ConflictsWithStrategyId).ToList(),
            });
        }

        [HttpPost, ValidateAntiForgeryToken]
        [RequestSizeLimit(ImageUploads.MaxBytes + 1024 * 1024)]
        public async Task<IActionResult> Save(StrategyForm form, CancellationToken cancellationToken)
        {
            DesignStrategy? strategy = null;
            if (form.Id is { } id)
            {
                strategy = await db.DesignStrategies.Include(s => s.ImageAsset).Include(s => s.Zones)
                    .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
                if (strategy is null)
                {
                    return NotFound();
                }
                form.Slug = strategy.Slug;
                form.CurrentImagePath = strategy.ImageAsset?.StoragePath;
            }
            else
            {
                form.Slug = MakeSlug(form.Name);
                if (form.Slug.Length == 0)
                {
                    ModelState.AddModelError(nameof(form.Name), "The name needs at least one letter or digit.");
                }
                else if (await db.DesignStrategies.AnyAsync(s => s.Slug == form.Slug, cancellationToken))
                {
                    ModelState.AddModelError(nameof(form.Name), $"A strategy with the address /design-strategy/{form.Slug} already exists.");
                }
            }

            if (!ModelState.IsValid)
            {
                await FillChoicesAsync(form.Id, cancellationToken);
                return View("Edit", form);
            }

            MediaAsset? newImage = null;
            if (form.Image is { Length: > 0 })
            {
                var result = await uploads.SaveAsync(form.Image, ImageFolder, form.Name, cancellationToken);
                if (result.Error is not null)
                {
                    ModelState.AddModelError(nameof(form.Image), result.Error);
                    await FillChoicesAsync(form.Id, cancellationToken);
                    return View("Edit", form);
                }
                newImage = result.Asset;
            }

            if (strategy is null)
            {
                strategy = new DesignStrategy { Slug = form.Slug! };
                db.DesignStrategies.Add(strategy);
            }

            strategy.Name = form.Name.Trim();
            strategy.Summary = string.IsNullOrWhiteSpace(form.Summary) ? null : form.Summary.Trim();
            strategy.BodyHtml = HtmlCleaner.Clean(form.BodyHtml) is { Length: > 0 } body ? body : null;
            strategy.Palette2030Url = string.IsNullOrWhiteSpace(form.Palette2030Url) ? null : form.Palette2030Url.Trim();
            strategy.IsActive = form.IsActive;
            if (newImage is not null)
            {
                strategy.ImageAsset = newImage;
            }

            // All strategies tracked, so the audit log can name both sides of each zone link and conflict.
            var strategies = await db.DesignStrategies.ToListAsync(cancellationToken);
            var zones = await db.ClimateZones.ToListAsync(cancellationToken);

            // Zones: drop the unticked, add the newly ticked (renumbered below).
            var wantedZones = form.ZoneIds.ToHashSet();
            var affected = strategy.Zones.Select(l => l.ZoneId).Union(wantedZones).ToList();
            strategy.Zones.RemoveAll(l => !wantedZones.Contains(l.ZoneId));
            foreach (var zone in zones.Where(z => wantedZones.Contains(z.Id) && strategy.Zones.All(l => l.ZoneId != z.Id)))
            {
                strategy.Zones.Add(new ClimateZoneStrategy { Zone = zone, Strategy = strategy });
            }

            // Conflicts in both directions: remove the pairs no longer wanted, add the new ones.
            var wantedConflicts = form.ConflictIds.Where(c => c != strategy.Id).ToHashSet();
            if (strategy.Id != 0)
            {
                var existing = await db.StrategyConflicts
                    .Where(c => c.StrategyId == strategy.Id || c.ConflictsWithStrategyId == strategy.Id).ToListAsync(cancellationToken);
                db.StrategyConflicts.RemoveRange(existing.Where(c =>
                    !wantedConflicts.Contains(c.StrategyId == strategy.Id ? c.ConflictsWithStrategyId : c.StrategyId)));
                var have = existing.Where(c => c.StrategyId == strategy.Id).Select(c => c.ConflictsWithStrategyId).ToHashSet();
                wantedConflicts.ExceptWith(have);
            }
            foreach (var other in strategies.Where(s => wantedConflicts.Contains(s.Id)))
            {
                db.StrategyConflicts.Add(new StrategyConflict { Strategy = strategy, ConflictsWith = other });
                db.StrategyConflicts.Add(new StrategyConflict { Strategy = other, ConflictsWith = strategy });
            }

            await db.SaveChangesAsync(cancellationToken);

            // Each zone's strategies stay numbered alphabetically (as imported and as the zone screen keeps them).
            var links = await db.ClimateZoneStrategies.Include(l => l.Strategy).Where(l => affected.Contains(l.ZoneId)).ToListAsync(cancellationToken);
            foreach (var zoneLinks in links.GroupBy(l => l.ZoneId))
            {
                var order = 1;
                foreach (var link in zoneLinks.OrderBy(l => l.Strategy.Name))
                {
                    link.SortOrder = order++;
                }
            }
            await db.SaveChangesAsync(cancellationToken);
            cache.Remove(SeoController.SitemapCacheKey);

            TempData["Status"] = $"Saved {strategy.Name}.";
            return RedirectToAction(nameof(Edit), new { id = strategy.Id });
        }

        // "Daylighting from Multiple Sides" → "daylighting-from-multiple-sides".
        public static string MakeSlug(string name) => NotSlug().Replace(name.Trim().ToLowerInvariant(), "-").Trim('-');

        private async Task FillChoicesAsync(int? strategyId, CancellationToken cancellationToken)
        {
            ViewData["Zones"] = await db.ClimateZones.AsNoTracking().Include(z => z.Group)
                .OrderBy(z => z.Group.SortOrder).ThenBy(z => z.SortOrder).ToListAsync(cancellationToken);
            ViewData["Strategies"] = await db.DesignStrategies.AsNoTracking().Where(s => s.Id != strategyId)
                .OrderBy(s => s.Name).ToListAsync(cancellationToken);
            ViewData["Projects"] = strategyId is null ? new List<ReferenceProject>() : await db.ReferenceProjects.AsNoTracking()
                .Include(p => p.ImageAsset).Where(p => p.StrategyId == strategyId)
                .OrderBy(p => p.SortOrder).ThenBy(p => p.Name).ToListAsync(cancellationToken);
        }

        [GeneratedRegex("[^a-z0-9]+")]
        private static partial Regex NotSlug();
    }
}
