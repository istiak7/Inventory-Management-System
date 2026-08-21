using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Products.Command.CreateProduct
{
    public class CreateProductCommand : IRequest<Result>
    {
        public string ProductName { get; set; }
        public string ProductDescription { get; set; } = string.Empty;
        public string ProductImageUrl { get; set; } = string.Empty;
        public int ProductSubCategoryId { get; set; }
        public int BrandId { get; set; }

        public string SKU { get; set; }
        public string Barcode { get; set; } = string.Empty;
        public decimal SellingPrice { get; set; }
        public bool IsSerialized { get; set; } = false;
        public string AttributesJson { get; set; } = "{}";
    }
}
