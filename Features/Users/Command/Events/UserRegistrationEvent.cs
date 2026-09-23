using MediatR;

namespace Inventory_Management_System.Features.Users.Command.Events
{
    // Published after a user is created.
    public sealed record UserRegistrationEvent(
        int userId,
        string Name,
        string Email
    ) : INotification;
}
