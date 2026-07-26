using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Stocks.Queries.GetStockByVariantAndBranch
{
    // The single stock row for one variant at one branch (the unique Stock key).
    public sealed record GetStockByVariantAndBranchQuery(int ProductVariantId, int BranchId) : IRequest<Result>;
}
