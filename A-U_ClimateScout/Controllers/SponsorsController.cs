using A_U_ClimateScout.Services;
using Microsoft.AspNetCore.Mvc;

namespace A_U_ClimateScout.Controllers
{
    // The sponsors page at /sponsors (plan §1): the visible sponsors, or an invitation to sponsor when there are none.
    public class SponsorsController(Sponsors sponsors) : Controller
    {
        [HttpGet("sponsors")]
        public async Task<IActionResult> Index(CancellationToken cancellationToken) =>
            View(await sponsors.VisibleAsync(cancellationToken));
    }
}
