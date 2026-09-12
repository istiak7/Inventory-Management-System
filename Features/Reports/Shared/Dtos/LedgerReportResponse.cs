using Inventory_Management_System.Shared.Extensions.PaginationExtensions;

namespace Inventory_Management_System.Features.Reports.Shared.Dtos
{
    public sealed record LedgerReportRow(
        int Id,
        string PartyType,
        int PartyId,
        string PartyName,
        string TransactionType,
        DateTime TransactionDate,
        string Reference,
        string? Description,
        decimal Debit,
        decimal Credit,
        decimal BalanceAfter,
        decimal RunningBalance
    );

    public sealed record LedgerReportSummary(
        decimal? OpeningBalance,
        decimal TotalDebit,
        decimal TotalCredit,
        decimal? ClosingBalance,
        long TransactionCount
    );

    public sealed record LedgerReportResponse(
        LedgerReportSummary Summary,
        PagedResult<LedgerReportRow> Rows
    );
}
