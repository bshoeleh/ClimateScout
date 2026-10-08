using System.ComponentModel.DataAnnotations;
using A_U_ClimateScout.Data;
using A_U_ClimateScout.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace A_U_ClimateScout.Areas.Admin.Controllers
{
    // The EPA equivalencies in the carbon calculator's infographic: label, factor, units, icon, group, order.
    // The six factors are fixed (the calculator expects them), so they're edited, not added or removed.
    public class EquivalencyFactorsController(ApplicationDbContext db) : AdminController
    {
        // The icons the infographic's sprite contains (Views/Carbon/Comparison.cshtml); an icon must be one of these.
        public static readonly string[] Icons =
            ["fuel-pump-fill", "car-front-fill", "house-fill", "lightning-charge-fill", "tree", "tree-fill", "cloud-fill"];

        public class FactorForm
        {
            public string Key { get; set; } = "";

            [Required, StringLength(200)]
            public string Label { get; set; } = "";

            [Range(typeof(decimal), "0.00000001", "1000000"), Display(Name = "Metric tons CO₂e per unit")]
            public decimal TonsCo2ePerUnit { get; set; }

            [Required, StringLength(50), Display(Name = "Unit (one)")]
            public string UnitSingular { get; set; } = "";

            [Required, StringLength(50), Display(Name = "Unit (several)")]
            public string UnitPlural { get; set; } = "";

            public EquivalencyKind Kind { get; set; }

            public string? Icon { get; set; }

            [Display(Name = "Order")]
            public int SortOrder { get; set; }

            [StringLength(500), Url, Display(Name = "Source")]
            public string? SourceUrl { get; set; }
        }

        public async Task<IActionResult> Index(CancellationToken cancellationToken) =>
            View(await db.EquivalencyFactors.AsNoTracking().OrderBy(f => f.SortOrder).ToListAsync(cancellationToken));

        public async Task<IActionResult> Edit(string key, CancellationToken cancellationToken)
        {
            var f = await db.EquivalencyFactors.AsNoTracking().FirstOrDefaultAsync(x => x.Key == key, cancellationToken);
            return f is null ? NotFound() : View(new FactorForm
            {
                Key = f.Key, Label = f.Label, TonsCo2ePerUnit = f.TonsCo2ePerUnit, UnitSingular = f.UnitSingular, UnitPlural = f.UnitPlural,
                Kind = f.Kind, Icon = f.Icon, SortOrder = f.SortOrder, SourceUrl = f.SourceUrl,
            });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(FactorForm form, CancellationToken cancellationToken)
        {
            var f = await db.EquivalencyFactors.FirstOrDefaultAsync(x => x.Key == form.Key, cancellationToken);
            if (f is null)
            {
                return NotFound();
            }
            if (form.Icon is not null && !Icons.Contains(form.Icon))
            {
                ModelState.AddModelError(nameof(form.Icon), "Choose one of the listed icons.");
            }
            if (!ModelState.IsValid)
            {
                return View(form);
            }

            f.Label = form.Label.Trim();
            f.TonsCo2ePerUnit = form.TonsCo2ePerUnit;
            f.UnitSingular = form.UnitSingular.Trim();
            f.UnitPlural = form.UnitPlural.Trim();
            f.Kind = form.Kind;
            f.Icon = form.Icon;
            f.SortOrder = form.SortOrder;
            f.SourceUrl = string.IsNullOrWhiteSpace(form.SourceUrl) ? null : form.SourceUrl.Trim();
            await db.SaveChangesAsync(cancellationToken);

            TempData["Status"] = $"Saved \"{f.Label}\".";
            return RedirectToAction(nameof(Index));
        }
    }
}
