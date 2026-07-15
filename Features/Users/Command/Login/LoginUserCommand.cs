using MediatR;
using Inventory_Management_System.Shared;

namespace Inventory_Management_System.Features.Users.Login
{
    public class LoginUserCommand : IRequest<Result>
    {
        public string Identifier { get; set; }
        public string Password { get; set; }
    }
}
