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
    ) : IRequest<Result>
    {
        /// <summary>
        /// Set by the matching export endpoint only. Lifts the page cap so one page holds the
        /// entire filtered result set; the filters themselves are untouched, which is what keeps
        /// an export identical to the report it was launched from.
        /// </summary>
        public bool IsExport { get; init; }
    }
}
