using MediatR;
using Inventory_Management_System.Shared;

namespace Inventory_Management_System.Features.Users.Command.CreateUsers
{
    public class CreateUserCommand : IRequest<Result>
    {
        public int RoleId { get; set; }

        // Null = all branches (admin). A value = one branch (staff).
        public int? BranchId { get; set; }

        // Extra permissions granted directly to this user, on top of the role.
        public List<int> PermissionIds { get; set; } = [];

        public required string Username { get; set; }
        public required string Email { get; set; }

        // Plain text here so the validator can check its length. The handler hashes it.
        public required string Password { get; set; }
    }
}
