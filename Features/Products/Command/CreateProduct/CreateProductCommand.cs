using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Products.Command.CreateProduct
{
    public class CreateProductCommand : IRequest<Result>
    {
        public string ProductName { get; set; }
        public string? ProductDescription { get; set; }
        public string? ProductImageUrl { get; set; }
        // Can this product be returned? Defaults to yes.
        public bool IsReturnable { get; set; } = true;
        // Optional warranty terms (free text).
        public string? Warranty { get; set; }
        public int ProductSubCategoryId { get; set; }
        public int BrandId { get; set; }

        public string? SKU { get; set; }
        public string? Barcode { get; set; }
        public decimal SellingPrice { get; set; }
        public bool IsSerialized { get; set; } = false;
        public string? AttributesJson { get; set; }
    }
}
