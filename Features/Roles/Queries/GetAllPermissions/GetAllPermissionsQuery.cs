using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Roles.Queries.GetAllPermissions
{
    public sealed record GetAllPermissionsQuery : IRequest<Result>;
}
