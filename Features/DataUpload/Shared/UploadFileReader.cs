using ClosedXML.Excel;
using System.Globalization;
using System.Text.Json;

namespace Inventory_Management_System.Features.DataUpload.Shared
{
    /// <summary>One data row of the uploaded file.</summary>
    public sealed class UploadRow
    {
        /// <summary>Row number as Excel shows it (the header is row 1, so data starts at 2).</summary>
        public required int RowNumber { get; init; }

        /// <summary>Cell text by system column order, already trimmed. Unmapped columns are missing.</summary>
        public required IReadOnlyDictionary<int, string> Values { get; init; }

        /// <summary>Every cell of the row in file order — written back unchanged in the error report.</summary>
        public required IReadOnlyList<string> Cells { get; init; }

        public string this[UploadColumn column] =>
            Values.TryGetValue(column.Order, out var value) ? value : string.Empty;
    }

    /// <summary>The opened workbook and the headings of its first sheet.</summary>
    public sealed class UploadSheet(XLWorkbook workbook, IXLWorksheet sheet, List<UploadedHeader> headers, int lastRow) : IDisposable
    {
        public IXLWorksheet Sheet { get; } = sheet;
        public List<UploadedHeader> Headers { get; } = headers;
        public int LastRow { get; } = lastRow;
        public void Dispose() => workbook.Dispose();
    }

    /// <summary>A pair sent by the page: "put the file's column C into system column 2".</summary>
    public sealed class ColumnMappingItem
    {
        public int SystemColumn { get; set; }
        public string UploadedColumn { get; set; } = string.Empty;
    }

    /// <summary>Reads the uploaded .xlsx: checks the file, reads the headings, applies the column mapping and reads the rows.</summary>
    public static class UploadFileReader
    {
        public const long MaxFileBytes = 5 * 1024 * 1024;
        public const int MaxDataRows = 5000;

        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

        // Returns an error message, or null when the file can be read.
        public static string? CheckFile(IFormFile? file)
        {
            if (file is null || file.Length == 0)
                return "Please choose an Excel file to upload.";

            if (!string.Equals(Path.GetExtension(file.FileName), ".xlsx", StringComparison.OrdinalIgnoreCase))
                return $"'{Path.GetExtension(file.FileName)}' files are not supported. Please upload an .xlsx file (Excel Workbook).";

            if (file.Length > MaxFileBytes)
                return $"The file is larger than {MaxFileBytes / 1024 / 1024} MB. Please split it into smaller files.";

            return null;
        }

        public static async Task<(UploadSheet? Sheet, string? Error)> OpenAsync(IFormFile file, CancellationToken cancellationToken)
        {
            var fileError = CheckFile(file);
            if (fileError is not null) return (null, fileError);

            XLWorkbook workbook;
            try
            {
                var buffer = new MemoryStream();
                await file.CopyToAsync(buffer, cancellationToken);
                buffer.Position = 0;
                workbook = new XLWorkbook(buffer);
            }
            catch (Exception) when (!cancellationToken.IsCancellationRequested)
            {
                return (null, "The file could not be opened. Please upload a valid, unprotected Excel workbook (.xlsx).");
            }

            var sheet = workbook.Worksheets.FirstOrDefault();
            var lastColumn = sheet?.LastColumnUsed()?.ColumnNumber() ?? 0;
            var lastRow = sheet?.LastRowUsed()?.RowNumber() ?? 0;

            if (sheet is null || lastColumn == 0)
            {
                workbook.Dispose();
                return (null, "The first sheet of the file is empty.");
            }

            var headers = Enumerable.Range(1, lastColumn)
                .Select(c => new UploadedHeader(XLHelper.GetColumnLetterFromNumber(c), CellText(sheet.Cell(1, c)), c))
                .ToList();

            if (headers.All(h => h.Name.Length == 0))
            {
                workbook.Dispose();
                return (null, "The first row of the sheet must contain the column headings.");
            }

            return (new UploadSheet(workbook, sheet, headers, lastRow), null);
        }

