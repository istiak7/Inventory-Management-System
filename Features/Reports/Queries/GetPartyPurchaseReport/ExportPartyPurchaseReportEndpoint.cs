using Inventory_Management_System.Features.Reports.Shared;
using Inventory_Management_System.Features.Reports.Shared.Dtos;
using Inventory_Management_System.Features.Reports.Shared.Export;
using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Reports.Queries.GetPartyPurchaseReport
{
    public class ExportPartyPurchaseReportEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("/export-party-purchase-report", async (
                IMediator mediator,
                DateTime? startDate = null,
                DateTime? endDate = null,
                int? supplierId = null,
                int? branchId = null,
                string? purchaseType = null,
                string? status = null,
                string? invoiceNumber = null,
                string? search = null,
                string? sortBy = null,
                bool sortDescending = true) =>
            {
                var result = await mediator.Send(new GetPartyPurchaseReportQuery(
                    1, ReportPaging.MaxExportPageSize, startDate, endDate, supplierId, branchId,
                    purchaseType, status, invoiceNumber, search, sortBy, sortDescending)
                { IsExport = true });

                if (!result.IsSuccess || result.Data is not PartyPurchaseReportResponse report)
                    return Results.Ok(result);

                var rows = report.Rows.Items;
                var summary = report.Summary;

                var bytes = ReportWorkbook.Build(
                    "Party Purchase",
                    "Party Purchase Report",
                    new[]
                    {
                        ("Date range", ReportWorkbook.DateRangeLabel(startDate, endDate)),
                        ("Purchase type", ReportWorkbook.Or(purchaseType)),
                        ("Status", ReportWorkbook.Or(status)),
                        ("Invoice", ReportWorkbook.Or(invoiceNumber, "-")),
                        ("Search", ReportWorkbook.Or(search, "-")),
                        ("Rows", rows.Count.ToString()),
                    },
                    new ReportColumn<PartyPurchaseRow>[]
                    {
                        new("Date", r => r.PurchaseDate, ReportWorkbook.DateTimeFormat),
                        new("Invoice", r => r.InvoiceNumber),
                        new("Supplier", r => r.SupplierName),
                        new("Phone", r => r.PhoneNumber),
                        new("Warehouse", r => r.BranchName),
                        new("Purchase type", r => r.PurchaseType ?? string.Empty),
                        new("Status", r => r.Status),
                        new("Total", r => r.TotalAmount, ReportWorkbook.MoneyFormat),
                        new("Paid", r => r.PaidAmount, ReportWorkbook.MoneyFormat),
                        new("Due", r => r.DueAmount, ReportWorkbook.MoneyFormat),
                        new("Return qty", r => r.ReturnAdjustmentQuantity, ReportWorkbook.WholeNumberFormat),
                        new("Return amount", r => r.ReturnAdjustmentAmount, ReportWorkbook.MoneyFormat),
                        new("Payments", r => FormatPayments(r.Payments)),
                        new("Remarks", r => r.Remarks ?? string.Empty),
                    },
                    rows,
                    new ReportTotal[]
                    {
                        new("Orders", summary.OrderCount, ReportWorkbook.WholeNumberFormat),
                        new("Total purchase", summary.TotalPurchase, ReportWorkbook.MoneyFormat),
                        new("Total paid", summary.TotalPaid, ReportWorkbook.MoneyFormat),
                        new("Total due", summary.TotalDue, ReportWorkbook.MoneyFormat),
                        new("Return adjustments", summary.ReturnAdjustmentAmount, ReportWorkbook.MoneyFormat),
                    });

                return Results.File(bytes, ReportWorkbook.ContentType,
                    ReportWorkbook.FileName("Party_Purchase", startDate, endDate));
            }).WithTags("Reports").RequirePermission(Permissions.ReportsView);
        }

        private static string FormatPayments(IReadOnlyList<ReportPaymentAllocation> payments) =>
            payments.Count == 0
                ? string.Empty
                : string.Join("; ", payments.Select(p =>
                    $"{p.PaymentDate:yyyy-MM-dd} {p.PaymentMethod} {p.Amount:0.00}"));
    }
}
