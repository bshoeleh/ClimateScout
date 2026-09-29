using Microsoft.AspNetCore.Mvc;

namespace A_U_ClimateScout.Areas.Admin.Controllers
{
    public class DashboardController : AdminController
    {
        public IActionResult Index() => View();
    }
}
