using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Quotations.Queries.GetAllQuotations
{
    public sealed record GetAllQuotationsQuery(
        int PageNumber = 1,
        int PageSize = 20,
        int? CustomerId = null,
        string? Status = null,
        string? Search = null,
        DateTime? StartDate = null,
        DateTime? EndDate = null
    ) : IRequest<Result>;
}
