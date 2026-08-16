using Inventory_Management_System.Database;
using Inventory_Management_System.Entities;
using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Orders.Command.CreateOrder
{
    public class CreateOrderCommandHandler(
        AppDbContext _dbContext,
        ILogger<CreateOrderCommandHandler> _logger
    ) : IRequestHandler<CreateOrderCommand, Result>
    {
        public async Task<Result> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var order = new Order
                {
                    ProductName = request.ProductName,
                    Price = request.Price
                };
             
                _dbContext.Orders.Add(order);
                using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
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
