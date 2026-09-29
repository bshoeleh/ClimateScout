using A_U_ClimateScout.Identity;

namespace A_U_ClimateScout.Models
{
    // A message sent through the Contact page.
    public class ContactMessage
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Email { get; set; } = "";
        public string Message { get; set; } = "";
        public DateTimeOffset CreatedAt { get; set; }
        public bool Handled { get; set; }
        public string? HandledById { get; set; }
        public DateTimeOffset? HandledAt { get; set; }

        public ApplicationUser? HandledBy { get; set; }
    }
}
