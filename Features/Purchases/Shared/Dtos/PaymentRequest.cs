namespace Inventory_Management_System.Features.Purchases.Shared.Dtos
{
    public class PaymentRequest
    {
        public decimal Amount { get; set; }
        public DateTime? PaymentDate { get; set; }
        public string PaymentMethod { get; set; } = "Cash"; // Cash, Bank Transfer, Cheque, Credit Card, Other
        public string PurchaseType { get; set; } = "Full"; // Full or Partial or Due
    }
}
