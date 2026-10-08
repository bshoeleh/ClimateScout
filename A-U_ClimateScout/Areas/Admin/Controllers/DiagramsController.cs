using System.ComponentModel.DataAnnotations;
using A_U_ClimateScout.Data;
using A_U_ClimateScout.Data.Configurations;
using A_U_ClimateScout.Models;
using A_U_ClimateScout.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace A_U_ClimateScout.Areas.Admin.Controllers
{
    // The four building diagrams: rename, replace the SVG artwork (cleaned by SvgCleaner before it's saved), and see
    // which strategy layers (<g id="ds-{slug}">) the artwork has. Zones choose their diagram on the zone screen.
    public class DiagramsController(ApplicationDbContext db, IWebHostEnvironment environment) : AdminController
    {
        private const long MaxBytes = 5 * 1024 * 1024;

        public record DiagramRow(Diagram Diagram, int Zones, IReadOnlySet<string> Layers, IReadOnlyList<string> Missing);

        public class DiagramForm
        {
            public int Id { get; set; }

            [Required, StringLength(FieldLengths.Name)]
            public string Name { get; set; } = "";

            [Display(Name = "New artwork (SVG)")]
            public IFormFile? Svg { get; set; }
        }

        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            var diagrams = await db.Diagrams.AsNoTracking().Include(d => d.SvgAsset).OrderBy(d => d.Name).ToListAsync(cancellationToken);
            var rows = new List<DiagramRow>();
            foreach (var diagram in diagrams)
            {
                var (layers, missing) = await LayersAsync(diagram, cancellationToken);
                rows.Add(new DiagramRow(diagram, await db.ClimateZones.CountAsync(z => z.DiagramId == diagram.Id, cancellationToken), layers, missing));
            }
            return View(rows);
        }

        public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
        {
            var diagram = await db.Diagrams.AsNoTracking().Include(d => d.SvgAsset).FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
            if (diagram is null)
            {
                return NotFound();
            }
            await FillAsync(diagram, cancellationToken);
            return View(new DiagramForm { Id = diagram.Id, Name = diagram.Name });
        }

        [HttpPost, ValidateAntiForgeryToken]
        [RequestSizeLimit(MaxBytes + 1024 * 1024)]
        public async Task<IActionResult> Edit(DiagramForm form, CancellationToken cancellationToken)
        {
            var diagram = await db.Diagrams.Include(d => d.SvgAsset).FirstOrDefaultAsync(d => d.Id == form.Id, cancellationToken);
            if (diagram is null)
            {
                return NotFound();
            }

            string? cleaned = null;
            if (form.Svg is { Length: > 0 })
            {
                if (form.Svg.Length > MaxBytes)
                {
                    ModelState.AddModelError(nameof(form.Svg), "The file is over 5 MB.");
                }
                else
                {
                    using var reader = new StreamReader(form.Svg.OpenReadStream());
                    cleaned = SvgCleaner.Clean(await reader.ReadToEndAsync(cancellationToken));
                    if (cleaned is null)
                    {
                        ModelState.AddModelError(nameof(form.Svg),
                            "This isn't an SVG the site can use. In Illustrator, use File › Export › Export As… › SVG (styling: presentation attributes), not the old \"Save As SVG\" with a DTD.");
                    }
                }
            }
            if (!ModelState.IsValid)
            {
                await FillAsync(diagram, cancellationToken);
                return View(form);
            }

            diagram.Name = form.Name.Trim();
            if (cleaned is not null)
            {
                // A new file under a new name: the zone pages cache diagrams by file, so they switch over at once.
                var storagePath = $"diagrams/{diagram.Slug}-{DateTime.UtcNow:yyyyMMddHHmmss}.svg";
                var fullPath = Path.Combine(environment.WebRootPath, "img", "media", storagePath);
                await System.IO.File.WriteAllTextAsync(fullPath, cleaned, cancellationToken);
                diagram.SvgAsset = new MediaAsset
                {
                    FileName = Path.GetFileName(form.Svg!.FileName),
                    ContentType = "image/svg+xml",
                    StoragePath = storagePath,
                    SizeBytes = new FileInfo(fullPath).Length,
                    AltText = $"{diagram.Name} building diagram",
                    UploadedAt = DateTimeOffset.UtcNow,
                };
            }
            await db.SaveChangesAsync(cancellationToken);

            TempData["Status"] = cleaned is null ? $"Saved {diagram.Name}." : $"Saved {diagram.Name} with the new artwork.";
            return RedirectToAction(nameof(Edit), new { id = diagram.Id });
        }

        private async Task FillAsync(Diagram diagram, CancellationToken cancellationToken)
        {
            var (layers, missing) = await LayersAsync(diagram, cancellationToken);
            ViewData["Diagram"] = diagram;
            ViewData["Layers"] = layers;
            ViewData["Missing"] = missing;
            ViewData["Zones"] = await db.ClimateZones.AsNoTracking().Where(z => z.DiagramId == diagram.Id)
                .OrderBy(z => z.SortOrder).Select(z => z.KoppenCode + " " + z.Name).ToListAsync(cancellationToken);
        }

        // The strategy layers the artwork has, and the ones missing for strategies of the zones that use it.
        private async Task<(IReadOnlySet<string> Layers, IReadOnlyList<string> Missing)> LayersAsync(Diagram diagram, CancellationToken cancellationToken)
        {
            var slugs = (await db.DesignStrategies.Select(s => s.Slug).ToListAsync(cancellationToken)).ToHashSet(StringComparer.OrdinalIgnoreCase);
            IReadOnlySet<string> layers = new HashSet<string>();
            if (diagram.SvgAsset is not null)
            {
                var path = Path.Combine(environment.WebRootPath, "img", "media", diagram.SvgAsset.StoragePath);
                if (System.IO.File.Exists(path))
                {
                    layers = DiagramMarkup.Prepare(await System.IO.File.ReadAllTextAsync(path, cancellationToken), slugs, "").Layers;
                }
            }
            var needed = await db.ClimateZoneStrategies.Where(l => l.Zone.DiagramId == diagram.Id && l.Strategy.IsActive)
                .Select(l => l.Strategy.Slug).Distinct().OrderBy(s => s).ToListAsync(cancellationToken);
            return (layers, needed.Where(s => !layers.Contains(s)).ToList());
        }
    }
}
