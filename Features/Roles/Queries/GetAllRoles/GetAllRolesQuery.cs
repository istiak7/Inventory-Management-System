using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Roles.Queries.GetAllRoles
{
    public sealed record GetAllRolesQuery : IRequest<Result>;
}
