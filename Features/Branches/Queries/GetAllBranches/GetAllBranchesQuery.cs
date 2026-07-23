using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Branches.Queries.GetAllBranches
{
    public sealed record GetAllBranchesQuery(int PageNumber = 1, int PageSize = 100) : IRequest<Result>;
}
