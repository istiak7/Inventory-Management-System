using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Roles.Command.DeleteRole
{
    public class DeleteRoleCommand : IRequest<Result>
    {
        public int Id { get; set; }
    }
}
