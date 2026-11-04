using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Roles.Command.CreateRole
{
    public class CreateRoleCommand : IRequest<Result>
    {
        public required string Name { get; set; }
        public string Description { get; set; } = string.Empty;

        // Fixed permissions (the fallback for routes no menu action covers).
        public List<int>? PermissionIds { get; set; }

        // Menu actions (System Setup -> Menu Action) every user with this role will get.
        // Null = leave as they are.
        public List<int>? MenuActionIds { get; set; }
    }
}
