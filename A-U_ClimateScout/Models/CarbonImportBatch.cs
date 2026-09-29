using A_U_ClimateScout.Identity;

namespace A_U_ClimateScout.Models
{
    // One carbon CSV upload, from preview to commit (or rollback). See plan §6.
    public class CarbonImportBatch
    {
        public int Id { get; set; }
        public string FileName { get; set; } = "";
        public int SourceId { get; set; }
        public string Profile { get; set; } = "";           // Ember-Countries, Ember-US-States, CER-Canada
        public CarbonImportStatus Status { get; set; }
        public string? UploadedById { get; set; }           // user; null when run from `tool import carbon`
        public DateTimeOffset UploadedAt { get; set; }
        public DateTimeOffset? CommittedAt { get; set; }
        public DateTimeOffset? RolledBackAt { get; set; }
        public int TotalRows { get; set; }
        public int NewRows { get; set; }
        public int ChangedRows { get; set; }
        public int UnchangedRows { get; set; }
        public int UnmatchedRows { get; set; }
        public int InvalidRows { get; set; }
        public string? ErrorsJson { get; set; }

        public CarbonDataSource Source { get; set; } = null!;
        public ApplicationUser? UploadedBy { get; set; }
    }
}
