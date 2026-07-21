using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Suppliers.Command.CreateSuppliers
{
    public class CreateSupplierCommand : IRequest<Result>
    {
        public string Group { get; set; }
        public string Name { get; set; }
        public string Description { get; set; } = string.Empty;
        public string PhoneNumber { get; set; }
        public string Email { get; set; }
        public string NID { get; set; }
        public decimal OpeningBalance { get; set; }
    }
}
