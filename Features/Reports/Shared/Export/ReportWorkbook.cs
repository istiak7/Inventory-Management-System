using ClosedXML.Excel;

namespace Inventory_Management_System.Features.Reports.Shared.Export
{
    /// <summary>
    /// One exported column: its heading, how to pull the value off a row, and the Excel number
    /// format to stamp on it. Values are handed over as <see cref="XLCellValue"/> rather than
    /// strings so numbers stay numbers and dates stay dates in the sheet — a reader can sum a
    /// column or filter a date range without cleaning the file up first.
    /// </summary>
    public sealed record ReportColumn<T>(string Header, Func<T, XLCellValue> Value, string? NumberFormat = null);

    /// <summary>One line of the totals block written under the table.</summary>
    public sealed record ReportTotal(string Label, XLCellValue Value, string? NumberFormat = null);

    /// <summary>
    /// Builds the .xlsx every report export returns. Every sheet has the same shape — title, the
    /// filters the export was run with, the table, then the report's own totals — so the five
    /// reports produce files that read alike.
    /// </summary>
    public static class ReportWorkbook
    {
        public const string ContentType =
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

        public const string DateFormat = "yyyy-mm-dd";
        public const string DateTimeFormat = "yyyy-mm-dd hh:mm";
        public const string MoneyFormat = "#,##0.00";
        public const string WholeNumberFormat = "#,##0";

        private static readonly XLColor HeaderFill = XLColor.FromHtml("#0D0D0F");
        private static readonly XLColor BandFill = XLColor.FromHtml("#F4F5F7");

        public static byte[] Build<T>(
            string sheetName,
            string title,
            IReadOnlyList<(string Label, string Value)> filters,
            IReadOnlyList<ReportColumn<T>> columns,
            IReadOnlyList<T> rows,
            IReadOnlyList<ReportTotal> totals)
        {
            using var workbook = new XLWorkbook();
            var sheet = workbook.Worksheets.Add(sheetName);

            var row = 1;

            sheet.Cell(row, 1).Value = title;
            sheet.Cell(row, 1).Style.Font.SetBold().Font.SetFontSize(14);
            sheet.Range(row, 1, row, Math.Max(columns.Count, 2)).Merge();
            row += 2;

            foreach (var (label, value) in filters)
            {
                sheet.Cell(row, 1).Value = label;
                sheet.Cell(row, 1).Style.Font.SetBold();
                sheet.Cell(row, 2).Value = value;
                row++;
            }

            if (filters.Count > 0) row++;

            var headerRow = row;
            for (var i = 0; i < columns.Count; i++)
            {
                var cell = sheet.Cell(headerRow, i + 1);
                cell.Value = columns[i].Header;
                cell.Style.Font.SetBold().Font.SetFontColor(XLColor.White);
                cell.Style.Fill.SetBackgroundColor(HeaderFill);
                cell.Style.Alignment.SetVertical(XLAlignmentVerticalValues.Center);
            }
            row++;

            var firstDataRow = row;
            foreach (var item in rows)
            {
                for (var i = 0; i < columns.Count; i++)
                {
                    var cell = sheet.Cell(row, i + 1);
                    cell.Value = columns[i].Value(item);
                    if (columns[i].NumberFormat is { } format)
                        cell.Style.NumberFormat.SetFormat(format);
                }

                // Banding survives the trip to Excel, unlike a table style the reader may not have.
                if ((row - firstDataRow) % 2 == 1)
                    sheet.Range(row, 1, row, columns.Count).Style.Fill.SetBackgroundColor(BandFill);

                row++;
            }

            var lastDataRow = row - 1;
            if (lastDataRow >= headerRow)
            {
                sheet.Range(headerRow, 1, Math.Max(lastDataRow, headerRow), columns.Count)
                     .SetAutoFilter();
            }

            // Keep the headings in view while scrolling a long export.
            sheet.SheetView.FreezeRows(headerRow);

            if (totals.Count > 0)
            {
                row++;
                sheet.Cell(row, 1).Value = "Totals";
                sheet.Cell(row, 1).Style.Font.SetBold().Font.SetFontSize(12);
                row++;

                foreach (var total in totals)
                {
                    sheet.Cell(row, 1).Value = total.Label;
                    sheet.Cell(row, 1).Style.Font.SetBold();

                    var cell = sheet.Cell(row, 2);
                    cell.Value = total.Value;
                    if (total.NumberFormat is { } format)
                        cell.Style.NumberFormat.SetFormat(format);

                    row++;
                }
            }

            sheet.Columns().AdjustToContents(1, 200, 8d, 60d);

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        /// <summary>
        /// Names the file after the report and the window it covers, e.g.
        /// <c>Ledger_2026-09-01_to_2026-09-14.xlsx</c>. An open-ended range says so rather than
        /// pretending to a boundary the user never set.
        /// </summary>
        public static string FileName(string prefix, DateTime? startDate, DateTime? endDate)
        {
            var start = startDate?.ToString("yyyy-MM-dd");
            var end = endDate?.ToString("yyyy-MM-dd");

            return (start, end) switch
            {
                (not null, not null) => $"{prefix}_{start}_to_{end}.xlsx",
                (not null, null) => $"{prefix}_from_{start}.xlsx",
                (null, not null) => $"{prefix}_upto_{end}.xlsx",
                _ => $"{prefix}_{DateTime.Today:yyyy-MM-dd}.xlsx",
            };
        }

        /// <summary>Renders the date window for the filter block at the top of the sheet.</summary>
        public static string DateRangeLabel(DateTime? startDate, DateTime? endDate) =>
            (startDate, endDate) switch
            {
                (not null, not null) => $"{startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}",
                (not null, null) => $"From {startDate:yyyy-MM-dd}",
                (null, not null) => $"Up to {endDate:yyyy-MM-dd}",
                _ => "All dates",
            };

        /// <summary>A filter the user left blank reads as "All" rather than as an empty cell.</summary>
        public static string Or(string? value, string fallback = "All") =>
            string.IsNullOrWhiteSpace(value) ? fallback : value;

        /// <summary>
        /// Some summary figures only exist for a single party — the ledger's opening and closing
        /// balances are null for any wider selection, exactly as the on-screen report shows them
        /// as a dash. Writing 0m instead would read as a real balance of zero.
        /// </summary>
        public static XLCellValue AmountOrNotApplicable(decimal? value) =>
            value.HasValue ? value.Value : "Not applicable";
    }
}
