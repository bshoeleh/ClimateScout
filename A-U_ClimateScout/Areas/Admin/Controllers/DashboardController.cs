using A_U_ClimateScout.Areas.Admin.Models;
using A_U_ClimateScout.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace A_U_ClimateScout.Areas.Admin.Controllers
{
    // /admin: counts, contact messages to handle, recent changes, and data gaps to fix ("Needs attention").
    public class DashboardController(ApplicationDbContext db) : AdminController
    {
        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            // Current carbon values: rows no later import has replaced.
            var currentCarbon = db.CarbonIntensities.Where(i => i.SupersededByBatchId == null);

            var counts = new List<DashboardCount>
            {
                new(await db.ClimateZones.CountAsync(z => z.IsActive, cancellationToken), "Climate zones"),
                new(await db.DesignStrategies.CountAsync(s => s.IsActive, cancellationToken), "Design strategies"),
                new(await db.ReferenceProjects.CountAsync(cancellationToken), "Reference projects"),
                new(await db.Sponsors.CountAsync(s => s.IsActive, cancellationToken), "Sponsors"),
                new(await db.ContentBlocks.CountAsync(cancellationToken), "Content blocks"),
                new(await currentCarbon.CountAsync(cancellationToken), "Carbon values"),
            };

            var lastImport = await db.CarbonImportBatches.MaxAsync(b => b.CommittedAt, cancellationToken);
            var unhandled = await db.ContactMessages.CountAsync(m => !m.Handled, cancellationToken);
            var recent = await db.AuditLogs.AsNoTracking()
                .OrderByDescending(a => a.OccurredAt).Take(10).ToListAsync(cancellationToken);

            return View(new DashboardViewModel(counts, lastImport, unhandled, recent, await NeedsAttentionAsync(cancellationToken)));
        }

        // Data gaps that can be found from the data itself (the rest of the owner's list is in the plan, Phase 9).
        private async Task<List<AttentionItem>> NeedsAttentionAsync(CancellationToken cancellationToken)
        {
            var items = new List<AttentionItem>();

            void Add(string title, string explanation, List<string> names)
            {
                if (names.Count > 0)
                {
                    items.Add(new AttentionItem(title, explanation, names));
                }
            }

            Add("Climate zones without a diagram",
                "The zone page shows no building diagram until one is chosen.",
                await db.ClimateZones.Where(z => z.IsActive && z.DiagramId == null)
                    .OrderBy(z => z.SortOrder).Select(z => z.KoppenCode + " " + z.Name).ToListAsync(cancellationToken));

            Add("Design strategies without a 2030 Palette link",
                "The strategy page has no \"Learn more at the 2030 Palette\" band.",
                await db.DesignStrategies.Where(s => s.IsActive && (s.Palette2030Url == null || s.Palette2030Url == ""))
                    .OrderBy(s => s.Name).Select(s => s.Name).ToListAsync(cancellationToken));

            Add("Reference projects without a location",
                "The project card shows only the name.",
                await db.ReferenceProjects.Where(p => p.Location == null || p.Location == "")
                    .OrderBy(p => p.Name).Select(p => p.Name + " (" + p.Strategy.Name + ")").ToListAsync(cancellationToken));

            Add("Reference project links to callisonrtkl.com",
                "These now redirect to a generic Arcadis page; replace them with the project's arcadis.com page.",
                await db.ReferenceProjects.Where(p => p.Url != null && p.Url.Contains("callisonrtkl.com"))
                    .OrderBy(p => p.Name).Select(p => p.Name).ToListAsync(cancellationToken));

            Add("Map regions without a carbon value",
                "They show grey (\"No data\") on the carbon map.",
                await db.CarbonRegions.Where(r => r.ShowOnMap && !r.Intensities.Any(i => i.SupersededByBatchId == null))
                    .OrderBy(r => r.Name).Select(r => r.Name).ToListAsync(cancellationToken));

            Add("Sponsors without a logo",
                "They are listed on the Sponsors page but left out of the footer strip.",
                await db.Sponsors.Where(s => s.IsActive && s.LogoAssetId == null)
                    .OrderBy(s => s.Name).Select(s => s.Name).ToListAsync(cancellationToken));

            return items;
        }
    }
}
