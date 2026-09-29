namespace A_U_ClimateScout.Models
{
    // "Strategy A conflicts with strategy B". Always stored in both directions (plan §10);
    // StrategyConflictService keeps the pair in step.
    public class StrategyConflict
    {
        public int StrategyId { get; set; }
        public int ConflictsWithStrategyId { get; set; }

        public DesignStrategy Strategy { get; set; } = null!;
        public DesignStrategy ConflictsWith { get; set; } = null!;
    }
}
