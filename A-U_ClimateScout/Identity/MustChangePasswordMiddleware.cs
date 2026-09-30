namespace A_U_ClimateScout.Identity
{
    // Sends a signed-in user who must change their password to the Change password page.
    // Everything else stays blocked until they do; sign-out, error pages and static files still work.
    public static class MustChangePasswordMiddleware
    {
        private const string ChangePasswordPath = "/Identity/Account/Manage/ChangePassword";

        private static readonly string[] AllowedPaths =
        [
            ChangePasswordPath,
            "/Identity/Account/Logout",
            "/error",
        ];

        public static IApplicationBuilder UseMustChangePassword(this IApplicationBuilder app) =>
            app.Use(async (context, next) =>
            {
                var path = context.Request.Path;
                var mustChange = context.User.HasClaim(c => c.Type == AppClaimsPrincipalFactory.MustChangePasswordClaim);
                var allowed = AllowedPaths.Any(p => path.StartsWithSegments(p, StringComparison.OrdinalIgnoreCase))
                    || Path.HasExtension(path.Value);   // CSS, JS, images, fonts

                if (mustChange && !allowed)
                {
                    context.Response.Redirect(context.Request.PathBase + ChangePasswordPath);
                    return;
                }

                await next();
            });
    }
}
