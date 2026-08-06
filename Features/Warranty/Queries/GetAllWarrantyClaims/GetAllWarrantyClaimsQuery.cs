using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Warranty.Queries.GetAllWarrantyClaims
{
    public sealed record GetAllWarrantyClaimsQuery(
        int PageNumber = 1,
        int PageSize = 20,
        string? Status = null,        // Open | InRepair | Resolved | Rejected | Delivered
        int? BranchId = null,
        int? CustomerId = null,
        string? Search = null         // claim number, serial, invoice, customer name or phone
    ) : IRequest<Result>;
}
