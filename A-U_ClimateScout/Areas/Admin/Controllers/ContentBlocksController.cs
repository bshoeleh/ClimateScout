using System.Security.Claims;
using A_U_ClimateScout.Data;
using A_U_ClimateScout.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace A_U_ClimateScout.Areas.Admin.Controllers
{
    // Editable texts on the public pages (About, carbon calculator, notes). The blocks are defined by the pages that
    // show them, so this screen edits them but does not add or delete any. Saved HTML is cleaned (HtmlCleaner).
    public class ContentBlocksController(ApplicationDbContext db) : AdminController
    {
        // The public page each block appears on, by key prefix (for the "View on site" link).
        public static string? PagePath(string key) => key switch
        {
            _ when key.StartsWith("about.") => "/about",
            _ when key.StartsWith("carbon.calculator.") || key.StartsWith("carbon.comparison.") => "/carbon-comparison",
            _ when key.StartsWith("carbon.map.") => "/carbon",
            _ => null,
        };

        public async Task<IActionResult> Index(CancellationToken cancellationToken) =>
            View(await db.ContentBlocks.AsNoTracking().Include(b => b.UpdatedBy)
                .OrderBy(b => b.Title).ToListAsync(cancellationToken));

        public async Task<IActionResult> Edit(string key, CancellationToken cancellationToken)
        {
            var block = await db.ContentBlocks.AsNoTracking().Include(b => b.UpdatedBy)
                .FirstOrDefaultAsync(b => b.Key == key, cancellationToken);
            return block is null ? NotFound() : View(block);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string key, string? html, CancellationToken cancellationToken)
        {
            var block = await db.ContentBlocks.FirstOrDefaultAsync(b => b.Key == key, cancellationToken);
            if (block is null)
            {
                return NotFound();
            }

            var clean = HtmlCleaner.Clean(html);
            if (clean == block.Html)
            {
                TempData["Status"] = "No changes to save.";
                return RedirectToAction(nameof(Edit), new { key });
            }

            block.Html = clean;
            block.UpdatedById = User.FindFirstValue(ClaimTypes.NameIdentifier);
            block.UpdatedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(cancellationToken);

            TempData["Status"] = $"Saved \"{block.Title}\".";
            return RedirectToAction(nameof(Edit), new { key });
        }
    }
}
