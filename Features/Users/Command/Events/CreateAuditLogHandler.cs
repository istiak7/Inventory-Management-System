using MediatR;

namespace Inventory_Management_System.Features.Users.Command.Events
{
    // Writes who was created to the application log. Only the id is logged, never the email,
    // so personal data does not end up in log files.
    public class CreateAuditLogHandler(ILogger<CreateAuditLogHandler> _logger) : INotificationHandler<UserRegistrationEvent>
    {
        public Task Handle(UserRegistrationEvent notification, CancellationToken cancellationToken)
        {
            _logger.LogInformation("User {UserId} was created", notification.userId);
            return Task.CompletedTask;
        }
    }
}
