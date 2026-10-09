namespace A_U_ClimateScout.Options
{
    // Map settings (plan §5), read from the "Maps" section of configuration. Changing tile provider
    // (OpenStreetMap now, Esri later) means changing these values, not code.
    public class MapsOptions
    {
        public const string SectionName = "Maps";
        public const string HttpClientName = "maps";   // one client (with our User-Agent) for tiles and address search

        public string TileUrl { get; set; } = "";       // upstream raster tiles, with {z}, {x} and {y} placeholders
        public string GeocodeUrl { get; set; } = "";    // upstream address search (Nominatim's /search for now)
        public string Attribution { get; set; } = "";   // HTML credit shown in the map corner (required by the provider)
        public int MinZoom { get; set; } = 2;
        public int MaxZoom { get; set; } = 8;
        public string ContactEmail { get; set; } = "";  // sent in our User-Agent, as OpenStreetMap's usage policy asks

        // Daily caps for the proxies (ProxyUsage); 0 = no cap. Admins are emailed at 80 %.
        public int DailyTileCap { get; set; } = 50_000;
        public int DailyGeocodeCap { get; set; } = 2_000;
    }
}
