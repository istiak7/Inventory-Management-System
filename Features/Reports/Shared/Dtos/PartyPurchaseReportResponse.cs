using Inventory_Management_System.Shared.Extensions.PaginationExtensions;

namespace Inventory_Management_System.Features.Reports.Shared.Dtos
{
    public sealed record PartyPurchaseRow(
        int PurchaseId,
        string InvoiceNumber,
        DateTime PurchaseDate,
        int SupplierId,
        string SupplierName,
        string PhoneNumber,
        int BranchId,
        string BranchName,
        string? PurchaseType,
        string Status,
        decimal TotalAmount,
        decimal PaidAmount,
        decimal DueAmount,
        int ReturnAdjustmentQuantity,
        decimal ReturnAdjustmentAmount,
        string? Remarks,
        IReadOnlyList<ReportPaymentAllocation> Payments
    );

    public sealed record PartyPurchaseSummary(
        long OrderCount,
        decimal TotalPurchase,
        decimal TotalPaid,
        decimal TotalDue,
        decimal ReturnAdjustmentAmount
    );

    public sealed record PartyPurchaseReportResponse(
        PartyPurchaseSummary Summary,
        PagedResult<PartyPurchaseRow> Rows
    );
}
