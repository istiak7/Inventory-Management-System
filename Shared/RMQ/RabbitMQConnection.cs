using Microsoft.Extensions.Options;
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
        public RabbitMQConnection(IOptions<RMQSettings> options)
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
        }

        public async Task<IConnection> GetConnectionAsync()
        {
            if (_connection == null || !_connection.IsOpen)
            {
                _connection = await _connectionFactory.CreateConnectionAsync();
                Console.WriteLine("RabbitMQ connection established.");
            }
            return _connection;
        }
    }
}
