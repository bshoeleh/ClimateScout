using Microsoft.AspNetCore.Mvc;

namespace A_U_ClimateScout.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
