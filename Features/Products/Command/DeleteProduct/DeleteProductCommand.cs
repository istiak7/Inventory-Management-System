using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Products.Command.DeleteProduct
{
    public class DeleteProductCommand : IRequest<Result>
    {
        public int Id { get; set; }
    }
}
