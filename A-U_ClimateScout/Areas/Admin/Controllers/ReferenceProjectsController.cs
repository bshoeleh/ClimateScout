using A_U_ClimateScout.Areas.Admin.Models;
using A_U_ClimateScout.Data;
using A_U_ClimateScout.Models;
using A_U_ClimateScout.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace A_U_ClimateScout.Areas.Admin.Controllers
{
    // A design strategy's reference projects: add, edit (photo upload), delete, and up/down ordering.
    // Reached from the strategy's edit page; every action returns there. Saves are audited.
    public class ReferenceProjectsController(ApplicationDbContext db, ImageUploads uploads) : AdminController
    {
        private const string PhotoFolder = "projects";

        public async Task<IActionResult> Create(int strategyId, CancellationToken cancellationToken)
        {
            var strategy = await db.DesignStrategies.AsNoTracking().FirstOrDefaultAsync(s => s.Id == strategyId, cancellationToken);
            return strategy is null ? NotFound() : View("Edit", new ReferenceProjectForm { StrategyId = strategy.Id, StrategyName = strategy.Name });
        }

        public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
        {
            var project = await db.ReferenceProjects.AsNoTracking().Include(p => p.Strategy).Include(p => p.ImageAsset)
                .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
            if (project is null)
            {
                return NotFound();
            }

            return View(new ReferenceProjectForm
            {
                Id = project.Id,
                StrategyId = project.StrategyId,
                StrategyName = project.Strategy.Name,
                Name = project.Name,
                Location = project.Location,
                Url = project.Url,
                CurrentPhotoPath = project.ImageAsset?.StoragePath,
            });
        }

        [HttpPost, ValidateAntiForgeryToken]
        [RequestSizeLimit(ImageUploads.MaxBytes + 1024 * 1024)]
        public async Task<IActionResult> Save(ReferenceProjectForm form, CancellationToken cancellationToken)
        {
            var strategy = await db.DesignStrategies.FirstOrDefaultAsync(s => s.Id == form.StrategyId, cancellationToken);
            if (strategy is null)
            {
                return NotFound();
            }
            form.StrategyName = strategy.Name;

            ReferenceProject? project = null;
            if (form.Id is { } id)
            {
                project = await db.ReferenceProjects.Include(p => p.ImageAsset)
                    .FirstOrDefaultAsync(p => p.Id == id && p.StrategyId == strategy.Id, cancellationToken);
                if (project is null)
                {
                    return NotFound();
                }
                form.CurrentPhotoPath = project.ImageAsset?.StoragePath;
            }

            if (!ModelState.IsValid)
            {
                return View("Edit", form);
            }

            MediaAsset? newPhoto = null;
            if (form.Photo is { Length: > 0 })
            {
                var result = await uploads.SaveAsync(form.Photo, PhotoFolder, form.Name, cancellationToken);
                if (result.Error is not null)
                {
                    ModelState.AddModelError(nameof(form.Photo), result.Error);
                    return View("Edit", form);
                }
                newPhoto = result.Asset;
            }

            if (project is null)
            {
                project = new ReferenceProject
                {
                    Strategy = strategy,
                    SortOrder = (await db.ReferenceProjects.Where(p => p.StrategyId == strategy.Id)
                        .MaxAsync(p => (int?)p.SortOrder, cancellationToken) ?? 0) + 1,
                };
                db.ReferenceProjects.Add(project);
            }

            project.Name = form.Name.Trim();
            project.Location = string.IsNullOrWhiteSpace(form.Location) ? null : form.Location.Trim();
            project.Url = string.IsNullOrWhiteSpace(form.Url) ? null : form.Url.Trim();
            if (newPhoto is not null)
            {
                project.ImageAsset = newPhoto;
            }
            else if (form.RemovePhoto)
            {
                project.ImageAssetId = null;
                project.ImageAsset = null;
            }

            await db.SaveChangesAsync(cancellationToken);
            TempData["Status"] = $"Saved the project {project.Name}.";
            return BackToStrategy(strategy.Id);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Move(int id, int direction, CancellationToken cancellationToken)
        {
            var project = await db.ReferenceProjects.FindAsync([id], cancellationToken);
            if (project is null)
            {
                return NotFound();
            }

            var siblings = await db.ReferenceProjects.Where(p => p.StrategyId == project.StrategyId)
                .OrderBy(p => p.SortOrder).ThenBy(p => p.Name).ToListAsync(cancellationToken);
            var index = siblings.FindIndex(p => p.Id == id);
            var other = index + Math.Sign(direction);
            if (direction != 0 && other >= 0 && other < siblings.Count)
            {
                (siblings[index], siblings[other]) = (siblings[other], siblings[index]);
                for (var i = 0; i < siblings.Count; i++)
                {
                    siblings[i].SortOrder = i + 1;
                }
                await db.SaveChangesAsync(cancellationToken);
            }
            return BackToStrategy(project.StrategyId);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
        {
            var project = await db.ReferenceProjects.Include(p => p.Strategy).FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
            if (project is null)
            {
                return NotFound();
            }

            db.ReferenceProjects.Remove(project);
            await db.SaveChangesAsync(cancellationToken);
            TempData["Status"] = $"Deleted the project {project.Name}.";
            return BackToStrategy(project.StrategyId);
        }

        private RedirectToActionResult BackToStrategy(int strategyId) =>
            RedirectToAction("Edit", "DesignStrategies", new { id = strategyId }, "projects");
    }
}
