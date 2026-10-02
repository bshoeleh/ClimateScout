using A_U_ClimateScout.Options;
using Microsoft.AspNetCore.Mvc;

namespace A_U_ClimateScout.Models
{
    // The base-map settings every map page passes to map-base.js (plan §5): our tile proxy and search endpoint,
    // the zoom range, and the credits — the tile provider's plus the page's own data credit.
    public record MapSettings(string TileUrl, string GeocodeUrl, string Attribution, int MinZoom, int MaxZoom)
    {
        public static MapSettings Create(IUrlHelper url, MapsOptions maps, string dataCredit) => new(
            url.Content("~/map/tiles/{z}/{x}/{y}"),
            url.Content("~/api/v1/geocode"),
            $"{maps.Attribution}, {dataCredit}",
            maps.MinZoom,
            maps.MaxZoom);
    }
}
