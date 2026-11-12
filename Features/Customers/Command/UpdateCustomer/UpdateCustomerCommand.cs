using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Customers.Command.UpdateCustomer
{
    public class UpdateCustomerCommand : IRequest<Result>
    {
        public int Id { get; set; }
        public string Group { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string NID { get; set; } = string.Empty;
        public decimal OpeningBalance { get; set; }
        // null = leave as it is. true = link (or create) a supplier with this phone number.
        // false = unlink: the supplier row stays, but no more automatic settlement.
        public bool? IsCreditParty { get; set; }
    }
}
