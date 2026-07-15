using MediatR;

namespace Inventory_Management_System.Features.Users.Command.Events
{
    public class CreateAuditLogHandler : INotificationHandler<UserRegistrationEvent>
    {
        public async Task Handle(UserRegistrationEvent notification, CancellationToken cancellationToken)
        {
            // Simulate creating an audit log
            Console.WriteLine($"Creating audit log for user registration: {notification.Email}");
            await Task.CompletedTask;
        }
    }
}
