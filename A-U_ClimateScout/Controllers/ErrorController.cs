using System.Diagnostics;
using A_U_ClimateScout.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace A_U_ClimateScout.Controllers
{
    // Styled error pages. Reached by the status-code and exception-handler middleware in Program.cs.
    [AllowAnonymous]
    [ApiExplorerSettings(IgnoreApi = true)]
    public class ErrorController : Controller
    {
        [Route("error/{statusCode:int}")]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Index(int statusCode)
        {
            // Visiting /error/123 directly: fall back to 404 for anything that isn't an error code.
            Response.StatusCode = statusCode is >= 400 and <= 599 ? statusCode : StatusCodes.Status404NotFound;

            if (Response.StatusCode == StatusCodes.Status404NotFound)
            {
                return View("NotFound");
            }

            return View("Error", new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
