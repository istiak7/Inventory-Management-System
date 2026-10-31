using Inventory_Management_System.Features.Reports.Shared;
using Inventory_Management_System.Features.Reports.Shared.Dtos;
using Inventory_Management_System.Features.Reports.Shared.Export;
using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Reports.Queries.GetExpenseReport
{
    public class ExportExpenseReportEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("/export-expense-report", async (
                IMediator mediator,
                DateTime? startDate = null,
                DateTime? endDate = null,
                int? categoryId = null,
                string? paymentMethod = null,
                int? branchId = null,
                string? search = null,
                string? sortBy = null,
                bool sortDescending = true) =>
            {
                var result = await mediator.Send(new GetExpenseReportQuery(
                    1, ReportPaging.MaxExportPageSize, startDate, endDate, categoryId, paymentMethod,
                    branchId, search, sortBy, sortDescending)
                { IsExport = true });

                if (!result.IsSuccess || result.Data is not ExpenseReportResponse report)
                    return Results.Ok(result);

                var rows = report.Rows.Items;
                var summary = report.Summary;

                var bytes = ReportWorkbook.Build(
                    "Expenses",
                    "Office Expenses",
                    new[]
                    {
                        ("Date range", ReportWorkbook.DateRangeLabel(startDate, endDate)),
                        ("Category", categoryId is int id ? rows.FirstOrDefault()?.CategoryName ?? $"#{id}" : "All"),
                        ("Method", ReportWorkbook.Or(paymentMethod)),
                        ("Search", ReportWorkbook.Or(search, "-")),
                        ("Rows", rows.Count.ToString()),
                    },
                    new ReportColumn<ExpenseReportRow>[]
                    {
                        new("Date", r => r.ExpenseDate, ReportWorkbook.DateTimeFormat),
                        new("Reference", r => r.Reference),
                        new("Expense", r => r.Name ?? string.Empty),
                        new("Category", r => r.CategoryName),
                        new("Description", r => r.Description ?? string.Empty),
                        new("Method", r => r.PaymentMethod),
                        new("Warehouse", r => r.BranchName),
                        new("Recorded by", r => r.RecordedBy ?? string.Empty),
                        new("Amount", r => r.Amount, ReportWorkbook.MoneyFormat),
                    },
                    rows,
                    new ReportTotal[]
                    {
                        new("Expenses", summary.ExpenseCount, ReportWorkbook.WholeNumberFormat),
                        new("Total spent", summary.TotalAmount, ReportWorkbook.MoneyFormat),
                        new("Average expense", summary.AverageAmount, ReportWorkbook.MoneyFormat),
                    });

                return Results.File(bytes, ReportWorkbook.ContentType,
                    ReportWorkbook.FileName("Office_Expenses", startDate, endDate));
            }).WithTags("Reports").RequirePermission(Permissions.ReportsView);
        }
    }
}
