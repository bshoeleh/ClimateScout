using System.ComponentModel.DataAnnotations;
using A_U_ClimateScout.Data.Configurations;

namespace A_U_ClimateScout.Areas.Admin.Models
{
    // The design strategy form: text, 2030 Palette link, illustration, and which zones use it and which strategies
    // it conflicts with. Reference projects have their own form (ReferenceProjectForm).
    public class StrategyForm : IValidatableObject
    {
        public int? Id { get; set; }
        public string? Slug { get; set; }   // fixed once created; shown read-only

        [Required, StringLength(FieldLengths.Name)]
        public string Name { get; set; } = "";

        [StringLength(FieldLengths.Summary)]
        [Display(Name = "Summary")]
        public string? Summary { get; set; }

        public string? BodyHtml { get; set; }

        [StringLength(FieldLengths.Url), Display(Name = "2030 Palette link")]
        public string? Palette2030Url { get; set; }

        [Display(Name = "Active (shown on the site)")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Illustration")]
        public IFormFile? Image { get; set; }

        public string? CurrentImagePath { get; set; }

        public List<int> ZoneIds { get; set; } = [];
        public List<int> ConflictIds { get; set; } = [];

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (Palette2030Url is { Length: > 0 } && !Palette2030Url.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
                && !Palette2030Url.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
            {
                yield return new ValidationResult("The link must start with https:// or http://.", [nameof(Palette2030Url)]);
            }
        }
    }

    public class ReferenceProjectForm : IValidatableObject
    {
        public int? Id { get; set; }
        public int StrategyId { get; set; }
        public string StrategyName { get; set; } = "";

        [Required, StringLength(FieldLengths.Name)]
        public string Name { get; set; } = "";

        [StringLength(FieldLengths.Name)]
        public string? Location { get; set; }

        [StringLength(FieldLengths.Url), Display(Name = "Project page")]
        public string? Url { get; set; }

        [Display(Name = "Photo")]
        public IFormFile? Photo { get; set; }

        [Display(Name = "Remove the current photo")]
        public bool RemovePhoto { get; set; }

        public string? CurrentPhotoPath { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (Url is { Length: > 0 } && !Url.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
                && !Url.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
            {
                yield return new ValidationResult("The link must start with https:// or http://.", [nameof(Url)]);
            }
        }
    }
}
