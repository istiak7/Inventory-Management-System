using Inventory_Management_System.Shared.Extensions.PaginationExtensions;

namespace Inventory_Management_System.Features.Reports.Shared.Dtos
{
    public sealed record PartySalesRow(
        int SaleId,
        string InvoiceNumber,
        DateTime SaleDate,
        int CustomerId,
        string CustomerName,
        string PhoneNumber,
        int BranchId,
        string BranchName,
        string SaleType,
        string Status,
        decimal SubTotal,
        decimal DiscountAmount,
        decimal TaxAmount,
        decimal TotalAmount,
        decimal PaidAmount,
        decimal DueAmount,
        int ReturnAdjustmentCount,
        decimal ReturnAdjustmentAmount,
        string? Remarks,
        IReadOnlyList<ReportPaymentAllocation> Payments
    );

    public sealed record PartySalesSummary(
        long InvoiceCount,
        decimal TotalSubTotal,
        decimal TotalDiscount,
        decimal TotalTax,
        decimal TotalSales,
        decimal TotalPaid,
        decimal TotalDue,
        decimal ReturnAdjustmentAmount
    );

    public sealed record PartySalesReportResponse(
        PartySalesSummary Summary,
        PagedResult<PartySalesRow> Rows
    );
}