        /// <summary>
        /// Turns the page's mapping into "system column -> file column number".
        /// With no mapping sent, the headings are matched automatically (exact and partial matches).
        /// </summary>
        public static (Dictionary<int, int>? Map, string? Error) ResolveMapping(
            UploadTypeDefinition definition,
            IReadOnlyList<UploadedHeader> headers,
            string? mappingsJson)
        {
            List<ColumnMappingItem> items;

            if (string.IsNullOrWhiteSpace(mappingsJson))
            {
                items = HeaderMatcher.Match(definition.Columns, headers)
                    .Where(m => m.UploadedColumn is not null)
                    .Select(m => new ColumnMappingItem { SystemColumn = m.SystemColumn, UploadedColumn = m.UploadedColumn! })
                    .ToList();
            }
            else
            {
                try
                {
                    items = JsonSerializer.Deserialize<List<ColumnMappingItem>>(mappingsJson, JsonOptions) ?? [];
                }
                catch (JsonException)
                {
                    return (null, "The column mapping could not be read. Please match the columns again.");
                }
            }

            var map = new Dictionary<int, int>();
            var usedFileColumns = new HashSet<int>();

            foreach (var item in items)
            {
                var column = definition.Column(item.SystemColumn);
                if (column is null)
                    return (null, $"System column {item.SystemColumn} does not exist for {definition.Name}.");

                var letter = item.UploadedColumn.Trim().ToUpperInvariant();
                var header = headers.FirstOrDefault(h => h.Column == letter);
                if (header is null)
                    return (null, $"Column '{letter}' does not exist in the uploaded file. Please match the columns again.");

                if (map.ContainsKey(column.Order))
                    return (null, $"'{column.Name}' is mapped more than once.");

                if (!usedFileColumns.Add(header.Order))
                    return (null, $"File column {letter} is mapped to more than one system column.");

                map[column.Order] = header.Order;
            }

            var missing = definition.Columns.Where(c => c.Required && !map.ContainsKey(c.Order)).Select(c => c.Name).ToList();
            if (missing.Count > 0)
                return (null, $"Required column(s) not mapped: {string.Join(", ", missing)}.");

            return (map, null);
        }

        /// <summary>Data rows (row 2 onward), skipping rows that are completely empty.</summary>
        public static List<UploadRow> ReadRows(UploadSheet upload, IReadOnlyDictionary<int, int> fileColumnBySystemColumn)
        {
            var rows = new List<UploadRow>();
            var columnCount = upload.Headers.Count;

            for (var r = 2; r <= upload.LastRow; r++)
            {
                var cells = new string[columnCount];
                for (var c = 1; c <= columnCount; c++)
                    cells[c - 1] = CellText(upload.Sheet.Cell(r, c));

                if (cells.All(string.IsNullOrEmpty)) continue;

                rows.Add(new UploadRow
                {
                    RowNumber = r,
                    Cells = cells,
                    Values = fileColumnBySystemColumn.ToDictionary(kv => kv.Key, kv => cells[kv.Value - 1]),
                });
            }

            return rows;
        }

        /// <summary>Number of non-empty data rows, without building them.</summary>
        public static int CountDataRows(UploadSheet upload)
        {
            var count = 0;
            for (var r = 2; r <= upload.LastRow; r++)
            {
                if (!upload.Sheet.Row(r).Cells(1, upload.Headers.Count).All(c => CellText(c).Length == 0))
                    count++;
            }
            return count;
        }

        /// <summary>
        /// Cell value as plain text. Numbers are written without thousand separators in invariant
        /// culture, so "1200.5" reads the same on every server.
        /// </summary>
        public static string CellText(IXLCell cell)
        {
            var value = cell.Value;

            if (value.IsBlank || value.IsError) return string.Empty;
            if (value.IsText) return value.GetText().Trim();
            if (value.IsNumber) return value.GetNumber().ToString(CultureInfo.InvariantCulture);
            if (value.IsBoolean) return value.GetBoolean() ? "Yes" : "No";
            if (value.IsDateTime) return value.GetDateTime().ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

            return value.ToString(CultureInfo.InvariantCulture).Trim();
        }
    }
}
