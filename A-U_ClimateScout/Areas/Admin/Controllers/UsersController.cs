using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using A_U_ClimateScout.Data;
using A_U_ClimateScout.Identity;
using A_U_ClimateScout.Models;
using A_U_ClimateScout.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace A_U_ClimateScout.Areas.Admin.Controllers
{
    // Staff accounts (plan §9): invite with a role and a one-time password, change role, disable / enable, reset
    // password. Admins only. Identity tables aren't covered by AuditInterceptor, so each action logs itself.
    // An Admin can't disable themselves or remove their own Admin role (no locking yourself out).
    [Authorize(Policy = Policies.AdminOnly)]
    public class UsersController(UserManager<ApplicationUser> users, ApplicationDbContext db, IEmailService email) : AdminController
    {
        public static readonly string[] AssignableRoles = [Roles.Admin, Roles.Editor];

        public record UserRow(ApplicationUser User, string? Role, bool Disabled);

        public class InviteForm
        {
            [Required, EmailAddress, StringLength(256)]
            public string Email { get; set; } = "";

            [Required]
            public string Role { get; set; } = Roles.Editor;
        }

        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            var rows = new List<UserRow>();
            foreach (var user in await users.Users.OrderBy(u => u.Email).ToListAsync(cancellationToken))
            {
                var roles = await users.GetRolesAsync(user);
                rows.Add(new UserRow(user, roles.FirstOrDefault(), user.LockoutEnd > DateTimeOffset.UtcNow));
            }
            ViewData["Me"] = users.GetUserId(User);
            return View(rows);
        }

        public IActionResult Invite() => View(new InviteForm());

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Invite(InviteForm form, CancellationToken cancellationToken)
        {
            if (!AssignableRoles.Contains(form.Role))
            {
                ModelState.AddModelError(nameof(form.Role), "Choose Admin or Editor.");
            }
            if (ModelState.IsValid && await users.FindByEmailAsync(form.Email.Trim()) is not null)
            {
                ModelState.AddModelError(nameof(form.Email), "There is already an account with this email address.");
            }
            if (!ModelState.IsValid)
            {
                return View(form);
            }

            var address = form.Email.Trim();
            var password = PasswordGenerator.Generate();
            var user = new ApplicationUser { UserName = address, Email = address, EmailConfirmed = true, MustChangePassword = true };
            var result = await users.CreateAsync(user, password);
            if (result.Succeeded)
            {
                result = await users.AddToRoleAsync(user, form.Role);
            }
            if (!result.Succeeded)
            {
                ModelState.AddModelError("", string.Join(" ", result.Errors.Select(e => e.Description)));
                return View(form);
            }

            await LogAsync("Create", user, $"Invited {address} as {form.Role}", cancellationToken);
            await SendPasswordAsync(user, password, invited: true, cancellationToken);
            return ShowPassword(user, password);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> SetRole(string id, string role, CancellationToken cancellationToken)
        {
            var user = await users.FindByIdAsync(id);
            if (user is null || !AssignableRoles.Contains(role))
            {
                return NotFound();
            }
            if (user.Id == users.GetUserId(User) && role != Roles.Admin)
            {
                TempData["Status"] = "You can't remove your own Admin role. Ask another Admin.";
                return RedirectToAction(nameof(Index));
            }

            var current = await users.GetRolesAsync(user);
            await users.RemoveFromRolesAsync(user, current);
            await users.AddToRoleAsync(user, role);
            await users.UpdateSecurityStampAsync(user);   // signs them out elsewhere, so the new role applies at once
            await LogAsync("Update", user, $"Changed {user.Email} to {role}", cancellationToken);

            TempData["Status"] = $"{user.Email} is now {role}.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> SetDisabled(string id, bool disabled, CancellationToken cancellationToken)
        {
            var user = await users.FindByIdAsync(id);
            if (user is null)
            {
                return NotFound();
            }
            if (user.Id == users.GetUserId(User))
            {
                TempData["Status"] = "You can't disable your own account.";
                return RedirectToAction(nameof(Index));
            }

            await users.SetLockoutEnabledAsync(user, true);
            await users.SetLockoutEndDateAsync(user, disabled ? DateTimeOffset.MaxValue : null);
            await users.UpdateSecurityStampAsync(user);
            await LogAsync("Update", user, $"{(disabled ? "Disabled" : "Enabled")} {user.Email}", cancellationToken);

            TempData["Status"] = $"{user.Email} is {(disabled ? "disabled and signed out" : "enabled again")}.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(string id, CancellationToken cancellationToken)
        {
            var user = await users.FindByIdAsync(id);
            if (user is null)
            {
                return NotFound();
            }

            // Remove + add (not a reset token), so AppUserManager keeps MustChangePassword set, as in tool init admin.
            var password = PasswordGenerator.Generate();
            if (await users.HasPasswordAsync(user))
            {
                await users.RemovePasswordAsync(user);
            }
            await users.AddPasswordAsync(user, password);
            user.MustChangePassword = true;
            await users.UpdateAsync(user);
            await users.ResetAccessFailedCountAsync(user);
            await users.UpdateSecurityStampAsync(user);
            await LogAsync("Update", user, $"Reset the password of {user.Email}", cancellationToken);

            await SendPasswordAsync(user, password, invited: false, cancellationToken);
            return ShowPassword(user, password);
        }

        // The one-time password is shown once, on the next page only (TempData is encrypted and read once).
        private IActionResult ShowPassword(ApplicationUser user, string password)
        {
            TempData["PasswordFor"] = user.Email;
            TempData["Password"] = password;
            return RedirectToAction(nameof(Password));
        }

        public IActionResult Password() =>
            TempData["Password"] is string ? View() : RedirectToAction(nameof(Index));

        // Also emailed when email is set up (Phase 8); until then the page is the only way to pass it on.
        private Task SendPasswordAsync(ApplicationUser user, string password, bool invited, CancellationToken cancellationToken)
        {
            var signIn = Url.Page("/Account/Login", null, new { area = "Identity" }, Request.Scheme)!;
            var subject = invited ? "Your Arcadis ClimateScout account" : "Your new Arcadis ClimateScout password";
            var text = (invited ? "An account has been created for you on Arcadis ClimateScout." : "Your Arcadis ClimateScout password has been reset.")
                + $"\n\nSign in at {signIn} with\n  email: {user.Email}\n  one-time password: {password}\n\nYou will be asked to choose your own password straight away.";
            return email.SendAsync(user.Email!, subject, text, cancellationToken);
        }

        private async Task LogAsync(string action, ApplicationUser user, string summary, CancellationToken cancellationToken)
        {
            db.AuditLogs.Add(new AuditLog
            {
                OccurredAt = DateTimeOffset.UtcNow,
                UserId = User.FindFirstValue(ClaimTypes.NameIdentifier),
                UserName = User.Identity?.Name,
                Action = action,
                EntityType = nameof(ApplicationUser),
                EntityId = user.Id,
                Summary = summary,
            });
            await db.SaveChangesAsync(cancellationToken);
        }
    }
}
