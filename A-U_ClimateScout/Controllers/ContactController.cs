using A_U_ClimateScout.Data;
using A_U_ClimateScout.Identity;
using A_U_ClimateScout.Models;
using A_U_ClimateScout.Options;
using A_U_ClimateScout.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace A_U_ClimateScout.Controllers
{
    // The Contact page at /contact (plan §8). Messages are saved for the Admin "Contact messages" screen and
    // emailed to Email:NotifyAddresses (or every Admin when that's empty). Spam guards: antiforgery token, a honeypot field, and at most
    // 5 messages per visitor per 15 minutes (the "contact" rate limit in Program.cs).
    public class ContactController(ApplicationDbContext db, ILogger<ContactController> logger, IEmailService email,
        IOptions<EmailOptions> emailOptions, UserManager<ApplicationUser> users) : Controller
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

            var message = new ContactMessage
            {
                Name = form.Name.Trim(),
                Email = form.Email.Trim(),
                Message = form.Message.Trim(),
                CreatedAt = DateTimeOffset.UtcNow,
            };
            db.ContactMessages.Add(message);
            await db.SaveChangesAsync(cancellationToken);
            await NotifyAsync(message, cancellationToken);
            return RedirectToAction(nameof(Sent));
        }

        // The visitor's text goes in a plain-text email, so nothing in it can act as a link or markup.
        private async Task NotifyAsync(ContactMessage message, CancellationToken cancellationToken)
        {
            if (!email.IsConfigured)
            {
                return;
            }

            var recipients = emailOptions.Value.NotifyAddresses.Count > 0
                ? emailOptions.Value.NotifyAddresses
                : (await users.GetUsersInRoleAsync(Roles.Admin)).Where(u => u.LockoutEnd is null).Select(u => u.Email!).ToList();
            var link = Url.Action("Details", "ContactMessages", new { area = "Admin", id = message.Id }, Request.Scheme);
            var text = $"From: {message.Name} <{message.Email}>\n\n{message.Message}\n\nOpen it in Admin: {link}";
            foreach (var recipient in recipients)
            {
                await email.SendAsync(recipient, $"ClimateScout contact message from {message.Name}", text, cancellationToken);
            }
        }

        [HttpGet("contact/sent")]
        public IActionResult Sent() => View();
    }
}
