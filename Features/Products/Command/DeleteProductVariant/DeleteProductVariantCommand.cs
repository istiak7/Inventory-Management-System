using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Products.Command.DeleteProductVariant
{
    public class DeleteProductVariantCommand : IRequest<Result>
    {
        public int Id { get; set; }
    }
}
