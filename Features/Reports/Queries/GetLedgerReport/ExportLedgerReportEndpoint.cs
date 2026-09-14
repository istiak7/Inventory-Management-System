using Inventory_Management_System.Features.Reports.Shared;
using Inventory_Management_System.Features.Reports.Shared.Dtos;
using Inventory_Management_System.Features.Reports.Shared.Export;
using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Reports.Queries.GetLedgerReport
{
    public class ExportLedgerReportEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("/export-ledger-report", async (
                IMediator mediator,
                string? partyType = null,
                int? partyId = null,
                DateTime? startDate = null,
                DateTime? endDate = null,
                string? transactionType = null,
                string? search = null,
                int? branchId = null) =>
            {
                // Same query, same handler, same filters as the on-screen report — only the page
                // size differs. That is what keeps the sheet from ever drifting from the table.
                var result = await mediator.Send(new GetLedgerReportQuery(
                    1, ReportPaging.MaxExportPageSize, partyType, partyId, startDate, endDate,
                    transactionType, search, branchId)
                { IsExport = true });

                // A rejected filter still answers in the normal envelope, so the caller sees the
                // same message it would have got from the report itself.
                if (!result.IsSuccess || result.Data is not LedgerReportResponse report)
                    return Results.Ok(result);

                var rows = report.Rows.Items;
                var summary = report.Summary;

                var bytes = ReportWorkbook.Build(
                    "Ledger",
                    "Ledger Report",
                    new[]
                    {
                        ("Date range", ReportWorkbook.DateRangeLabel(startDate, endDate)),
                        ("Party type", ReportWorkbook.Or(partyType)),
                        ("Transaction", ReportWorkbook.Or(transactionType)),
                        ("Search", ReportWorkbook.Or(search, "-")),
                        ("Rows", rows.Count.ToString()),
                    },
                    new ReportColumn<LedgerReportRow>[]
                    {
                        new("Date", r => r.TransactionDate, ReportWorkbook.DateTimeFormat),
                        new("Reference", r => r.Reference),
                        new("Party", r => r.PartyName),
                        new("Party type", r => r.PartyType),
                        new("Transaction", r => r.TransactionType),
                        new("Description", r => r.Description ?? string.Empty),
                        new("Debit", r => r.Debit, ReportWorkbook.MoneyFormat),
                        new("Credit", r => r.Credit, ReportWorkbook.MoneyFormat),
                        new("Balance", r => r.RunningBalance, ReportWorkbook.MoneyFormat),
                    },
                    rows,
                    new ReportTotal[]
                    {
                        new("Opening balance", ReportWorkbook.AmountOrNotApplicable(summary.OpeningBalance), ReportWorkbook.MoneyFormat),
                        new("Total debit", summary.TotalDebit, ReportWorkbook.MoneyFormat),
                        new("Total credit", summary.TotalCredit, ReportWorkbook.MoneyFormat),
                        new("Closing balance", ReportWorkbook.AmountOrNotApplicable(summary.ClosingBalance), ReportWorkbook.MoneyFormat),
                        new("Transactions", summary.TransactionCount, ReportWorkbook.WholeNumberFormat),
                    });

                return Results.File(bytes, ReportWorkbook.ContentType,
                    ReportWorkbook.FileName("Ledger", startDate, endDate));
            }).WithTags("Reports").RequirePermission(Permissions.ReportsView);
        }
    }
}
