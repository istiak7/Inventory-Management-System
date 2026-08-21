using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Stocks.Queries.GetStockByVariantAndBranch
{
    public sealed record GetStockByVariantAndBranchQuery(int ProductVariantId, int BranchId) : IRequest<Result>;
}
