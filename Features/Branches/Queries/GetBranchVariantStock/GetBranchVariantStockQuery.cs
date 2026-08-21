using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Branches.Queries.GetBranchVariantStock
{
    public sealed record GetBranchVariantStockQuery(int BranchId, int ProductVariantId) : IRequest<Result>;
}
