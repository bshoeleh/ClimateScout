using A_U_ClimateScout.Areas.Admin.Models;
using A_U_ClimateScout.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace A_U_ClimateScout.Areas.Admin.Controllers
{
    // The five Köppen groups (A–E): their names and colours can be edited; the groups themselves are fixed.
    public class ZoneGroupsController(ApplicationDbContext db) : AdminController
    {
        public async Task<IActionResult> Index(CancellationToken cancellationToken) =>
            View(await db.ClimateZoneGroups.AsNoTracking().Include(g => g.Zones)
                .OrderBy(g => g.SortOrder).ToListAsync(cancellationToken));

        public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
        {
            var group = await db.ClimateZoneGroups.AsNoTracking().FirstOrDefaultAsync(g => g.Id == id, cancellationToken);
            return group is null ? NotFound() : View(new ZoneGroupForm { Id = group.Id, Code = group.Code, Name = group.Name, Color = group.Color });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ZoneGroupForm form, CancellationToken cancellationToken)
        {
            var group = await db.ClimateZoneGroups.FirstOrDefaultAsync(g => g.Id == form.Id, cancellationToken);
            if (group is null)
            {
                return NotFound();
            }
            if (!ModelState.IsValid)
            {
                form.Code = group.Code;
                return View(form);
            }

            group.Name = form.Name.Trim();
            group.Color = form.Color.ToUpperInvariant();
            await db.SaveChangesAsync(cancellationToken);

            TempData["Status"] = $"Saved {group.Name}.";
            return RedirectToAction(nameof(Index));
        }
    }
}
