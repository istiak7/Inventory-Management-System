using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Branches.Queries.GetBranchVariantStock
{
    // The number of stock for one variant at one branch.
    public sealed record GetBranchVariantStockQuery(int BranchId, int ProductVariantId) : IRequest<Result>;
}
