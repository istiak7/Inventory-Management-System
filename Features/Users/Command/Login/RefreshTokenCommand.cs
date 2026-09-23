using MediatR;
using Inventory_Management_System.Shared;
using System.Text.Json.Serialization;

namespace Inventory_Management_System.Features.Users.Login
{
    // Exchange a valid refresh token for a new access token (and a new refresh token).
    public class RefreshTokenCommand : IRequest<Result>
    {
        public string RefreshToken { get; set; } = string.Empty;
    }

    // Sign out: the refresh token of this device stops working.
    // Without a refresh token the user is signed out on every device.
    public class LogoutCommand : IRequest<Result>
    {
        // Filled from the JWT by the endpoint, never from the request body.
        [JsonIgnore]
        public int UserId { get; set; }

        public string? RefreshToken { get; set; }
    }
}
