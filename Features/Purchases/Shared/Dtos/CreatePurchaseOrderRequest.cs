namespace Inventory_Management_System.Features.Purchases.Shared.Dtos
{
    public class CreatePurchaseOrderRequest
    {
        public int SupplierId { get; set; }
        public int BranchId { get; set; }
        public DateTime? PurchaseDate { get; set; }
        public string? InvoiceNumber { get; set; }
        public string? Remarks { get; set; }
        public List<PurchaseItemRequest> Items { get; set; } = [];
    }
}
