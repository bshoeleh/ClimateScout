using System.Security.Claims;
using A_U_ClimateScout.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace A_U_ClimateScout.Areas.Admin.Controllers
{
    // Messages from the Contact page: list (to handle / handled / all), read one, mark it handled or not, delete spam.
    // Every change is audited by AuditInterceptor.
    public class ContactMessagesController(ApplicationDbContext db) : AdminController
    {
        public const string ToHandle = "to-handle", Handled = "handled", All = "all";

        public async Task<IActionResult> Index(string show = ToHandle, CancellationToken cancellationToken = default)
        {
            var messages = db.ContactMessages.AsNoTracking().Include(m => m.HandledBy).AsQueryable();
            messages = show switch
            {
                Handled => messages.Where(m => m.Handled),
                All => messages,
                _ => messages.Where(m => !m.Handled),
            };
            ViewData["Show"] = show is Handled or All ? show : ToHandle;
            return View(await messages.OrderByDescending(m => m.CreatedAt).ToListAsync(cancellationToken));
        }

        public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
        {
            var message = await db.ContactMessages.AsNoTracking().Include(m => m.HandledBy)
                .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);
            return message is null ? NotFound() : View(message);
        }

        // handled = true marks it handled (by the signed-in user, now); false puts it back on the to-handle list.
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> SetHandled(int id, bool handled, CancellationToken cancellationToken)
        {
            var message = await db.ContactMessages.FindAsync([id], cancellationToken);
            if (message is null)
            {
                return NotFound();
            }

            message.Handled = handled;
            message.HandledById = handled ? User.FindFirstValue(ClaimTypes.NameIdentifier) : null;
            message.HandledAt = handled ? DateTimeOffset.UtcNow : null;
            await db.SaveChangesAsync(cancellationToken);

            TempData["Status"] = handled ? "Marked as handled." : "Moved back to the messages to handle.";
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
        {
            var message = await db.ContactMessages.FindAsync([id], cancellationToken);
            if (message is null)
            {
                return NotFound();
            }

            db.ContactMessages.Remove(message);
            await db.SaveChangesAsync(cancellationToken);

            TempData["Status"] = $"Deleted the message from {message.Name}.";
            return RedirectToAction(nameof(Index));
        }
    }
}
