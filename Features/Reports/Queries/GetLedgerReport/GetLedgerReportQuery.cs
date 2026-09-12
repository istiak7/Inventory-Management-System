using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Reports.Queries.GetLedgerReport
{
    public sealed record GetLedgerReportQuery(
        int PageNumber = 1,
        int PageSize = 20,
        string? PartyType = null,
        int? PartyId = null,
        DateTime? StartDate = null,
        DateTime? EndDate = null,
        string? TransactionType = null,
        string? Search = null,
        int? BranchId = null
    ) : IRequest<Result>;
}
