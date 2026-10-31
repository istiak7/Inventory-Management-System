using Inventory_Management_System.Shared.Extensions.PaginationExtensions;

namespace Inventory_Management_System.Features.Reports.Shared.Dtos
{
    public sealed record ExpenseReportRow(
        int ExpenseId,
        string Reference,
        DateTime ExpenseDate,
        string? Name,
        string? Description,
        int CategoryId,
        string CategoryName,
        int BranchId,
        string BranchName,
        string PaymentMethod,
        string? RecordedBy,
        decimal Amount
    );

    public sealed record ExpenseReportSummary(
        long ExpenseCount,
        decimal TotalAmount,
        decimal AverageAmount,
        int CategoryCount,
        // The category the most money went to in the period; null when there were no expenses.
        string? TopCategoryName,
        decimal TopCategoryAmount
    );

    public sealed record ExpenseReportResponse(
        ExpenseReportSummary Summary,
        PagedResult<ExpenseReportRow> Rows
    );
}
