using Inventory_Management_System.Shared;
using MediatR;
using System.Text.Json.Serialization;

namespace Inventory_Management_System.Features.Users.Command.ChangePassword
{
    // The logged-in user changes their own password.
    public class ChangePasswordCommand : IRequest<Result>
    {
        // Filled from the JWT by the endpoint, never from the request body.
        [JsonIgnore]
        public int UserId { get; set; }

        public required string CurrentPassword { get; set; }
        public required string NewPassword { get; set; }
    }
}
