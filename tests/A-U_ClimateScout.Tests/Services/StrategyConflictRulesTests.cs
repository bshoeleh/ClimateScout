using A_U_ClimateScout.Services;

namespace A_U_ClimateScout.Tests.Services
{
    // Tests for StrategyConflictRules.MakeSymmetric (plan §10). Pairs are (StrategyId, ConflictsWithStrategyId).
    public class StrategyConflictRulesTests
    {
        [Fact]
        public void OneWayConflict_IsStoredBothWays()
        {
            // Arrange: 1 conflicts with 2, but not the other way round (as in some old WordPress data).
            var input = new[] { (1, 2) };

            // Act
            var result = StrategyConflictRules.MakeSymmetric(input);

            // Assert
            Assert.Equal(new[] { (1, 2), (2, 1) }, result);
        }

        [Fact]
        public void TwoWayConflict_IsNotDuplicated()
        {
            var result = StrategyConflictRules.MakeSymmetric(new[] { (1, 2), (2, 1) });

            Assert.Equal(new[] { (1, 2), (2, 1) }, result);
        }

        [Fact]
        public void RepeatedPairs_AreStoredOnce()
        {
            var result = StrategyConflictRules.MakeSymmetric(new[] { (1, 2), (1, 2), (2, 1) });

            Assert.Equal(new[] { (1, 2), (2, 1) }, result);
        }

        [Fact]
        public void SelfConflict_IsDropped()
        {
            // A strategy can't conflict with itself; the database would reject the row (CK_StrategyConflicts_NotSelf).
            var result = StrategyConflictRules.MakeSymmetric(new[] { (3, 3), (1, 2) });

            Assert.Equal(new[] { (1, 2), (2, 1) }, result);
        }

        [Fact]
        public void NoPairs_GivesNoRows()
        {
            var result = StrategyConflictRules.MakeSymmetric([]);

            Assert.Empty(result);
        }

        [Fact]
        public void Result_IsSortedByStrategyThenConflict()
        {
            var result = StrategyConflictRules.MakeSymmetric(new[] { (5, 4), (2, 1) });

            Assert.Equal(new[] { (1, 2), (2, 1), (4, 5), (5, 4) }, result);
        }
    }
}
