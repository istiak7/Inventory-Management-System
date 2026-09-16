using Inventory_Management_System.Features.Reports.Shared;
using Inventory_Management_System.Features.Reports.Shared.Dtos;
using Inventory_Management_System.Features.Reports.Shared.Export;
using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Reports.Queries.GetPartySalesReport
{
    public class ExportPartySalesReportEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("/export-party-sales-report", async (
                IMediator mediator,
                DateTime? startDate = null,
                DateTime? endDate = null,
                int? customerId = null,
                int? branchId = null,
                string? saleType = null,
                string? invoiceNumber = null,
                string? search = null,
                string? sortBy = null,
                bool sortDescending = true) =>
            {
                var result = await mediator.Send(new GetPartySalesReportQuery(
                    1, ReportPaging.MaxExportPageSize, startDate, endDate, customerId, branchId,
                    saleType, invoiceNumber, search, sortBy, sortDescending)
                { IsExport = true });

                if (!result.IsSuccess || result.Data is not PartySalesReportResponse report)
                    return Results.Ok(result);

                var rows = report.Rows.Items;
                var summary = report.Summary;

                var bytes = ReportWorkbook.Build(
                    "Party Sales",
                    "Party Sales Report",
                    new[]
                    {
                        ("Date range", ReportWorkbook.DateRangeLabel(startDate, endDate)),
                        ("Sale type", ReportWorkbook.Or(saleType)),
                        ("Invoice", ReportWorkbook.Or(invoiceNumber, "-")),
                        ("Search", ReportWorkbook.Or(search, "-")),
                        ("Rows", rows.Count.ToString()),
                    },
                    new ReportColumn<PartySalesRow>[]
                    {
                        new("Date", r => r.SaleDate, ReportWorkbook.DateTimeFormat),
                        new("Invoice", r => r.InvoiceNumber),
                        new("Customer", r => r.CustomerName),
                        new("Phone", r => r.PhoneNumber),
                        new("Warehouse", r => r.BranchName),
                        new("Sale type", r => r.SaleType),
                        new("Status", r => r.Status),
                        new("Sub total", r => r.SubTotal, ReportWorkbook.MoneyFormat),
                        new("Discount", r => r.DiscountAmount, ReportWorkbook.MoneyFormat),
                        new("Tax", r => r.TaxAmount, ReportWorkbook.MoneyFormat),
                        new("Total", r => r.TotalAmount, ReportWorkbook.MoneyFormat),
                        new("Paid", r => r.PaidAmount, ReportWorkbook.MoneyFormat),
                        new("Due", r => r.DueAmount, ReportWorkbook.MoneyFormat),
                        new("Returns", r => r.ReturnAdjustmentCount, ReportWorkbook.WholeNumberFormat),
                        new("Return amount", r => r.ReturnAdjustmentAmount, ReportWorkbook.MoneyFormat),
                        new("Payments", r => FormatPayments(r.Payments)),
                        new("Remarks", r => r.Remarks ?? string.Empty),
                    },
                    rows,
                    new ReportTotal[]
                    {
                        new("Invoices", summary.InvoiceCount, ReportWorkbook.WholeNumberFormat),
                        new("Sub total", summary.TotalSubTotal, ReportWorkbook.MoneyFormat),
                        new("Discount", summary.TotalDiscount, ReportWorkbook.MoneyFormat),
                        new("Tax", summary.TotalTax, ReportWorkbook.MoneyFormat),
                        new("Total sales", summary.TotalSales, ReportWorkbook.MoneyFormat),
                        new("Total paid", summary.TotalPaid, ReportWorkbook.MoneyFormat),
                        new("Total due", summary.TotalDue, ReportWorkbook.MoneyFormat),
                        new("Return adjustments", summary.ReturnAdjustmentAmount, ReportWorkbook.MoneyFormat),
                    });

                return Results.File(bytes, ReportWorkbook.ContentType,
                    ReportWorkbook.FileName("Party_Sales", startDate, endDate));
            }).WithTags("Reports").RequirePermission(Permissions.ReportsView);
        }

        /// <summary>
        /// The invoice's payments collapse into one cell so a row stays a row; the table on screen
        /// shows the same allocations in its expanded detail.
        /// </summary>
        private static string FormatPayments(IReadOnlyList<ReportPaymentAllocation> payments) =>
            payments.Count == 0
                ? string.Empty
                : string.Join("; ", payments.Select(p =>
                    $"{p.PaymentDate:yyyy-MM-dd} {p.PaymentMethod} {p.Amount:0.00}"));
    }
}
