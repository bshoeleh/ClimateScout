public class ProjectAddress
{
    public int Id { get; set; }
    public string Address1 { get; set; } = default!;
    public string? Address2 { get; set; }
    public string City { get; set; } = default!;
    public string PostalCode { get; set; } = default!;
    public int CountryId { get; set; }
    public Country Country { get; set; } = default!;
    public int StateId { get; set; }
    public State State { get; set; } = default!;
    public int? ClimateZoneId { get; set; }
    public ClimateZone? ClimateZone { get; set; }
    public int? KoppenClimateZoneId { get; set; }
    public KoppenClimateZone? KoppenClimateZone { get; set; }
    public double? HDD { get; set; }
    public double? CDD { get; set; }
    public double? WindowWallRatio { get; set; }
    public string? Location { get; set; } // lat,long
    public string? DesignStrategyIds { get; set; } // Comma-separated
}