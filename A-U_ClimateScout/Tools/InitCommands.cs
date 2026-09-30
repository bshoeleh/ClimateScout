using System.CommandLine;
using A_U_ClimateScout.Data;
using A_U_ClimateScout.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace A_U_ClimateScout.Tools
{
    // "tool init …": create lookup data and the first Admin account (plan §3.3).
    public static class InitCommands
    {
        public static Command Create(IServiceProvider services)
        {
            return new Command("init", "Create lookup data and the first Admin account.")
            {
                CreateReference(services),
                CreateAdmin(services),
            };
        }

        // tool init reference [--dry-run]
        private static Command CreateReference(IServiceProvider services)
        {
            var dryRun = new Option<bool>("--dry-run") { Description = "Report what would be created without saving." };
            var command = new Command("reference", "Create missing roles, zone groups, diagrams, equivalency factors and carbon sources.") { dryRun };
            command.SetAction(async (parseResult, cancellationToken) =>
            {
                using var scope = services.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
                var logger = scope.ServiceProvider.GetRequiredService<ILogger<ApplicationDbContext>>();
                var isDryRun = parseResult.GetValue(dryRun);

                // Roles go through RoleManager (it fills in the normalized name), not straight into the table.
                var rolesCreated = 0;
                foreach (var role in ReferenceData.Roles)
                {
                    if (await roleManager.RoleExistsAsync(role))
                    {
                        continue;
                    }

                    rolesCreated++;
                    if (!isDryRun)
                    {
                        EnsureSucceeded(await roleManager.CreateAsync(new IdentityRole(role)), $"create role {role}");
                    }
                }
                Report(logger, "Roles", rolesCreated, ReferenceData.Roles.Length, isDryRun);

                await AddMissingAsync(db, logger, "Zone groups", ReferenceData.ZoneGroups, db.ClimateZoneGroups, g => g.Code, isDryRun, cancellationToken);
                await AddMissingAsync(db, logger, "Diagrams", ReferenceData.Diagrams, db.Diagrams, d => d.Slug, isDryRun, cancellationToken);
                await AddMissingAsync(db, logger, "Equivalency factors", ReferenceData.EquivalencyFactors, db.EquivalencyFactors, f => f.Key, isDryRun, cancellationToken);
                await AddMissingAsync(db, logger, "Carbon sources", ReferenceData.CarbonDataSources, db.CarbonDataSources, s => s.Name, isDryRun, cancellationToken);

                if (!isDryRun)
                {
                    await db.SaveChangesAsync(cancellationToken);
                }

                return 0;
            });
            return command;
        }

        // tool init admin --email <email> [--dry-run]
        private static Command CreateAdmin(IServiceProvider services)
        {
            var email = new Option<string>("--email") { Description = "Email address (also the user name) of the Admin account.", Required = true };
            var dryRun = new Option<bool>("--dry-run") { Description = "Report what would happen without saving." };
            var command = new Command("admin", "Create the Admin account, or reset its password if it exists. Prints a one-time password.") { email, dryRun };
            command.SetAction(async (parseResult, cancellationToken) =>
            {
                using var scope = services.CreateScope();
                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
                var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
                var logger = scope.ServiceProvider.GetRequiredService<ILogger<ApplicationDbContext>>();
                var address = parseResult.GetValue(email)!.Trim();

                if (!await roleManager.RoleExistsAsync(Roles.Admin))
                {
                    logger.LogError("The {Role} role doesn't exist yet. Run \"tool init reference\" first.", Roles.Admin);
                    return 1;
                }

                var user = await userManager.FindByEmailAsync(address);
                if (parseResult.GetValue(dryRun))
                {
                    logger.LogInformation("Dry run: would {Action} the Admin account {Email}.", user is null ? "create" : "reset", address);
                    return 0;
                }

                var password = PasswordGenerator.Generate();
                if (user is null)
                {
                    user = new ApplicationUser { UserName = address, Email = address, EmailConfirmed = true, MustChangePassword = true };
                    EnsureSucceeded(await userManager.CreateAsync(user, password), "create user");
                    logger.LogInformation("Created account {Email}.", address);
                }
                else
                {
                    // Remove + add rather than a reset token, so AppUserManager doesn't clear MustChangePassword.
                    if (await userManager.HasPasswordAsync(user))
                    {
                        EnsureSucceeded(await userManager.RemovePasswordAsync(user), "remove old password");
                    }
                    EnsureSucceeded(await userManager.AddPasswordAsync(user, password), "set password");

                    user.EmailConfirmed = true;
                    user.MustChangePassword = true;
                    EnsureSucceeded(await userManager.UpdateAsync(user), "update user");
                    EnsureSucceeded(await userManager.SetLockoutEndDateAsync(user, null), "clear lockout");
                    EnsureSucceeded(await userManager.ResetAccessFailedCountAsync(user), "reset failed sign-in count");
                    logger.LogInformation("Reset password and lockout for {Email}.", address);
                }

                if (!await userManager.IsInRoleAsync(user, Roles.Admin))
                {
                    EnsureSucceeded(await userManager.AddToRoleAsync(user, Roles.Admin), "add Admin role");
                    logger.LogInformation("Added {Email} to the {Role} role.", address, Roles.Admin);
                }

                // Written to the console only, never to the logger, so the password stays out of the log files.
                Console.WriteLine();
                Console.WriteLine($"  One-time password for {address}:  {password}");
                Console.WriteLine("  It is shown only once. The user must change it at first sign-in.");
                Console.WriteLine();
                return 0;
            });
            return command;
        }

        // Adds the rows whose key isn't in the table yet; existing rows are left alone.
        private static async Task AddMissingAsync<T>(ApplicationDbContext db, ILogger logger, string label,
            IReadOnlyList<T> wanted, DbSet<T> table, Func<T, string> key, bool isDryRun, CancellationToken cancellationToken)
            where T : class
        {
            var existingKeys = (await table.AsNoTracking().ToListAsync(cancellationToken)).Select(key).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var missing = wanted.Where(row => !existingKeys.Contains(key(row))).ToList();
            if (!isDryRun)
            {
                table.AddRange(missing);
            }
            Report(logger, label, missing.Count, wanted.Count, isDryRun);
        }

        private static void Report(ILogger logger, string label, int created, int total, bool isDryRun) =>
            logger.LogInformation("{Label}: {Verb} {Created}, skipped {Skipped} (already present).",
                label, isDryRun ? "would create" : "created", created, total - created);

        private static void EnsureSucceeded(IdentityResult result, string action)
        {
            if (!result.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Could not {action}: {string.Join(" ", result.Errors.Select(e => e.Description))}");
            }
        }
    }
}
