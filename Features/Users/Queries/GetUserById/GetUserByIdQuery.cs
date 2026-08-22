using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Users.Queries.GetUserById
{
    public sealed record GetUserByIdQuery(int Id) : IRequest<Result>;
}
