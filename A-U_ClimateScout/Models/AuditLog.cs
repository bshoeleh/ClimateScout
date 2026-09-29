namespace A_U_ClimateScout.Models
{
    // Who changed what, when: every admin write, import and rollback.
    // No foreign keys on purpose: the log must outlive deleted users and records.
    public class AuditLog
    {
        public long Id { get; set; }
        public DateTimeOffset OccurredAt { get; set; }
        public string? UserId { get; set; }
        public string? UserName { get; set; }               // copied at the time, so it survives the user being deleted
        public string Action { get; set; } = "";            // Create, Update, Delete, Import, Rollback, SignIn …
        public string EntityType { get; set; } = "";        // "DesignStrategy"
        public string? EntityId { get; set; }               // "12"; text so any key type fits
        public string? Summary { get; set; }                // "Renamed 'Cool roof' to 'Cool Roof'"
        public string? ChangesJson { get; set; }            // field-level before/after
    }
}
