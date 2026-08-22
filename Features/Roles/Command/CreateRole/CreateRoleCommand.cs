using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Roles.Command.CreateRole
{
    public class CreateRoleCommand : IRequest<Result>
    {
        public required string Name { get; set; }
        public string Description { get; set; } = string.Empty;

        // Permissions every user with this role will get.
        public List<int> PermissionIds { get; set; } = [];
    }
}
