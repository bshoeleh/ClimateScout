using System.ComponentModel.DataAnnotations;
using A_U_ClimateScout.Data.Configurations;

namespace A_U_ClimateScout.Areas.Admin.Models
{
    // The add / edit sponsor form. The logo is a separate upload field; CurrentLogoPath shows the existing one.
    public class SponsorForm : IValidatableObject
    {
        public int? Id { get; set; }

        [Required, StringLength(FieldLengths.Name)]
        public string Name { get; set; } = "";

        [Display(Name = "Website"), StringLength(FieldLengths.Url), Url]
        public string? Url { get; set; }

        [Range(1, 3), Display(Name = "Tier")]
        public int Tier { get; set; } = 1;

        [Display(Name = "Starts")]
        public DateOnly? StartDate { get; set; }

        [Display(Name = "Ends")]
        public DateOnly? EndDate { get; set; }

        [Display(Name = "Show in the footer logo strip")]
        public bool ShowInFooter { get; set; } = true;

        [Display(Name = "Active")]
        public bool IsActive { get; set; } = true;

        public string? DescriptionHtml { get; set; }

        [Display(Name = "Logo")]
        public IFormFile? Logo { get; set; }

        [Display(Name = "Remove the current logo")]
        public bool RemoveLogo { get; set; }

        public string? CurrentLogoPath { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (StartDate is { } start && EndDate is { } end && end < start)
            {
                yield return new ValidationResult("The end date is before the start date.", [nameof(EndDate)]);
            }
            if (Url is { Length: > 0 } && !Url.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                && !Url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                yield return new ValidationResult("The website must start with https:// or http://.", [nameof(Url)]);
            }
        }
    }
}
