namespace Inventory_Management_System.Features.Sales.Shared.Dtos
{
    public class SaleItemRequest
    {
        public int ProductVariantId { get; set; }
        public int Quantity { get; set; }

        public decimal? UnitPrice { get; set; }

        public decimal? DiscountPerItem { get; set; }
        public int? WarrantyMonths { get; set; }

        public string? SerialNumber { get; set; }
    }
}
