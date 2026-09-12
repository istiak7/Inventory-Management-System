using Inventory_Management_System.Shared.Extensions.PaginationExtensions;

namespace Inventory_Management_System.Features.Reports.Shared.Dtos
{
    public sealed record PaymentReportRow(
        int PaymentId,
        string PartyType,
        int PartyId,
        string PartyName,
        string Direction,
        DateTime PaymentDate,
        string PaymentMethod,
        int BranchId,
        string BranchName,
        string Reference,
        decimal Amount,
        decimal AllocatedAmount,
        decimal UnallocatedAmount,
        string Status,
        string? Remarks,
        IReadOnlyList<string> Invoices
    );

    public sealed record PaymentReportSummary(
        long PaymentCount,
        decimal TotalReceived,
        decimal TotalPaid,
        decimal NetAmount
    );

    public sealed record PaymentReportResponse(
        PaymentReportSummary Summary,
        PagedResult<PaymentReportRow> Rows
    );
}
