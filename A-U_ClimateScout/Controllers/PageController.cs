using A_U_ClimateScout.Data;
using A_U_ClimateScout.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace A_U_ClimateScout.Controllers
{
    // Pages whose whole content is a content block, editable in Admin (plan Phase 5): /about, the old site's URL.
    public class PageController(ApplicationDbContext db) : Controller
    {
        [HttpGet("about")]
        public Task<IActionResult> About(CancellationToken cancellationToken) =>
            ContentPageAsync("about.body", "About", cancellationToken);

        private async Task<IActionResult> ContentPageAsync(string key, string title, CancellationToken cancellationToken)
        {
            var html = await db.ContentBlocks.AsNoTracking()
                .Where(b => b.Key == key)
                .Select(b => b.Html)
                .FirstOrDefaultAsync(cancellationToken);
            return View("Content", new ContentPageViewModel(title, html));
        }
    }
}
