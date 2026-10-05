using A_U_ClimateScout.Services;
using Microsoft.AspNetCore.Mvc;

namespace A_U_ClimateScout.ViewComponents
{
    // The sponsor logo strip in the footer (plan §1): visible sponsors that have a logo and ShowInFooter set.
    // Renders nothing when there are none.
    public class SponsorStripViewComponent(Sponsors sponsors) : ViewComponent
    {
        public async Task<IViewComponentResult> InvokeAsync()
        {
            var inFooter = (await sponsors.VisibleAsync(HttpContext.RequestAborted))
                .Where(s => s.ShowInFooter && s.LogoAsset is not null)
                .ToList();
            return inFooter.Count == 0 ? Content("") : View(inFooter);
        }
    }
}
