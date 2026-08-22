using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Roles.Command.UpdateRole
{
    public class UpdateRoleCommand : IRequest<Result>
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public string Description { get; set; } = string.Empty;

        // Full replacement of the role's permissions.
        public List<int> PermissionIds { get; set; } = [];
    }
}
