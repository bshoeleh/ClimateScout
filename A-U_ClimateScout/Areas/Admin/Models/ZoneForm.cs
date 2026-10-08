using System.ComponentModel.DataAnnotations;
using A_U_ClimateScout.Data.Configurations;

namespace A_U_ClimateScout.Areas.Admin.Models
{
    // The climate zone form. Köppen code and slug are shown but not editable: the code ties the zone to the map
    // data and the slug is its public address (/zone/{slug}).
    public class ZoneForm
    {
        public int Id { get; set; }
        public string KoppenCode { get; set; } = "";
        public string Slug { get; set; } = "";

        [Required, StringLength(FieldLengths.Name)]
        public string Name { get; set; } = "";

        [Required, RegularExpression("^#[0-9A-Fa-f]{6}$", ErrorMessage = "Use a colour like #1A2B3C.")]
        [Display(Name = "Map colour")]
        public string Color { get; set; } = "#000000";

        public string? DescriptionHtml { get; set; }

        [Display(Name = "Building diagram")]
        public int? DiagramId { get; set; }

        [Display(Name = "Active (shown on the site)")]
        public bool IsActive { get; set; } = true;

        // The design strategies recommended for this zone.
        public List<int> StrategyIds { get; set; } = [];
    }

    // A Köppen group (A–E): only its name and colour are editable.
    public class ZoneGroupForm
    {
        public int Id { get; set; }
        public string Code { get; set; } = "";

        [Required, StringLength(FieldLengths.Name)]
        public string Name { get; set; } = "";

        [Required, RegularExpression("^#[0-9A-Fa-f]{6}$", ErrorMessage = "Use a colour like #1A2B3C.")]
        [Display(Name = "Colour")]
        public string Color { get; set; } = "#000000";
    }
}
