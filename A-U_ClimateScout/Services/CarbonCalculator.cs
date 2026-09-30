namespace A_U_ClimateScout.Services
{
    public enum EuiUnit { KbtuPerSquareFoot, KwhPerSquareMetre, GigajoulesPerSquareMetre }

    public enum AreaUnit { SquareFeet, SquareMetres }

    // KgPerSquareMetre is kg CO2e per m² per year; totals are per year.
    public record CarbonResult(decimal KgPerSquareMetre, decimal TotalKg, decimal TotalTonnes);

    // The carbon calculator from the old carbon comparison page (plan §2):
    // EUI × grid carbon intensity × area, with everything converted to metric first.
    // Grid intensity is always g CO2e/kWh; the old page's other carbon units were never offered
    // and their code set the wrong factor (the "dead unit-branch bug"), so they are not ported.
    public static class CarbonCalculator
    {
        private const decimal KwhPerM2PerKbtuPerFt2 = 3.1547m;
        private const decimal KwhPerGigajoule = 1m / 0.0036m;   // 277.78
        private const decimal SquareFeetPerSquareMetre = 10.7639m;

        public static CarbonResult Calculate(decimal eui, EuiUnit euiUnit, decimal area, AreaUnit areaUnit, decimal gridIntensity)
        {
            // EUI and area must be above 0 (a 0 is almost always a typo). Grid intensity may be 0: a fully renewable grid.
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(eui);
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(area);
            ArgumentOutOfRangeException.ThrowIfNegative(gridIntensity);

            var kwhPerSquareMetre = euiUnit switch
            {
                EuiUnit.KbtuPerSquareFoot => eui * KwhPerM2PerKbtuPerFt2,
                EuiUnit.KwhPerSquareMetre => eui,
                EuiUnit.GigajoulesPerSquareMetre => eui * KwhPerGigajoule,
                _ => throw new ArgumentOutOfRangeException(nameof(euiUnit)),
            };

            var squareMetres = areaUnit switch
            {
                AreaUnit.SquareFeet => area / SquareFeetPerSquareMetre,
                AreaUnit.SquareMetres => area,
                _ => throw new ArgumentOutOfRangeException(nameof(areaUnit)),
            };

            var kgPerSquareMetre = kwhPerSquareMetre * gridIntensity / 1000m;   // g → kg
            var totalKg = kgPerSquareMetre * squareMetres;
            return new CarbonResult(kgPerSquareMetre, totalKg, totalKg / 1000m);
        }
    }
}
