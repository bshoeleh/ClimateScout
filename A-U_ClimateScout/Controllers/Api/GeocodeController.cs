using A_U_ClimateScout.Services;
using Microsoft.AspNetCore.Mvc;

namespace A_U_ClimateScout.Controllers.Api
{
    // GET /api/v1/geocode?q=… (plan §7): address search for the maps' search box.
    // Rate limits and the daily cap come with the rest of the API (Phase 7).
    [ApiController]
    [Route("api/v1/geocode")]
    public class GeocodeController(Geocoder geocoder, ILogger<GeocodeController> logger) : ControllerBase
    {
        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<GeocodeResult>>> Get([FromQuery] string? q, CancellationToken cancellationToken)
        {
            if (q is null || q.Trim().Length is < 3 or > 200)
            {
                return Problem("Search text must be 3 to 200 characters.", statusCode: StatusCodes.Status400BadRequest);
            }

            try
            {
                return Ok(await geocoder.SearchAsync(q, cancellationToken));
            }
            catch (HttpRequestException exception)
            {
                logger.LogWarning(exception, "Address search failed for {Query}", q);
                return Problem("Address search is unavailable right now.", statusCode: StatusCodes.Status502BadGateway);
            }
        }
    }
}
