using System.Text.Json;
using A_U_ClimateScout.Data;
using A_U_ClimateScout.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace A_U_ClimateScout.Areas.Admin.Controllers
{
    // The audit log (written by AuditInterceptor): newest first, filtered by person, record type or text,
    // 50 a page; one entry shows each changed field before and after. Admins only.
    [Authorize(Policy = Policies.AdminOnly)]
    public class AuditLogController(ApplicationDbContext db) : AdminController
    {
        private const int PageSize = 50;

        public record Filter(string? User, string? Type, string? Search, int Page = 1);

        public async Task<IActionResult> Index([FromQuery] Filter filter, CancellationToken cancellationToken)
        {
            var query = db.AuditLogs.AsNoTracking();
            if (!string.IsNullOrWhiteSpace(filter.User))
            {
                query = query.Where(a => a.UserName == filter.User);
            }
            if (!string.IsNullOrWhiteSpace(filter.Type))
            {
                query = query.Where(a => a.EntityType == filter.Type);
            }
            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                query = query.Where(a => a.Summary!.Contains(filter.Search));
            }

            var page = Math.Max(1, filter.Page);
            ViewData["Total"] = await query.CountAsync(cancellationToken);
            ViewData["Users"] = await db.AuditLogs.Select(a => a.UserName).Distinct().OrderBy(u => u).ToListAsync(cancellationToken);
            ViewData["Types"] = await db.AuditLogs.Select(a => a.EntityType).Distinct().OrderBy(t => t).ToListAsync(cancellationToken);
            ViewData["Filter"] = filter with { Page = page };
            ViewData["PageSize"] = PageSize;

            return View(await query.OrderByDescending(a => a.Id).Skip((page - 1) * PageSize).Take(PageSize).ToListAsync(cancellationToken));
        }

        public async Task<IActionResult> Details(long id, CancellationToken cancellationToken)
        {
            var entry = await db.AuditLogs.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
            if (entry is null)
            {
                return NotFound();
            }

            // {"Name":{"from":"a","to":"b"}, …} → rows of field, before, after.
            var changes = new List<(string Field, string? From, string? To)>();
            if (!string.IsNullOrEmpty(entry.ChangesJson))
            {
                using var json = JsonDocument.Parse(entry.ChangesJson);
                foreach (var field in json.RootElement.EnumerateObject())
                {
                    changes.Add((field.Name, Text(field.Value, "from"), Text(field.Value, "to")));
                }
            }
            ViewData["Changes"] = changes;
            return View(entry);
        }

        private static string? Text(JsonElement change, string name) =>
            change.TryGetProperty(name, out var value) && value.ValueKind != JsonValueKind.Null
                ? value.ValueKind == JsonValueKind.String ? value.GetString() : value.GetRawText()
                : null;
    }
}
