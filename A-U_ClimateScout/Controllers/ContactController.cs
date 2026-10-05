using A_U_ClimateScout.Data;
using A_U_ClimateScout.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace A_U_ClimateScout.Controllers
{
    // The Contact page at /contact (plan §8). Messages are saved for the Admin "Contact messages" screen (Phase 5);
    // the email to site admins comes with Phase 8. Spam guards: antiforgery token, a honeypot field, and at most
    // 5 messages per visitor per 15 minutes (the "contact" rate limit in Program.cs).
    public class ContactController(ApplicationDbContext db, ILogger<ContactController> logger) : Controller
    {
        public const string RateLimitPolicy = "contact";

        [HttpGet("contact")]
        public IActionResult Index(string? about)
        {
            var form = new ContactFormModel();
            if (about == "sponsoring")
            {
                form.Message = "We're interested in sponsoring ClimateScout.\n\n";
            }
            return View(form);
        }

        [HttpPost("contact")]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting(RateLimitPolicy)]
        public async Task<IActionResult> Index(ContactFormModel form, CancellationToken cancellationToken)
        {
            // A filled-in honeypot means a bot: act as if it worked, but keep nothing.
            if (!string.IsNullOrEmpty(form.Website))
            {
                logger.LogInformation("Contact form: honeypot filled in, message discarded.");
                return RedirectToAction(nameof(Sent));
            }
            if (!ModelState.IsValid)
            {
                return View(form);
            }

            db.ContactMessages.Add(new ContactMessage
            {
                Name = form.Name.Trim(),
                Email = form.Email.Trim(),
                Message = form.Message.Trim(),
                CreatedAt = DateTimeOffset.UtcNow,
            });
            await db.SaveChangesAsync(cancellationToken);
            return RedirectToAction(nameof(Sent));
        }

        [HttpGet("contact/sent")]
        public IActionResult Sent() => View();
    }
}
