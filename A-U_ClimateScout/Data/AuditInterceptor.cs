using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;
using A_U_ClimateScout.Data.Configurations;
using A_U_ClimateScout.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace A_U_ClimateScout.Data
{
    // Writes an AuditLog row for every record created, changed or deleted through ApplicationDbContext (plan §9),
    // so each admin screen gets an audit trail without code of its own.
    //
    // How: before a save, note each changed record and its before/after values; after the save succeeds (new records
    // now have their ids), add the AuditLog rows and save them. The audit rows are a second save, so a crash between
    // the two would lose them; acceptable for an audit trail of a small admin team.
    //
    // Left out: the audit log itself, Identity tables (not in A_U_ClimateScout.Models), individual carbon values
    // (an import logs one entry for the whole batch), and saves by signed-out visitors (the public contact form).
    // Command-line tools (no web request) are logged as "System".
    public partial class AuditInterceptor(IHttpContextAccessor httpContextAccessor) : SaveChangesInterceptor
    {
        private static readonly HashSet<Type> Skipped = [typeof(AuditLog), typeof(CarbonIntensity)];

        // Properties tried, in order, for the record's name in the summary.
        private static readonly string[] NameProperties = ["Name", "Title", "Key", "KoppenCode", "Code", "FileName"];

        // Readable JSON: <, > and & stay as they are (it is stored, never written into a page unencoded).
        private static readonly JsonSerializerOptions JsonOptions = new() { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

        private record Pending(EntityEntry Entry, string Action, Dictionary<string, Change> Changes);

        private record Change(object? From, object? To);

        // Changes noted before the save, per context (the interceptor is scoped, like the context).
        private List<Pending> pending = [];

        public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
        {
            Collect(eventData.Context);
            return result;
        }

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            Collect(eventData.Context);
            return ValueTask.FromResult(result);
        }

        public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
        {
            if (eventData.Context is { } context && AddAuditRows(context))
            {
                context.SaveChanges();
            }
            return result;
        }

        public override async ValueTask<int> SavedChangesAsync(
            SaveChangesCompletedEventData eventData, int result, CancellationToken cancellationToken = default)
        {
            if (eventData.Context is { } context && AddAuditRows(context))
            {
                await context.SaveChangesAsync(cancellationToken);
            }
            return result;
        }

        public override void SaveChangesFailed(DbContextErrorEventData eventData) => pending = [];

        public override Task SaveChangesFailedAsync(DbContextErrorEventData eventData, CancellationToken cancellationToken = default)
        {
            pending = [];
            return Task.CompletedTask;
        }

        private void Collect(DbContext? context)
        {
            pending = [];
            var user = httpContextAccessor.HttpContext?.User;
            if (context is null || (httpContextAccessor.HttpContext is not null && user?.Identity?.IsAuthenticated != true))
            {
                return;
            }

            foreach (var entry in context.ChangeTracker.Entries())
            {
                if (entry.Entity.GetType().Namespace != typeof(AuditLog).Namespace || Skipped.Contains(entry.Entity.GetType()))
                {
                    continue;
                }

                var changes = new Dictionary<string, Change>();
                switch (entry.State)
                {
                    case EntityState.Added:
                        foreach (var property in entry.Properties.Where(p => !p.Metadata.IsPrimaryKey()))
                        {
                            changes[property.Metadata.Name] = new Change(null, property.CurrentValue);
                        }
                        pending.Add(new Pending(entry, "Create", changes));
                        break;

                    case EntityState.Modified:
                        foreach (var property in entry.Properties.Where(p => p.IsModified && !Equals(p.OriginalValue, p.CurrentValue)))
                        {
                            changes[property.Metadata.Name] = new Change(property.OriginalValue, property.CurrentValue);
                        }
                        if (changes.Count > 0)   // a save that set values to what they already were changes nothing
                        {
                            pending.Add(new Pending(entry, "Update", changes));
                        }
                        break;

                    case EntityState.Deleted:
                        foreach (var property in entry.Properties.Where(p => !p.Metadata.IsPrimaryKey()))
                        {
                            changes[property.Metadata.Name] = new Change(property.OriginalValue, null);
                        }
                        pending.Add(new Pending(entry, "Delete", changes));
                        break;
                }
            }
        }

        // Turns the noted changes into AuditLog rows on the context; true if there are any to save.
        private bool AddAuditRows(DbContext context)
        {
            if (pending.Count == 0)
            {
                return false;
            }

            var user = httpContextAccessor.HttpContext?.User;
            var userId = user?.FindFirstValue(ClaimTypes.NameIdentifier);
            var userName = user?.Identity?.Name ?? "System";
            var now = DateTimeOffset.UtcNow;

            foreach (var (entry, action, changes) in pending)
            {
                var type = entry.Metadata.ClrType.Name;
                context.Add(new AuditLog
                {
                    OccurredAt = now,
                    UserId = userId,
                    UserName = userName,
                    Action = action,
                    EntityType = type,
                    EntityId = string.Join(",", entry.Metadata.FindPrimaryKey()!.Properties.Select(p => entry.Property(p.Name).CurrentValue)),
                    Summary = Truncate(Summarize(entry, action, type, changes), FieldLengths.Summary),
                    ChangesJson = JsonSerializer.Serialize(changes.ToDictionary(c => c.Key, c => new { from = c.Value.From, to = c.Value.To }), JsonOptions),
                });
            }

            pending = [];   // the audit rows' own save comes back through here with nothing to note
            return true;
        }

        // "Updated design strategy "Cool Roof": Name, Summary"
        private static string Summarize(EntityEntry entry, string action, string type, Dictionary<string, Change> changes)
        {
            var verb = action switch { "Create" => "Created", "Update" => "Updated", _ => "Deleted" };
            var name = NameProperties
                .Select(p => entry.Metadata.FindProperty(p) is null ? null : entry.Property(p).CurrentValue ?? entry.Property(p).OriginalValue)
                .Select(v => v?.ToString())
                .FirstOrDefault(v => !string.IsNullOrEmpty(v));
            var what = WordBoundary().Replace(type, " $1").ToLowerInvariant();
            var summary = name is null ? $"{verb} {what}" : $"{verb} {what} \"{name}\"";
            return action == "Update" ? $"{summary}: {string.Join(", ", changes.Keys)}" : summary;
        }

        private static string Truncate(string text, int length) => text.Length <= length ? text : text[..(length - 1)] + "…";

        [GeneratedRegex("(?<=[a-z])([A-Z])")]
        private static partial Regex WordBoundary();
    }
}
