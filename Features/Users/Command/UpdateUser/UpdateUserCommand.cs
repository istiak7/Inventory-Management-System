using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Users.Command.UpdateUser
{
    public class UpdateUserCommand : IRequest<Result>
    {
        public int Id { get; set; }
        public required string Username { get; set; }
        public int RoleId { get; set; }

        // Null = all branches (admin). A value = one branch (staff).
        public int? BranchId { get; set; }

        // Full replacement of the user's direct (extra) permissions.
        public List<int> PermissionIds { get; set; } = [];

        // Uses EntityStatus: 0 = Active, 1 = InActive, 2 = Deleted.
        public int IsActive { get; set; }
    }
}
