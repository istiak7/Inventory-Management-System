using Inventory_Management_System.Shared.Background;
using Microsoft.Extensions.Options;
using Quartz;
using RabbitMQ.Client;

namespace Inventory_Management_System.Shared.RMQ
{
    public interface IRabbitMQConnection
    {
        public Task<IConnection> GetConnectionAsync();
    }
    public class RabbitMQConnection : IRabbitMQConnection
    {
        private IConnection _connection;
        private readonly ConnectionFactory _connectionFactory;
        private readonly ISchedulerFactory _schedulerFactory;
        private static readonly JobKey OutboxJobKey = new JobKey("OutboxProcessorJob");
        private readonly SemaphoreSlim _ConnectionLock = new SemaphoreSlim(1, 1);
        private readonly ILogger<RabbitMQConnection> _logger;
        public RabbitMQConnection(IOptions<RMQSettings> options, ISchedulerFactory schedulerFactory)
        {
            var settings = options.Value;
            _connectionFactory = new ConnectionFactory
            {
                HostName = settings.HostName,
                Port = int.Parse(settings.Port),
                UserName = settings.UserName,
                Password = settings.Password,
                AutomaticRecoveryEnabled = true
            };
            _schedulerFactory = schedulerFactory;
        }

        public async Task<IConnection> GetConnectionAsync()
        {
            if (_connection != null && _connection.IsOpen)
                return _connection;

            await _ConnectionLock.WaitAsync();
            try
            {
                if (_connection != null && _connection.IsOpen)
                    return _connection;

                _connection = await _connectionFactory.CreateConnectionAsync();
                _logger?.LogInformation("RabbitMQ connection established.");

                _connection.RecoverySucceededAsync += async (sender, args) =>
                {
                    try
                    {
                        _logger?.LogInformation("RabbitMQ connection recovered. Triggering Outbox job...");
                        await TriggerOutboxJobAsync();
                    }
                    catch (Exception ex)
                    {         
                        _logger?.LogError(ex, "Failed to trigger Outbox job after RabbitMQ recovery.");
                    }
                };

                _connection.ConnectionShutdownAsync += (sender, args) =>
                {
                    Console.WriteLine($"RabbitMQ connection shutdown: {args.ReplyText}");
                    return Task.CompletedTask;
                };

                return _connection;
            }
            finally
            {
                _ConnectionLock.Release();
            }
        }


        private async Task TriggerOutboxJobAsync()
        {
            var scheduler = await _schedulerFactory.GetScheduler();
            await scheduler.TriggerJob(OutboxJobKey); /// call background
        }
    }
}
