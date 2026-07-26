using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Products.Command.UpdateProductVariant
{
    public class UpdateProductVariantCommand : IRequest<Result>
    {
        public int Id { get; set; }
        public string SKU { get; set; }
        public string Barcode { get; set; } = string.Empty;
        public decimal SellingPrice { get; set; }
        public bool IsSerialized { get; set; } = false;
        public string AttributesJson { get; set; } = "{}";
    }
}
