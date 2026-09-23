using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Users.Queries.GetAllUsers
{
    public sealed record GetAllUsersQuery(int PageNumber = 1, int PageSize = 10, string? Search = null) : IRequest<Result>;
}
