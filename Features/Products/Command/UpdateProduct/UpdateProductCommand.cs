using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Products.Command.UpdateProduct
{
    // Catalog-only update. SKU / selling price / serialized live on the variant and are edited
    // through a separate variant endpoint (not here).
    public class UpdateProductCommand : IRequest<Result>
    {
        public int Id { get; set; }
        public string ProductName { get; set; }
        public string ProductDescription { get; set; } = string.Empty;
        public string ProductImageUrl { get; set; } = string.Empty;
        public int ProductSubCategoryId { get; set; }
        public int BrandId { get; set; }
    }
}
