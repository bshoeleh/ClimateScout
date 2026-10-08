using System.Globalization;
using System.Text;
using ClosedXML.Excel;

namespace A_U_ClimateScout.Services.CarbonImport
{
    // Turns an uploaded carbon file into CSV text for CarbonCsvParser: a .csv as it is, an .xlsx from its first sheet
    // (so a file opened and saved in Excel imports the same way). Returns null for any other file.
    public static class CarbonFileReader
    {
        public static async Task<string?> ReadAsCsvAsync(IFormFile file, CancellationToken cancellationToken)
        {
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (extension == ".csv")
            {
                using var reader = new StreamReader(file.OpenReadStream(), Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
                return await reader.ReadToEndAsync(cancellationToken);
            }
            if (extension != ".xlsx")
            {
                return null;
            }

            try
            {
                using var stream = new MemoryStream();
                await file.CopyToAsync(stream, cancellationToken);
                using var workbook = new XLWorkbook(stream);
                var sheet = workbook.Worksheets.First();
                var csv = new StringBuilder();
                foreach (var row in sheet.RowsUsed())
                {
                    var last = row.LastCellUsed()?.Address.ColumnNumber ?? 0;
                    var cells = Enumerable.Range(1, last).Select(c => Quote(CellText(row.Cell(c))));
                    csv.AppendLine(string.Join(",", cells));
                }
                return csv.ToString();
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                return null;   // not a readable workbook
            }
        }

        // Numbers in invariant culture (1234.5, never "1 234,5"), so the CSV parser reads them the same everywhere.
        private static string CellText(IXLCell cell) =>
            cell.DataType == XLDataType.Number ? cell.GetDouble().ToString(CultureInfo.InvariantCulture) : cell.GetFormattedString();

        private static string Quote(string value) =>
            value.IndexOfAny([',', '"', '\n', '\r']) >= 0 ? $"\"{value.Replace("\"", "\"\"")}\"" : value;
    }
}
