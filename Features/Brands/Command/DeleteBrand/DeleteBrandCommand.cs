using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Brands.Command.DeleteBrand
{
    public class DeleteBrandCommand : IRequest<Result>
    {
        public int Id { get; set; }
    }
}
