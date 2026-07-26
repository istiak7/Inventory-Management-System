using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Branches.Queries.GetBranchStock
{
    // All stock at a branch, with total units on hand.
    public sealed record GetBranchStockQuery(int BranchId) : IRequest<Result>;
}
