using System.ComponentModel.DataAnnotations;

namespace A_U_ClimateScout.Models
{
    // The Contact form (plan §8). Website is a honeypot: hidden from people, so only bots fill it in.
    public class ContactFormModel
    {
        [Required(ErrorMessage = "Please enter your name.")]
        [StringLength(200)]
        public string Name { get; set; } = "";

        [Required(ErrorMessage = "Please enter your email address.")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
        [StringLength(256)]
        public string Email { get; set; } = "";

        [Required(ErrorMessage = "Please enter a message.")]
        [StringLength(5000, ErrorMessage = "Please keep your message under 5,000 characters.")]
        public string Message { get; set; } = "";

        public string? Website { get; set; }
    }
}
