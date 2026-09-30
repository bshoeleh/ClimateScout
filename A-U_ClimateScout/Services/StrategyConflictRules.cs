namespace A_U_ClimateScout.Services
{
    // The rules for strategy conflicts (plan §10): a conflict always goes both ways, a strategy can't
    // conflict with itself, and each direction is stored once. Used by the WordPress import (whose data
    // is often one-directional) and by the Admin conflicts matrix.
    public static class StrategyConflictRules
    {
        // Turns any list of "A conflicts with B" pairs into the full set of rows to store:
        // both directions, no self-conflicts, no duplicates, sorted so the result is predictable.
        public static IReadOnlyList<(int StrategyId, int ConflictsWithStrategyId)> MakeSymmetric(
            IEnumerable<(int StrategyId, int ConflictsWithStrategyId)> pairs)
        {
            var rows = new SortedSet<(int, int)>();
            foreach (var (a, b) in pairs)
            {
                if (a == b)
                {
                    continue;
                }

                rows.Add((a, b));
                rows.Add((b, a));
            }

            return [.. rows];
        }
    }
}
