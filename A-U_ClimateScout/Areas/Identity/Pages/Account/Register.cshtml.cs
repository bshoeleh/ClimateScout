using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace A_U_ClimateScout.Areas.Identity.Pages.Account
{
    // Replaces the Identity UI's Register page. Public registration is disabled (plan §9):
    // an Admin creates accounts, so this page always returns 404.
    [AllowAnonymous]
    public class RegisterModel : PageModel
    {
        public IActionResult OnGet() => NotFound();

        public IActionResult OnPost() => NotFound();
    }
}
