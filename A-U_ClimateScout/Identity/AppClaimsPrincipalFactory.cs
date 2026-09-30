using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace A_U_ClimateScout.Identity
{
    // Builds the signed-in user's claims (stored in the login cookie). Adds a marker claim when the user
    // must change their password, so MustChangePasswordMiddleware can check it without a database lookup.
    public class AppClaimsPrincipalFactory(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IOptions<IdentityOptions> options)
        : UserClaimsPrincipalFactory<ApplicationUser, IdentityRole>(userManager, roleManager, options)
    {
        public const string MustChangePasswordClaim = "cs:must_change_password";

        protected override async Task<ClaimsIdentity> GenerateClaimsAsync(ApplicationUser user)
        {
            var identity = await base.GenerateClaimsAsync(user);
            if (user.MustChangePassword)
            {
                identity.AddClaim(new Claim(MustChangePasswordClaim, "true"));
            }

            return identity;
        }
    }
}
