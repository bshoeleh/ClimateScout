public enum DesignStrategyDiagram
{
    HotHumid,
    HotDry,
    Temperate,
    Cold
}

public class ClimateScoutClimateZone
{
    public int Id { get; set; }
    public string Name { get; set; } = default!;
    public DesignStrategyDiagram DesignStrategyDiagram { get; set; }
    public ICollection<DesignStrategy> DesignStrategies { get; set; } = new List<DesignStrategy>();
}