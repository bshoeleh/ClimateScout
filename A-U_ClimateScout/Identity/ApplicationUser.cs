using Microsoft.AspNetCore.Identity;

namespace A_U_ClimateScout.Identity
{
    // The app's user account. Extends the built-in IdentityUser (email, password hash, lockout, 2FA, etc.)
    // so we can add our own fields later without changing every place that references the user type.
    public class ApplicationUser : IdentityUser
    {
    }
}
