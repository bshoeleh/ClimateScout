using System.ComponentModel.DataAnnotations;
using A_U_ClimateScout.Data;
using A_U_ClimateScout.Data.Configurations;
using A_U_ClimateScout.Models;
using A_U_ClimateScout.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace A_U_ClimateScout.Areas.Admin.Controllers
{
    // Every uploaded or imported file (wwwroot/img/media/…): where it's used, its alt text, replacing an image with a
    // new file (every page using it updates), and deleting files nothing uses. New files are uploaded on the screen
    // that uses them (sponsor, strategy, project, diagram), which also decides their folder (plan Phase 5).
    public class MediaLibraryController(ApplicationDbContext db, ImageUploads uploads, IWebHostEnvironment environment) : AdminController
    {
        public record MediaRow(MediaAsset Asset, IReadOnlyList<string> UsedBy);

        public class MediaForm
        {
            public int Id { get; set; }

            [StringLength(FieldLengths.AltText), Display(Name = "Alt text")]
            public string? AltText { get; set; }

            [Display(Name = "Replace with")]
            public IFormFile? File { get; set; }
        }

        public async Task<IActionResult> Index(string? folder, bool unused = false, CancellationToken cancellationToken = default)
        {
            var usage = await UsageAsync(cancellationToken);
            var query = db.MediaAssets.AsNoTracking();
            if (!string.IsNullOrEmpty(folder))
            {
                query = query.Where(m => m.StoragePath.StartsWith(folder + "/"));
            }
            var rows = (await query.OrderBy(m => m.StoragePath).ToListAsync(cancellationToken))
                .Select(m => new MediaRow(m, usage.GetValueOrDefault(m.Id) ?? []))
                .Where(r => !unused || r.UsedBy.Count == 0)
                .ToList();

            ViewData["Folder"] = folder;
            ViewData["Unused"] = unused;
            ViewData["Folders"] = (await db.MediaAssets.Select(m => m.StoragePath).ToListAsync(cancellationToken))
                .Select(p => p.Split('/')[0]).Distinct().Order().ToList();
            return View(rows);
        }

        public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
        {
            var asset = await db.MediaAssets.AsNoTracking().FirstOrDefaultAsync(m => m.Id == id, cancellationToken);
            if (asset is null)
            {
                return NotFound();
            }
            ViewData["Asset"] = asset;
            ViewData["UsedBy"] = (await UsageAsync(cancellationToken)).GetValueOrDefault(id) ?? [];
            return View(new MediaForm { Id = asset.Id, AltText = asset.AltText });
        }

        [HttpPost, ValidateAntiForgeryToken]
        [RequestSizeLimit(ImageUploads.MaxBytes + 1024 * 1024)]
        public async Task<IActionResult> Edit(MediaForm form, CancellationToken cancellationToken)
        {
            var asset = await db.MediaAssets.FirstOrDefaultAsync(m => m.Id == form.Id, cancellationToken);
            if (asset is null)
            {
                return NotFound();
            }

            string? oldFile = null;
            if (form.File is { Length: > 0 })
            {
                if (asset.ContentType == "image/svg+xml")
                {
                    ModelState.AddModelError(nameof(form.File), "Diagram artwork is replaced on the Diagrams screen.");
                }
                else
                {
                    var folder = asset.StoragePath.Split('/')[0];
                    var result = await uploads.SaveAsync(form.File, folder, form.AltText, cancellationToken);
                    if (result.Error is not null)
                    {
                        ModelState.AddModelError(nameof(form.File), result.Error);
                    }
                    else
                    {
                        // Same row, new file: every sponsor / strategy / project using this image shows the new one.
                        oldFile = asset.StoragePath;
                        asset.FileName = result.Asset!.FileName;
                        asset.ContentType = result.Asset.ContentType;
                        asset.StoragePath = result.Asset.StoragePath;
                        asset.SizeBytes = result.Asset.SizeBytes;
                        asset.Width = result.Asset.Width;
                        asset.Height = result.Asset.Height;
                        asset.UploadedAt = result.Asset.UploadedAt;
                    }
                }
            }
            if (!ModelState.IsValid)
            {
                ViewData["Asset"] = asset;
                ViewData["UsedBy"] = (await UsageAsync(cancellationToken)).GetValueOrDefault(asset.Id) ?? [];
                return View(form);
            }

            asset.AltText = string.IsNullOrWhiteSpace(form.AltText) ? null : form.AltText.Trim();
            await db.SaveChangesAsync(cancellationToken);
            if (oldFile is not null)
            {
                DeleteFile(oldFile);
            }

            TempData["Status"] = oldFile is null ? "Saved." : "Replaced the image.";
            return RedirectToAction(nameof(Edit), new { id = asset.Id });
        }

        // Only files nothing uses can be deleted (the database also refuses to delete a logo a sponsor still uses).
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
        {
            var asset = await db.MediaAssets.FirstOrDefaultAsync(m => m.Id == id, cancellationToken);
            if (asset is null)
            {
                return NotFound();
            }
            if ((await UsageAsync(cancellationToken)).ContainsKey(id))
            {
                TempData["Status"] = "This file is still in use, so it wasn't deleted.";
                return RedirectToAction(nameof(Edit), new { id });
            }

            db.MediaAssets.Remove(asset);
            await db.SaveChangesAsync(cancellationToken);
            DeleteFile(asset.StoragePath);

            TempData["Status"] = $"Deleted {asset.FileName}.";
            return RedirectToAction(nameof(Index), new { unused = true });
        }

        // Asset id → what uses it ("Strategy: Cool Roof").
        private async Task<Dictionary<int, List<string>>> UsageAsync(CancellationToken cancellationToken)
        {
            var uses = new List<(int Id, string What)>();
            uses.AddRange((await db.Sponsors.Where(s => s.LogoAssetId != null).Select(s => new { Id = s.LogoAssetId!.Value, s.Name }).ToListAsync(cancellationToken)).Select(x => (x.Id, $"Sponsor logo: {x.Name}")));
            uses.AddRange((await db.DesignStrategies.Where(s => s.ImageAssetId != null).Select(s => new { Id = s.ImageAssetId!.Value, s.Name }).ToListAsync(cancellationToken)).Select(x => (x.Id, $"Strategy icon: {x.Name}")));
            uses.AddRange((await db.DesignStrategies.Where(s => s.DiagramImageAssetId != null).Select(s => new { Id = s.DiagramImageAssetId!.Value, s.Name }).ToListAsync(cancellationToken)).Select(x => (x.Id, $"Strategy diagram image: {x.Name}")));
            uses.AddRange((await db.ReferenceProjects.Where(p => p.ImageAssetId != null).Select(p => new { Id = p.ImageAssetId!.Value, p.Name }).ToListAsync(cancellationToken)).Select(x => (x.Id, $"Project photo: {x.Name}")));
            uses.AddRange((await db.Diagrams.Where(d => d.SvgAssetId != null).Select(d => new { Id = d.SvgAssetId!.Value, d.Name }).ToListAsync(cancellationToken)).Select(x => (x.Id, $"Diagram: {x.Name}")));
            return uses.GroupBy(u => u.Id).ToDictionary(g => g.Key, g => g.Select(u => u.What).ToList());
        }

        private void DeleteFile(string storagePath)
        {
            var path = Path.GetFullPath(Path.Combine(environment.WebRootPath, "img", "media", storagePath));
            var root = Path.GetFullPath(Path.Combine(environment.WebRootPath, "img", "media"));
            if (path.StartsWith(root, StringComparison.OrdinalIgnoreCase) && System.IO.File.Exists(path))
            {
                System.IO.File.Delete(path);
            }
        }
    }
}
