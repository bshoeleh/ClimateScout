using A_U_ClimateScout.Identity;

namespace A_U_ClimateScout.Models
{
    // An editable piece of page text, looked up by key ("about.body", "carbon.calculator.intro").
    public class ContentBlock
    {
        public string Key { get; set; } = "";               // primary key
        public string Title { get; set; } = "";             // label in the admin list
        public string Html { get; set; } = "";
        public string? UpdatedById { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }

        public ApplicationUser? UpdatedBy { get; set; }
    }
}
