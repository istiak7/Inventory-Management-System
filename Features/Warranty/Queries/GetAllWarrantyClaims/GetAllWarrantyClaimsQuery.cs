using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Warranty.Queries.GetAllWarrantyClaims
{
    public sealed record GetAllWarrantyClaimsQuery(
        int PageNumber = 1,
        int PageSize = 20,
        string? Status = null,
        int? BranchId = null,
        int? CustomerId = null,
        string? Search = null
    ) : IRequest<Result>;
}
