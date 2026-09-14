using Inventory_Management_System.Features.Reports.Shared;
using Inventory_Management_System.Features.Reports.Shared.Dtos;
using Inventory_Management_System.Features.Reports.Shared.Export;
using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Reports.Queries.GetPaymentReport
{
    public class ExportPaymentReportEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("/export-payment-report", async (
                IMediator mediator,
                DateTime? startDate = null,
                DateTime? endDate = null,
                string? partyType = null,
                int? partyId = null,
                string? direction = null,
                string? paymentMethod = null,
                string? status = null,
                int? branchId = null,
                string? search = null,
                string? sortBy = null,
                bool sortDescending = true) =>
            {
                var result = await mediator.Send(new GetPaymentReportQuery(
                    1, ReportPaging.MaxExportPageSize, startDate, endDate, partyType, partyId, direction,
                    paymentMethod, status, branchId, search, sortBy, sortDescending)
                { IsExport = true });

                if (!result.IsSuccess || result.Data is not PaymentReportResponse report)
                    return Results.Ok(result);

                var rows = report.Rows.Items;
                var summary = report.Summary;

                var bytes = ReportWorkbook.Build(
                    "Payments",
                    "Payment Report",
                    new[]
                    {
                        ("Date range", ReportWorkbook.DateRangeLabel(startDate, endDate)),
                        ("Party type", ReportWorkbook.Or(partyType)),
                        ("Direction", ReportWorkbook.Or(direction)),
                        ("Method", ReportWorkbook.Or(paymentMethod)),
                        ("Status", ReportWorkbook.Or(status)),
                        ("Search", ReportWorkbook.Or(search, "-")),
                        ("Rows", rows.Count.ToString()),
                    },
                    new ReportColumn<PaymentReportRow>[]
                    {
                        new("Date", r => r.PaymentDate, ReportWorkbook.DateTimeFormat),
                        new("Reference", r => r.Reference),
                        new("Party", r => r.PartyName),
                        new("Party type", r => r.PartyType),
                        new("Direction", r => r.Direction),
                        new("Method", r => r.PaymentMethod),
                        new("Warehouse", r => r.BranchName),
                        new("Status", r => r.Status),
                        new("Amount", r => r.Amount, ReportWorkbook.MoneyFormat),
                        new("Allocated", r => r.AllocatedAmount, ReportWorkbook.MoneyFormat),
                        new("Unallocated", r => r.UnallocatedAmount, ReportWorkbook.MoneyFormat),
                        new("Invoices", r => string.Join(", ", r.Invoices)),
                        new("Remarks", r => r.Remarks ?? string.Empty),
                    },
                    rows,
                    new ReportTotal[]
                    {
                        new("Payments", summary.PaymentCount, ReportWorkbook.WholeNumberFormat),
                        new("Total received", summary.TotalReceived, ReportWorkbook.MoneyFormat),
                        new("Total paid", summary.TotalPaid, ReportWorkbook.MoneyFormat),
                        new("Net amount", summary.NetAmount, ReportWorkbook.MoneyFormat),
                    });

                return Results.File(bytes, ReportWorkbook.ContentType,
                    ReportWorkbook.FileName("Payment", startDate, endDate));
            }).WithTags("Reports").RequirePermission(Permissions.ReportsView);
        }
    }
}
