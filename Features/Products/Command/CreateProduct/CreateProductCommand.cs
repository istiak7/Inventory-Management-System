using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Products.Command.CreateProduct
{
    public class CreateProductCommand : IRequest<Result>
    {
        public string ProductName { get; set; }
        public string ProductDescription { get; set; } = string.Empty;
        public string ProductImageUrl { get; set; } = string.Empty;
        public string ProductCode { get; set; }
        public decimal ProductPrice { get; set; }
        public int ProductSubCategoryId { get; set; }
        public int BrandId { get; set; }
    }
}
