using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Customers.Command.DeleteCustomer
{
    public class DeleteCustomerCommand : IRequest<Result>
    {
        public int Id { get; set; }
    }
}
