using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Branches.Queries.GetBranchStock
{
    public sealed record GetBranchStockQuery(int BranchId) : IRequest<Result>;
}
