using MediatR;

namespace Inventory_Management_System.Features.Users.Command.Events
{
    public sealed record UserRegistrationEvent(
        int userId,
        string Name,
        string Email
    ) : INotification;
    public class SendWelcomeEmailHandler : INotificationHandler<UserRegistrationEvent>
    {
        public async Task Handle(UserRegistrationEvent notification, CancellationToken cancellationToken)
        {
            Console.WriteLine($"Sending welcome email to {notification.Email} for user {notification.Name} (ID: {notification.userId})");
            await Task.CompletedTask;
        }
    }
}
