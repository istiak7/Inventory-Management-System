using Inventory_Management_System.Database;
using Inventory_Management_System.Entities;
using Inventory_Management_System.Shared.RMQ;

namespace Inventory_Management_System.Shared.Services.Outbox
{
    public interface IOutboxService
    {
        public Task ProcessOutboxMessagesAsync(CancellationToken cancellationToken);
    }
    public class OutboxService : IOutboxService
    {
        private readonly AppDbContext _dbContext;
        private readonly IMassageProducer _producer;
        private readonly ILogger<OutboxService> _logger;
        private const int BatchSize = 10;           // Number of messages to process in each batch
        private const int MaxRetryAttempts = 3;     // Maximum number of retry attempts for failed messages
        public OutboxService(AppDbContext appDbContext,
                            IMassageProducer producer,
                            ILogger<OutboxService> logger
                            )
        {
            _dbContext = appDbContext;
            _producer = producer;
            _logger = logger;
        }
        public async Task ProcessOutboxMessagesAsync(CancellationToken cancellationToken)
        {
           var outboxMessages = _dbContext.OutboxMessages
                .Where(m => m.Status == OutBoxStatus.Pending || m.Status == OutBoxStatus.Failed)
                .OrderBy(m => m.CreatedAt)
                .Take(BatchSize)
                .ToList();
            foreach (var message in outboxMessages)
            {
                try
                {
                    await _producer.PublishMessageAsync(message.EventType, message.Content, cancellationToken);
                    message.Status = OutBoxStatus.Processed;
                    message.ProcessedOn = DateTime.UtcNow;

                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to process outbox message with ID {MessageId}", message.Id);
                    message.RetryCount++;
                    if (message.RetryCount >= MaxRetryAttempts)
                    {
                        message.Status = OutBoxStatus.Failed;
                        message.Errors = ex.Message;
                    }
                    else
                    {
                        message.Status = OutBoxStatus.Pending;
                    }
                }
            }
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
