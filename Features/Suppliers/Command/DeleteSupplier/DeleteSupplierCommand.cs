using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Suppliers.Command.DeleteSupplier
{
    public class DeleteSupplierCommand : IRequest<Result>
    {
        public int Id { get; set; }
    }
}
