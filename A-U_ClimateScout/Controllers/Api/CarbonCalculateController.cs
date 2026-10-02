using System.ComponentModel.DataAnnotations;
using A_U_ClimateScout.Data;
using A_U_ClimateScout.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace A_U_ClimateScout.Controllers.Api
{
    // POST /api/v1/carbon/calculate (plan §7): yearly operational carbon from EUI, floor area and grid carbon
    // intensity, with EPA equivalencies. The grid intensity is either entered (gridIntensity) or the current value
    // of a region (regionCode); an entered value wins, so a visitor can use a better local figure.
    [ApiController]
    [Route("api/v1/carbon/calculate")]
    public class CarbonCalculateController(ApplicationDbContext db, CarbonValues carbonValues) : ControllerBase
    {
        [HttpPost]
        public async Task<ActionResult<CarbonCalculateResponse>> Post(CarbonCalculateRequest request, CancellationToken cancellationToken)
        {
            RegionDto? region = null;
            if (!string.IsNullOrWhiteSpace(request.RegionCode))
            {
                region = await db.CarbonRegions.AsNoTracking()
                    .Where(r => r.Code == request.RegionCode)
                    .Select(r => new RegionDto(r.Id, r.Code, r.Name))
                    .FirstOrDefaultAsync(cancellationToken);
                if (region is null)
                {
                    ModelState.AddModelError(nameof(request.RegionCode), $"There is no region with the code \"{request.RegionCode}\".");
                    return ValidationProblem(ModelState);
                }
            }

            GridIntensityDto grid;
            if (request.GridIntensity is { } entered)
            {
                grid = new GridIntensityDto(entered, Year: null, Source: null, SourceUrl: null, region, Entered: true);
            }
            else if (region is null)
            {
                ModelState.AddModelError(nameof(request.GridIntensity), "Give either a region code or a grid carbon intensity.");
                return ValidationProblem(ModelState);
            }
            else if ((await carbonValues.CurrentByRegionIdAsync(cancellationToken)).TryGetValue(region.Id, out var current))
            {
                grid = new GridIntensityDto(current.ValueGPerKWh, current.Year, current.Source, current.SourceUrl, region, Entered: false);
            }
            else
            {
                ModelState.AddModelError(nameof(request.GridIntensity), $"There is no grid carbon intensity for {region.Name}; enter one.");
                return ValidationProblem(ModelState);
            }

            var result = CarbonCalculator.Calculate(request.Eui, request.EuiUnit, request.Area, request.AreaUnit, grid.Value);
            var equivalencies = await db.EquivalencyFactors.AsNoTracking()
                .OrderBy(f => f.SortOrder)
                .Select(f => new { f.Key, f.Label, f.TonsCo2ePerUnit, f.SourceUrl })
                .ToListAsync(cancellationToken);

            return new CarbonCalculateResponse(
                grid,
                KgPerSquareMetre: Math.Round(result.KgPerSquareMetre, 2),
                LbPerSquareFoot: Math.Round(result.LbPerSquareFoot, 2),
                TotalKg: Math.Round(result.TotalKg, 0),
                TotalTonnes: Math.Round(result.TotalTonnes, 2),
                TotalLb: Math.Round(result.TotalLb, 0),
                Equivalencies: equivalencies
                    .Select(f => new EquivalencyDto(f.Key, f.Label, Math.Round(result.TotalTonnes / f.TonsCo2ePerUnit, 0), f.SourceUrl))
                    .ToList());
        }
    }

    // EUI and area must be above 0 (a 0 is almost always a typo); grid intensity may be 0 (a fully renewable grid).
    // The upper limits only catch typing mistakes. (MVC reads validation attributes from a record's constructor
    // parameters, so they go there, not on the properties.)
    public record CarbonCalculateRequest(
        [Range(0d, 100_000d, MinimumIsExclusive = true)] decimal Eui,
        EuiUnit EuiUnit,
        [Range(0d, 1_000_000_000d, MinimumIsExclusive = true)] decimal Area,
        AreaUnit AreaUnit,
        [StringLength(20)] string? RegionCode,
        [Range(0d, 2_000d)] decimal? GridIntensity);

    public record CarbonCalculateResponse(GridIntensityDto GridIntensity, decimal KgPerSquareMetre, decimal LbPerSquareFoot,
        decimal TotalKg, decimal TotalTonnes, decimal TotalLb, IReadOnlyList<EquivalencyDto> Equivalencies);

    // The grid intensity the result used: a region's current value (with year and source), or one entered by the caller.
    public record GridIntensityDto(decimal Value, int? Year, string? Source, string? SourceUrl, RegionDto? Region, bool Entered);

    public record RegionDto([property: System.Text.Json.Serialization.JsonIgnore] int Id, string Code, string Name);

    public record EquivalencyDto(string Key, string Label, decimal Amount, string? SourceUrl);
}
