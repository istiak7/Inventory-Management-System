using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace Inventory_Management_System.Shared.RMQ
{
    public interface IMassageProducer
    {
        public Task PublishMessageAsync(string eventType, string content, CancellationToken cancellationToken);
    }
    public class MessageProducer : IMassageProducer
    {
        private readonly IRabbitMQConnection _rabbitMQConnection;
        private readonly string _exchangeName;
        public MessageProducer(IRabbitMQConnection rabbitMQConnection, IOptions<RMQSettings> options)
        {
            _rabbitMQConnection = rabbitMQConnection;
            _exchangeName = options.Value.ExchangeName;
        }

        public async Task PublishMessageAsync(string eventType, string content, CancellationToken cancellationToken)
        {
            var connection = await _rabbitMQConnection.GetConnectionAsync();
            var channelOptions = new CreateChannelOptions(
                publisherConfirmationsEnabled: true,
                publisherConfirmationTrackingEnabled: true);
            using var channel = await connection.CreateChannelAsync(channelOptions, cancellationToken);

            await channel.ExchangeDeclareAsync(
                exchange: _exchangeName,
                type: ExchangeType.Topic,
                durable: true,
                cancellationToken: cancellationToken);

            var routingKey = eventType;
            var body = System.Text.Encoding.UTF8.GetBytes(content);
            var properties = new BasicProperties
            {
                Persistent = true,
                ContentType = "application/json"
            };

            await channel.BasicPublishAsync(
                exchange: _exchangeName,
                routingKey: routingKey,
                mandatory: true,
                basicProperties: properties,
                body: body,
                cancellationToken: cancellationToken);

        }

    }
}
