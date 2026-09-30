using A_U_ClimateScout.Services;

namespace A_U_ClimateScout.Tests.Services
{
    // Tests for CarbonCalculator (plan §2 "Calculator"). Expected values are worked out by hand.
    public class CarbonCalculatorTests
    {
        [Fact]
        public void MetricUnits_GiveExpectedCarbon()
        {
            // Arrange: EUI 100 kWh/m²·yr, area 1,000 m², grid 400 g/kWh (test list, line 1).
            var eui = 100m;
            var area = 1000m;
            var gridIntensity = 400m;

            // Act
            var result = CarbonCalculator.Calculate(eui, EuiUnit.KwhPerSquareMetre, area, AreaUnit.SquareMetres, gridIntensity);

            // Assert: 100 × 0.4 = 40 kg/m²·yr; × 1,000 m² = 40,000 kg = 40 tonnes.
            Assert.Equal(40m, result.KgPerSquareMetre, 2);
            Assert.Equal(40m, result.TotalTonnes, 2);
        }

        [Fact]
        public void ImperialUnits_GiveExpectedCarbon()
        {
            // Arrange: EUI 25 kBtu/ft²·yr, area 15,000 ft², grid 400 g/kWh (test list, line 2).
            var eui = 25m;
            var area = 15000m;
            var gridIntensity = 400m;

            // Act
            var result = CarbonCalculator.Calculate(eui, EuiUnit.KbtuPerSquareFoot, area, AreaUnit.SquareFeet, gridIntensity);

            // Assert: 25 × 3.1547 = 78.87 kWh/m²; × 0.4 = 31.55 kg/m²; × 1,393.55 m² = 43,962 kg = 43.96 tonnes.
            Assert.Equal(31.55m, result.KgPerSquareMetre, 2);
            Assert.Equal(43.96m, result.TotalTonnes, 2);
        }

        [Fact]
        public void ZeroEui_IsRejected()
        {
            // Act + Assert: EUI 0 must be refused (test list, line 3). The other inputs are normal values.
            var ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
                CarbonCalculator.Calculate(0m, EuiUnit.KwhPerSquareMetre, 1000m, AreaUnit.SquareMetres, 400m));

            Assert.Equal("eui", ex.ParamName);
        }

        [Fact]
        public void ZeroGridIntensity_GivesZeroCarbon()
        {
            // Act: EUI 100, area 1,000 m², grid 0 (test list, line 4). A fully renewable grid is realistic, so 0 is allowed.
            var result = CarbonCalculator.Calculate(100m, EuiUnit.KwhPerSquareMetre, 1000m, AreaUnit.SquareMetres, 0m);

            // Assert: anything × 0 = 0.
            Assert.Equal(0m, result.KgPerSquareMetre, 2);
            Assert.Equal(0m, result.TotalTonnes, 2);
        }

        [Fact]
        public void ZeroArea_IsRejected()
        {
            // Act + Assert: area 0 must be refused (test list, line 5). The other inputs are normal values.
            var ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
                CarbonCalculator.Calculate(100m, EuiUnit.KwhPerSquareMetre, 0m, AreaUnit.SquareMetres, 400m));

            Assert.Equal("area", ex.ParamName);
        }
    }
}
