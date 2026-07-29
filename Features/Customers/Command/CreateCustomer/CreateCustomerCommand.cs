using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Customers.Command.CreateCustomer
{
    public class CreateCustomerCommand : IRequest<Result>
    {
        public string Group { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string NID { get; set; } = string.Empty;
        public decimal OpeningBalance { get; set; }
    }
}
