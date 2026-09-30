using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace A_U_ClimateScout.Identity
{
    // The standard UserManager, plus: choosing a new password (change or reset) clears MustChangePassword.
    // Registered in Program.cs with AddUserManager, so the Identity pages use it too.
    public class AppUserManager(
        IUserStore<ApplicationUser> store,
        IOptions<IdentityOptions> optionsAccessor,
        IPasswordHasher<ApplicationUser> passwordHasher,
        IEnumerable<IUserValidator<ApplicationUser>> userValidators,
        IEnumerable<IPasswordValidator<ApplicationUser>> passwordValidators,
        ILookupNormalizer keyNormalizer,
        IdentityErrorDescriber errors,
        IServiceProvider services,
        ILogger<UserManager<ApplicationUser>> logger)
        : UserManager<ApplicationUser>(store, optionsAccessor, passwordHasher, userValidators,
            passwordValidators, keyNormalizer, errors, services, logger)
    {
        public override async Task<IdentityResult> ChangePasswordAsync(ApplicationUser user, string currentPassword, string newPassword)
        {
            var result = await base.ChangePasswordAsync(user, currentPassword, newPassword);
            return result.Succeeded ? await ClearMustChangePasswordAsync(user) : result;
        }

        public override async Task<IdentityResult> ResetPasswordAsync(ApplicationUser user, string token, string newPassword)
        {
            var result = await base.ResetPasswordAsync(user, token, newPassword);
            return result.Succeeded ? await ClearMustChangePasswordAsync(user) : result;
        }

        private async Task<IdentityResult> ClearMustChangePasswordAsync(ApplicationUser user)
        {
            if (!user.MustChangePassword)
            {
                return IdentityResult.Success;
            }

            user.MustChangePassword = false;
            return await UpdateAsync(user);
        }
    }
}
