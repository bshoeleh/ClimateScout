using System.Globalization;
using Microsoft.VisualBasic.FileIO;

namespace A_U_ClimateScout.Services.CarbonImport
{
    // Reads a carbon CSV (plan §6): detects its profile from the header row, then turns each data line into a row
    // or a problem. A file that can't be imported at all (empty, unknown layout) throws CarbonImportException;
    // single bad lines are reported and skipped, so one typo doesn't block the other 200 values.
    public static class CarbonCsvParser
    {
        public const string ExpectedVariable = "CO2 intensity";
        public const string ExpectedUnit = "gCO2/kWh";
        public const int EarliestYear = 1990;
        public const decimal MaximumValue = 2000m;     // the dirtiest grids are around 1,000–1,300 g/kWh

        // currentYear is passed in (rather than read from the clock) so tests can fix it.
        public static CarbonCsvResult Parse(TextReader reader, int currentYear)
        {
            using var parser = new TextFieldParser(reader)
            {
                TextFieldType = FieldType.Delimited,
                HasFieldsEnclosedInQuotes = true,
                TrimWhiteSpace = true,
            };
            parser.SetDelimiters(",");

            var headers = parser.ReadFields() ?? throw new CarbonImportException("The file is empty.");
            var profile = CarbonImportProfile.Detect(headers) ?? throw new CarbonImportException(
                $"The columns ({string.Join(", ", headers)}) don't match any known carbon file layout.");
            int Column(string name) => Array.FindIndex(headers, h => string.Equals(h.Trim(), name, StringComparison.OrdinalIgnoreCase));
            var columns = new Columns(Column(profile.RegionColumn), Column("Year"), Column("Variable"), Column("Unit"), Column("Value"));

            var rows = new List<CarbonCsvRow>();
            var problems = new List<CarbonCsvProblem>();
            var firstLineFor = new Dictionary<(string Name, int Year), int>();
            while (!parser.EndOfData)
            {
                var line = (int)parser.LineNumber;
                var fields = parser.ReadFields();
                if (fields is null || fields.All(string.IsNullOrWhiteSpace))
                {
                    continue;
                }

                if (fields.Length != headers.Length)
                {
                    problems.Add(new(line, null, $"Expected {headers.Length} columns, found {fields.Length}."));
                    continue;
                }

                var (row, problem) = ReadRow(line, fields, columns, currentYear);
                if (problem is not null)
                {
                    problems.Add(problem);
                }
                else if (firstLineFor.TryGetValue((row!.RegionName.ToLowerInvariant(), row.Year), out var firstLine))
                {
                    problems.Add(new(line, row.RegionName, $"Same region and year as line {firstLine}."));
                }
                else
                {
                    firstLineFor[(row.RegionName.ToLowerInvariant(), row.Year)] = line;
                    rows.Add(row);
                }
            }

            return new CarbonCsvResult(profile, rows, problems);
        }

        private static (CarbonCsvRow? Row, CarbonCsvProblem? Problem) ReadRow(int line, string[] fields, Columns columns, int currentYear)
        {
            var name = fields[columns.Region];
            CarbonCsvProblem Problem(string message) => new(line, name.Length > 0 ? name : null, message);

            if (name.Length == 0)
            {
                return (null, Problem("No region name."));
            }
            if (!string.Equals(fields[columns.Variable], ExpectedVariable, StringComparison.OrdinalIgnoreCase))
            {
                return (null, Problem($"Variable is \"{fields[columns.Variable]}\", expected \"{ExpectedVariable}\"."));
            }
            if (!string.Equals(fields[columns.Unit], ExpectedUnit, StringComparison.OrdinalIgnoreCase))
            {
                return (null, Problem($"Unit is \"{fields[columns.Unit]}\", expected \"{ExpectedUnit}\"."));
            }
            if (!int.TryParse(fields[columns.Year], NumberStyles.None, CultureInfo.InvariantCulture, out var year)
                || year < EarliestYear || year > currentYear)
            {
                return (null, Problem($"Year \"{fields[columns.Year]}\" is not between {EarliestYear} and {currentYear}."));
            }
            if (fields[columns.Value].Length == 0)
            {
                return (null, Problem("No value."));
            }
            if (!decimal.TryParse(fields[columns.Value], NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var value)
                || value > MaximumValue)
            {
                return (null, Problem($"Value \"{fields[columns.Value]}\" is not a number from 0 to {MaximumValue:0}."));
            }

            return (new CarbonCsvRow(line, name, year, value), null);
        }

        private record Columns(int Region, int Year, int Variable, int Unit, int Value);
    }

    // A usable line: the region name exactly as written in the file, the year and the value in g/kWh.
    public record CarbonCsvRow(int Line, string RegionName, int Year, decimal Value);

    // A line that was skipped, and why. RegionName is null when the line has none.
    public record CarbonCsvProblem(int Line, string? RegionName, string Message);

    public record CarbonCsvResult(CarbonImportProfile Profile, IReadOnlyList<CarbonCsvRow> Rows, IReadOnlyList<CarbonCsvProblem> Problems);

    // The file as a whole can't be imported (empty, unknown layout …).
    public class CarbonImportException(string message) : Exception(message);
}
