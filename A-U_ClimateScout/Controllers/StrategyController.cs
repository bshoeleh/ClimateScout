using A_U_ClimateScout.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace A_U_ClimateScout.Controllers
{
    // Public design strategy pages at /design-strategy/{slug}, the same URLs as the old site, plus a list of all strategies.
    public class StrategyController(ApplicationDbContext db) : Controller
    {
        [HttpGet("design-strategy")]
        public async Task<IActionResult> List(CancellationToken cancellationToken)
        {
            var strategies = await db.DesignStrategies
                .AsNoTracking()
                .Include(s => s.ImageAsset)
                .Where(s => s.IsActive)
                .OrderBy(s => s.Name)
                .ToListAsync(cancellationToken);

            return View(strategies);
        }

        [HttpGet("design-strategy/{slug}")]
        public async Task<IActionResult> Index(string slug, CancellationToken cancellationToken)
        {
            var strategy = await db.DesignStrategies
                .AsNoTracking()
                .Include(s => s.ImageAsset)
                .Include(s => s.ReferenceProjects.OrderBy(p => p.SortOrder))
                    .ThenInclude(p => p.ImageAsset)
                .Include(s => s.Zones.Where(l => l.Zone.IsActive).OrderBy(l => l.Zone.KoppenCode))
                    .ThenInclude(l => l.Zone)
                .AsSplitQuery()
                .FirstOrDefaultAsync(s => s.Slug == slug && s.IsActive, cancellationToken);

            return strategy is null ? NotFound() : View(strategy);
        }

        // The old site had a page per image of a strategy (/design-strategy/cool-roof/cool-roof-2x/); send them to the strategy.
        [HttpGet("design-strategy/{slug}/{attachment}")]
        public IActionResult OldAttachmentPage(string slug) =>
            RedirectToActionPermanent(nameof(Index), new { slug });
    }
}
