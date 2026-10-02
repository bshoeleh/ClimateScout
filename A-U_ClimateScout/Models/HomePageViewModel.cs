namespace A_U_ClimateScout.Models
{
    // What the home page shows: the zone groups with their zones (for the filter buttons and the zone list),
    // and the settings and zone lookup the map script needs (serialised into the page as JSON).
    public record HomePageViewModel(IReadOnlyList<ClimateZoneGroup> Groups, object MapData);
}
