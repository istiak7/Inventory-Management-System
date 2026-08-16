using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Orders.Command.CreateOrder
{
    public class CreateOrderCommand : IRequest<Result>
    {
        public string ProductName { get; set; }
        public decimal Price { get; set; }
    }
}
