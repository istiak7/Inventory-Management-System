using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Roles.Command.UpdateRole
{
    public class UpdateRoleCommand : IRequest<Result>
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public string Description { get; set; } = string.Empty;

        // Full replacement of the role's fixed permissions (the fallback for routes no menu
        // action covers). Null = leave them as they are.
        public List<int>? PermissionIds { get; set; }

        // Menu actions (System Setup -> Menu Action) every user with this role will get.
        // Null = leave as they are.
        public List<int>? MenuActionIds { get; set; }
    }
}
