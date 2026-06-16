public class EditAddressVM
{
    public int? ProjectId { get; set; }
    public int? AddressId { get; set; }
    public string? Address1 { get; set; }
    public string? Address2 { get; set; }
    public string? City { get; set; }
    public string? PostalCode { get; set; }
    public int? CountryId { get; set; }
    public int? StateId { get; set; }
    public int? ClimateZoneId { get; set; }
    public int? KoppenClimateZoneId { get; set; }
    public double? HDD { get; set; }
    public double? CDD { get; set; }
    public double? WindowWallRatio { get; set; }
    public string? Location { get; set; }
    public string? DesignStrategyIds { get; set; }

    // Dropdowns
    public List<SelectListItem> Countries { get; set; } = new();
    public List<SelectListItem> States { get; set; } = new();
    public List<SelectListItem> ClimateZones { get; set; } = new();
    public List<SelectListItem> KoppenClimateZones { get; set; } = new();

    // For displaying strategies
    public ClimateScoutClimateZone? ClimateZoneDetail { get; set; }
}