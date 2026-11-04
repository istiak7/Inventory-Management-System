using ClosedXML.Excel;

namespace Inventory_Management_System.Features.DataUpload.Shared
{
    /// <summary>Builds the two .xlsx files of the upload screen: the empty template and the error report.</summary>
    public static class DataUploadWorkbook
    {
        public const string ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

        // Same heading colours as the report exports, so every file from the app looks alike.
        private static readonly XLColor HeaderFill = XLColor.FromHtml("#0D0D0F");
        private static readonly XLColor RequiredFill = XLColor.FromHtml("#053148");
        private static readonly XLColor ErrorInk = XLColor.FromHtml("#B2192C");

        public static string FileName(UploadTypeDefinition definition, string suffix) =>
            $"{definition.Name.Replace(" & ", "_and_").Replace(' ', '_')}_{suffix}.xlsx";

        /// <summary>
        /// Sheet 1: just the heading row (required headings end with " *" — the matcher ignores
        /// the star). Sheet 2: what every column means.
        /// </summary>
        public static byte[] Template(UploadTypeDefinition definition)
        {
            using var workbook = new XLWorkbook();
            var sheet = workbook.Worksheets.Add(SheetName(definition.Name));

            foreach (var column in definition.Columns)
            {
                var cell = sheet.Cell(1, column.Order);
                cell.Value = column.Required ? $"{column.Name} *" : column.Name;
                cell.Style.Font.SetBold().Font.SetFontColor(XLColor.White);
                cell.Style.Fill.SetBackgroundColor(column.Required ? RequiredFill : HeaderFill);
                cell.CreateComment().AddText(column.Hint);

                // Text columns stay text, so Excel keeps the leading 0 of phone numbers and codes.
                if (column.MaxLength is not null)
                    sheet.Column(column.Order).Style.NumberFormat.SetFormat("@");

                sheet.Column(column.Order).Width = Math.Clamp(column.Name.Length + 6, 14, 40);
            }

            sheet.SheetView.FreezeRows(1);

            var help = workbook.Worksheets.Add("Instructions");
            help.Cell(1, 1).Value = $"{definition.Name} upload";
            help.Cell(1, 1).Style.Font.SetBold().Font.SetFontSize(14);
            help.Cell(2, 1).Value = definition.Description;

            var notes = new[]
            {
                "Fill the first sheet from row 2. Keep the heading row; columns marked * are required.",
                $"Only .xlsx files up to {UploadFileReader.MaxFileBytes / 1024 / 1024} MB and {UploadFileReader.MaxDataRows:N0} rows per upload.",
                "All rows are saved together: if any row has an error, nothing is saved. Fix the rows in the error report and upload again.",
            };
            for (var i = 0; i < notes.Length; i++)
                help.Cell(4 + i, 1).Value = $"• {notes[i]}";

            var tableRow = 4 + notes.Length + 1;
            string[] headings = ["Column", "Heading", "Required", "Notes"];
            for (var i = 0; i < headings.Length; i++)
            {
                var cell = help.Cell(tableRow, i + 1);
                cell.Value = headings[i];
                cell.Style.Font.SetBold().Font.SetFontColor(XLColor.White);
                cell.Style.Fill.SetBackgroundColor(HeaderFill);
            }

            foreach (var column in definition.Columns)
            {
                var r = tableRow + column.Order;
                help.Cell(r, 1).Value = XLHelper.GetColumnLetterFromNumber(column.Order);
                help.Cell(r, 2).Value = column.Name;
                help.Cell(r, 3).Value = column.Required ? "Yes" : "No";
                help.Cell(r, 4).Value = column.MaxLength is int max ? $"{column.Hint} (max {max} characters)" : column.Hint;
            }

            help.Column(1).Width = 10;
            help.Column(2).Width = 24;
            help.Column(3).Width = 10;
            help.Column(4).Width = 90;

            return Save(workbook);
        }

        /// <summary>
        /// The failed rows exactly as they were uploaded (same headings, same columns), with the
        /// row number in front and the reasons at the end. Fix it and upload it again as it is.
        /// </summary>
        public static byte[] ErrorReport(
            UploadTypeDefinition definition,
            IReadOnlyList<UploadedHeader> headers,
            IReadOnlyList<UploadRow> rows,
            IReadOnlyList<RowError> errors)
        {
            using var workbook = new XLWorkbook();
            var sheet = workbook.Worksheets.Add(SheetName($"{definition.Name} errors"));

            var errorColumn = headers.Count + 2;

            WriteHeading(sheet.Cell(1, 1), "Row No");
            for (var c = 0; c < headers.Count; c++)
                WriteHeading(sheet.Cell(1, c + 2), headers[c].Name.Length > 0 ? headers[c].Name : $"Column {headers[c].Column}");
            WriteHeading(sheet.Cell(1, errorColumn), "Errors");

            var rowsByNumber = rows.ToDictionary(r => r.RowNumber);
            var line = 2;

            foreach (var error in errors.OrderBy(e => e.RowNumber))
            {
                sheet.Cell(line, 1).Value = error.RowNumber;

                if (rowsByNumber.TryGetValue(error.RowNumber, out var row))
                {
                    for (var c = 0; c < row.Cells.Count; c++)
                    {
                        // Written as text so "0171…" and "00123" come back exactly as typed.
                        var cell = sheet.Cell(line, c + 2);
                        cell.Style.NumberFormat.SetFormat("@");
                        cell.Value = row.Cells[c];
                    }
                }

                var errorCell = sheet.Cell(line, errorColumn);
                errorCell.Value = string.Join(Environment.NewLine, error.Messages);
                errorCell.Style.Font.SetFontColor(ErrorInk);
                errorCell.Style.Alignment.SetWrapText();

                line++;
            }

            sheet.Columns(1, errorColumn - 1).AdjustToContents(1, Math.Min(line, 200));
            sheet.Column(errorColumn).Width = 80;
            sheet.SheetView.FreezeRows(1);

            return Save(workbook);
        }

        private static void WriteHeading(IXLCell cell, string text)
        {
            cell.Value = text;
            cell.Style.Font.SetBold().Font.SetFontColor(XLColor.White);
            cell.Style.Fill.SetBackgroundColor(HeaderFill);
        }

        // Excel sheet names: at most 31 characters and none of : \ / ? * [ ].
        private static string SheetName(string name)
        {
            var clean = new string(name.Where(ch => !":\\/?*[]".Contains(ch)).ToArray());
            return clean.Length > 31 ? clean[..31] : clean;
        }

        private static byte[] Save(XLWorkbook workbook)
        {
            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }
    }
}
