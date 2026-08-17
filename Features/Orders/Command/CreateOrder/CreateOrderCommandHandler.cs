using Inventory_Management_System.Database;
using Inventory_Management_System.Entities;
using Inventory_Management_System.Shared;
using Inventory_Management_System.Shared.RMQ;
using MediatR;

namespace Inventory_Management_System.Features.Orders.Command.CreateOrder
{
    public class CreateOrderCommandHandler(
        AppDbContext _dbContext,
        IMassageProducer _producer,
        ILogger<CreateOrderCommandHandler> _logger
    ) : IRequestHandler<CreateOrderCommand, Result>
    {
        public async Task<Result> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
        {
            using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                var order = new Order
                {
                    ProductName = request.ProductName,
                    Price = request.Price
                };
             
                _dbContext.Orders.Add(order);
                await _dbContext.SaveChangesAsync(cancellationToken);

                var OrderCreateEvent = new
                {
                    order.Id,
                    order.ProductName,
                    order.Price,
                };

                var OutboxMessage = new OutboxMessage
                {
                    EventType = "OrderCreated",
                    Content = System.Text.Json.JsonSerializer.Serialize(OrderCreateEvent),
                    OccurredOn = DateTime.UtcNow
                };

                _dbContext.OutboxMessages.Add(OutboxMessage);
                
                await _dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                bool publishedSucceded = false;
                try
                {
                    await _producer.PublishMessageAsync("OrderCreated", System.Text.Json.JsonSerializer.Serialize(OrderCreateEvent), cancellationToken);
                    publishedSucceded = true;
                 
                }

                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error publishing message to RabbitMQ");
                }
                try
                {
                    if (publishedSucceded)
                    {
                        OutboxMessage.Status = OutBoxStatus.Processed;
                        OutboxMessage.ProcessedOn = DateTime.UtcNow;
                       
                    }
                    await _dbContext.SaveChangesAsync(cancellationToken);
                }
                catch
                {
                    _logger.LogError("Error updating OutboxMessage status");
                }

                return new Result
                {
                    IsSuccess = true,
                    StatusCode = 201,
                    Status = "Success",
                    Message = "Order created successfully",
                    Data = order
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating order");
                return new Result
                {
                    IsSuccess = false,
                    StatusCode = 500,
                    Status = "Error",
                    Message = "An error occurred while creating the order."
                };
            }
        }
    }
}
