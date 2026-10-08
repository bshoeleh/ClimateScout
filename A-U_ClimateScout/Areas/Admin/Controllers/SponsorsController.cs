using A_U_ClimateScout.Areas.Admin.Models;
using A_U_ClimateScout.Data;
using A_U_ClimateScout.Models;
using A_U_ClimateScout.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace A_U_ClimateScout.Areas.Admin.Controllers
{
    // Sponsors: list by tier with up/down ordering, add, edit (logo upload, dates, footer), delete.
    // Every change clears the public sponsor cache so /sponsors and the footer update at once; all saves are audited.
    public class SponsorsController(ApplicationDbContext db, ImageUploads uploads, IMemoryCache cache) : AdminController
    {
        private const string LogoFolder = "sponsors";

        public async Task<IActionResult> Index(CancellationToken cancellationToken) =>
            View(await db.Sponsors.AsNoTracking().Include(s => s.LogoAsset)
                .OrderBy(s => s.Tier).ThenBy(s => s.SortOrder).ThenBy(s => s.Name).ToListAsync(cancellationToken));

        public IActionResult Create() => View("Edit", new SponsorForm());

        public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
        {
            var sponsor = await db.Sponsors.AsNoTracking().Include(s => s.LogoAsset).FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
            if (sponsor is null)
            {
                return NotFound();
            }

            return View(new SponsorForm
            {
                Id = sponsor.Id,
                Name = sponsor.Name,
                Url = sponsor.Url,
                Tier = sponsor.Tier,
                StartDate = sponsor.StartDate,
                EndDate = sponsor.EndDate,
                ShowInFooter = sponsor.ShowInFooter,
                IsActive = sponsor.IsActive,
                DescriptionHtml = sponsor.DescriptionHtml,
                CurrentLogoPath = sponsor.LogoAsset?.StoragePath,
            });
        }

        [HttpPost, ValidateAntiForgeryToken]
        [RequestSizeLimit(ImageUploads.MaxBytes + 1024 * 1024)]   // the logo plus the rest of the form
        public async Task<IActionResult> Save(SponsorForm form, CancellationToken cancellationToken)
        {
            Sponsor? sponsor = null;
            if (form.Id is { } id)
            {
                sponsor = await db.Sponsors.Include(s => s.LogoAsset).FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
                if (sponsor is null)
                {
                    return NotFound();
                }
                form.CurrentLogoPath = sponsor.LogoAsset?.StoragePath;
            }

            if (!ModelState.IsValid)
            {
                return View("Edit", form);
            }

            // Upload first: if the logo is refused, nothing else is saved and the form comes back with the message.
            MediaAsset? newLogo = null;
            if (form.Logo is { Length: > 0 })
            {
                var result = await uploads.SaveAsync(form.Logo, LogoFolder, form.Name, cancellationToken);
                if (result.Error is not null)
                {
                    ModelState.AddModelError(nameof(form.Logo), result.Error);
                    return View("Edit", form);
                }
                newLogo = result.Asset;
            }

            if (sponsor is null)
            {
                sponsor = new Sponsor();
                db.Sponsors.Add(sponsor);
            }

            // A new sponsor, or one moved to another tier, goes to the end of its tier.
            if (sponsor.Id == 0 || sponsor.Tier != form.Tier)
            {
                sponsor.SortOrder = (await db.Sponsors.Where(s => s.Tier == form.Tier && s.Id != sponsor.Id)
                    .MaxAsync(s => (int?)s.SortOrder, cancellationToken) ?? 0) + 1;
            }

            sponsor.Name = form.Name.Trim();
            sponsor.Url = string.IsNullOrWhiteSpace(form.Url) ? null : form.Url.Trim();
            sponsor.Tier = form.Tier;
            sponsor.StartDate = form.StartDate;
            sponsor.EndDate = form.EndDate;
            sponsor.ShowInFooter = form.ShowInFooter;
            sponsor.IsActive = form.IsActive;
            sponsor.DescriptionHtml = HtmlCleaner.Clean(form.DescriptionHtml) is { Length: > 0 } description ? description : null;

            if (newLogo is not null)
            {
                sponsor.LogoAsset = newLogo;   // the old logo's file and row stay; the media library can tidy them later
            }
            else if (form.RemoveLogo)
            {
                sponsor.LogoAssetId = null;
                sponsor.LogoAsset = null;
            }

            await db.SaveChangesAsync(cancellationToken);
            cache.Remove(Sponsors.CacheKey);

            TempData["Status"] = $"Saved {sponsor.Name}.";
            return RedirectToAction(nameof(Index));
        }

        // Moves a sponsor one place up (-1) or down (+1) within its tier, by swapping with its neighbour.
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Move(int id, int direction, CancellationToken cancellationToken)
        {
            var sponsor = await db.Sponsors.FindAsync([id], cancellationToken);
            if (sponsor is null)
            {
                return NotFound();
            }

            var tier = await db.Sponsors.Where(s => s.Tier == sponsor.Tier)
                .OrderBy(s => s.SortOrder).ThenBy(s => s.Name).ToListAsync(cancellationToken);
            var index = tier.FindIndex(s => s.Id == id);
            var other = index + Math.Sign(direction);
            if (direction != 0 && other >= 0 && other < tier.Count)
            {
                (tier[index], tier[other]) = (tier[other], tier[index]);
                for (var i = 0; i < tier.Count; i++)
                {
                    tier[i].SortOrder = i + 1;   // renumber, so equal or missing sort orders can't get stuck
                }
                await db.SaveChangesAsync(cancellationToken);
                cache.Remove(Sponsors.CacheKey);
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
        {
            var sponsor = await db.Sponsors.FindAsync([id], cancellationToken);
            if (sponsor is null)
            {
                return NotFound();
            }

            db.Sponsors.Remove(sponsor);
            await db.SaveChangesAsync(cancellationToken);
            cache.Remove(Sponsors.CacheKey);

            TempData["Status"] = $"Deleted {sponsor.Name}.";
            return RedirectToAction(nameof(Index));
        }
    }
}
