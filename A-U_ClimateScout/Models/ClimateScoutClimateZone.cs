using A_U_ClimateScout.Models;


public class ClimateScoutClimateZone
{
    public int Id { get; set; }

    public int? ParentId { get; set; }

    public string Name { get; set; } = string.Empty;

    public ClimateZoneType Type { get; set; } = ClimateZoneType.Koppen;

    public string? Slug { get; set; }

    public string? Description { get; set; }

    public string? Excerpt { get; set; }

    public string? ClimateCode { get; set; } = string.Empty;

    public string? Color { get; set; } = "FFFFFF";

    public string? MapId { get; set; }

    public bool Active { get; set; } = true;

    public DesignStrategyDiagram DesignStrategyDiagram { get; set; } = DesignStrategyDiagram.None;

    public virtual ICollection<DesignStrategy> DesignStrategies { get; set; } = new List<DesignStrategy>();

}