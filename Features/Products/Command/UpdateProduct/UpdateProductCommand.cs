using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Products.Command.UpdateProduct
{
    public class UpdateProductCommand : IRequest<Result>
    {
        public int Id { get; set; }
        public string ProductName { get; set; }
        public string? ProductDescription { get; set; }
        public string? ProductImageUrl { get; set; }
        // null = leave as it is (older clients do not send it).
        public bool? IsReturnable { get; set; }
        // null = leave as it is; an empty string clears the warranty.
        public string? Warranty { get; set; }
        public int ProductSubCategoryId { get; set; }
        public int BrandId { get; set; }
    }
}
