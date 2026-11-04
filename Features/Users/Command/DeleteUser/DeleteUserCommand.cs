using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Users.Command.DeleteUser
{
    public class DeleteUserCommand : IRequest<Result>
    {
        public int Id { get; set; }
    }
}
