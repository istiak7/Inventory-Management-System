using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Reports.Queries.GetPaymentReport
{
    public sealed record GetPaymentReportQuery(
        int PageNumber = 1,
        int PageSize = 20,
        DateTime? StartDate = null,
        DateTime? EndDate = null,
        string? PartyType = null,
        int? PartyId = null,
        string? Direction = null,
        string? PaymentMethod = null,
        string? Status = null,
        int? BranchId = null,
        string? Search = null,
        string? SortBy = null,
        bool SortDescending = true
    ) : IRequest<Result>;
}
