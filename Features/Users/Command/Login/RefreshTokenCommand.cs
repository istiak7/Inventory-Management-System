using MediatR;
using Inventory_Management_System.Shared;

namespace Inventory_Management_System.Features.Users.Login
{
    public class RefreshTokenCommand : IRequest<Result>
    {
        public int UserId { get; set; }
        public string RefreshToken { get; set; }
    }
}
