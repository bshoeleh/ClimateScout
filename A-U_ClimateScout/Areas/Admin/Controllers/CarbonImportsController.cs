using System.Security.Claims;
using System.Text.RegularExpressions;
using A_U_ClimateScout.Data;
using A_U_ClimateScout.Identity;
using A_U_ClimateScout.Models;
using A_U_ClimateScout.Services;
using A_U_ClimateScout.Services.CarbonImport;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace A_U_ClimateScout.Areas.Admin.Controllers
{
    // Carbon data imports (plan §6, Phase 6): upload a .csv or .xlsx → preview what would change (nothing saved yet;
    // unmatched names can be mapped to a region, which adds an alias) → commit → history with rollback. The uploaded
    // file waits in App_Data/carbon-imports between preview and commit (removed after a day). Admins only.
    [Authorize(Policy = Policies.AdminOnly)]
    public partial class CarbonImportsController(ApplicationDbContext db, CarbonImporter importer, IWebHostEnvironment environment,
        IEmailService email) : AdminController
    {
        private const long MaxBytes = 10 * 1024 * 1024;

        public record Preview(string Token, string FileName, CarbonImportSummary? Summary, string? Error, List<CarbonRegion> Regions);

        public async Task<IActionResult> Index(CancellationToken cancellationToken) =>
            View(await db.CarbonImportBatches.AsNoTracking().Include(b => b.Source).Include(b => b.UploadedBy)
                .OrderByDescending(b => b.Id).ToListAsync(cancellationToken));

        [HttpPost, ValidateAntiForgeryToken]
        [RequestSizeLimit(MaxBytes + 1024 * 1024)]
        public async Task<IActionResult> Upload(IFormFile? file, CancellationToken cancellationToken)
        {
            if (file is not { Length: > 0 } || file.Length > MaxBytes)
            {
                TempData["Status"] = "Choose a .csv or .xlsx file of up to 10 MB.";
                return RedirectToAction(nameof(Index));
            }
            var csv = await CarbonFileReader.ReadAsCsvAsync(file, cancellationToken);
            if (csv is null)
            {
                TempData["Status"] = "That file couldn't be read. Upload the .csv as downloaded, or an .xlsx saved from Excel.";
                return RedirectToAction(nameof(Index));
            }

            RemoveOldUploads();
            var token = Guid.NewGuid().ToString("N");
            await System.IO.File.WriteAllTextAsync(UploadPath(token), csv, cancellationToken);
            await System.IO.File.WriteAllTextAsync(UploadPath(token) + ".name", Path.GetFileName(file.FileName), cancellationToken);
            return RedirectToAction(nameof(Review), new { token });
        }

        // The preview: a dry run of the stored file, run again after each alias is added.
        public async Task<IActionResult> Review(string token, CancellationToken cancellationToken)
        {
            if (!ValidToken().IsMatch(token) || !System.IO.File.Exists(UploadPath(token)))
            {
                TempData["Status"] = "That upload has expired; please upload the file again.";
                return RedirectToAction(nameof(Index));
            }

            var fileName = await System.IO.File.ReadAllTextAsync(UploadPath(token) + ".name", cancellationToken);
            CarbonImportSummary? summary = null;
            string? error = null;
            try
            {
                using var reader = new StreamReader(UploadPath(token));
                summary = await importer.ImportAsync(fileName, reader, dryRun: true, cancellationToken);
            }
            catch (CarbonImportException exception)
            {
                error = exception.Message;
            }

            var regions = await db.CarbonRegions.AsNoTracking().OrderBy(r => r.Name).ToListAsync(cancellationToken);
            return View(new Preview(token, fileName, summary, error, regions));
        }

        // "This name in the file is that region": saves an alias, then the preview runs again.
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> MapName(string token, string name, int regionId, CancellationToken cancellationToken)
        {
            name = name.Trim();
            if (name.Length > 0 && await db.CarbonRegions.AnyAsync(r => r.Id == regionId, cancellationToken)
                && !await db.CarbonRegionAliases.AnyAsync(a => a.Alias == name, cancellationToken))
            {
                db.CarbonRegionAliases.Add(new CarbonRegionAlias { RegionId = regionId, Alias = name, Source = "manual" });
                await db.SaveChangesAsync(cancellationToken);
                TempData["Status"] = $"\"{name}\" will now match that region.";
            }
            return RedirectToAction(nameof(Review), new { token });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Commit(string token, CancellationToken cancellationToken)
        {
            if (!ValidToken().IsMatch(token) || !System.IO.File.Exists(UploadPath(token)))
            {
                TempData["Status"] = "That upload has expired; please upload the file again.";
                return RedirectToAction(nameof(Index));
            }

            var fileName = await System.IO.File.ReadAllTextAsync(UploadPath(token) + ".name", cancellationToken);
            CarbonImportSummary summary;
            try
            {
                using var reader = new StreamReader(UploadPath(token));
                summary = await importer.ImportAsync(fileName, reader, dryRun: false, cancellationToken,
                    uploadedById: User.FindFirstValue(ClaimTypes.NameIdentifier));
            }
            catch (CarbonImportException exception)
            {
                TempData["Status"] = exception.Message;
                return RedirectToAction(nameof(Review), new { token });
            }
            DeleteUpload(token);

            var text = $"{fileName} ({summary.Source}, {summary.Profile}) was imported as batch #{summary.BatchId}.\n\n"
                + $"New values: {summary.NewRows}\nChanged values: {summary.ChangedRows}\nUnchanged: {summary.UnchangedRows}\nRows skipped: {summary.Problems.Count}\n\n"
                + $"History and rollback: {Url.Action(nameof(Index), null, null, Request.Scheme)}";
            if ((User.FindFirstValue(ClaimTypes.Email) ?? User.Identity?.Name) is { } address)
            {
                await email.SendAsync(address, $"Carbon import #{summary.BatchId} done", text, cancellationToken);
            }

            TempData["Status"] = $"Imported batch #{summary.BatchId}: {summary.NewRows} new, {summary.ChangedRows} changed, {summary.UnchangedRows} unchanged.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Rollback(int id, CancellationToken cancellationToken)
        {
            var refusal = await importer.RollbackAsync(id, cancellationToken);
            TempData["Status"] = refusal ?? $"Rolled back batch #{id}; the values it replaced are current again.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
        {
            var batch = await db.CarbonImportBatches.AsNoTracking().Include(b => b.Source).Include(b => b.UploadedBy)
                .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);
            if (batch is null)
            {
                return NotFound();
            }
            ViewData["Problems"] = string.IsNullOrEmpty(batch.ErrorsJson) ? new List<CarbonCsvProblem>()
                : System.Text.Json.JsonSerializer.Deserialize<List<CarbonCsvProblem>>(batch.ErrorsJson) ?? [];
            return View(batch);
        }

        private string UploadFolder => Path.Combine(environment.ContentRootPath, "App_Data", "carbon-imports");

        private string UploadPath(string token)
        {
            Directory.CreateDirectory(UploadFolder);
            return Path.Combine(UploadFolder, token + ".csv");
        }

        private void DeleteUpload(string token)
        {
            System.IO.File.Delete(UploadPath(token));
            System.IO.File.Delete(UploadPath(token) + ".name");
        }

        private void RemoveOldUploads()
        {
            foreach (var file in Directory.Exists(UploadFolder) ? Directory.GetFiles(UploadFolder) : [])
            {
                if (System.IO.File.GetLastWriteTimeUtc(file) < DateTime.UtcNow.AddDays(-1))
                {
                    System.IO.File.Delete(file);
                }
            }
        }

        [GeneratedRegex("^[0-9a-f]{32}$")]
        private static partial Regex ValidToken();
    }
}
