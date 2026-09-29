using A_U_ClimateScout.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace A_U_ClimateScout.Areas.Admin.Controllers
{
    /// <summary>
    /// Base class for every Admin-area controller: puts it in the Admin area and requires the AdminArea policy.
    /// </summary>
    [Area("Admin")]
    [Authorize(Policy = Policies.AdminArea)]
    public abstract class AdminController : Controller
    {
    }
}
