using A_U_ClimateScout.Models;

namespace A_U_ClimateScout.Areas.Admin.Models
{
    public record DashboardViewModel(
        IReadOnlyList<DashboardCount> Counts,
        DateTimeOffset? LastCarbonImport,
        int UnhandledMessages,
        IReadOnlyList<AuditLog> RecentActivity,
        IReadOnlyList<AttentionItem> NeedsAttention);

    // One tile: "31 climate zones".
    public record DashboardCount(int Value, string Label);

    // One data gap found live in the database; it disappears from the dashboard once fixed.
    // Names lists the records concerned (zone codes, strategy names …).
    public record AttentionItem(string Title, string Explanation, IReadOnlyList<string> Names);
}
