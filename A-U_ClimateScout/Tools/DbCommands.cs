using System.CommandLine;
using A_U_ClimateScout.Data;
using Microsoft.EntityFrameworkCore;

namespace A_U_ClimateScout.Tools
{
    // "tool db …": database schema commands.
    public static class DbCommands
    {
        public static Command Create(IServiceProvider services)
        {
            var dryRun = new Option<bool>("--dry-run") { Description = "List pending migrations without applying them." };
            var migrate = new Command("migrate", "Apply pending EF Core migrations (creates the database if missing).") { dryRun };
            migrate.SetAction(async (parseResult, cancellationToken) =>
            {
                using var scope = services.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var logger = scope.ServiceProvider.GetRequiredService<ILogger<ApplicationDbContext>>();

                var pending = (await db.Database.GetPendingMigrationsAsync(cancellationToken)).ToList();
                if (pending.Count == 0)
                {
                    logger.LogInformation("Database is up to date; nothing to apply.");
                    return 0;
                }

                foreach (var name in pending)
                {
                    logger.LogInformation("Pending migration: {Migration}", name);
                }

                if (parseResult.GetValue(dryRun))
                {
                    logger.LogInformation("Dry run: {Count} migration(s) not applied.", pending.Count);
                    return 0;
                }

                await db.Database.MigrateAsync(cancellationToken);
                logger.LogInformation("Applied {Count} migration(s).", pending.Count);
                return 0;
            });

            return new Command("db", "Database schema commands.") { migrate };
        }
    }
}
