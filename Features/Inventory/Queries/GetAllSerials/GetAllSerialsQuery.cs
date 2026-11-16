using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Inventory.Queries.GetAllSerials
{
    public sealed record GetAllSerialsQuery(
        int PageNumber = 1,
        int PageSize = 20,
        int? BranchId = null,
        int? ProductVariantId = null,
        string? Status = null,
        string? Search = null
    ) : IRequest<Result>;
}
