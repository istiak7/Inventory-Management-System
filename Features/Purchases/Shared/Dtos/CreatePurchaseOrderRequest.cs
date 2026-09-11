namespace Inventory_Management_System.Features.Purchases.Shared.Dtos
{
    public class CreatePurchaseOrderRequest
    {
        public int SupplierId { get; set; }
        public int BranchId { get; set; }
        public DateTime? PurchaseDate { get; set; }
        public string? InvoiceNumber { get; set; }
        public string? Remarks { get; set; }
        /// <summary>How the order will be settled on approval: "Cash" or "Debit".</summary>
        public string? PaymentType { get; set; }
        public List<PurchaseItemRequest> Items { get; set; } = [];
    }
}
